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
using SnmpSharpNet;
using System.Net;
using System.Collections.ObjectModel;
using NCMMS.CommonClass;
using System.Threading;

namespace NCMMS.Topology
{
    /// <summary>
    /// TopoStart.xaml 的交互逻辑
    /// </summary>
    public partial class TopoStart : UserControl
    {
        ObservableCollection<Subnet> subnetList = new ObservableCollection<Subnet>();
        public TopoStart()
        {
            InitializeComponent();
            lbSubnetlist.ItemsSource = subnetList;

            //实验室调试用
            tbIPClue.IP = IPAddress.Parse("10.0.199.1");
            tbIP.IP = IPAddress.Parse("3.0.0.0");
            tbMask.IP = IPAddress.Parse("255.0.0.0");
        }

        private void btnDelSubnet_Click(object sender, RoutedEventArgs e)
        {
            int count = lbSubnetlist.SelectedItems.Count;
            if (count == 0)
            {
                MessageBox.Show("请在选择需要删除的子网");
                return;
            }
            for (int i = count - 1; i >= 0; i--)
            {
                subnetList.Remove(lbSubnetlist.SelectedItems[i] as Subnet);
            }
        }

        private void btnClearSubnet_Click(object sender, RoutedEventArgs e)
        {
            subnetList.Clear();
        }

        private void btnAddSubnet_Click(object sender, RoutedEventArgs e)
        {
            if (tbIP.IP == null && tbMask.IP == null)
            {
                MessageBox.Show("子网地址或掩码不是正确的IP格式");
                return;
            }
            IpAddress ip = new IpAddress(tbIP.IP);
            IpAddress mask = new IpAddress(tbMask.IP);
            if (!mask.IsValidMask())
            {
                MessageBox.Show("掩码的格式不正确");
                return;
            }
            IpAddress subnet = ip.GetSubnetAddress(mask);
            subnetList.Add(new Subnet(subnet, mask));
            tbIP.Clear();
            tbMask.Clear();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            IPAddress ip = tbIPClue.IP;
            //IPAddress ip = IPAddress.Parse("192.168.3.1");
            if (ip == null)
            {
                MessageBox.Show("初始IP地址格式不正确");
                return;
            }
            int maxStep = 0;
            string stepstr = tbMaxStep.Text.Trim();

            if (stepstr.Length > 0)
            {
                try
                {
                    maxStep = Convert.ToInt32(stepstr);
                }
                catch
                {
                    MessageBox.Show("跳数需为正整数");
                    return;
                }
                if (maxStep < 0)
                {
                    MessageBox.Show("跳数需为正整数");
                    return;
                }
            }
            Thread t = new Thread(() =>
            {
                TopoMain topo;
                if (subnetList.Count > 0)
                {
                    List<Subnet> list = subnetList.ToList<Subnet>();
                    topo = new TopoMain(new IpAddress(ip), maxStep, list);
                }
                else
                    topo = new TopoMain(new IpAddress(ip), maxStep);
                topo.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                topo.ShowDialog();
            });
            t.SetApartmentState(ApartmentState.STA);
            t.IsBackground = true;
            t.Start();

        }

    }
}
