using System;

namespace ReconocimientoFacial.Models
{
    public class DetectionLog
    {
        public int Id { get; set; }
        
        public string VideoFileName { get; set; }
        
        // Foreing Key hacia Employee
        public int EmployeeId { get; set; }
        
        // Momento exacto del video (ejemplo: 00:01:15)
        public string Timestamp { get; set; }
        
        // Porcentaje de similitud
        public double Confidence { get; set; }
    }
}