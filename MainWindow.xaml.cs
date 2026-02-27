using System;
using System.Windows;
using System.Windows.Data;
using DouyinLiveReceiver.ViewModels;

namespace DouyinLiveReceiver
{
    public class BooleanToTextConverter : IValueConverter
    {
        public string TrueText { get; set; } = "";
        public string FalseText { get; set; } = "";

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? TrueText : FalseText;
            }
            return FalseText;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string strValue)
            {
                return strValue == TrueText;
            }
            return false;
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
    }
}