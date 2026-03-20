using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;

namespace ReconocimientoFacial.Services
{
    public interface ICameraService
    {
        event EventHandler<Mat> OnFrameCaptured;
        bool IsRunning { get; }
        
        Task StartCameraAsync(int deviceIndex = 0);
        Task StopCameraAsync();
    }

    public class CameraService : ICameraService
    {
        public event EventHandler<Mat> OnFrameCaptured;
        
        private VideoCapture _capture;
        private CancellationTokenSource _cts;
        private Task _captureTask;
        
        public bool IsRunning => _capture != null && _capture.IsOpened() && !_cts.IsCancellationRequested;

        public Task StartCameraAsync(int deviceIndex = 0)
        {
            if (IsRunning) return Task.CompletedTask;

            _cts = new CancellationTokenSource();
            
            _captureTask = Task.Run(async () => 
            {
                _capture = new VideoCapture(deviceIndex);
                if (!_capture.IsOpened())
                {
                    throw new Exception("No se pudo acceder a la cámara seleccionada.");
                }

                using Mat frame = new Mat();
                
                while (!_cts.Token.IsCancellationRequested)
                {
                    if (_capture.Read(frame))
                    {
                        if (!frame.Empty())
                        {
                            // Disparar el evento con el fotograma capturado
                            OnFrameCaptured?.Invoke(this, frame.Clone());
                        }
                    }

                    // Pausa ligera para estabilizar el recolector de basura y no quemar el 100% CPU
                    await Task.Delay(30, _cts.Token);
                }

                _capture.Release();
                _capture.Dispose();
                _capture = null;

            }, _cts.Token).ContinueWith(t => 
            {
                // Manejar error silencioso o registrar log si falló
                if (t.IsFaulted)
                {
                    Console.WriteLine($"Error de cámara: {t.Exception.InnerException?.Message}");
                }
            });

            return Task.CompletedTask;
        }

        public async Task StopCameraAsync()
        {
            if (!IsRunning) return;

            _cts?.Cancel();
            
            if (_captureTask != null)
            {
                await _captureTask;
            }
        }
    }
}
