using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;

namespace ReconocimientoFacial
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new ViewModels.MainViewModel();
        }

        private void MenuToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (SidebarColumn.Width.Value > 80)
            {
                SidebarColumn.Width = new GridLength(80);
                MenuText1.Visibility = Visibility.Collapsed;
                MenuText2.Visibility = Visibility.Collapsed;
                MenuText3.Visibility = Visibility.Collapsed;
                LogoText1.Visibility = Visibility.Collapsed;
                LogoText2.Visibility = Visibility.Collapsed;
            }
            else
            {
                SidebarColumn.Width = new GridLength(220);
                MenuText1.Visibility = Visibility.Visible;
                MenuText2.Visibility = Visibility.Visible;
                MenuText3.Visibility = Visibility.Visible;
                LogoText1.Visibility = Visibility.Visible;
                LogoText2.Visibility = Visibility.Visible;
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            // Ocultar la ventana inmediatamente para respuesta táctil de la UI
            this.Hide();

            // OpenCV / Hardware a veces se queda colgado en los finalizadores de .NET.
            // Al matar el proceso directamente, obligamos a Windows a revocar el acceso a la webcam al instante.
            Process.GetCurrentProcess().Kill();
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
                this.DragMove();
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
                this.WindowState = WindowState.Maximized;
            else
                this.WindowState = WindowState.Normal;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}