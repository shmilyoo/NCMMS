using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SnmpSharpNet;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Visifire.Charts;
using System.Net;
using System.Windows;
using System.Windows.Threading;
using System.Threading;

namespace NCMMS.PortMonitor
{
    public enum InterfaceType
    {
        //网口、光口、Console口、Vlan口、Loopback口、其他
        RJ45, Optical, Console, Vlan, Loopback, Other
    };

    public class Interface : INotifyPropertyChanged
    {
        //         [If_Index]
        //       ,[If_Oid]
        //       ,[If_Descr]
        //       ,[If_Type]
        //       ,[If_Name]
        //       ,[If_EquipID]

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }

        public Dispatcher uiDispatcher;
        int ifIndex, equipID;
        Oid oid;
        string descr, name, equipName; //mib中的descr，name为用户自定义的端口名称
        InterfaceType type;
        IpAddress ip;
        Oid interfaceEntryOid = new Oid("1.3.6.1.2.1.2.2.1");
        //System.Timers.Timer timer1 = new System.Timers.Timer(); //计算端口速率的计时器
        DispatcherTimer timer = new DispatcherTimer();
        Counter32 oldInOctets, newInOctets, oldIOutOctets, newOutOctets;
        DateTime oldTime;
        double inSpeed = -1; // 以kbyte为单位,设置为-1是为了方便第一个数据包来的时候进行判断
        double outSpeed = -1;
        double maxInSpeed = -1, maxOutSpeed = -1; //设置进出速度的最大阈值

        bool adminStatus, operStatus; //端口状态，管理员设置状态和协议状态,正常为true
        private DataPointCollection inSpeedDataPointList = new DataPointCollection();

        private DataPointCollection outSpeedDataPointList = new DataPointCollection();


        bool? isRunning = false;
        Oid[] requestOids;
        string tipMessage;//显示一些提示信息或者出错信息
        string speedRealTime;
        ObservableCollection<string> logList = new ObservableCollection<string>();
        bool isShowSpeedAlarm = false;

        #region  属性

        public string SpeedRealTime
        {
            get { return speedRealTime; }
            set { speedRealTime = value; NotifyPropertyChanged("SpeedRealTime"); }
        }
        public DataPointCollection OutSpeedDataPointList
        {
            get { return outSpeedDataPointList; }
            set { outSpeedDataPointList = value; }
        }
        public DataPointCollection InSpeedDataPointList
        {
            get { return inSpeedDataPointList; }
            set { inSpeedDataPointList = value; }
        }
        public double MaxOutSpeed
        {
            get { return maxOutSpeed; }
            set { maxOutSpeed = value; NotifyPropertyChanged("MaxOutSpeed"); }
        }

        public double MaxInSpeed
        {
            get { return maxInSpeed; }
            set { maxInSpeed = value; NotifyPropertyChanged("MaxInSpeed"); }
        }
        /// <summary>
        /// 日志记录，每一个tipmessage都添加进来，用于在鼠标移动到tipmessage的时候显示。
        /// </summary>
        public ObservableCollection<string> LogList
        {
            get { return logList; }
            set { logList = value; }
        }

        public string EquipName
        {
            get { return equipName; }
            set { equipName = value; }
        }
        /// <summary>
        /// 是否限速报警，绑定到前台的checkbox
        /// </summary>
        public bool IsShowSpeedAlarm
        {
            get { return isShowSpeedAlarm; }
            set { 
                isShowSpeedAlarm = value;
                NotifyPropertyChanged("IsShowSpeedAlarm");
                NotifyPropertyChanged("TotalStatus"); 
            }
        }

        /// <summary>
        /// 计时器间隔，单位为秒
        /// </summary>
        public double TimerInteral
        {
            get
            {
                return timer.Interval.TotalSeconds;
            }
            set
            {
                timer.Interval = TimeSpan.FromSeconds(value);
                NotifyPropertyChanged("TimerInteral");
            }
        }
        public int IfIndex
        {
            get { return ifIndex; }
            set
            {
                ifIndex = value;
                requestOids = new Oid[]{
                    new Oid("1.3.6.1.2.1.2.2.1.7." + ifIndex.ToString()),//ifAdminStatus
                    new Oid("1.3.6.1.2.1.2.2.1.8." + ifIndex.ToString()),//ifOperStatus
                    new Oid("1.3.6.1.2.1.2.2.1.10." + ifIndex.ToString()),//ifInOctets
                    new Oid("1.3.6.1.2.1.2.2.1.16." + ifIndex.ToString()),//ifOutOctets
                };
            }
        }
        public int EquipID
        {
            get { return equipID; }
            set { equipID = value; }
        }
        public Oid Oid
        {
            get { return oid; }
            set { oid = value; }
        }
        public string Descr
        {
            get { return descr; }
            set { descr = value; }
        }
        public string Name
        {
            get { return name; }
            set { name = value; }
        }
        internal InterfaceType Type
        {
            get { return type; }
            set { type = value; }
        }
        /// <summary>
        /// 端口所属设备的管理地址
        /// </summary>
        public IpAddress IP
        {
            get { return ip; }
            set { ip = value; }
        }
        public string DescrInListBox
        {
            get
            {
                if (string.IsNullOrEmpty(equipName))
                {
                    return ip.ToString() + "上的" + descr;
                }
                return equipName + "上的" + descr;
            }
        }
        bool? speedAlarmStatus = null;
        /// <summary>
        /// 正常为true
        /// </summary>
        public bool? SpeedAlarmStatus
        {
            get {
                if (isShowSpeedAlarm)
                    return speedAlarmStatus;
                else
                    return null;
            }
            set
            {
                speedAlarmStatus = value;
                NotifyPropertyChanged("SpeedAlarmStatus");
                NotifyPropertyChanged("TotalStatus");
            }
        }
        public bool? Status
        {
            //关联到状态灯指示，显示端口状态，只要一个是down，就亮灯
            get
            {
                if (isRunning != null)
                    return adminStatus && operStatus;
                else
                    return null;
            }
        }

        bool errorStatus = true; //true代表正常


        //界面最上方表示每行状态的灯，SpeedAlarmStatus、Status有一个是异常的时候，就会报警
        public bool? TotalStatus
        {
            get
            {
                if (isRunning != null)
                {
                    if (Status == false || SpeedAlarmStatus == false || errorStatus == false)
                        return false;
                    else
                        return true;
                }
                else
                    return null;
            }
        }

        public string StatusDescr
        {
            get
            {
                return string.Format("AdminStatus:{0}\nOperStatus:{1}", adminStatus ? "up" : "down", operStatus ? "up" : "down");
            }
        }
        public bool? IsRunning
        {
            get { return isRunning; }
            set
            {
                if (isRunning != value)
                {
                    if (value == true)
                    {
                        if (isRunning == false)
                        {
                            inSpeedDataPointList.Clear();
                            outSpeedDataPointList.Clear();
                            logList.Clear();
                        }
                        timer.Start();
                        TipMessage = "开始获取目标端口信息";
                    }
                    else if (value == false)
                    {
                        timer.Stop();
                        TipMessage = "停止获取目标端口信息";
                    }
                    else
                    {
                        timer.Stop();
                        TipMessage = "暂停获取目标端口信息";
                    }
                    isRunning = value;
                    NotifyPropertyChanged("IsRunning");
                    NotifyPropertyChanged("Status");
                }
            }
        }
        public string TipMessage
        {
            get { return tipMessage; }
            set
            {
                tipMessage = value;
                if (Thread.CurrentThread != uiDispatcher.Thread)
                {
                    uiDispatcher.Invoke(new Action(() =>
                    {
                        logList.Insert(0, DateTime.Now.ToLongTimeString() + " " + tipMessage);
                    }));
                }
                else
                {
                    logList.Insert(0, DateTime.Now.ToLongTimeString() + " " + tipMessage);
                }
                NotifyPropertyChanged("TipMessage");
            }
        }


        #endregion

        //         public Interface(int _ifIndex, IpAddress _ip)
        //         {
        //             ifIndex = _ifIndex;
        //             ip = _ip;
        //             timer.Elapsed += new System.Timers.ElapsedEventHandler(timer_Elapsed);
        //         }


        public Interface(IpAddress _ip)
        {
            ip = _ip;
            timer.Tick += new EventHandler(timer_Tick);
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(name))
            {
                return descr;
            }
            return string.Format("({0}){1}", name, descr);
        }
        UdpTarget target;
        void timer_Tick(object sender, EventArgs e)
        {
            AgentParameters param = new AgentParameters(SnmpVersion.Ver2, new OctetString(App.snmpCommunity));
            param.DisableReplySourceCheck = !App.snmpCheckSrcFlag;
            target = new UdpTarget((IPAddress)ip, App.snmpPort, App.snmpTimeout, App.snmpRetry);
            Pdu pdu = new Pdu(PduType.Get);
            for (int i = 0; i < requestOids.Length; i++)
                pdu.VbList.Add(requestOids[i]);
            try
            {
                target.RequestAsync(pdu, param, new SnmpAsyncResponse(SnmpAsyncResponseCallback));
            }
            catch (System.Exception ex)
            {
                TipMessage = "发送数据包异常";
            }
        }


        void SnmpAsyncResponseCallback(AsyncRequestResult result, SnmpPacket packet)
        {
            //If result is null then agent didn't reply or we couldn't parse the reply.
            if (result == AsyncRequestResult.NoError && packet != null)
            {
                // ErrorStatus other then 0 is an error returned by 
                // the Agent - see SnmpConstants for error definitions
                if (packet.Pdu.ErrorStatus != 0)
                {
                    // agent reported an error with the request
                    TipMessage = string.Format("SNMP应答数据包中有错误信息. Error {0} index {1}", packet.Pdu.ErrorStatus, packet.Pdu.ErrorIndex);
                    errorStatus = false;
                    NotifyPropertyChanged("TotalStatus");
                }
                else
                {
                    errorStatus = true;
                    //ifAdminStatus//ifOperStatus//ifInOctets //ifOutOctets
                    bool tempAdminStatus, tempOperStatus;
                    if (packet.Pdu.VbList[0].Value as Integer32 == 1)
                        tempAdminStatus = true;
                    else
                        tempAdminStatus = false;
                    if (packet.Pdu.VbList[1].Value as Integer32 == 1)
                        tempOperStatus = true;
                    else
                        tempOperStatus = false;
                    if (tempAdminStatus != adminStatus || tempOperStatus != operStatus)
                    {
                        adminStatus = tempAdminStatus;
                        operStatus = tempOperStatus;
                        NotifyPropertyChanged("Status");
                        NotifyPropertyChanged("StatusDescr");
                        NotifyPropertyChanged("TotalStatus");
                        if (inSpeed != -1)
                        {
                            TipMessage = string.Format("AdminStatus:{0}\nOperStatus:{1}", adminStatus ? "up" : "down", operStatus ? "up" : "down");
                        }
                    }

                    if (inSpeed == -1)
                    {
                        //这次是第一次获取数据
                        oldInOctets = packet.Pdu.VbList[2].Value as Counter32;
                        oldIOutOctets = packet.Pdu.VbList[3].Value as Counter32;
                        oldTime = DateTime.Now;
                        inSpeed = 0;
                        outSpeed = 0;
                    }
                    else
                    {
                        newInOctets = packet.Pdu.VbList[2].Value as Counter32;
                        newOutOctets = packet.Pdu.VbList[3].Value as Counter32;
                        DateTime now = DateTime.Now;
                        double interval = (now - oldTime).TotalSeconds;
                        oldTime = now;
                        inSpeed = Math.Round((newInOctets - oldInOctets) * 0.008 / interval, 2); //结果为 kb/s
                        outSpeed = Math.Round((newOutOctets - oldIOutOctets) * 0.008 / interval, 2);
                        SpeedRealTime = string.Format("In: {0}kb/s; Out: {1}kb/s", inSpeed, outSpeed);
                        oldInOctets = newInOctets;
                        oldIOutOctets = newOutOctets;
                        uiDispatcher.Invoke(new Action(() =>
                        {
                            DataPoint dpIn = new DataPoint();
                            dpIn.XValue = DateTime.Now;
                            dpIn.YValue = inSpeed;
                            DataPoint dpOut = new DataPoint();
                            dpOut.XValue = DateTime.Now;
                            dpOut.YValue = outSpeed;
                            if (InSpeedDataPointList.Count >= 30)
                                InSpeedDataPointList.RemoveAt(0);
                            InSpeedDataPointList.Add(dpIn);
                            if (OutSpeedDataPointList.Count >= 30)
                                OutSpeedDataPointList.RemoveAt(0);
                            OutSpeedDataPointList.Add(dpOut);
                        }));
                        if (IsShowSpeedAlarm)
                        {
                            //SpeedAlarmStatus
                            bool tempSpeedAlarmStatus = (inSpeed <= maxInSpeed) && (outSpeed <= maxOutSpeed);
                            if (tempSpeedAlarmStatus != SpeedAlarmStatus)
                            {
                                SpeedAlarmStatus = tempSpeedAlarmStatus;
                                TipMessage = string.Format("In:{0}kb/s{1};Out:{2}kb/s{3}", inSpeed, (inSpeed <= maxInSpeed) ? "正常" : "超限!", outSpeed, (outSpeed <= maxOutSpeed) ? "正常" : "超限!");
                            }
                        }

                    }
                }
            }
            else
            {
                TipMessage = "没有SNMP应答数据包";
            }
            target.Close();
        }
    }
}
