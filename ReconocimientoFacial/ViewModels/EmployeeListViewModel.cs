using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Dapper;
using Microsoft.Data.Sqlite;

namespace ReconocimientoFacial.ViewModels
{
    public partial class EmployeeListViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<Models.Employee> _employees;

        [ObservableProperty]
        private string _searchQuery = "";

        // Para filtrar u obtener datos iniciales
        public EmployeeListViewModel()
        {
            Employees = new ObservableCollection<Models.Employee>();
            LoadEmployees();
        }

        public void LoadEmployees()
        {
            using var connection = new SqliteConnection(Data.DatabaseInitializer.ConnectionString);
            connection.Open();

            string query = "SELECT * FROM Employees";
            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                query += " WHERE FullName LIKE @Search OR EmployeeCode LIKE @Search";
            }

            var results = connection.Query<Models.Employee>(query, new { Search = $"%{SearchQuery}%" });
            
            Employees.Clear();
            foreach (var emp in results)
            {
                Employees.Add(emp);
            }
        }

        partial void OnSearchQueryChanged(string value)
        {
            LoadEmployees();
        }
    }
}
