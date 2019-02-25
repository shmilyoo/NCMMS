using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.NetworkInformation;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Net;
using NCMMS.CommonClass;
using System.Threading;
using System.Windows.Threading;


namespace NCMMS.MultiPing
{
    /// <summary>
    /// 表示ping的三个状态进行中、暂停、停止
    /// </summary>
    public enum PingTargetState
    {
        Run,Pause,Stop
    }

    public class ShowReplyAddMessageEventArgs
    {
        public ShowReplyAddMessageEventArgs(string s) { Message = s; }
        public String Message {get; private set;} // readonly
    }

    public class PingTarget : INotifyPropertyChanged
    {
        public event ShowReplyAddMessageEventHandler ShowReplyAddMessageEvent;
        public delegate void ShowReplyAddMessageEventHandler(object sender, ShowReplyAddMessageEventArgs e);

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }

        Ping pingSender;
        PingOptions pingOptions;
        PingReply reply;
        System.Timers.Timer timer;
        byte[] data;
        IPAddress ip;
        string ipName;
        int timeOut, dataSize, ttl, number, interval; //超时时间(ms)，数据大小，ttl，发送个数，发送间隔(ms)；
        bool? isPingOK = null;  //用来控制状态灯是红色绿色还是灰色
        int packetLostNum = 0;
        int packetSendNum = 0;
        int delay = -1;
        bool isRecord;
        bool isPingCompleted = true;  //用来使timeout这种情况正常显示，timeout下，等超时之后才发后续的包
        string message; 

        PingTargetState pingState = PingTargetState.Stop;// 用来控制状态以及开始暂停按钮是显示开始图标还是暂停图标
        public List<string> recordMessageList = new List<string>();
        //public ObservableCollection<string> recordMessageList = new ObservableCollection<string>();

        public PingTargetState PingState
        {
            get { return pingState; }
            set 
            {
                if (pingState == PingTargetState.Stop)
                {
                    if (value == PingTargetState.Run)
                    {
                        timer.Start();
                    }
                }
                else if (pingState == PingTargetState.Pause)
                {
                    if (value == PingTargetState.Stop)
                    {
                        PacketLostNum = 0;
                        PacketSendNum = 0;
                        Delay = -1;
                        recordMessageList.Clear();
                    }
                    else if (value == PingTargetState.Run)
                    {
                        timer.Start();
                    }
                }
                else
                {
                    if (value == PingTargetState.Stop)
                    {
                        timer.Stop();
                        IsPingOK = null;
                        PacketLostNum = 0;
                        PacketSendNum = 0;
                        Delay = -1;
                        recordMessageList.Clear();
                    }
                    else if (value == PingTargetState.Pause)
                    {
                        timer.Stop();
                    }
                }
                pingState = value;
                NotifyPropertyChanged("PingState"); 
            }
        }

        public bool IsRecord
        {
            get { return isRecord; }
            set { isRecord = value; NotifyPropertyChanged("IsRecord"); }
        }

        public bool? IsPingOK
        {
            get { return isPingOK; }
            set { isPingOK = value; NotifyPropertyChanged("IsPingOK"); }
        }

        public int PacketLostNum
        {
            get { return packetLostNum; }
            set { packetLostNum = value; NotifyPropertyChanged("PacketLostNum"); }
        }

        public int PacketSendNum
        {
            get { return packetSendNum; }
            set { packetSendNum = value; NotifyPropertyChanged("PacketSendNum"); }
        }

        public int Delay
        {
            get { return delay; }
            set { delay = value; NotifyPropertyChanged("Delay"); }
        }

        public IPAddress IP
        {
            get { return ip; }
            set { ip = value; }
        }

        public string StrIP
        {
            get { return ip.ToString(); }
        }

        public string IPName
        {
            get { return ipName; }
            set { ipName = value; }
        }

        public int Interval
        {
            get { return interval; }
            set { interval = value; timer.Interval = value; }
        }
        public int Number
        {
            get { return number; }
            set { number = value; }
        }
        public int TTL
        {
            get { return ttl; }
            set { ttl = value; pingOptions.Ttl = value; }
        }
        public int DataSize
        {
            get { return dataSize; }
            set
            {
                dataSize = value;
                if (data.Length != value)
                    data = new byte[value];
            }
        }
        public int TimeOut
        {
            get { return timeOut; }
            set { timeOut = value; }
        }

        public PingTarget(IPAddress _ip, string _IPName)
        {
            ip = _ip;
            ipName = _IPName;
            InitParameters();
        }

        public PingTarget(string _ip, string _IPName)
        {
            ip = IPAddress.Parse(_ip);
            ipName = _IPName;
            InitParameters();
        }

        private void InitParameters()
        {
            timeOut = (int)Properties.Settings.Default["MPTimeOut"];
            dataSize = (int)Properties.Settings.Default["MPDataSize"];
            ttl = (int)Properties.Settings.Default["MPTTL"];
            number = (int)Properties.Settings.Default["MPNumber"];
            interval = (int)Properties.Settings.Default["MPInterval"];
            isRecord = (bool)Properties.Settings.Default["MPIsRecord"];
            pingSender = new Ping();
            pingSender.PingCompleted += new PingCompletedEventHandler(pingSender_PingCompleted);
            pingOptions = new PingOptions(ttl, true);
            timer = new System.Timers.Timer(interval);
            timer.Elapsed += new System.Timers.ElapsedEventHandler(timer_Elapsed);
            timer.Enabled = false;
            data = new byte[dataSize];
        }
        void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!isPingCompleted)
                return;
            //ping三种情况，可以通，timeout，不能抵达
            if (packetSendNum >= number && number != 0)
                {
                    timer.Stop();
                    return;
                }
            isPingCompleted = false;    
            try
            {
                pingSender.SendAsync(ip, timeOut, data, pingOptions,null);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message + "\n" + ex.InnerException.Message);
            }
            PacketSendNum++;
        }

        void pingSender_PingCompleted(object sender, PingCompletedEventArgs e)
        {
            isPingCompleted = true;
            if (pingState == PingTargetState.Stop)
                return;
            reply = e.Reply;
            if (reply.Status == IPStatus.Success)
            {
                Delay = (int)reply.RoundtripTime;
                IsPingOK = true;
                if (Delay < 1)
                {
                    message = String.Format("{5} 来自 {0} 的回复：字节={1} 时间<1ms，TTL={2},共{3}包，丢{4}包。", reply.Address.ToString(), reply.Buffer.Length, reply.Options.Ttl, PacketSendNum, PacketLostNum, DateTime.Now.ToLongTimeString());
                    recordMessageList.Add(message);
                    if (ShowReplyAddMessageEvent != null)
                        ShowReplyAddMessageEvent(this, new ShowReplyAddMessageEventArgs(message));
                }
                else
                {
                    message = String.Format("{6} 来自 {0} 的回复：字节={1} 时间{2}ms，TTL={3},共{4}包，丢{5}包。", reply.Address.ToString(), reply.Buffer.Length, delay, reply.Options.Ttl, PacketSendNum, PacketLostNum, DateTime.Now.ToLongTimeString());
                    recordMessageList.Add(message);
                    if (ShowReplyAddMessageEvent != null)
                        ShowReplyAddMessageEvent(this, new ShowReplyAddMessageEventArgs(message));
                }
            }
            else
            {
                Delay = -2;
                PacketLostNum++;
                IsPingOK = false;
                App.multiPingIsPlayAlarm = true;
                message = String.Format("{0} {1} 共{2}包，丢{3}包。", DateTime.Now.ToLongTimeString(), reply.Status.ToString(), PacketSendNum, PacketLostNum);
                recordMessageList.Add(message);
                if (ShowReplyAddMessageEvent != null)
                    ShowReplyAddMessageEvent(this, new ShowReplyAddMessageEventArgs(message));
            }
        }

        public override string ToString()
        {
            return ip.ToString();
        }

        public void Close()
        {
            timer.Enabled = false;
            recordMessageList.Clear();
            pingSender.Dispose();
        }

    }
}
