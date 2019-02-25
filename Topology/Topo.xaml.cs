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
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;
using NCMMS.CommonClass;
using NCMMS.UC;
using System.ComponentModel;
using SnmpSharpNet;
using System.Collections.ObjectModel;
using System.Threading;
using System.Net.Sockets;
using System.Net;

namespace NCMMS.Topology
{
    /// <summary>
    /// Topo.xaml 的交互逻辑
    /// </summary>
    public partial class Topo : MyWindow, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
        /// <summary>
        /// 鼠标选中的设备或者线
        /// </summary>
        object selectedItem = null;
        /// <summary>
        /// 左侧绘图栏中选中的东西
        /// </summary>
        object drawItem = null;

        Point preMousePoint, preObjectPoint;//鼠标点击前的绝对坐标,鼠标点击前物体相对于canvas的坐标
        bool isMove = false; //是否正在移动

        int x, y, segmentLength = 80;
        bool[,] grid22; //根据canvas的长宽 分割成一个二维的矩形矩阵，每个矩阵初始值为false，代表没有东西占用，当canvas添加设备的时候，根据一定的算法来填充，同时在对应格子里面设置为true
        double ucEquipLength=50d;
        /// <summary>
        /// 获取设备信息，sysDescr,sysObjectID,sysServices
        /// </summary>
        string[] equipInfoRequestOids = new string[] { "1.3.6.1.2.1.1.1.0", "1.3.6.1.2.1.1.2.0", "1.3.6.1.2.1.1.7.0" };
        string tipMessage;
        ObservableCollection<string> tipMessageList = new ObservableCollection<string>();
        int equipNumNotInDB = -1;//不在数据库中的设备，发现后其id 顺序-- 
        int listboxIndex = 1; //信息listbox列表前面的序号
        bool isListenTrap = false; //是否开启trap监听
        
        /// <summary>
        /// 从此IP开始探索网络
        /// </summary>
        IpAddress ipClue;
        /// <summary>
        /// 探索时累加跳数，大于此值时这条线路就会终止，0代表不限制。
        /// 初始IP地址所在设备为第一跳，即如果设置为1，则只发现初始IP对应设备
        /// </summary>
        int maxStep;
        /// <summary>
        /// 探索时发现下一跳地址时应在此范围内，若为NULL，则不限制
        /// </summary>
        List<Subnet> subnetRangeList;
        /// <summary>
        /// 待发现的下一跳信息列表，检索路由表，若满足子网规则，则加入其中
        /// </summary>
        List<WaitForDetectInfomation> WaitForDetectList = new List<WaitForDetectInfomation>();
        /// <summary>
        /// 已发现IP地址键值集合，通过ip找到IPInformation，其中有设备id号，可定位设备
        /// </summary>
        Dictionary<IpAddress, IPInformation> ipAlreadyFindList = new Dictionary<IpAddress, IPInformation>();
        /// <summary>
        /// 已发现设备集合，id>0：数据库中有对应设备；id小于0：不在数据库中设备
        /// </summary>
        Dictionary<int, Equipment> equipAlreadyFindList = new Dictionary<int, Equipment>();
        /// <summary>
        /// 未知设备或手动添加设备
        /// </summary>
        Dictionary<int, Equipment> equipManualAddList = new Dictionary<int, Equipment>();

        bool isDetecting;
        Socket socket;

        public bool IsDetecting
        {
            get { return isDetecting; }
            set 
            { 
                if (isDetecting != value)
                {
                    isDetecting = value; 
                    if (value)
                    {
                        loadingGrid.Visibility = Visibility.Visible;
                        (this.FindResource("sbSearching") as Storyboard).Begin();
                        SetTextBlockAsync(tbDetectlabel,"正在探测网络、绘制拓扑……");
                    }
                    else
                    {
                        (this.FindResource("sbSearching") as Storyboard).Stop();
                        SetTextBlockAsync(tbDetectlabel, "网络拓扑发现绘制完成");
                    }
                }
            }
        }
        /// <summary>
        /// 侦听trap标志，同时判断启动接收线程，接收线程通过此值判断是否结束接收，与前台checkbox绑定
        /// </summary>
        public bool IsListenTrap
        {
            get { return isListenTrap; }
            set
            {
                if (isListenTrap != value)
                {
                    isListenTrap = value;
                    if (value)
                    {   //true，开始接收线程
                        Thread receiveTrapThread = new Thread(new ThreadStart(AcceptTrap));
                        receiveTrapThread.Name = "后台接收Trap线程";
                        receiveTrapThread.IsBackground = true;
                        receiveTrapThread.Start();
                        tbTrapStatus.Text = "Trap接收: 开";
                    }
                    else
                    {
                        socket.Close();
                        SetTextBlockAsync(tbTrapStatus, "Trap接收: 关");
                    }
                    NotifyPropertyChanged("IsListenTrap");
                }
            }
        }
        /// <summary>
        /// 控件中间图标的长宽，整个控件的长宽需要转换，长度需加60，高度加20；
        /// </summary>
        public double UCEquipLength
        {
            get { return ucEquipLength; }
            set { ucEquipLength = value; NotifyPropertyChanged("UCEquipLength"); }
        }

        public Topo(IpAddress _ipClue, int _maxStep = 0, List<Subnet> _subnetRangeList = null)
        {
            InitializeComponent();
            ipClue = _ipClue;
            maxStep = _maxStep;
            subnetRangeList = _subnetRangeList;
            lbSearchingMessage.ItemsSource = tipMessageList;
            this.Loaded += new RoutedEventHandler(Topo_Loaded);
        }

        void Topo_Loaded(object sender, RoutedEventArgs e)
        {
            CalculateGridInCanvas();
            WaitForDetectList.Add(new WaitForDetectInfomation(ipClue, null, 0));
            IsDetecting = true;
            ThreadPool.QueueUserWorkItem(StartDetecNetwork);
            cbIsListenTrap.SetBinding(CheckBox.IsCheckedProperty, new Binding() { Path = new PropertyPath("IsListenTrap"), Source = this, Mode = BindingMode.TwoWay });
        }

        /// <summary>
        /// 初始化二维矩阵，每一个值初始为fase，代表这个格子里没有放置设备图标
        /// </summary>
        private void CalculateGridInCanvas()
        {
            int width = (int)canvas.ActualWidth;
            int height = (int)canvas.ActualHeight;
            x = width / segmentLength;
            y = height / segmentLength;
            grid22 = new bool[x,y];
        }

        /// <summary>
        /// 重新绘制拓扑图的时候，初始化相关控件，集合，属性等
        /// </summary>
        private void InitParameters()
        {
            CalculateGridInCanvas();
            listboxIndex = 1; 
            equipNumNotInDB = -1;
            WaitForDetectList.Clear();
            WaitForDetectList.Add(new WaitForDetectInfomation(ipClue, null, 0));
            tipMessageList.Clear();
            ipAlreadyFindList.Clear();
            equipAlreadyFindList.Clear();
            equipManualAddList.Clear();
            canvas.Children.Clear();
        }


        /// <summary>
        /// 探测网络拓扑全过程，应该要在新线程中工作，后面改
        /// </summary>
        private void StartDetecNetwork(Object stateInfo)
        {
            string strIP; 
            VbCollection equipInfo;
            AddMessage("网络拓扑探测和绘制开始");
            while (WaitForDetectList.Count > 0)
            {
                if (!isDetecting)
                {
                    AddMessage("网络拓扑探测和绘制被强制结束");
                    return;
                }
                WaitForDetectInfomation waitInfo = WaitForDetectList[0];
                IpAddress ip = waitInfo.Ip;
                Equipment lastEquip = waitInfo.Equip;
                int step = waitInfo.Step;
                WaitForDetectList.RemoveAt(0);
                //需要获取的单个snmp变量信息包括设备描述、objectid（enterprises.311.1.1.3.1.2，可用来判断设备厂家，待验证）
                /*objectid验证整理    enterprises = 1.3.6.1.4.1
                 虚拟机2003操作系统：enterprises.311.1.1.3.1.2
                 */
                
                if (ipAlreadyFindList.ContainsKey(ip))
                {   //这里应该不可能执行到，试一下
//                     MessageBox.Show("这里应该不可能执行到，试一下");
//                     continue;
                    //下一跳设备已经存在，只画线即可
                    if (ipAlreadyFindList[ip].Equip.AdminIPAddress.ToString() == "67.250.5.1" && lastEquip.AdminIPAddress.ToString() == "3.250.5.1")
                    {
                    }
                    DrawLine(ipAlreadyFindList[ip].Equip, lastEquip);
                    AddMessage("直接画线" + ip.ToString() + "到" + lastEquip.AdminIPAddress.ToString());
                    continue;
                }
                equipInfo = SnmpHelper.GetResultsFromOids(ip,equipInfoRequestOids,out tipMessage);
                if (equipInfo == null)
                {
                    AddMessage(string.Format("出错，错误信息：{0}",tipMessage));
                    AddMessage(string.Format("SNMP获取ip:{0}设备system信息失败，进入下一轮",ip.ToString()));
                    continue;
                }
                string sysDescr = equipInfo[0].Value.ToString();
                string sysObjectID = equipInfo[1].Value.ToString();
                byte sysServices = Convert.ToByte(equipInfo[2].Value.ToString());//可以根据此值尝试获取设备类型，当数据库中没有值的时候使用此猜测值。

                //开始建立ucequip，equipment，搜索静态列表是否有这个设备，取得名字等信息
                strIP = ip.ToString();
                Equipment equip = GetEquipment(strIP, sysDescr, lastEquip);
                equip.EquipBrand = SnmpHelper.GetBrandFromObjectID(sysObjectID);
                if (equip == null)
                {
                    AddMessage(string.Format("SNMP获取ip:{0}设备信息失败，进入下一轮", strIP));
                    continue;
                }
                equipAlreadyFindList.Add(equip.Index, equip);
                AddMessage("发现设备信息，设备名称：" + equip.Name);

                UCEquipIcon ucEquipIcon = GetUCEquipIcon(equip);
                if (ucEquipIcon == null)
                {
                    AddMessage(string.Format("添加设备{0}图标、定位失败，进入下一轮", strIP));
                    continue;
                }
                AddMessage(string.Format("添加设备图标，坐标x:{0}，y:{1}", equip.X, equip.Y));
                //GetTextBlock(ucEquipIcon);
                FilterRouteInfo(equip,step);
                if (equip.AdminIPAddress.ToString() == "67.250.5.1" && lastEquip.AdminIPAddress.ToString() == "3.250.5.1")
                {
                }
                if (lastEquip != null)
                    DrawLine(equip,lastEquip);
            }
            IsDetecting = false;
            AddMessage("网络拓扑发现和绘制完毕");
        }
        //Dictionary
        private void DrawLine(Equipment equip, Equipment lastEquip)
        {
            Line l = null;


#region  废弃，拓扑发现时不获取设备连接相关信息，trap来的时候获取
            LineInfo lInfo = new LineInfo();
            lInfo.UCEquipA = equip.UCEquipIcon;
            lInfo.UCEquipB = lastEquip.UCEquipIcon;
            try
            {
                if (equip.IpFirstGet.ToString() == "3.245.1.1" && lastEquip.IpFirstGet.ToString() == "3.247.1.1")
                {
                }
                //随便获取一个对方设备一个地址所属的子网，这样才可以从路由表中得到路由信息和其中的ifIndex
                IpAddress subnetA = equip.IpFirstGet.GetSubnetAddress(equip.IPAndInfoList[equip.IpFirstGet].IpMask);
                if (lastEquip.IpDstAndRouteInfoLIst.ContainsKey(subnetA))
                    lInfo.IfIDB = lastEquip.IpDstAndRouteInfoLIst[subnetA].IfIndex;
                else
                {
                    //考虑默认路由聚合路由等情况，比如到3.11.1.0/24的路由，它用3.0.0.0/8囊括了
                    //还需要考虑一种情况，使用IpFirstGet所在子网作为目的地址获取路由，万一这个是一个互联子网或者不重要的
                    //且路由协议不是动态的，它不把此子网加入到路由表怎么办？
                    bool isContainSubnet = false;
                    foreach (KeyValuePair<IpAddress, RouteInfomation> pair in lastEquip.IpDstAndRouteInfoLIst)
                    {
                        if (pair.Value.RouteType != 4)
                            continue;   //既然是默认路由，肯定是indirect(4)
                        //寻找是否有路由包含目的子网
                        if (pair.Key.CompareTo(subnetA.GetSubnetAddress(pair.Value.DstMask)) == 0)
                        {
                            isContainSubnet = true;
                            lInfo.IfIDB = pair.Value.IfIndex;
                            break;
                        }
                    }
                    if (!isContainSubnet)
                        lInfo.IfIDB = -1; //标注为-1 代表未获取到合适的端口id，在后面的链路判断中，需要进行区分
                }
                IpAddress subnetB = lastEquip.IpFirstGet.GetSubnetAddress(lastEquip.IPAndInfoList[lastEquip.IpFirstGet].IpMask);
                if (equip.IpDstAndRouteInfoLIst.ContainsKey(subnetB))
                    lInfo.IfIDA = equip.IpDstAndRouteInfoLIst[subnetB].IfIndex;
                else
                {
                    bool isContainSubnet = false;
                    foreach (KeyValuePair<IpAddress, RouteInfomation> pair in equip.IpDstAndRouteInfoLIst)
                    {
                        if (pair.Value.RouteType != 4)
                            continue;   //既然是默认路由，肯定是indirect(4)
                        //寻找是否有路由包含目的子网
                        if (pair.Key.CompareTo(subnetB.GetSubnetAddress(pair.Value.DstMask)) == 0)
                        {
                            isContainSubnet = true;
                            lInfo.IfIDA = pair.Value.IfIndex;
                            break;
                        }
                    }
                    if (!isContainSubnet)
                        lInfo.IfIDA = -1;
                }
                //将接口ID和连接线的信息加入到两边设备
                equip.IfIDandLineInfoList.Add(lInfo.IfIDA, lInfo);
                lastEquip.IfIDandLineInfoList.Add(lInfo.IfIDB, lInfo);
            }
            catch (System.Exception ex)
            {
                //lInfo = null;
                AddMessage("为连接线设置附加信息出现问题 " + ex.Message);
            }
#endregion

            this.Dispatcher.Invoke(new Action(() =>
            {
                l = new Line();
                l.SetBinding(Line.X1Property, new Binding() { Path = new PropertyPath("CenterPointX"), Source = equip.UCEquipIcon });
                l.SetBinding(Line.Y1Property, new Binding() { Path = new PropertyPath("CenterPointY"), Source = equip.UCEquipIcon });
                l.SetBinding(Line.X2Property, new Binding() { Path = new PropertyPath("CenterPointX"), Source = lastEquip.UCEquipIcon });
                l.SetBinding(Line.Y2Property, new Binding() { Path = new PropertyPath("CenterPointY"), Source = lastEquip.UCEquipIcon });
                l.Tag = lInfo;
                l.ContextMenu = this.FindResource("lineContextMenu") as ContextMenu;
                canvas.Children.Add(l);
            }));
            if (lInfo == null)
            {
                //出现null引用错误，调试用
            }
            lInfo.L = l;
            l.MouseLeftButtonDown += new MouseButtonEventHandler(l_MouseLeftButtonDown);
        }

        void l_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Line l = sender as Line;
            if (drawItem == null)  //不在画图状态，选定此设备，移动物体准备
            {
                if (selectedItem == null)
                {
                    selectedItem = l;
                    l.StrokeThickness = 5d;
                }
                else if (selectedItem == l)
                {
                }
                else
                {
                    if (selectedItem.GetType().Equals(typeof(UCEquipIcon)))
                        (selectedItem as UCEquipIcon).IsSelected = false;
                    else
                        (selectedItem as Line).StrokeThickness = 2d;
                    selectedItem = l;
                    l.StrokeThickness = 5d;
                }
            }
            else //画图状态,只有画线起作用
            {

            }
            e.Handled = true;
        }

        /// <summary>
        /// 过滤设备的路由信息，将未发现的下一跳地址和本设备图标类加入待发现列表中
        /// </summary>
        /// <param name="equip">待过滤路由信息的设备类</param>
        /// <param name="ucEquipIcon">本设备的图标控件</param>
        private void FilterRouteInfo(Equipment equip, int step)
        {
            if (maxStep > 0 && step + 1 >= maxStep)
            {
                AddMessage(string.Format("本条拓扑发现路径跳数已经达到最大跳数{0}，本条路径探索终止", maxStep));
                return;
            }
            bool isRepeatIP;
            foreach (KeyValuePair<IpAddress, RouteInfomation> pair in equip.IpDstAndRouteInfoLIst)
            {
                IpAddress ipNextHop = pair.Value.IpNextHop;
                if (ipNextHop.ToString() == "3.248.1.2")
                {
                    //实验室调试用，重复IP
                }
                if (ipAlreadyFindList.ContainsKey(ipNextHop))
                    continue; //当已发现IP地址列表中存在路由中的下一跳地址，则跳过此条路由
                // || pair.Value.DstMask.ToString().Equals("255.255.255.255") 去掉了，否则部分路由器发现不了
                if ((pair.Value.RouteType != 4 && pair.Value.RouteType != 0) || ipNextHop.ToString().Equals("127.0.0.1") || ipNextHop.ToString().Equals("0.0.0.0"))
                    continue; //过滤不需要的路由 是否只是4还需要考虑，虚拟机中发现了0，是添加的恒久路由，查看静态路由是几！
                //开始过滤路由表中下一跳地址相同的路由
                isRepeatIP = false;
                foreach (WaitForDetectInfomation info in WaitForDetectList)
                {
                    if (info.Ip.Equals(ipNextHop))
                        isRepeatIP = true;
                }
                if (isRepeatIP)
                    continue;
                //开始应用子网列表范围规则
                if (subnetRangeList != null)
                {
                    bool subnetAreaFilterFlag = false;
                    foreach (Subnet subnet in subnetRangeList)
                        if (subnet.Contains(ipNextHop))
                            subnetAreaFilterFlag = true;
                    if (!subnetAreaFilterFlag)
                    {
                        AddMessage(string.Format("下一跳地址{0}不在指定子网列表范围内，本条路径探索终止", ipNextHop.ToString()));
                        return;
                    }
                }
                //所有判断通过，将此下一跳路由信息加入等待探测列表
                if (ipNextHop.ToString() == "3.247.1.1")
                {
                }
                if (equip.IpFirstGet.ToString() == "3.250.1.2")
                {
                }
                if (equip.IpFirstGet.ToString() == "3.247.1.1")
                {
                }
                WaitForDetectList.Add(new WaitForDetectInfomation(ipNextHop, equip, step + 1));
            }
        }

        /// <summary>
        /// 生成设备类信息，获取此设备的ip地址信息列表、接口信息列表、路由信息列表
        /// </summary>
        /// <param name="strIP"></param>
        /// <param name="sysDescr"></param>
        /// <returns></returns>
        private Equipment GetEquipment(string strIP, string sysDescr, Equipment lastEquip)
        {
            Equipment equip = new Equipment();
            equip.IpFirstGet = new IpAddress(strIP);
            if (App.ipAndIPinfoList.ContainsKey(strIP) && App.idAndEquipList.ContainsKey(App.ipAndIPinfoList[strIP].EquipIndex))
            {  //数据库中有此设备，从程序静态数据列表中取相应值
                IPInformation ipInfo = App.ipAndIPinfoList[strIP];
                int equipID = ipInfo.EquipIndex;
                Equipment tempEquip = App.idAndEquipList[equipID];
                equip.Index = equipID;
                equip.Name = tempEquip.Name;
                equip.TypeName = tempEquip.TypeName;
                equip.TypeIndex = tempEquip.TypeIndex;
                equip.X = tempEquip.X;
                equip.Y = tempEquip.Y;
                if (string.IsNullOrEmpty(tempEquip.SysDescr))
                    equip.SysDescr = sysDescr;
                else
                    equip.SysDescr = tempEquip.SysDescr;
            }
            else
            {   //数据库中无此设备
                equip.Index = equipNumNotInDB;
                equipNumNotInDB--;
                equip.TypeName = "其他";
                equip.TypeIndex = 8;
                equip.Name = strIP;
                equip.SysDescr = sysDescr;
                //根据上个设备的位置来优化本设备位置
                SetEquipPosition(equip,lastEquip);
            }
            if (!equip.GetIPAndInfoListFromSNMP(out tipMessage))
            {
                AddMessage(tipMessage);
                AddMessage(string.Format("获取设备(ID:{0},Name:{1})IP地址信息列表时出现错误", equip.Index, equip.Name));
                return null;
            }
            AddMessage(string.Format("获取设备(ID:{0},Name:{1})IP地址信息列表", equip.Index, equip.Name));
            //将本设备的ip地址和ip信息对列表加入到已发现地址和ip信息总列表中
            foreach (KeyValuePair<IpAddress, IPInformation> pair in equip.IPAndInfoList)
            {
                if (pair.Key.ToString() == "3.248.1.2")
                {
                }
                if (ipAlreadyFindList.ContainsKey(pair.Key))
                {
                    AddMessage(string.Format("已发现IP地址列表中存在相同的IP地址{0},请确保网内无重复地址！\n已存在设备首要地址为{1},新添加地址所属设备首要地址为{2}", pair.Key.ToString(), ipAlreadyFindList[pair.Key].Equip.AdminIPAddress.ToString(),equip.AdminIPAddress.ToString()));
                }
                else
                    ipAlreadyFindList.Add(pair.Key, pair.Value);
            }
            if (!equip.GetIFIDandIFInfoList(out tipMessage))
            {
                AddMessage(tipMessage);
                AddMessage(string.Format("获取设备(ID:{0},Name:{1})接口信息列表时出现错误", equip.Index, equip.Name));
                return null;
            }
            AddMessage(string.Format("获取设备(ID:{0},Name:{1})接口信息列表", equip.Index, equip.Name));
            if (!equip.GetIPAndRouteInfoList(out tipMessage))
            {
                AddMessage(tipMessage);
                AddMessage(string.Format("获取设备(ID:{0},Name:{1})路由信息列表时出现错误", equip.Index, equip.Name));
                return null;
            }
            AddMessage(string.Format("获取设备(ID:{0},Name:{1})路由信息列表", equip.Index, equip.Name));
            return equip;
        }

        bool isLeftPositionFirst;
        /// <summary>
        /// 根据上个设备的位置来优化本设备位置
        /// </summary>
        /// <param name="equip">需要定位的设备</param>
        /// <param name="lastEquip">辅助定位的上个设备</param>
        private void SetEquipPosition(Equipment equip, Equipment lastEquip)
        {
            Random r = new Random();
            if (lastEquip == null)
            {   //没有上一个设备，代表这个是第一个发现的设备，放置在窗体的中下部
                int leftNum = x/2;
                int topNum = y -2;
                equip.X = segmentLength * leftNum + 5;
                equip.Y = segmentLength * topNum + 5;
                grid22[leftNum, topNum] = true;
            }
            else
            {
                int lastLeftNum = (int)lastEquip.X / segmentLength;
                int lastTopNum = (int)lastEquip.Y / segmentLength;
                int topNum = lastTopNum - 2;
                if (topNum < 0)
                    topNum = lastTopNum + 1;
                int xx = 0, yy = 0;
                int leftNum = lastLeftNum;
                bool isSetPositionOK = false;
                while (true)
                {   //从中间格子开始，左右依次减加2，进行布局
                    if (lastLeftNum + xx >=0 && grid22[lastLeftNum + xx, topNum] == false)
                    {
                        grid22[lastLeftNum + xx, topNum] = true;
                        leftNum = lastLeftNum + xx;
                        isSetPositionOK = true;
                        break;
                    }
                    if (lastLeftNum + yy <= y && grid22[lastLeftNum + yy, topNum] == false)
                    {
                        grid22[lastLeftNum + yy, topNum] = true;
                        leftNum = lastLeftNum + yy;
                        isSetPositionOK = true;
                        break;
                    }
                    if (lastLeftNum + xx < 0 && lastLeftNum + yy > y)
                        break;
                    xx = xx - 2;
                    yy = yy + 2;
                }
                if (!isSetPositionOK)
                {  //左右依次减加2，进行布局不成功的话，以-1 +1 开始，差2进行布局
                    xx = -1;
                    yy = 1;
                    while (true)
                    {
                        if (lastLeftNum + xx >= 0 && grid22[lastLeftNum + xx, topNum] == false)
                        {
                            grid22[lastLeftNum + xx, topNum] = true;
                            leftNum = lastLeftNum + xx;
                            isSetPositionOK = true;
                            break;
                        }
                        if (lastLeftNum + yy <= y && grid22[lastLeftNum + yy, topNum] == false)
                        {
                            grid22[lastLeftNum + yy, topNum] = true;
                            leftNum = lastLeftNum + yy;
                            isSetPositionOK = true;
                            break;
                        }
                        if (lastLeftNum + xx < 0 && lastLeftNum + yy > y)
                            break;
                        xx = xx - 2;
                        yy = yy + 2;
                    }
                }
                if (!isSetPositionOK)
                {
                    leftNum = lastLeftNum;
                    isSetPositionOK = true;
                }
                equip.X = segmentLength * leftNum + 5;
                equip.Y = segmentLength * topNum + 5;
            }
        }

        /// <summary>
        /// 根据设备类信息和上个设备图标信息来生成本设备图标并定位
        /// </summary>
        /// <param name="equip"></param>
        /// <param name="ucLastEquip"></param>
        /// <returns></returns>
        private UCEquipIcon GetUCEquipIcon(Equipment equip)
        {
            //将图标长宽绑定到程序设置的UCEquipLength变量上来，这个变量后面可以让用户调节
            Binding bindWidth = new Binding() { Path = new PropertyPath("UCEquipLength"), Source = this };
            bindWidth.Converter = new PicWidthToUCWidthConverter();
            Binding bindHeight = new Binding() { Path = new PropertyPath("UCEquipLength"), Source = this };
            bindHeight.Converter = new PicHeightToUCHeightConverter();

            //图标控件的坐标绑定到设备类的坐标上
            Binding bindX = new Binding { Path = new PropertyPath("X"), Source = equip, Mode = BindingMode.TwoWay };
            Binding bindY = new Binding { Path = new PropertyPath("Y"), Source = equip, Mode = BindingMode.TwoWay };

            //图标控件的中心点绑定到设备类的坐标上
            Binding bindCenterX = new Binding { Path = new PropertyPath("X"), Source = equip };
            bindCenterX.Converter = new PointToCenterPointConverter();
            bindCenterX.ConverterParameter = ucEquipLength;

            Binding bindCenterY = new Binding { Path = new PropertyPath("Y"), Source = equip };
            bindCenterY.Converter = new PointYToCenterPointYConverter();
            bindCenterY.ConverterParameter = ucEquipLength;
            UCEquipIcon ucEquipIcon = null;
            this.Dispatcher.Invoke(new Action(() =>
            {
                ucEquipIcon = new UCEquipIcon();
                ucEquipIcon.SetBinding(UCEquipIcon.WidthProperty, bindWidth);
                ucEquipIcon.SetBinding(UCEquipIcon.HeightProperty, bindHeight);
                ucEquipIcon.SetBinding(Canvas.LeftProperty, bindX);
                ucEquipIcon.SetBinding(Canvas.TopProperty, bindY);
                ucEquipIcon.SetBinding(UCEquipIcon.CenterPointXProperty, bindCenterX);
                ucEquipIcon.SetBinding(UCEquipIcon.CenterPointYProperty, bindCenterY);
                canvas.Children.Add(ucEquipIcon);
            }));
            //图标控件和设备类互相引用
            ucEquipIcon.Equip = equip;
            equip.UCEquipIcon = ucEquipIcon;
            //图标控件的属性和事件
            ucEquipIcon.MouseLeftButtonDown += new MouseButtonEventHandler(ucEquip_MouseLeftButtonDown);
            ucEquipIcon.MouseMove += new MouseEventHandler(ucEquip_MouseMove);
            ucEquipIcon.MouseLeftButtonUp += new MouseButtonEventHandler(ucEquip_MouseLeftButtonUp);
            ucEquipIcon.MouseRightButtonDown += new MouseButtonEventHandler(ucEquip_MouseRightButtonDown);

            return ucEquipIcon;
        }

        private void AddMessage(string _s)
        {
            _s = string.Format("{0}. {1} {2}", listboxIndex++, DateTime.Now.ToLongTimeString(), _s);
            if (this.Dispatcher.Thread.Equals(Thread.CurrentThread))
                tipMessageList.Add(_s);
            else
                this.Dispatcher.Invoke(new Action(() =>
                {
                    tipMessageList.Add(_s);
                    lbSearchingMessage.ScrollIntoView(lbSearchingMessage.Items[lbSearchingMessage.Items.Count - 1]);
                    //Thread.Sleep(50);
                }));
        }

        private void SetTextBlockAsync(TextBlock tb, string s)
        {
            if (this.Dispatcher.Thread == Thread.CurrentThread)
                tb.Text = s;
            else
            {
                this.Dispatcher.Invoke(new Action(() =>
                {
                    tb.Text = s;
                }));
            }
        }

        void ucEquip_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true; //在canvas空白处右击，取消画图状态，需要在uc和line上的右键事件进行拦截，否则路由事件会传递，目的是在图中空白处右击取消画图状态
        }

        void ucEquip_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            UCEquipIcon uc = e.Source as UCEquipIcon;
            if (drawItem == null)  //不在画图状态，选定此设备，移动物体准备
            {
                if (selectedItem == null)
                {
                    selectedItem = uc;
                    uc.IsSelected = true;
                
                }
                else if (selectedItem == uc)
                {
                }
                else
                {
                    if (selectedItem.GetType().Equals(typeof(UCEquipIcon)))
                        (selectedItem as UCEquipIcon).IsSelected = false;
                    else
                        (selectedItem as Line).StrokeThickness = 2d;
                    selectedItem = uc;
                    uc.IsSelected = true;
                }
                preObjectPoint = e.GetPosition(this);//获取点击前鼠标相对窗体坐标
                preMousePoint = new Point(Canvas.GetLeft(uc), Canvas.GetTop(uc));//获取点击前物体的坐标
                isMove = true;//开始移动了
                canvas.Cursor = Cursors.SizeAll;
                uc.CaptureMouse();//鼠标捕获此圆
            }
            else //画图状态,只有画线起作用
            { 
                    
            }
            e.Handled = true;
        }
        void ucEquip_MouseMove(object sender, MouseEventArgs e)
        {
            if (isMove)
            {
                UCEquipIcon uc = e.Source as UCEquipIcon;
                Point p = e.GetPosition(this);//获取鼠标移动中的相对窗体坐标
                double x = preMousePoint.X + (p.X - preObjectPoint.X);
                double y = preMousePoint.Y + (p.Y - preObjectPoint.Y);
                if (x > canvas.ActualWidth - uc.ActualWidth)
                    x = canvas.ActualWidth - uc.ActualWidth;
                if (x < 0)
                    x = 0;
                if (y > canvas.ActualHeight - uc.ActualHeight)
                    y = canvas.ActualHeight - uc.ActualHeight;
                if (y < 0)
                    y = 0;
                Canvas.SetLeft(uc, x);
                Canvas.SetTop(uc, y);
            }
        }
        void ucEquip_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isMove)
            {
                UCEquipIcon uc = e.Source as UCEquipIcon;
                isMove = false;//结束移动了
                canvas.Cursor = Cursors.Arrow;
                uc.ReleaseMouseCapture();//鼠标释放此圆

                //考虑在移动最后结束的时候，将数据库中有的设备（index>0）坐标变化及时写入数据库
                if (App.databaseConState == true && uc.Equip.Index > 0)
                {
                    Equipment equip = uc.Equip;
                    int id = equip.Index;
                    double x = equip.X;
                    double y = equip.Y;
                    if (App.idAndEquipList.ContainsKey(id))
                    {
                        App.idAndEquipList[id].X = x;
                        App.idAndEquipList[id].Y = y;
                    }
                    else
                    {
                        tbStatusMessage.Text = "App.idAndEquipList全局列表不包含此设备";
                    }
                    if (App.DBHelper.ExecuteReturnBool(string.Format("UPDATE Equipments SET Equip_X = {0},Equip_Y = {1} WHERE Equip_Index = {2}", x, y, id)))
                        tbStatusMessage.Text = string.Format("{0}位置更新 {1},{2}",equip.Name,x,y);
                    else
                        tbStatusMessage.Text = equip.Name + "位置更新失败";
                }

            }
        }

        private void sliderBtn_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (drawToolbar.Width > 0)
                (this.FindResource("sbDrawToolbarSliderClose") as Storyboard).Begin();
            else
                (this.FindResource("sbDrawToolbarSlider") as Storyboard).Begin();
        }

        private void btnDrawRouter_Click(object sender, RoutedEventArgs e)
        {
            InitDrawEquip(EquipType.Router);
        }

        private void btnDrawL3Sw_Click(object sender, RoutedEventArgs e)
        {
            InitDrawEquip(EquipType.Layer3Switch);
        }

        private void btnDrawL2Sw_Click(object sender, RoutedEventArgs e)
        {
            InitDrawEquip(EquipType.Layer2Switch);
        }

        private void btnDrawServer_Click(object sender, RoutedEventArgs e)
        {
            InitDrawEquip(EquipType.ServerInTable);
        }

        private void btnDrawPC_Click(object sender, RoutedEventArgs e)
        {
            InitDrawEquip(EquipType.PC);
        }

        private void btnDrawFireWall_Click(object sender, RoutedEventArgs e)
        {
            InitDrawEquip(EquipType.FireWall);
        }

        private void btnDrawLine_Click(object sender, RoutedEventArgs e)
        {
            if (drawItem != null && drawItem.GetType().Equals(typeof(UCEquipIcon)))
            {
                canvas.Children.Remove(drawItem as UCEquipIcon);
                drawItem = null;
            }
            drawItem = new Line();
        }

        private void btnDrawCancel_Click(object sender, RoutedEventArgs e)
        {
            if (drawItem != null)
            {
                canvas.Children.Remove(drawItem as UIElement);
                drawItem = null;
            }
        }

        private void InitDrawEquip(EquipType equipType)
        {
            if (drawItem != null)
            {
                canvas.Children.Remove(drawItem as UCEquipIcon);
                drawItem = null;
            }
            UCEquipIcon ucEquip = new UCEquipIcon();
            Binding bindWidth = new Binding() { Path = new PropertyPath("UCEquipLength"), Source = this };
            bindWidth.Converter = new PicWidthToUCWidthConverter();
            Binding bindHeight = new Binding() { Path = new PropertyPath("UCEquipLength"), Source = this };
            bindHeight.Converter = new PicHeightToUCHeightConverter();
            ucEquip.SetBinding(UCEquipIcon.WidthProperty, bindWidth);
            ucEquip.SetBinding(UCEquipIcon.HeightProperty, bindHeight);
            Equipment equip = new Equipment();
            ucEquip.Equip = equip;
            equip.Type = equipType;
            ucEquip.Visibility = Visibility.Hidden;
            drawItem = ucEquip;
            canvas.Children.Add(ucEquip as UCEquipIcon);
        }

        private void canvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (drawItem != null)
            {
                canvas.Children.Remove(drawItem as UIElement);
                drawItem = null;
                canvas.Cursor = Cursors.Arrow;
            }
        }

        private void canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (drawItem != null)
            {
                if (drawItem.GetType() == typeof(UCEquipIcon))
                {
                    UCEquipIcon element = drawItem as UCEquipIcon;
                    element.Visibility = Visibility.Visible;
                    Canvas.SetLeft(element, e.GetPosition(canvas).X - 25);
                    Canvas.SetTop(element, e.GetPosition(canvas).Y + 5);
                }
                else //除了UCEquipIcon就是线
                {
                    canvas.Cursor = Cursors.Pen;
                }
            }
        }

        private void canvas_MouseEnter(object sender, MouseEventArgs e)
        {
        }

        private void canvas_MouseLeave(object sender, MouseEventArgs e)
        {
            canvas.Cursor = Cursors.Arrow;
        }

        private void canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (selectedItem != null)
            {
                if (selectedItem.GetType().Equals(typeof(UCEquipIcon)))
                {
                    (selectedItem as UCEquipIcon).IsSelected = false;
                }
                else
                {
                    (selectedItem as Line).StrokeThickness = 2d;
                }
            }
            selectedItem = null;
        }

        private void btnCloseLoadingGrid_Click(object sender, RoutedEventArgs e)
        {
            IsDetecting = false;
            loadingGrid.Visibility = Visibility.Hidden;
        }

        private void btnStopDetect_Click(object sender, RoutedEventArgs e)
        {
            IsDetecting = false;
        }

        private void btnRestartDetect_Click(object sender, RoutedEventArgs e)
        {
            if (!isDetecting)
            {
                IsListenTrap = false;
                IsDetecting = true;
                InitParameters();
                ThreadPool.QueueUserWorkItem(StartDetecNetwork);
            }
        }

        private void AcceptTrap()
        {
            if (!isListenTrap)
                return;
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint ipep = new IPEndPoint(IPAddress.Any, App.snmpTrapPort);
            EndPoint ep = (EndPoint)ipep;
            try
            {
                socket.Bind(ep);
            }
            catch (Exception ex)
            {
                IsListenTrap = false;
                MessageBox.Show(string.Format("打开TRAP监听端口{0}失败\n详细信息:{1}", App.snmpTrapPort, ex.Message));
                return;
            }
            SetTextBlockAsync(tbStatusMessage, string.Format("开始后台接收Trap消息，udp端口{0}", App.snmpTrapPort));
            // Disable timeout processing. Just block until packet is received 
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 0);
            IPEndPoint peer = new IPEndPoint(IPAddress.Any, 0);
            EndPoint inep = (EndPoint)peer;
            while (isListenTrap)
            {
                // 16KB receive buffer 
                byte[] indata = new byte[16 * 1024];
                int inlen = 0;
                try
                {
                    inlen = socket.ReceiveFrom(indata, ref inep);//inlen为udp包的负载
                }
                catch (Exception ex)
                {
                    string errorMessage;
                    if (isListenTrap)
                    {
                        errorMessage = string.Format("异常 {0}", ex.Message);
                        SetTextBlockAsync(tbStatusMessage, errorMessage);
                    }
                    else
                    {
                        errorMessage = "后台接收Trap线程被强行终止";
                        SetTextBlockAsync(tbStatusMessage, errorMessage);
                        return;
                    }
                    inlen = -1;
                }
                if (inlen > 0)
                {
                    // Check protocol version 
                    int ver = SnmpPacket.GetProtocolVersion(indata, inlen);
                    //在snmphelper中建立静态函数分别解析V1 V2的trap。。。
                    if (ver == (int)SnmpVersion.Ver1)
                    {
                        // Parse SNMP Version 1 TRAP packet 
                        SnmpV1TrapPacket pkt = new SnmpV1TrapPacket();
                        pkt.decode(indata, inlen);
                        Console.WriteLine("** SNMP Version 1 TRAP received from {0}:", inep.ToString());
                        Console.WriteLine("*** Trap generic: {0}", pkt.Pdu.Generic);
                        Console.WriteLine("*** Trap specific: {0}", pkt.Pdu.Specific);
                        Console.WriteLine("*** Agent address: {0}", pkt.Pdu.AgentAddress.ToString());
                        Console.WriteLine("*** Timestamp: {0}", pkt.Pdu.TimeStamp.ToString());
                        Console.WriteLine("*** VarBind count: {0}", pkt.Pdu.VbList.Count);
                        Console.WriteLine("*** VarBind content:");
                        foreach (Vb v in pkt.Pdu.VbList)
                        {
                            Console.WriteLine("**** {0} {1}: {2}", v.Oid.ToString(), SnmpConstants.GetTypeName(v.Value.Type), v.Value.ToString());
                        }
                        Console.WriteLine("** End of SNMP Version 1 TRAP data.");
                    }
                    else
                    {
                        // Parse SNMP Version 2 TRAP packet 
                        SnmpV2Packet pkt = new SnmpV2Packet();
                        pkt.decode(indata, inlen);
                        Console.WriteLine("** SNMP Version 2 TRAP received from {0}:", inep.ToString());
                        if (pkt.Pdu.Type != PduType.V2Trap)
                        {
                            Console.WriteLine("*** NOT an SNMPv2 trap ****");
                        }
                        else
                        {
                            Console.WriteLine("*** Community: {0}", pkt.Community.ToString());
                            Console.WriteLine("*** VarBind count: {0}", pkt.Pdu.VbList.Count);
                            Console.WriteLine("*** VarBind content:");
                            foreach (Vb v in pkt.Pdu.VbList)
                            {
                                Console.WriteLine("**** {0} {1}: {2}",
                                   v.Oid.ToString(), SnmpConstants.GetTypeName(v.Value.Type), v.Value.ToString());
                            }
                            Console.WriteLine("** End of SNMP Version 2 TRAP data.");
                        }
                    }
                }
                else
                {
                    if (inlen == 0)
                        Console.WriteLine("Zero length packet received.");
                }
            }
            SetTextBlockAsync(tbStatusMessage, "后台接收Trap线程结束");
        }

        private void btnUpdateDB_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("即将更新数据库中设备、IP等相关数据，\n用户自行添加的非本网内数据不会丢失，\n是否继续？","提示",MessageBoxButton.YesNo,MessageBoxImage.Information) == MessageBoxResult.Yes)
            {
                if (App.databaseConState == false)
                {
                    MessageBox.Show("数据库连接异常，请检查数据库连接");
                    return;
                }
                bool isSaveAllOK = true;
                StringBuilder sb = new StringBuilder();
                foreach (Equipment equip in equipAlreadyFindList.Values)
                {
                     if (!equip.SaveInformation())
                     {
                         isSaveAllOK = false;
                         sb.Append(equip.Name + ",");
                     }
                }
                if (isSaveAllOK)
                    MessageBox.Show("全部设备保存成功");
                else
                    MessageBox.Show("设备 " + sb.ToString().TrimEnd(',') + " 保存失败");
            }
        }

    }

    /// <summary>
    /// 等待发现的信息类，包含等待发现的IP地址，以及此IP对应的上一个设备类。即通过此设备，发现的此IP。
    /// 在提取判断的过程中，如果此IP在已发现列表中，则只画线；否则同时画线并生成此IP对应设备信息。
    /// </summary>
    public class WaitForDetectInfomation
    {
        IpAddress ip;
        Equipment equip;
        int step;

        /// <summary>
        /// 记录每条路径发现所经历的跳数
        /// </summary>
        public int Step
        {
            get { return step; }
            set { step = value; }
        }

        /// <summary>
        /// 待探测IP地址
        /// </summary>
        public IpAddress Ip
        {
            get { return ip; }
            set { ip = value; }
        }
        /// <summary>
        /// 从此设备路由表生成的这个类信息，用于回溯画线及确定坐标。
        /// </summary>
        public Equipment Equip
        {
            get { return equip; }
            set { equip = value; }
        }
        public WaitForDetectInfomation(IpAddress _ip, Equipment _uc, int _step)
        {
            ip = _ip;
            equip = _uc;
            step = _step;
        }

        public override string ToString()
        {
            if (equip == null)
                return string.Format("下一跳:{0},源设备:无", ip.ToString());
            else
                return string.Format("下一跳:{0},源设备:{1}", ip.ToString(), equip.IpFirstGet.ToString());
        }
    }


}
