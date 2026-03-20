using System;
using System.IO;
using Dapper;
using Microsoft.Data.Sqlite;

namespace ReconocimientoFacial.Data
{
    public static class DatabaseInitializer
    {
        public static string DbPath { get; private set; }
        public static string ConnectionString => $"Data Source={DbPath}";

        public static void Initialize()
        {
            // La base de datos se debe crear en la misma carpeta del ejecutable
            string appDataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            if (!Directory.Exists(appDataFolder))
            {
                Directory.CreateDirectory(appDataFolder);
            }

            DbPath = Path.Combine(appDataFolder, "database.db");

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            // Creación de tabla Employees
            string createEmployeesTable = @"
                CREATE TABLE IF NOT EXISTS Employees (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    FullName TEXT NOT NULL,
                    EmployeeCode TEXT NOT NULL,
                    FaceEmbedding BLOB NOT NULL,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                );";
            
            connection.Execute(createEmployeesTable);

            // Creación de tabla DetectionLogs
            string createDetectionLogsTable = @"
                CREATE TABLE IF NOT EXISTS DetectionLogs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    VideoFileName TEXT NOT NULL,
                    EmployeeId INTEGER NOT NULL,
                    Timestamp TEXT NOT NULL,
                    Confidence REAL NOT NULL,
                    FOREIGN KEY(EmployeeId) REFERENCES Employees(Id)
                );";

            connection.Execute(createDetectionLogsTable);
        }
    }
}
