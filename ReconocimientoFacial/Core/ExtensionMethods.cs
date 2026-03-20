using System;
using System.Runtime.InteropServices;

namespace ReconocimientoFacial.Core
{
    public static class ExtensionMethods
    {
        public static byte[] ToByteArray(this float[] floatArray)
        {
            if (floatArray == null) return null;
            var byteArray = new byte[floatArray.Length * 4];
            Buffer.BlockCopy(floatArray, 0, byteArray, 0, byteArray.Length);
            return byteArray;
        }

        public static float[] ToFloatArray(this byte[] byteArray)
        {
            if (byteArray == null) return null;
            if (byteArray.Length % 4 != 0) throw new ArgumentException("Byte array length should be divisible by 4");
            var floatArray = new float[byteArray.Length / 4];
            Buffer.BlockCopy(byteArray, 0, floatArray, 0, byteArray.Length);
            return floatArray;
        }
    }
}
