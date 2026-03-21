using System.Windows;
using System.Windows.Controls;
using ReconocimientoFacial.ViewModels;

namespace ReconocimientoFacial.Views
{
    public partial class VideoAnalysisView : UserControl
    {
        public VideoAnalysisView()
        {
            InitializeComponent();
            DataContext = new VideoAnalysisViewModel();
        }

        private void UserControl_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (DataContext is VideoAnalysisViewModel vm)
                {
                    vm.HandleDroppedFiles(files);
                }
            }
        }
    }
}
