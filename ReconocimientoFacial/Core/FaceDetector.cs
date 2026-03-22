using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using OpenCvSharp.Dnn;

namespace ReconocimientoFacial.Core
{
    public class FaceDetector : IFaceDetector
    {
        private InferenceSession _session;

        // YOLO base model path expects the onnx in AI_Models folder
        public FaceDetector(string modelFileName = "yolo_face.onnx")
        {
            var modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AI_Models", modelFileName);

            if (!File.Exists(modelPath))
            {
                Console.WriteLine($"[ADVERTENCIA] No se encontró el modelo: {modelPath}");
                return;
            }

            // Opciones de configuración para aceleración con CPU
            var options = new SessionOptions();
            options.AppendExecutionProvider_CPU();

            // Intialize Inference Session
            _session = new InferenceSession(modelPath, options);
        }

        public Rect[] DetectFaces(Mat frame)
        {
            if (_session == null || frame == null || frame.Empty())
            {
                return Array.Empty<Rect>();
            }

            int targetSize = 640;

            // Paso 1: Usar nuestra utilidad ImageUtils para crear el Tensor NCHW Normalizado YOLO aplicando Letterbox
            var inputTensor = ImageUtils.ConvertToYoloTensor(frame, out float ratio, out float padX, out float padY, targetSize);

            // Paso 2: Ejecutar Inferencia de YOLO
            string inputName = _session.InputMetadata.Keys.First(); 
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(inputName, inputTensor)
            };

            // Ejecuta el ONNX Runtime.
            using var results = _session.Run(inputs);

            // Extraer tensor de resultados
            var outputName = _session.OutputMetadata.Keys.First();
            var outputTensor = results.First(r => r.Name == outputName).AsTensor<float>();

            var dimensions = outputTensor.Dimensions;
            // Salida de YOLO v8 es típicamente: [1, features (5 a 15), 8400 (anclas)]
            if (dimensions.Length < 3) return Array.Empty<Rect>();

            int rows = dimensions[1];
            int cols = dimensions[2];

            // Determinar orientación transpuesta en diferentes versiones de ONNX
            bool isTransposed = rows > cols; 
            int numFeatures = isTransposed ? cols : rows;
            int numAnchors = isTransposed ? rows : cols;

            var boxes = new List<Rect>();
            var confidences = new List<float>();

            float confidenceThreshold = 0.5f;

            for (int i = 0; i < numAnchors; i++)
            {
                // Extraer atributos dependiendo de la orientación
                float xCenter = isTransposed ? outputTensor[0, i, 0] : outputTensor[0, 0, i];
                float yCenter = isTransposed ? outputTensor[0, i, 1] : outputTensor[0, 1, i];
                float width   = isTransposed ? outputTensor[0, i, 2] : outputTensor[0, 2, i];
                float height  = isTransposed ? outputTensor[0, i, 3] : outputTensor[0, 3, i];
                float conf    = isTransposed ? outputTensor[0, i, 4] : outputTensor[0, 4, i];

                if (conf > confidenceThreshold)
                {
                    // Remover el padding de Letterbox y escalar de vuelta a la imagen original
                    float rawX = (xCenter - padX) / ratio;
                    float rawY = (yCenter - padY) / ratio;
                    float rawW = width / ratio;
                    float rawH = height / ratio;

                    int x = (int)(rawX - rawW / 2);
                    int y = (int)(rawY - rawH / 2);
                    int w = (int)rawW;
                    int h = (int)rawH;

                    // Clamp a las dimensiones para evitar colapsos al recortar fuera de la imagen
                    x = Math.Max(0, x);
                    y = Math.Max(0, y);
                    w = Math.Min(frame.Width - x, w);
                    h = Math.Min(frame.Height - y, h);

                    // Solo guardamos si el Bounding Box tiene algo de sentido (Evitar errores de OpenCV)
                    if (w > 0 && h > 0)
                    {
                        boxes.Add(new Rect(x, y, w, h));
                        confidences.Add(conf);
                    }
                }
            }

            if (boxes.Count == 0) return Array.Empty<Rect>();

            // Supresión de no-máximos (NMS): Evita múltiples recuadros solapados
            CvDnn.NMSBoxes(boxes, confidences, confidenceThreshold, 0.4f, out int[] indices);

            return indices.Select(i => boxes[i]).ToArray();
        }

        public void Dispose()
        {
            _session?.Dispose();
        }
    }
}
