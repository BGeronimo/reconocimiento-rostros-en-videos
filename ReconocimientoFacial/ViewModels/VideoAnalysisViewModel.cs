using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.Sqlite;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using Dapper;

namespace ReconocimientoFacial.ViewModels
{
    public partial class VideoFileItem : ObservableObject
    {
        public string FilePath { get; set; }
        
        [ObservableProperty]
        private string _fileName;
        
        [ObservableProperty]
        private string _fileSize;
        
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(InfoString))]
        private string _resolution;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(InfoString))]
        private string _fps;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(InfoString))]
        private string _duration;
        
        public string InfoString => $"{Resolution} • {Fps} FPS • {Duration}";
    }

    public partial class FaceHit : ObservableObject
    {
        [ObservableProperty]
        private ImageSource _faceSnapshot;
        
        [ObservableProperty]
        private string _name;
        
        [ObservableProperty]
        private string _certaintyText;
        
        [ObservableProperty]
        private string _clearanceLevel;
        
        [ObservableProperty]
        private bool _isUnknown;

        [ObservableProperty]
        private string _timestamp;

        public Brush ClearanceColor => IsUnknown ? new SolidColorBrush(Color.FromRgb(0, 242, 255)) : new SolidColorBrush(Color.FromRgb(207, 159, 255));
    }

    public partial class VideoAnalysisViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<VideoFileItem> _videoList = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsVideoLoaded))]
        private VideoFileItem _selectedVideo;

        [ObservableProperty]
        private ImageSource _currentFrame;

        [ObservableProperty]
        private int _identifiedCount = 0;

        [ObservableProperty]
        private int _unknownCount = 0;

        [ObservableProperty]
        private double _currentProgress = 0;

        [ObservableProperty]
        private ObservableCollection<FaceHit> _recentHits = new();

        public bool IsVideoLoaded => SelectedVideo != null;

        private CancellationTokenSource _cts;
        
        public void HandleDroppedFiles(string[] files)
        {
            foreach (var file in files)
            {
                if (file.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
                {
                    AddVideoFile(file);
                }
            }
        }

        [RelayCommand]
        private void OpenFiles()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Multiselect = true,
                Filter = "Archivos de Video (*.mp4)|*.mp4|Todos los archivos (*.*)|*.*",
                Title = "Seleccionar videos para análisis"
            };

            if (dialog.ShowDialog() == true)
            {
                HandleDroppedFiles(dialog.FileNames);
            }
        }

        private void AddVideoFile(string filePath)
        {
            // Evitar duplicados
            if (VideoList.Any(v => v.FilePath == filePath)) return;

            try
            {
                var fileInfo = new FileInfo(filePath);
                double mb = fileInfo.Length / (1024.0 * 1024.0);
                string sizeStr = mb > 1000 ? $"{mb / 1024.0:F2} GB" : $"{mb:F1} MB";

                var vi = new VideoFileItem
                {
                    FilePath = filePath,
                    FileName = Path.GetFileName(filePath),
                    FileSize = sizeStr,
                    Resolution = "Calculando...",
                    Fps = "--",
                    Duration = "--"
                };

                VideoList.Add(vi);

                // Leer métricas rápido con OpenCV
                Task.Run(() => {
                    try
                    {
                        using var cap = new VideoCapture(filePath);
                        if (cap.IsOpened())
                        {
                            var w = cap.Get(VideoCaptureProperties.FrameWidth);
                            var h = cap.Get(VideoCaptureProperties.FrameHeight);
                            var fps = cap.Get(VideoCaptureProperties.Fps);
                            var count = cap.Get(VideoCaptureProperties.FrameCount);
                            
                            var duration = TimeSpan.FromSeconds(fps > 0 ? count / fps : 0);

                            Application.Current.Dispatcher.Invoke(() => {
                                vi.Resolution = $"{w}x{h}";
                                vi.Fps = fps > 0 ? $"{fps:F1}" : "N/A";
                                vi.Duration = duration.ToString(@"hh\:mm\:ss");
                                OnPropertyChanged(nameof(VideoList)); // Refresh list
                            });
                        }
                    }
                    catch (Exception ex) 
                    { 
                        Application.Current.Dispatcher.Invoke(() => {
                            vi.Resolution = "Error al leer";
                            vi.Fps = "--";
                            vi.Duration = "--";
                        });
                        Console.WriteLine($"Error al leer video: {ex.Message}");
                    }
                });

                if (SelectedVideo == null)
                    SelectedVideo = vi;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding file {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task StartAnalysisAsync()
        {
            if (SelectedVideo == null) return;

            StopAnalysis(); // cleanup previo

            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            // Resetear contadores
            IdentifiedCount = 0;
            UnknownCount = 0;
            CurrentProgress = 0;
            RecentHits.Clear();

            var videoPath = SelectedVideo.FilePath;

            // Cargar Empleados desde BD
            System.Collections.Generic.List<Models.Employee> dbEmployees = new();
            try {
                using var conn = new SqliteConnection(Data.DatabaseInitializer.ConnectionString);
                conn.Open();
                dbEmployees = conn.Query<Models.Employee>("SELECT * FROM Employees").ToList();
            } catch { }

            // Instanciar Detector Real (YOLO)
            Core.FaceDetector detector = null;
            try
            {
                detector = new Core.FaceDetector();
            }
            catch { }

            // Simularemos o integraremos la red aquí iterando los frames
            await Task.Run(async () => {
                using var cap = new VideoCapture(videoPath);
                if (!cap.IsOpened()) return;

                var totalFrames = cap.Get(VideoCaptureProperties.FrameCount);
                if (totalFrames <= 0) totalFrames = 100; // fallback

                double fps = cap.Get(VideoCaptureProperties.Fps);
                if(fps <= 0) fps = 30; // fallback

                using var frame = new Mat();

                // Mantenemos un hashset de caras identificadas (usando el Id interno o un GUID para extraños)
                var knownFacesSet = new System.Collections.Generic.HashSet<string>();

                long frameIndex = 0;
                while (!token.IsCancellationRequested && cap.Read(frame))
                {
                    if (frame.Empty()) break;
                    frameIndex++;

                    // Mostrar frame
                    Application.Current.Dispatcher.Invoke(() => {
                        CurrentFrame = frame.ToWriteableBitmap();
                        CurrentProgress = (frameIndex / totalFrames) * 100.0;
                    });

                    // Analizar 3 veces por segundo (ej. si es a 30fps, analiza cada 10 frames)
                    int frameInterval = Math.Max(1, (int)(fps / 3.0));

                    if (frameIndex % frameInterval == 0) 
                    {
                        // 1. Usar YOLO para detectar rostros
                        OpenCvSharp.Rect[] faces = Array.Empty<OpenCvSharp.Rect>();
                        if (detector != null)
                        {
                            faces = detector.DetectFaces(frame);
                        }

                        // 2. Si no hay rostros, simplemente no hacemos nada (Ignorar el fotograma vacío)
                        if (faces.Length == 0)
                        {
                            // Prevenir bloqueo del CPU y seguir al siguiente frame
                            await Task.Delay((int)(1000.0 / fps), token);
                            continue;
                        }

                        // 3. Por cada rostro encontrado, decidimos interactuar
                        foreach (var faceRect in faces)
                        {
                            // Recortar la cara detectada para mostrarla
                            using var faceCropMat = new Mat(frame, faceRect);
                            var faceSnapshot = faceCropMat.ToWriteableBitmap();
                            faceSnapshot.Freeze(); // Importante para enviar a la UI desde un thread secundario

                            double ran = Random.Shared.NextDouble();
                            Application.Current.Dispatcher.Invoke(() => {
                                if (ran > 0.5 && dbEmployees.Any())
                                {
                                    // Simular Empleado Reconocido
                                    var randomEmp = dbEmployees[Random.Shared.Next(dbEmployees.Count)];
                                    string matchId = $"EMP_{randomEmp.Id}";

                                    if (!knownFacesSet.Contains(matchId))
                                    {
                                        knownFacesSet.Add(matchId);
                                        IdentifiedCount++;

                                        double exactitude = 70.0 + (Random.Shared.NextDouble() * 29.0);
                                        var currentSeconds = frameIndex / fps;
                                        var timeSpan = TimeSpan.FromSeconds(currentSeconds);
                                        string timestampStr = timeSpan.ToString(@"mm\:ss");

                                        ImageSource employeeImg = null;
                                        if (System.IO.File.Exists(randomEmp.LocalProfileImagePath))
                                        {
                                            var bmp = new BitmapImage();
                                            bmp.BeginInit();
                                            bmp.CacheOption = BitmapCacheOption.OnLoad;
                                            bmp.UriSource = new Uri(randomEmp.LocalProfileImagePath, UriKind.Absolute);
                                            bmp.EndInit();
                                            bmp.Freeze();
                                            employeeImg = bmp;
                                        }

                                        RecentHits.Insert(0, new FaceHit{
                                            FaceSnapshot = employeeImg, // Muestra la foto de la BD
                                            Name = randomEmp.FullName,
                                            CertaintyText = exactitude.ToString("0.0") + "%",
                                            ClearanceLevel = "L-4 CLEARANCE",
                                            IsUnknown = false,
                                            Timestamp = timestampStr
                                        });
                                    }
                                }
                                else 
                                {
                                    // Simular Extraño
                                    string unknownId = $"Unknown_{Guid.NewGuid().ToString().Substring(0,4)}";
                                    if (!knownFacesSet.Contains(unknownId))
                                    {
                                        knownFacesSet.Add(unknownId);
                                        UnknownCount++;

                                        var currentSeconds = frameIndex / fps;
                                        var timeSpan = TimeSpan.FromSeconds(currentSeconds);
                                        string timestampStr = timeSpan.ToString(@"mm\:ss");

                                        RecentHits.Insert(0, new FaceHit{
                                            FaceSnapshot = faceSnapshot, // Usa la cara recortada real de YOLO
                                            Name = "Unknown Subject",
                                            CertaintyText = "N/A",
                                            ClearanceLevel = "UNAUTHORIZED",
                                            IsUnknown = true,
                                            Timestamp = timestampStr
                                        });
                                    }
                                }
                            });
                        }
                    }

                    // Prevenir bloqueo del CPU (Ajustar a 1/fps para reproducir a velocidad normal)
                    await Task.Delay((int)(1000.0 / fps), token);
                }

            }, token);
        }

        [RelayCommand]
        private void StopAnalysis()
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                _cts.Cancel();
            }
            CurrentProgress = 0;
            CurrentFrame = null;
        }

        [RelayCommand]
        private void RemoveVideo(VideoFileItem videoToRemove)
        {
            if (videoToRemove != null && VideoList.Contains(videoToRemove))
            {
                if (SelectedVideo == videoToRemove)
                {
                    StopAnalysis();
                    SelectedVideo = null;
                }
                VideoList.Remove(videoToRemove);
            }
        }
    }
}
