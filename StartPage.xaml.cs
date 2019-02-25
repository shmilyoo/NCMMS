using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NCMMS
{
    /// <summary>
    /// StartPage.xaml 的交互逻辑
    /// </summary>
    public partial class StartPage : UserControl
    {
        public StartPage()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(StartPage_Loaded);
        }

        void StartPage_Loaded(object sender, RoutedEventArgs e)
        {
            double x = a.Text.Length;
            double xx = a.ActualWidth;
            double xxx = a.Width;
            double x1 = b.Text.Length;
            double xx1 = b.ActualWidth;
            double xx1x = b.Width;
        }
    }
}
