using System.Windows.Controls;
using ReconocimientoFacial.ViewModels;

namespace ReconocimientoFacial.Views
{
    public partial class EnrollmentView : UserControl
    {
        public EnrollmentView()
        {
            InitializeComponent();
            
            // Iniciar la cámara al cargar la vista
            this.Loaded += async (s, e) => 
            {
                if (DataContext is EnrollmentViewModel vm)
                {
                    await vm.InitializeCameraAsync();
                }
            };
            
            // Detener cámara al salir
            this.Unloaded += (s, e) => 
            {
                if (DataContext is EnrollmentViewModel vm)
                {
                    vm.Dispose();
                }
            };
        }
    }
}