using System.Configuration;
using System.Data;
using System.Windows;
using DouyinLiveReceiver.ViewModels;

namespace DouyinLiveReceiver
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnExit(ExitEventArgs e)
        {
            // 确保在应用关闭时清理资源
            if (MainWindow?.DataContext is MainViewModel viewModel)
            {
                viewModel.Dispose();
            }
            base.OnExit(e);
        }
    }

}
