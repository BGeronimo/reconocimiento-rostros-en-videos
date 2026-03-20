using System.Configuration;
using System.Data;
using System.Windows;

namespace ReconocimientoFacial
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Inicializar base de datos
            Data.DatabaseInitializer.Initialize();
        }
    }

}
