using System;
using System.Numerics.Tensors;

namespace ReconocimientoFacial.Core
{
    public static class SimilarityCalculator
    {
        /// <summary>
        /// Calcula la Similitud del Coseno entre dos embeddings usando operaciones SIMD para rendimiento extremo.
        /// Retorna un valor entre -1 y 1 (1 = exactitud perfecta).
        /// </summary>
        public static float CalculateCosineSimilarity(ReadOnlySpan<float> vectorA, ReadOnlySpan<float> vectorB)
        {
            if (vectorA.Length != vectorB.Length)
                throw new ArgumentException("Los vectores deben tener la misma dimensión.");

            // En .NET 8/9/10, TensorPrimitives utiliza aceleración por hardware (SIMD, AVX2, AVX-512) 
            // de manera automática si está disponible en la CPU.
            return TensorPrimitives.CosineSimilarity(vectorA, vectorB);
        }
    }
}
