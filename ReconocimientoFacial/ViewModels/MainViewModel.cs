using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ReconocimientoFacial.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsRegistrarVisible))]
        [NotifyPropertyChangedFor(nameof(IsListadoVisible))]
        [NotifyPropertyChangedFor(nameof(IsVideoAnalysisVisible))]
        [NotifyPropertyChangedFor(nameof(IsSettingsVisible))]
        [NotifyPropertyChangedFor(nameof(IsUserManagementActive))]
        private string _currentSidebarSection = "UserManagement";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsRegistrarVisible))]
        [NotifyPropertyChangedFor(nameof(IsListadoVisible))]
        private string _currentUserManagementTab = "Registrar";

        public bool IsUserManagementActive => CurrentSidebarSection == "UserManagement";
        public bool IsRegistrarVisible => CurrentSidebarSection == "UserManagement" && CurrentUserManagementTab == "Registrar";
        public bool IsListadoVisible => CurrentSidebarSection == "UserManagement" && CurrentUserManagementTab == "ListadoEmpleados";
        public bool IsVideoAnalysisVisible => CurrentSidebarSection == "VideoAnalysis";
        public bool IsSettingsVisible => CurrentSidebarSection == "Settings";

        [RelayCommand]
        private void ChangeSection(string section)
        {
            CurrentSidebarSection = section;
        }

        [RelayCommand]
        private void ChangeTab(string tab)
        {
            CurrentUserManagementTab = tab;
        }
    }
}