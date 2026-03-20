using System;
using OpenCvSharp;

namespace ReconocimientoFacial.Core
{
    public interface IFaceDetector : IDisposable
    {
        /// <summary>
        /// Recibe una imagen como Matrice (Mat) y devuelve un array con las cajas de las caras detectadas.
        /// </summary>
        Rect[] DetectFaces(Mat frame);
    }

    public interface IFaceRecognizer : IDisposable
    {
        /// <summary>
        /// Recibe un rostro recortado (Mat) y devuelve el Embedding asociado de 512 dimensiones en un float[].
        /// </summary>
        float[] GetFaceEmbedding(Mat faceCrop);
    }
}
