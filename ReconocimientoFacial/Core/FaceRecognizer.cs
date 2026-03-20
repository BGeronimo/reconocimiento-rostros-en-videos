using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;

namespace ReconocimientoFacial.Core
{
    public class FaceRecognizer : IFaceRecognizer
    {
        private InferenceSession _session;

        // InsightFace model expect 112x112 input dimensions (ej. w600k_r50.onnx / arcface)
        public FaceRecognizer(string modelFileName = "insightface.onnx")
        {
            var modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AI_Models", modelFileName);

            if (!File.Exists(modelPath))
            {
                // Solo para desarrollo si no hay modelo físico: no explotará, pero el reconocimiento se salteará
                Console.WriteLine($"[ADVERTENCIA] No se encontró el modelo: {modelPath}");
                return;
            }

            // Opciones de configuración para aceleración con CPU.
            var options = new SessionOptions();
            options.AppendExecutionProvider_CPU();

            // Initializing session
            _session = new InferenceSession(modelPath, options);
        }

        public float[] GetFaceEmbedding(Mat faceCrop)
        {
            if (_session == null || faceCrop == null || faceCrop.Empty())
            {
                return new float[512]; // Fallback o si el modelo ONNX no está montado localmente aún.
            }

            // Paso 1: Convertir la imagen a un Tensor (112x112 NCHW formato -127.5/127.5) usando nuestra utilidad
            var inputTensor = ImageUtils.ConvertToArcFaceTensor(faceCrop);

            // Paso 2: Preparar la entrada para la sesión de ONNX
            string inputName = _session.InputMetadata.Keys.First(); // Obtener dinámicamente el nombre de la entrada (ej. "data" o "input.1")
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(inputName, inputTensor)
            };

            // Paso 3: Inferencia Matemática dentro de la "Caja Negra"
            using var results = _session.Run(inputs);

            // Paso 4: Extraer el resultado (El Embedding: un Tensor flotante unidimensional de tamaño 512)
            string outputName = _session.OutputMetadata.Keys.First(); // Nombre de salida (ej. "fc1" o "683")
            var outputTensor = results.First(r => r.Name == outputName).AsTensor<float>();

            // Convertir a un Array estándar de C#
            float[] embedding = outputTensor.ToArray();

            // L2 Normalization (InsightFace requiere que el vector esté normalizado al final para que Cosine Similarity funcione perfecto)
            NormalizeL2(embedding);

            return embedding;
        }

        private void NormalizeL2(float[] vector)
        {
            float norm = 0f;
            foreach (float v in vector)
            {
                norm += v * v;
            }
            norm = (float)Math.Sqrt(norm);

            for (int i = 0; i < vector.Length; i++)
            {
                vector[i] /= norm;
            }
        }

        public void Dispose()
        {
            _session?.Dispose();
        }
    }
}
