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
        // Evento global para notificar sobre nuevo registro
        public static event System.EventHandler UserEnrolled;

        public static void NotifyUserEnrolled()
        {
            UserEnrolled?.Invoke(null, System.EventArgs.Empty);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Inicializar base de datos
            Data.DatabaseInitializer.Initialize();
        }
    }

}
