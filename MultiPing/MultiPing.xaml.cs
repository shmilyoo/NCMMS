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
using System.Windows.Shapes;
using System.ComponentModel;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Net;
using System.Media;
using System.Threading;

namespace NCMMS.MultiPing
{
    /// <summary>
    /// MultiPing.xaml 的交互逻辑
    /// </summary>
    public partial class MultiPing : MyWindow, INotifyPropertyChanged
    {
        StringCollection strIPList1, strIPList2;
        public ObservableCollection<PingTarget> pingTargetList1 = new ObservableCollection<PingTarget>();
        public ObservableCollection<PingTarget> pingTargetList2 = new ObservableCollection<PingTarget>();
        private bool isSingleColumn, isAlarm;
        public SoundPlayer alarmPlayer = new SoundPlayer();
        System.Windows.Threading.DispatcherTimer alarmTimer = new System.Windows.Threading.DispatcherTimer();

        public bool IsAlarm
        {
            get { return isAlarm; }
            set
            {
                isAlarm = value;
                if (value)
                {
                    alarmTimer.Start();
                    Properties.Settings.Default["MPIsAlarm"] = true;
                }
                else
                {
                    Properties.Settings.Default["MPIsAlarm"] = false;
                    alarmTimer.Stop();
                }
                Properties.Settings.Default.Save();
                NotifyPropertyChanged("IsAlarm");
            }
        }

        public bool IsSingleColumn
        {
            get { return isSingleColumn; }
            set
            {
                if (isSingleColumn == value)
                    return;
                isSingleColumn = value;
                if (value)
                {
                    this.Width = 700;
                    ToSingleColumn();
                }
                else
                {
                    this.Width = 1200;
                    ToDoubleColumn();
                }
                NotifyPropertyChanged("IsSingleColumn");
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }

        public MultiPing(StringCollection strIPList = null)
        {
            isSingleColumn = (bool)Properties.Settings.Default["MPIsSingleColumn"];
            if (isSingleColumn)
                this.Width = 700;
            else
                this.Width = 1200;
            if (strIPList == null)
                strIPList1 = Properties.Settings.Default["MPStrIPList1"] as StringCollection;
            else
                strIPList1 = strIPList;
            strIPList2 = Properties.Settings.Default["MPStrIPList2"] as StringCollection;
            isAlarm = (bool)Properties.Settings.Default["MPIsAlarm"];
            alarmPlayer.SoundLocation = Properties.Settings.Default["MPAlarmFileUrl"].ToString();
            alarmTimer.Interval = TimeSpan.FromSeconds(3);
            alarmTimer.Tick += new EventHandler(alarmTimer_Tick);
            alarmTimer.IsEnabled = isAlarm;
            this.Loaded += new RoutedEventHandler(MultiPing_Loaded);
            this.Closing += new CancelEventHandler(MultiPing_Closing);

            InitializeComponent();
            
        }

        void alarmTimer_Tick(object sender, EventArgs e)
        {
            if (!isAlarm || !(bool)btnAlarm.IsChecked)
            {
                MessageBox.Show("有问题，在isalarm或alarm按钮没开启的情况下有报警了");
            }
            if (App.multiPingIsPlayAlarm)
            {
                alarmPlayer.Play();
                App.multiPingIsPlayAlarm = false;
            }
        }

        void MultiPing_Loaded(object sender, RoutedEventArgs e)
        {
            InitGrid();
            multiIPInsert.AddIPEvent += new UC.UCIPInsert.AddIPEventHandler(multiIPInsert_AddIPEvent);
        }

        void InitGrid()
        {
            ColumnDefinition col0 = new ColumnDefinition();
            col0.Width = new GridLength(1, GridUnitType.Star);
            grid.ColumnDefinitions.Add(col0);
            foreach (string strIP in strIPList1)
            {
                string ipName;
                if (App.ipAndIPinfoList.ContainsKey(strIP))
                    ipName = App.ipAndIPinfoList[strIP].IpName;
                else
                    ipName = null;
                PingTarget pingTarget = new PingTarget(strIP, ipName);
                pingTargetList1.Add(pingTarget);
            }
            listBox1.ItemsSource = pingTargetList1;
            if (!isSingleColumn)
            {
                //第一次初始化时，如果双栏，就添加相应的grid列
                ColumnDefinition col1 = new ColumnDefinition();
                col1.Width = GridLength.Auto;
                ColumnDefinition col2 = new ColumnDefinition();
                col2.Width = new GridLength(1, GridUnitType.Star);
                grid.ColumnDefinitions.Add(col1);
                grid.ColumnDefinitions.Add(col2);
            }
            foreach (string strIP in strIPList2)
            {
                string ipName;
                if (App.ipAndIPinfoList.ContainsKey(strIP))
                    ipName = App.ipAndIPinfoList[strIP].IpName;
                else
                    ipName = null;
                PingTarget pingTarget = new PingTarget(strIP, ipName);
                pingTargetList2.Add(pingTarget);
            }
            listBox2.ItemsSource = pingTargetList2;
        }


        void ToSingleColumn()
        {
            grid.ColumnDefinitions.RemoveRange(1, 2);
        }

        void ToDoubleColumn()
        {
            ColumnDefinition col1 = new ColumnDefinition();
            col1.Width = GridLength.Auto;
            ColumnDefinition col2 = new ColumnDefinition();
            col2.Width = new GridLength(1, GridUnitType.Star);
            grid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            grid.ColumnDefinitions.Add(col1);
            grid.ColumnDefinitions.Add(col2);
        }

        void MultiPing_Closing(object sender, CancelEventArgs e)
        {
            foreach (PingTarget p in pingTargetList1)
                p.PingState = PingTargetState.Stop;
            foreach (PingTarget p in pingTargetList2)
                p.PingState = PingTargetState.Stop;
        }

        private void btnStartPause_Click(object sender, RoutedEventArgs e)
        {
            PingTarget pingTarget = ((Button)sender).DataContext as PingTarget;
            if (pingTarget.PingState != PingTargetState.Run)
                pingTarget.PingState = PingTargetState.Run;
            else
                pingTarget.PingState = PingTargetState.Pause;
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            PingTarget pingTarget = ((Button)sender).DataContext as PingTarget;
            if (pingTarget.PingState != PingTargetState.Stop)
            {
                pingTarget.PingState = PingTargetState.Stop;
            }
        }

        private void btnDel_Click(object sender, RoutedEventArgs e)
        {
            PingTarget pingTarget = ((Button)sender).DataContext as PingTarget;
            pingTarget.PingState = PingTargetState.Stop;
            if (pingTargetList1.Contains(pingTarget))
                pingTargetList1.Remove(pingTarget);
            else
                pingTargetList2.Remove(pingTarget);
        }

        private void btnConfig_Click(object sender, RoutedEventArgs e)
        {
            MultiPingConfig config = new MultiPingConfig();
            config.Owner = this;
            config.ShowDialog();
        }

        private void cBoxSelectAll_Click(object sender, RoutedEventArgs e)
        {
            bool state = (bool)cBoxSelectAll.IsChecked;
            foreach (PingTarget pt in pingTargetList1)
            {
                if (pt.IsRecord == !state)
                    pt.IsRecord = state;
            }
            if (!isSingleColumn)
            {
                foreach (PingTarget pt in pingTargetList2)
                {
                    if (pt.IsRecord == !state)
                        pt.IsRecord = state;
                }
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default["MPIsSingleColumn"] = isSingleColumn;
            strIPList1.Clear();
            strIPList2.Clear();
            foreach (PingTarget pingTarget in pingTargetList1)
                strIPList1.Add(pingTarget.StrIP);
            foreach (PingTarget pingTarget in pingTargetList2)
                strIPList2.Add(pingTarget.StrIP);
            Properties.Settings.Default["MPStrIPList1"] = strIPList1;
            Properties.Settings.Default["MPStrIPList2"] = strIPList2;
            Properties.Settings.Default.Save();
            MessageBox.Show("布局保存成功");
        }

        private void btnAllStart_Click(object sender, RoutedEventArgs e)
        {
            SetPingTargetListState(pingTargetList1, PingTargetState.Run);
        }

        private void btnAllPause_Click(object sender, RoutedEventArgs e)
        {
            SetPingTargetListState(pingTargetList1, PingTargetState.Pause);
        }

        private void btnAllStop_Click(object sender, RoutedEventArgs e)
        {
            SetPingTargetListState(pingTargetList1, PingTargetState.Stop);
        }
        private void btnMoveUp_Click(object sender, RoutedEventArgs e)
        {
            if (listBox1.SelectedItems.Count > 0)
            {
                int oldIndex;
                for (int i = 0; i < listBox1.SelectedItems.Count;i++ )
                {
                    PingTarget pingTarget = listBox1.SelectedItems[i] as PingTarget;
                    oldIndex = pingTargetList1.IndexOf(pingTarget);
                    if (i == 0 && oldIndex == 0)
                        return;
                    pingTargetList1.Move(oldIndex, oldIndex - 1);
                }
            }
        }
        private void btnMoveDown_Click(object sender, RoutedEventArgs e)
        {
            int n = listBox1.SelectedItems.Count;
            if (n > 0)
            {
                int oldIndex;
                for (int i = n-1; i >= 0; i--)
                {
                    PingTarget pingTarget = listBox1.SelectedItems[i] as PingTarget;
                    oldIndex = pingTargetList1.IndexOf(pingTarget);
                    if (i == n - 1 && oldIndex == listBox1.Items.Count- 1)
                        return;
                    pingTargetList1.Move(oldIndex, oldIndex + 1);
                }
            }
        }
        private void btnMoveTop_Click(object sender, RoutedEventArgs e)
        {
            if (listBox1.SelectedItems.Count > 0)
            {
                int oldIndex;
                for (int i = 0; i < listBox1.SelectedItems.Count; i++)
                {
                    PingTarget pingTarget = listBox1.SelectedItems[i] as PingTarget;
                    oldIndex = pingTargetList1.IndexOf(pingTarget);
                    if (i == 0 && oldIndex == 0)
                        return;
                    pingTargetList1.Move(oldIndex, i);
                }
            }
        }
        private void btnMoveBottom_Click(object sender, RoutedEventArgs e)
        {
            int n = listBox1.SelectedItems.Count;
            int m = listBox1.Items.Count;
            if (n > 0)
            {
                int oldIndex;
                int j = 0;
                for (int i = n - 1; i >= 0; i--)
                {
                    PingTarget pingTarget = listBox1.SelectedItems[i] as PingTarget;
                    oldIndex = pingTargetList1.IndexOf(pingTarget);
                    if (i == n - 1 && oldIndex == m - 1)
                        return;
                    pingTargetList1.Move(oldIndex, m - 1 - j);
                    j++;
                }
            }
        }
        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            foreach (PingTarget pt in pingTargetList1)
                pt.PingState = PingTargetState.Stop;
            pingTargetList1.Clear();
        }

        private void btnAllStart2_Click(object sender, RoutedEventArgs e)
        {
            SetPingTargetListState(pingTargetList2, PingTargetState.Run);
        }

        private void btnAllPause2_Click(object sender, RoutedEventArgs e)
        {
            SetPingTargetListState(pingTargetList2, PingTargetState.Pause);
        }

        private void btnAllStop2_Click(object sender, RoutedEventArgs e)
        {
            SetPingTargetListState(pingTargetList2, PingTargetState.Stop);
        }
        private void btnMoveUp2_Click(object sender, RoutedEventArgs e)
        {
            if (listBox2.SelectedItems.Count > 0)
            {
                int oldIndex;
                for (int i = 0; i < listBox2.SelectedItems.Count; i++)
                {
                    PingTarget pingTarget = listBox2.SelectedItems[i] as PingTarget;
                    oldIndex = pingTargetList2.IndexOf(pingTarget);
                    if (i == 0 && oldIndex == 0)
                        return;
                    pingTargetList2.Move(oldIndex, oldIndex - 1);
                }
            }
        }
        private void btnMoveDown2_Click(object sender, RoutedEventArgs e)
        {
            int n = listBox2.SelectedItems.Count;
            if (n > 0)
            {
                int oldIndex;
                for (int i = n - 1; i >= 0; i--)
                {
                    PingTarget pingTarget = listBox2.SelectedItems[i] as PingTarget;
                    oldIndex = pingTargetList2.IndexOf(pingTarget);
                    if (i == n - 1 && oldIndex == listBox2.Items.Count - 1)
                        return;
                    pingTargetList2.Move(oldIndex, oldIndex + 1);
                }
            }
        }
        private void btnMoveTop2_Click(object sender, RoutedEventArgs e)
        {
            if (listBox2.SelectedItems.Count > 0)
            {
                int oldIndex;
                for (int i = 0; i < listBox2.SelectedItems.Count; i++)
                {
                    PingTarget pingTarget = listBox2.SelectedItems[i] as PingTarget;
                    oldIndex = pingTargetList2.IndexOf(pingTarget);
                    if (i == 0 && oldIndex == 0)
                        return;
                    pingTargetList2.Move(oldIndex, i);
                }
            }
        }
        private void btnMoveBottom2_Click(object sender, RoutedEventArgs e)
        {
            int n = listBox2.SelectedItems.Count;
            int m = listBox2.Items.Count;
            if (n > 0)
            {
                int oldIndex;
                int j = 0;
                for (int i = n - 1; i >= 0; i--)
                {
                    PingTarget pingTarget = listBox2.SelectedItems[i] as PingTarget;
                    oldIndex = pingTargetList2.IndexOf(pingTarget);
                    if (i == n - 1 && oldIndex == m - 1)
                        return;
                    pingTargetList2.Move(oldIndex, m - 1 - j);
                    j++;
                }
            }
        }
        private void btnClear2_Click(object sender, RoutedEventArgs e)
        {
            foreach (PingTarget pt in pingTargetList2)
                pt.PingState = PingTargetState.Stop;
            pingTargetList2.Clear();
        }

        private void SetPingTargetListState(ObservableCollection<PingTarget> pingTargetList,PingTargetState state)
        {
            if (pingTargetList.Equals(pingTargetList2) && isSingleColumn)
                return;
            if (state == PingTargetState.Pause)
            {
                foreach (PingTarget pt in pingTargetList)
                {
                    if (pt.PingState == PingTargetState.Run)
                        pt.PingState = state;
                }
            }
            else
            {
                foreach (PingTarget pt in pingTargetList)
                {
                    if (pt.PingState != state)
                        pt.PingState = state;
                }
            }
        }

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {

        }

        private void StrIP_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            PingTarget pt = ((TextBlock)sender).DataContext as PingTarget;
            MultiPingConfigForSingleIP configWindow = new MultiPingConfigForSingleIP(pt);
            configWindow.ShowDialog();
        }

        private void Ellipse_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
             PingTarget pt = ((Ellipse)sender).DataContext as PingTarget;
             Thread threadShowReplyPage = new Thread(delegate()
             {
                 MultiPingShowReply showPingReply = new MultiPingShowReply(pt);
                 showPingReply.ShowDialog();
             });
             threadShowReplyPage.SetApartmentState(ApartmentState.STA);
             threadShowReplyPage.Start();

//              MultiPingShowReply showPingReply = new MultiPingShowReply(pt);
//              showPingReply.Show();
        }

        void multiIPInsert_AddIPEvent(object sender, UC.AddIPEventArgs e)
        {
            //解决重复IP问题
            strIPList1.Clear();
            strIPList2.Clear();
            foreach (PingTarget pingTarget in pingTargetList1)
                strIPList1.Add(pingTarget.StrIP);
            foreach (PingTarget pingTarget in pingTargetList2)
                strIPList2.Add(pingTarget.StrIP);
            List<IPAddress> ipList = e.IPList;
            foreach (IPAddress ip in ipList)
            {
                string strip = ip.ToString();
                string ipName;
                if (App.ipAndIPinfoList.ContainsKey(strip))
                    ipName = App.ipAndIPinfoList[strip].IpName;
                else
                    ipName = null;
                if (cbSelectSide.SelectedIndex == 1 && !strIPList1.Contains(strip))
                    pingTargetList1.Add(new PingTarget(strip, ipName));
                if (cbSelectSide.SelectedIndex == 2 && !strIPList2.Contains(strip))
                    pingTargetList2.Add(new PingTarget(strip, ipName));
                if (cbSelectSide.SelectedIndex == 0)
                {
                    if (!strIPList1.Contains(strip))
                        pingTargetList1.Add(new PingTarget(strip, ipName));
                    if (!strIPList2.Contains(strip))
                        pingTargetList2.Add(new PingTarget(strip, ipName));
                }
            }
        }

        private void btnAddIP_Click(object sender, RoutedEventArgs e)
        {
            IPAddress tempIP;
            string strIP = ipSelector.SelectedIP;
            if (!IPAddress.TryParse(strIP, out tempIP))
            {
                MessageBox.Show("选择的不是正确的IP地址");
                return;
            }
            //解决重复IP问题
            strIPList1.Clear();
            strIPList2.Clear();
            foreach (PingTarget pingTarget in pingTargetList1)
                strIPList1.Add(pingTarget.StrIP);
            foreach (PingTarget pingTarget in pingTargetList2)
                strIPList2.Add(pingTarget.StrIP);
            string ipName;
            if (App.ipAndIPinfoList.ContainsKey(strIP))
                ipName = App.ipAndIPinfoList[strIP].IpName;
            else
                ipName = null;
            if (cbSelectSide.SelectedIndex == 1 && !strIPList1.Contains(strIP))
                pingTargetList1.Add(new PingTarget(strIP, ipName));
            if (cbSelectSide.SelectedIndex == 2 && !strIPList2.Contains(strIP))
                pingTargetList2.Add(new PingTarget(strIP, ipName));
            if (cbSelectSide.SelectedIndex == 0)
            {
                if (!strIPList1.Contains(strIP))
                    pingTargetList1.Add(new PingTarget(strIP, ipName));
                if (!strIPList2.Contains(strIP))
                    pingTargetList2.Add(new PingTarget(strIP, ipName));
            }
        }
    }
}
