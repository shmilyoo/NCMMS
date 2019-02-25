using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Net;

namespace NCMMS.UC
{
    /// <summary>
    /// UCIPInsert.xaml 的交互逻辑
    /// </summary>
    public partial class UCIPTextBox : UserControl
    {
        int ipA, ipB, ipC, ipD;
        IPAddress ip;
        bool isValidIP = false;

        /// <summary>
        /// 如果IP地址无效，返回null
        /// </summary>
        public IPAddress IP
        {
            get
            {
                if (isValidIP)
                    return ip;
                else
                    return null;
            }
            set
            {
                ip = value;
                byte[] bytes = value.GetAddressBytes();
                ip1.Text = bytes[0].ToString();
                ip2.Text = bytes[1].ToString();
                ip3.Text = bytes[2].ToString();
                ip4.Text = bytes[3].ToString();
            }

        }

        public UCIPTextBox()
        {
            InitializeComponent();
        }

        private void MultiToSingleIPInsert()
        {

        }

        public bool InsertIP(string strIP)
        {
            if (IPAddress.TryParse(strIP, out ip))
            {
                string[] strIPs = strIP.Trim().Split('.');
                ip1.Text = strIPs[0];
                ip2.Text = strIPs[1];
                ip3.Text = strIPs[2];
                ip4.Text = strIPs[3];
                return true;
            }
            else
            {
                return false;
            }
        }

        private void ip1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.OemPeriod || e.Key == Key.Decimal)
            {
                ip2.Focus();
                ip2.SelectAll();
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
                if (int.TryParse(ip1.Text, out ipA) && ipA <= 255 && ipA >= 0)
                {
                    ip1.Foreground = Brushes.Black;
                }
                else
                {
                    ip1.Foreground = Brushes.Red;
                }
            }
            SetIP();
        }

        private void ip2_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.OemPeriod || e.Key == Key.Decimal)
            {
                ip3.Focus();
                ip3.SelectAll();
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
                if (int.TryParse(ip2.Text, out ipB) && ipB <= 255 && ipB >= 0)
                {
                    ip2.Foreground = Brushes.Black;
                }
                else
                {
                    ip2.Foreground = Brushes.Red;
                }
            }
            SetIP();
        }
        private void ip3_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.OemPeriod || e.Key == Key.Decimal)
            {
                ip4.Focus();
                ip4.SelectAll();
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
                if (int.TryParse(ip3.Text, out ipC) && ipC <= 255 && ipC >= 0)
                {
                    ip3.Foreground = Brushes.Black;
                }
                else
                {
                    ip3.Foreground = Brushes.Red;
                }
            }
            SetIP();
        }

        private void ip4_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ip4.Text.Length > 0)
            {
                if (int.TryParse(ip4.Text, out ipD) && ipD <= 255 && ipD >= 0)
                {
                    ip4.Foreground = Brushes.Black;
                }
                else
                {
                    ip4.Foreground = Brushes.Red;
                }
            }
            SetIP();
        }
        private void SetIP()
        {
            if (IPAddress.TryParse(ip1.Text + "." + ip2.Text + "." + ip3.Text + "." + ip4.Text, out ip))
                isValidIP = true;
            else
                isValidIP = false;
        }
        public void Clear()
        {
            ip1.Text = "";
            ip2.Text = "";
            ip3.Text = "";
            ip4.Text = "";
        }
    }

}
