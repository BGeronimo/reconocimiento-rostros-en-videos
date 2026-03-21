using System;

namespace ReconocimientoFacial.Models
{
    public class Employee
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string EmployeeCode { get; set; }
        
        // Propiedad que guarda los bytes de la base de datos
        public byte[] FaceEmbedding { get; set; }
        
        public DateTime CreatedAt { get; set; }

        // Ruta de la imagen local
        public string LocalProfileImagePath => System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EmployeeFaces", $"{EmployeeCode}.jpg");
        
        // Propiedad auxiliar para trabajar en memoria (no se guarda en SQLite)
        public float[] GetEmbeddingArray()
        {
            return Core.ExtensionMethods.ToFloatArray(FaceEmbedding);
        }
    }
}