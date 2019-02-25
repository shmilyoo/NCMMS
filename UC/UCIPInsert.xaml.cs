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
using System.ComponentModel;
using System.Net;

namespace NCMMS.UC
{
    /// <summary>
    /// UCIPInsert.xaml 的交互逻辑
    /// </summary>
    public partial class UCIPInsert : UserControl
    {
        int ipA, ipB, ipC, ipD, ipE;
        List<IPAddress> ipList = new List<IPAddress>();
        IPAddress ipStart,ipEnd;
        public bool isIPAviable = true;
        private bool isMultiInsert = true;
        public bool IsMultiInsert
        {
            get { return isMultiInsert; }
            set
            {
                if (value)
                    SingleToMultiIPInsert();
                else
                    MultiToSingleIPInsert();
                isMultiInsert = value;
                NotifyPropertyChanged("ListenFlag");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }


        public delegate void AddIPEventHandler(object sender, AddIPEventArgs e);
        // Declare the event.
        public event AddIPEventHandler AddIPEvent;





        public UCIPInsert()
        {
            InitializeComponent();
        }

        private void MultiToSingleIPInsert()
        {
            line.Visibility = Visibility.Collapsed;
            ip5.Visibility = Visibility.Collapsed;
            
        }
        private void SingleToMultiIPInsert()
        {
            line.Visibility = Visibility.Visible;
            ip5.Visibility = Visibility.Visible;
        }

        private void btnAddIP_Click(object sender, RoutedEventArgs e)
        {
            int startNum, EndNum;
            string strIPStart = ip1.Text + "." + ip2.Text + "." + ip3.Text + "." + ip4.Text;
            if (!IPAddress.TryParse(strIPStart,out ipStart))
            {
                MessageBox.Show("不是有效的IP地址");
                return;
            }
            startNum = int.Parse(ip4.Text);
            ipList.Clear();
            ipList.Add(ipStart);
            string strIPEnd;
            if (isMultiInsert)
	        {
                strIPEnd = ip1.Text + "." + ip2.Text + "." + ip3.Text + "." + ip5.Text;
                if (!IPAddress.TryParse(strIPEnd,out ipEnd))
                {
                    MessageBox.Show("不是有效的IP地址");
                    return;
                }
                EndNum = int.Parse(ip5.Text);
                if (startNum > EndNum)
                {
                    MessageBox.Show("起始IP地址大于终止IP地址");
                    return;
                }
                startNum++;
                while (startNum <= EndNum)
                {
                    ipList.Add(IPAddress.Parse(ip1.Text + "." + ip2.Text + "." + ip3.Text + "." + startNum.ToString()));
                    startNum++;
                }
	        }
            if (AddIPEvent != null)
                AddIPEvent(this, new AddIPEventArgs(ipList));
        }

        private void ip1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.OemPeriod || e.Key == Key.Decimal)
            {
                ip2.Focus();
                ip2.SelectAll();
            }
            else if (e.Key == Key.Enter)
            {
                btnAddIP.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
        }

        private void ip1_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ip1.Text.Length > 0)
            {
                if (ip1.Text[ip1.Text.Length - 1] == '.')
                {
                    ip1.Text = ip1.Text.TrimEnd('.');
                }
                if (int.TryParse(ip1.Text, out ipA) && ipA < 255 && ipA > 0)
                {
                    ip1.Foreground = Brushes.Black;
                }
                else
                {
                    ip1.Foreground = Brushes.Red;
                }

            }
        }

        private void ip2_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.OemPeriod || e.Key == Key.Decimal)
            {
                ip3.Focus();
                ip3.SelectAll();
            }
            else if (e.Key == Key.Enter)
            {
                btnAddIP.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
        }

        private void ip2_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ip2.Text.Length > 0)
            {
                if (ip2.Text[ip2.Text.Length - 1] == '.')
                {
                    ip2.Text = ip2.Text.TrimEnd('.');
                }
                if (int.TryParse(ip2.Text, out ipB) && ipB < 255 && ipB >= 0)
                {
                    ip2.Foreground = Brushes.Black;
                }
                else
                {
                    ip2.Foreground = Brushes.Red;
                }

            }
        }
        private void ip3_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.OemPeriod || e.Key == Key.Decimal)
            {
                ip4.Focus();
                ip4.SelectAll();
            }
            else if (e.Key == Key.Enter)
            {
                btnAddIP.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
        }

        private void ip3_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ip3.Text.Length > 0)
            {
                if (ip3.Text[ip3.Text.Length - 1] == '.')
                {
                    ip3.Text = ip3.Text.TrimEnd('.');
                }
                if (int.TryParse(ip3.Text, out ipC) && ipC < 255 && ipC >= 0)
                {
                    ip3.Foreground = Brushes.Black;
                }
                else
                {
                    ip3.Foreground = Brushes.Red;
                }

            }
        }
        private void ip4_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Subtract || e.Key == Key.OemMinus)
            {
                if (isMultiInsert)
                {
                    ip5.Focus();
                    ip5.SelectAll();
                }
                else
                    btnAddIP.Focus();
            }
            else if (e.Key == Key.Enter)
            {
                btnAddIP.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
        }

        private void ip4_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ip4.Text.Length > 0)
            {
                if (ip4.Text[ip4.Text.Length - 1] == '-')
                {
                    ip4.Text = ip4.Text.TrimEnd('-');
                }
                if (int.TryParse(ip4.Text, out ipD) && ipD < 255 && ipD > 0)
                {
                    ip4.Foreground = Brushes.Black;
                }
                else
                {
                    ip4.Foreground = Brushes.Red;
                }
            }
        }
        private void ip5_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btnAddIP.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
        }

        private void ip5_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ip5.Text.Length > 0)
            {
                if (int.TryParse(ip5.Text, out ipE) && ipE < 255 && ipE > 0)
                {
                    ip5.Foreground = Brushes.Black;
                }
                else
                {
                    ip5.Foreground = Brushes.Red;
                }

            }
        }


    }

     public class AddIPEventArgs
     {
        public AddIPEventArgs(List<IPAddress> ipList) { IPList = ipList; }
        public List<IPAddress> IPList { get; private set; } // readonly
     }
}
