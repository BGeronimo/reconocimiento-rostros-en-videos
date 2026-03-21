using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using ReconocimientoFacial.Core;
using ReconocimientoFacial.Models;

namespace ReconocimientoFacial.ViewModels
{
    public partial class EnrollmentViewModel : ObservableObject, IDisposable
    {
        private readonly Services.ICameraService _cameraService;
        private readonly IFaceDetector _faceDetector;
        private readonly IFaceRecognizer _faceRecognizer;

        // Propiedades enlazadas (Binding) a la UI
        [ObservableProperty]
        private WriteableBitmap _cameraFrame;

        [ObservableProperty]
        private string _fullName;

        [ObservableProperty]
        private string _employeeCode;

        [ObservableProperty]
        private string _statusMessage = "Cámara lista. Llene los datos y presione Iniciar.";

        [ObservableProperty]
        private bool _isCapturing = false;

        [ObservableProperty]
        private int _captureProgress = 0;

        [ObservableProperty]
        private bool _isSelectionMode = false;

        [ObservableProperty]
        private WriteableBitmap _selectedFace;

        // Lista temporal de Embeddings durante el proceso de captura (9 fotos)
        private readonly ObservableCollection<float[]> _pendingEmbeddings = new();

        public ObservableCollection<WriteableBitmap> CapturedFaces { get; } = new();

        public EnrollmentViewModel()
        {
            // Instanciar dependencias temporales (idealmente usar Dependency Injection - Microsoft.Extensions.DependencyInjection después)
            _cameraService = new Services.CameraService();
            _faceDetector = new Core.FaceDetector(); 
            _faceRecognizer = new Core.FaceRecognizer();

            // Suscribirse a la cámara
            _cameraService.OnFrameCaptured += OnFrameCaptured;
        }

        public async Task InitializeCameraAsync()
        {
            try
            {
                await _cameraService.StartCameraAsync();
                StatusMessage = "Cámara iniciada.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        private void OnFrameCaptured(object sender, Mat frame)
        {
            if (frame == null || frame.Empty()) return;

            // 1. Detección (Sin importar si estamos capturando para guardar, 
            // siempre dibujaremos el rectángulo si hay una cara para dar feedback visual)
            OpenCvSharp.Rect[] faces = _faceDetector.DetectFaces(frame);

            // Dibujar rectángulo verde en la primera cara encontrada
            if (faces.Length > 0)
            {
                Cv2.Rectangle(frame, faces[0], Scalar.LightGreen, 2);
            }

            // 2. Transición del frame a la Interfaz de Usuario
            // Esto necesita correr en el hilo principal de Dispatcher
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Usar OpenCvSharp.WpfExtensions para no bloquear memoria (se actualiza en lote)
                CameraFrame = frame.ToWriteableBitmap(); 
            });

            // Si estamos en proceso de registrar al empleado y encontramos una cara
            if (IsCapturing && faces.Length > 0 && CaptureProgress < 9)
            {
                ProcessCaptureIteration(frame, faces[0]);
            }

            // Liberar memoria del puntero OpenCV de cada frame inmediatamente
            frame.Dispose();
        }

        private void ProcessCaptureIteration(Mat frame, OpenCvSharp.Rect faceRect)
        {
            // Poner el ViewModel en pausa 500ms entre fotos (lo simulamos desactivando IsCapturing fugazmente)
            IsCapturing = false; 

            // Recorte de la cara
            using Mat faceCrop = frame.Clone(faceRect);
            
            // Extracción con Onnx InsightFace
            float[] embedding = _faceRecognizer.GetFaceEmbedding(faceCrop);

            _pendingEmbeddings.Add(embedding);
            CaptureProgress = _pendingEmbeddings.Count;

            Application.Current.Dispatcher.Invoke(() =>
            {
                var bmp = faceCrop.ToWriteableBitmap();
                CapturedFaces.Add(bmp);
                StatusMessage = $"Captura {CaptureProgress}/9 completada. Gire levemente la cabeza...";
            });

            if (CaptureProgress >= 9)
            {
                FinishEnrollment();
                return;
            }

            // Pausa de medio segundo antes de reaunar para la siguiente de las 9 fotos
            Task.Delay(500).ContinueWith(_ => IsCapturing = true);
        }

        [RelayCommand]
        private void StartCapture()
        {
            if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(EmployeeCode))
            {
                MessageBox.Show("Por favor ingrese el Nombre y Código de Empleado.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var connection = new Microsoft.Data.Sqlite.SqliteConnection(Data.DatabaseInitializer.ConnectionString);
                connection.Open();

                // Asegurar que el código sea único usando Dapper
                int count = Dapper.SqlMapper.QuerySingle<int>(connection, "SELECT COUNT(1) FROM Employees WHERE EmployeeCode = @Code", new { Code = EmployeeCode });
                if (count > 0)
                {
                    MessageBox.Show($"El código o ID corporativo '{EmployeeCode}' ya se encuentra registrado asociado a otra persona.", "Validación de Código", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Error validando datos base: " + ex.Message;
                return;
            }

            _pendingEmbeddings.Clear();
            Application.Current.Dispatcher.Invoke(() => CapturedFaces.Clear());
            CaptureProgress = 0;
            IsSelectionMode = false;
            SelectedFace = null;
            StatusMessage = "Iniciando secuencia de reconocimiento...";
            IsCapturing = true; // El evento de cámara tomará el control a partir de aquí
        }

        private void FinishEnrollment()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                IsSelectionMode = true;
                StatusMessage = "Secuencia completada. Seleccione su foto principal del perfil y confirme.";
                if (CapturedFaces.Count > 0)
                {
                    SelectedFace = CapturedFaces[0]; // Auto select the first one
                }
            });
        }

        [RelayCommand]
        private void FinalizeRegistration()
        {
            if (SelectedFace == null)
            {
                MessageBox.Show("Por favor, seleccione una fotografía visualmente cliqueandola o confirmela para poder guardar el registro.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            StatusMessage = "- Calculando Promedio y Verificando Conflicto -";

            try
            {
                // Promedio Matemático de los 9 arrays y re-normalización
                float[] avgEmbedding = CalculateAverageEmbedding(_pendingEmbeddings.ToList());

                // --- CONEXIÓN DIRECTA CON BASE DE DATOS E INSERCIÓN ---
                using var connection = new Microsoft.Data.Sqlite.SqliteConnection(Data.DatabaseInitializer.ConnectionString);
                connection.Open();

                // 1. Obtener todos los empleados previamente guardados para validar que no sea uno similar
                var dbEmployees = Dapper.SqlMapper.Query<Employee>(connection, "SELECT * FROM Employees").ToList();

                float maxSimilarity = 0f;
                Employee conflictEmployee = null;

                foreach (var emp in dbEmployees)
                {
                    if (emp.FaceEmbedding != null)
                    {
                        var empVector = emp.GetEmbeddingArray();
                        float similarity = SimilarityCalculator.CalculateCosineSimilarity(avgEmbedding, empVector);

                        if (similarity > maxSimilarity)
                        {
                            maxSimilarity = similarity;
                            conflictEmployee = emp;
                        }
                    }
                }

                // 2. Umbral de advertencia si se parecen más de un 75%
                if (maxSimilarity > 0.75f && conflictEmployee != null)
                {
                    var answer = MessageBox.Show($"Este rostro es muy similar a {conflictEmployee.FullName} (Similitud del {(maxSimilarity * 100):0.0}%). ¿Está seguro que desea registrar a esta nueva persona?", "Conflicto Biométrico", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                    if (answer == MessageBoxResult.No)
                    {
                        StatusMessage = "Registro cancelado por decisión del usuario.";
                        ResetForm();
                        return;
                    }
                }

                // --- 2.5 GUARDAR IMAGEN EN CARPETA --- 
                string folderPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EmployeeFaces");
                if (!System.IO.Directory.Exists(folderPath))
                {
                    System.IO.Directory.CreateDirectory(folderPath);
                }

                string imagePath = System.IO.Path.Combine(folderPath, $"{EmployeeCode}.jpg");
                SaveWriteableBitmapAsJpeg(SelectedFace, imagePath);

                // 3. Inserción Definitiva usando Dapper
                string insertSql = @"
                    INSERT INTO Employees (FullName, EmployeeCode, FaceEmbedding, CreatedAt) 
                    VALUES (@FullName, @EmployeeCode, @FaceEmbedding, @CreatedAt)";

                Dapper.SqlMapper.Execute(connection, insertSql, new
                {
                    FullName = this.FullName,
                    EmployeeCode = this.EmployeeCode,
                    FaceEmbedding = avgEmbedding.ToByteArray(),
                    CreatedAt = DateTime.UtcNow
                });

                // Extraer conteo de lo que ya está guardado para que el usuario confirme
                int totalRegistros = Dapper.SqlMapper.QuerySingle<int>(connection, "SELECT COUNT(*) FROM Employees");

                MessageBox.Show($"¡{FullName} registrado con éxito en la Base de Datos!\nSu foto ha sido guardada.\nActualmente hay {totalRegistros} persona(s) registrada(s).", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                ResetForm();

                // Notificar que hay un nuevo empleado registrado
                App.NotifyUserEnrolled();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hubo un error configurando la base de datos o guardando imagen: {ex.Message}", "Error Interno", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveWriteableBitmapAsJpeg(WriteableBitmap wbmp, string filePath)
        {
            using (var fileStream = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
            {
                BitmapEncoder encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(wbmp));
                encoder.Save(fileStream);
            }
        }

        private void ResetForm()
        {
            FullName = string.Empty;
            EmployeeCode = string.Empty;
            CaptureProgress = 0;
            _pendingEmbeddings.Clear();
            Application.Current.Dispatcher.Invoke(() => CapturedFaces.Clear());
            IsSelectionMode = false;
            SelectedFace = null;
            StatusMessage = "Cámara lista. Llene los datos y presione Iniciar.";
        }

        private float[] CalculateAverageEmbedding(System.Collections.Generic.List<float[]> embeddingsList)
        {
            float[] avg = new float[512];
            int count = embeddingsList.Count;

            // Sumatoria vectorial
            foreach (var vec in embeddingsList)
            {
                for (int i = 0; i < 512; i++)
                {
                    avg[i] += vec[i];
                }
            }

            // División
            for (int i = 0; i < 512; i++)
            {
                avg[i] /= count;
            }

            // Regla de Oro: Normalizar L2 el vector resultante
            float norm = 0f;
            foreach (float v in avg)
            {
                norm += v * v;
            }
            norm = (float)Math.Sqrt(norm);
            for (int i = 0; i < avg.Length; i++)
            {
                avg[i] /= norm;
            }

            return avg;
        }

        public void Dispose()
        {
            _cameraService.OnFrameCaptured -= OnFrameCaptured;
            _cameraService.StopCameraAsync().Wait();
            _faceDetector?.Dispose();
            _faceRecognizer?.Dispose();
        }
    }
}
