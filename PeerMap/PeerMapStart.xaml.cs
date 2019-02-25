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
using SharpPcap;
using System.Threading;

namespace NCMMS.PeerMap
{
    /// <summary>
    /// PeerMapStart.xaml 的交互逻辑
    /// </summary>
    public partial class PeerMapStart : UserControl
    {
        ICaptureDevice dev;
        List<RadioButton> radioBtns = new List<RadioButton>();
        CaptureDeviceList devices;
        //ToolTip toolTip1 = new ToolTip();
        
        public PeerMapStart()
        {
            InitializeComponent();
            lblMessage.Visibility = Visibility.Collapsed;
            try
            {
                devices = CaptureDeviceList.Instance;
            }
            catch
            {
                lblMessage.Text = "没有发现可用的网络适配器,请检查网卡设置以及是否安装WinPcap";
                lblMessage.Visibility = Visibility.Visible;
                return;
            }
            for (int i = 0; i < devices.Count; i++)
            {
                ICaptureDevice dev = devices[i];
                RadioButton radioBtn = new RadioButton();
                
                radioBtn.Content = dev.Description;
                //radioBtn.AutoSize = true;
                radioBtn.HorizontalContentAlignment = HorizontalAlignment.Left;
                //radioBtn.TextAlign = ContentAlignment.MiddleLeft;
                radioBtn.ToolTip = dev.ToString();
                //toolTip1.SetToolTip(radioBtn, dev.ToString());
                radioBtns.Add(radioBtn);
                canvas.Children.Add(radioBtn);
                Canvas.SetLeft(radioBtn, 30d);
                Canvas.SetTop(radioBtn, 20.0 + 20.0 * i);
                
//                 radioBtn.Location = new Point(30, 20 + 20 * i);
//                 this.gBoxPeerMap.Controls.Add(radioBtns[i]);
            }
            if (devices.Count == 1)
            {
                radioBtns[0].IsChecked = true;
            }
        }

        private void BtnPeerMapWPF_Click(object sender, RoutedEventArgs e)
        {
            int index = 0;
            if (radioBtns.Count == 0)
            {
                MessageBox.Show("没有合适的网卡", "错误", MessageBoxButton.OK);
                return;
            }
            foreach (RadioButton R in radioBtns)
            {
                if ((bool)R.IsChecked)
                    break;
                index++;
            }
            if (index == radioBtns.Count)
            {
                MessageBox.Show("请选择需要捕获的网卡", "提示", MessageBoxButton.OK);
                return;
            }
            dev = devices[index];

            if (dev.Started)
            {
                MessageBox.Show("此网卡已经在抓包");
                return;
            }

            //传递dev参数，打开PeerMap显示页面。匿名函数
            Thread threadPeerMapPage = new Thread(delegate()
            {
                PeerMapShow peerMapShow = new PeerMapShow(dev);
                peerMapShow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                peerMapShow.ShowDialog();
            });
             threadPeerMapPage.SetApartmentState(ApartmentState.STA);
             threadPeerMapPage.Start();
            
        }
    }
}
