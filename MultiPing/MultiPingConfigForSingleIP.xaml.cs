using System.Windows;
using System.Windows.Controls;

namespace NCMMS.MultiPing
{
    /// <summary>
    /// MultiPingConfig.xaml 的交互逻辑
    /// </summary>
    public partial class MultiPingConfigForSingleIP : MyWindow
    {
        private PingTarget pt;
        public MultiPingConfigForSingleIP(PingTarget _pt)
        {
            InitializeComponent();
            pt = _pt;
            WindowTitle = pt.StrIP + " 的参数配置";
            this.Loaded += new RoutedEventHandler(MultiPingConfigForSingleIP_Loaded);
        }

        void MultiPingConfigForSingleIP_Loaded(object sender, RoutedEventArgs e)
        {
            timeOut.Text = pt.TimeOut.ToString();
            packetSize.Text = pt.DataSize.ToString();
            ttl.Text = pt.TTL.ToString();
            packetNum.Text = pt.Number.ToString();
            sendInterval.Text = pt.Interval.ToString();
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            int mptimeOut, mppacketSize, mpttl, mppacketNum, mpsendInterval;

            if (!int.TryParse(timeOut.Text, out mptimeOut) || mptimeOut <= 0)
            {
                MessageBox.Show("超时时间必须是正整数");
                return;
            }
            if (!int.TryParse(packetSize.Text, out mppacketSize) || mppacketSize < 0) // || mppacketSize > 1472
            {
                MessageBox.Show("包大小输入不正确");
                return;
            }
            if (!int.TryParse(ttl.Text, out mpttl) || mpttl <= 0)
            {
                MessageBox.Show("TTL值输入不正确");
                return;
            }
            if (!int.TryParse(packetNum.Text, out mppacketNum) || mppacketNum < 0)
            {
                MessageBox.Show("包数量输入不正确");
                return;
            }
            if (!int.TryParse(sendInterval.Text, out mpsendInterval) || mpsendInterval <= 0)
            {
                MessageBox.Show("发送间隔输入不正确");
                return;
            }
            pt.TimeOut = mptimeOut;
            pt.DataSize = mppacketSize;
            pt.TTL = mpttl;
            pt.Number = mppacketNum;
            pt.Interval = mpsendInterval;
            this.Close();
        }

             
    }
}
