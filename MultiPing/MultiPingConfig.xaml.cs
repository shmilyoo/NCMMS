using System.Windows;
using System.Windows.Controls;

namespace NCMMS.MultiPing
{
    /// <summary>
    /// MultiPingConfig.xaml 的交互逻辑
    /// </summary>
    public partial class MultiPingConfig : MyWindow
    {
        public MultiPingConfig()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(MultiPingConfig_Loaded);
        }

        void MultiPingConfig_Loaded(object sender, RoutedEventArgs e)
        {
            timeOut.Text = Properties.Settings.Default["MPTimeOut"].ToString();
            packetSize.Text = Properties.Settings.Default["MPDataSize"].ToString();
            ttl.Text = Properties.Settings.Default["MPTTL"].ToString();
            packetNum.Text = Properties.Settings.Default["MPNumber"].ToString();
            sendInterval.Text = Properties.Settings.Default["MPInterval"].ToString();
            isShowOpenFile.IsChecked = (bool)Properties.Settings.Default["MPIsAlarm"];
            openFileUrl.Text = Properties.Settings.Default["MPAlarmFileUrl"].ToString();
            if ((bool)isShowOpenFile.IsChecked)
            {
                openFileUrl.Visibility = Visibility.Visible;
                btnOpenFile.Visibility = Visibility.Visible;
            }
            else
            {
                openFileUrl.Visibility = Visibility.Hidden;
                btnOpenFile.Visibility = Visibility.Hidden;
            }
        }

        private void isShowOpenFile_Click(object sender, RoutedEventArgs e)
        {
            bool isShow = (bool)isShowOpenFile.IsChecked;
            if (isShow)
            {
                openFileUrl.Visibility = Visibility.Visible;
                btnOpenFile.Visibility = Visibility.Visible;
                btnPlaySound.Visibility = Visibility.Visible;
            }
            else
            {
                openFileUrl.Visibility = Visibility.Hidden;
                btnOpenFile.Visibility = Visibility.Hidden;
                btnPlaySound.Visibility = Visibility.Hidden;
            }
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {

            int mptimeOut, mppacketSize, mpttl, mppacketNum, mpsendInterval;

            if (!int.TryParse(timeOut.Text, out mptimeOut) || mptimeOut <= 0)
            {
                MessageBox.Show("超时时间必须是正整数");
                return;
            }
            if (!int.TryParse(packetSize.Text, out mppacketSize) || mppacketSize < 0)  // || mppacketSize > 1472
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

            //更新保存用户设置
            Properties.Settings.Default["MPTimeOut"] = mptimeOut;
            Properties.Settings.Default["MPDataSize"] = mppacketSize;
            Properties.Settings.Default["MPTTL"] = mpttl;
            Properties.Settings.Default["MPNumber"] = mppacketNum;
            Properties.Settings.Default["MPInterval"] = mpsendInterval;
            Properties.Settings.Default["MPIsAlarm"] = isShowOpenFile.IsChecked;
            if ((bool)isShowOpenFile.IsChecked)
                Properties.Settings.Default["MPAlarmFileUrl"] = openFileUrl.Text;
            Properties.Settings.Default.Save();

            //更新主页面的属性以及每个ping对象的参数
            MultiPing multiPing = (MultiPing)this.Owner;
            multiPing.IsAlarm = (bool)isShowOpenFile.IsChecked;
            multiPing.alarmPlayer.SoundLocation = openFileUrl.Text;
            foreach (PingTarget pt in multiPing.pingTargetList1)
            {
                pt.TimeOut = mptimeOut;
                pt.DataSize = mppacketSize;
                pt.TTL = mpttl;
                pt.Number = mppacketNum;
                pt.Interval = mpsendInterval;
            }
            this.Close();
        }

        private void btnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "声音文件(wav,mp3)|*.wav;*.mp3";
            if ((bool)openFileDialog.ShowDialog())
            {
                openFileUrl.Text = openFileDialog.FileName;
            }
        }
        private void btnPlaySound_Click(object sender, RoutedEventArgs e)
        {
            System.Media.SoundPlayer alarm = new System.Media.SoundPlayer();
            try
            {
                alarm.SoundLocation = openFileUrl.Text;
                //alarm.Load();
                alarm.Play();
            }
            catch
            {
                MessageBox.Show("指定的声音文件格式或地址错误");
            }
            finally
            {
                alarm.Dispose();
            }
        }
             
    }
}
