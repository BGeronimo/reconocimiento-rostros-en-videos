using System;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;

namespace ReconocimientoFacial.Core
{
    public static class ImageUtils
    {
        /// <summary>
        /// Convierte una imagen a un Tensor NCHW para InsightFace (ArcFace).
        /// Normalización: (pixel - 127.5) / 127.5
        /// </summary>
        public static DenseTensor<float> ConvertToArcFaceTensor(Mat image)
        {
            // ArcFace espera exactamente 112x112
            int width = 112;
            int height = 112;

            using Mat resized = new Mat();
            Cv2.Resize(image, resized, new Size(width, height));

            using Mat rgbImage = new Mat();
            Cv2.CvtColor(resized, rgbImage, ColorConversionCodes.BGR2RGB);

            // NCHW shape: 1 batch, 3 channels (RGB), 112 height, 112 width
            var tensor = new DenseTensor<float>(new[] { 1, 3, height, width });

            // Llenar el tensor optimizando el acceso
            unsafe
            {
                byte* ptr = (byte*)rgbImage.DataPointer;
                int stride = (int)rgbImage.Step(); // bytes per row (width * 3)

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int pixelOffset = (y * stride) + (x * 3);
                        
                        // Normalización matemática para InsightFace
                        tensor[0, 0, y, x] = (ptr[pixelOffset + 0] - 127.5f) / 127.5f; // R
                        tensor[0, 1, y, x] = (ptr[pixelOffset + 1] - 127.5f) / 127.5f; // G
                        tensor[0, 2, y, x] = (ptr[pixelOffset + 2] - 127.5f) / 127.5f; // B
                    }
                }
            }

            return tensor;
        }

        /// <summary>
        /// Convierte una imagen a un Tensor NCHW para YOLO (Detección de cara).
        /// Normalización: pixel / 255.0
        /// </summary>
        public static DenseTensor<float> ConvertToYoloTensor(Mat image, int targetSize = 640)
        {
            using Mat resized = new Mat();
            Cv2.Resize(image, resized, new Size(targetSize, targetSize)); // En la vida real, YOLO prefiere mantener el aspect ratio con padding, pero usaremos resize directo para simplicidad inicial

            using Mat rgbImage = new Mat();
            Cv2.CvtColor(resized, rgbImage, ColorConversionCodes.BGR2RGB);

            var tensor = new DenseTensor<float>(new[] { 1, 3, targetSize, targetSize });

            unsafe
            {
                byte* ptr = (byte*)rgbImage.DataPointer;
                int stride = (int)rgbImage.Step();

                for (int y = 0; y < targetSize; y++)
                {
                    for (int x = 0; x < targetSize; x++)
                    {
                        int pixelOffset = (y * stride) + (x * 3);
                        
                        // Normalización matemática para YOLO v8/v10
                        tensor[0, 0, y, x] = ptr[pixelOffset + 0] / 255.0f; // R
                        tensor[0, 1, y, x] = ptr[pixelOffset + 1] / 255.0f; // G
                        tensor[0, 2, y, x] = ptr[pixelOffset + 2] / 255.0f; // B
                    }
                }
            }

            return tensor;
        }
    }
}
