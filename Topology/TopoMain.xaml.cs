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
    public partial class TopoMain : MyWindow, INotifyPropertyChanged
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
        double ucEquipLength=50d;
        TopoData<Equipment, LineInfo> topoData;
        /// <summary>
        /// 获取设备信息，sysDescr,sysObjectID,sysServices
        /// </summary>
        string[] equipInfoRequestOids = new string[] { "1.3.6.1.2.1.1.1.0", "1.3.6.1.2.1.1.2.0", "1.3.6.1.2.1.1.7.0" };
        string tipMessage;
        ObservableCollection<string> tipMessageList = new ObservableCollection<string>(); //发现拓扑时界面上显示的信息
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
       // List<WaitForDetectInfomation> WaitForDetectList = new List<WaitForDetectInfomation>();
        /// <summary>
        /// 已发现IP地址键值集合，通过ip找到IPInformation，其中有设备id号，可定位设备
        /// </summary>
        Dictionary<IpAddress, IPInformation> ipAlreadyFindList = new Dictionary<IpAddress, IPInformation>();
        /// <summary>
        /// 已发现设备集合，id>0：数据库中有对应设备；id小于0：不在数据库中设备
        /// </summary>
        //Dictionary<int, Equipment> equipAlreadyFindList = new Dictionary<int, Equipment>();  有了矩阵应该不需要了
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
                        SetTextBlockAsync(tbDetectlabel, "正在探测网络、获取拓扑数据……");
                    }
                    else
                    {
                        (this.FindResource("sbSearching") as Storyboard).Stop();
                        SetTextBlockAsync(tbDetectlabel, "网络拓扑探测完毕");
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

        public TopoMain(IpAddress _ipClue, int _maxStep = 0, List<Subnet> _subnetRangeList = null)
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
            //CalculateGridInCanvas();   选择网格树形绘图才使用网格计算
           // WaitForDetectList.Add(new WaitForDetectInfomation(ipClue, null, 0));
            IsDetecting = true;
            InitParameters();
            ThreadPool.QueueUserWorkItem(StartDetecTopo);
            cbIsListenTrap.SetBinding(CheckBox.IsCheckedProperty, new Binding() { Path = new PropertyPath("IsListenTrap"), Source = this, Mode = BindingMode.TwoWay });
        }


        /// <summary>
        /// 重新绘制拓扑图的时候，初始化相关控件，集合，属性等
        /// </summary>
        private void InitParameters()
        {
            listboxIndex = 1; 
            equipNumNotInDB = -1;
            topoData = new TopoData<Equipment, LineInfo>();
            tipMessageList.Clear();
            ipAlreadyFindList.Clear();
            equipManualAddList.Clear();
            canvas.Children.Clear();
        }


        /// <summary>
        /// 探测网络拓扑全过程，应该要在新线程中工作，后面改
        /// </summary>
        private void StartDetecTopo(Object stateInfo)
        {
            int detectingLineNumber = -1; //正在探测的矩阵行数值，同时也是列数值
            AddMessage("网络拓扑探测开始");
            while (detectingLineNumber < topoData.Number)  //说明存在未发现的topo
            {
                if (!isDetecting)
                {
                    AddMessage("！！！网络拓扑探测和绘制被强制结束");
                    return;
                }
                if (detectingLineNumber == -1)
                {
                    //初始发现状态，需先添加第一个发现的设备，才能继续进行拓扑发现
                    Equipment equip = GetEquipInfo(ipClue);
                    equip.Step = 1;
                    topoData.AddV(equip);
                    AddMessage("发现设备信息，设备名称：" + equip.Name);
                    detectingLineNumber++;
                }
                else
                {
                    List<IpAddress> waitForDetectIP = new List<IpAddress>(); // 在每一个已发现设备中过滤出来的下一跳地址
                    Equipment detectingEquip = topoData.GetV(detectingLineNumber);
                    if (maxStep != 0 && detectingEquip.Step >= maxStep)
                    {
                        AddMessage(string.Format("本条拓扑发现路径跳数已经达到最大跳数{0}，本条路径探索终止", maxStep));
                        detectingLineNumber++;
                        continue;
                    }
                    foreach (KeyValuePair<IpAddress, RouteInfomation> pair in detectingEquip.IpDstAndRouteInfoLIst)
                    {
                        IpAddress ipNextHop = pair.Value.IpNextHop;
                        // || pair.Value.DstMask.ToString().Equals("255.255.255.255") 去掉了，否则部分路由器发现不了
                        if ((pair.Value.RouteType != 4 && pair.Value.RouteType != 0) || ipNextHop.ToString().Equals("127.0.0.1") || ipNextHop.ToString().Equals("0.0.0.0"))
                            continue; //过滤不需要的路由 是否只是4还需要考虑，虚拟机中发现了0，是添加的恒久路由，查看静态路由是几！
                        //开始应用子网列表范围规则
                        if (subnetRangeList != null)
                        {
                            bool subnetAreaFilterFlag = false;
                            foreach (Subnet subnet in subnetRangeList)
                                if (subnet.Contains(ipNextHop))
                                    subnetAreaFilterFlag = true;
                            if (!subnetAreaFilterFlag)
                            {
                                AddMessage(string.Format("设备{0}的下一跳地址{1}不在指定子网列表范围内，略过", detectingEquip.Name, ipNextHop.ToString()));
                                continue;
                            }
                        }
                        if (ipAlreadyFindList.ContainsKey(ipNextHop))
                        {
                            AddLineInfo2TopoData(detectingEquip, ipAlreadyFindList[ipNextHop].Equip);
                            continue; //当已发现IP地址列表中存在路由中的下一跳地址，则跳过此条路由
                        }
                        //所有判断通过，将此下一跳路由信息加入等待探测列表
                        if (!waitForDetectIP.Contains(pair.Value.IpNextHop))
                            waitForDetectIP.Add(pair.Value.IpNextHop);
                    }
                    if (waitForDetectIP.Count == 0)
                    {
                        AddMessage(string.Format("设备{0}没有符合定义的下一跳地址，本探测路径终止", detectingEquip.Name));
                        detectingLineNumber++;
                        continue;
                    }
                    foreach (IpAddress ip in waitForDetectIP)
                    {
                        if (!isDetecting)
                        {
                            AddMessage("！！！网络拓扑探测和绘制被强制结束");
                            return;
                        }
                        Equipment equip = GetEquipInfo(ip);
                        if (equip == null)
                            continue;
                        topoData.AddV(equip);
                        equip.Step = detectingEquip.Step + 1;
                        AddMessage("发现设备信息，设备名称：" + equip.Name);
                        AddLineInfo2TopoData(detectingEquip, equip);
                        AddMessage(string.Format("添加设备{0}与设备{1}的连接信息" ,detectingEquip.Name, equip.Name));
                    }
                    detectingLineNumber++;
                }
            }
            IsDetecting = false;
            AddMessage("网络拓扑发现完毕");
        }

        /// <summary>
        /// 根据IP地址发现此设备的信息
        /// </summary>
        /// <param name="nextIP"></param>
        /// <returns></returns>
        private Equipment GetEquipInfo(IpAddress nextIP)
        {
            VbCollection equipInfo;
            string strIP;
            equipInfo = SnmpHelper.GetResultsFromOids(nextIP, equipInfoRequestOids, out tipMessage);
            if (equipInfo == null)
            {
                AddMessage(string.Format("！！！出错，错误信息：{0}", tipMessage));
                AddMessage(string.Format("！！！SNMP获取初始设备ip:{0}system信息失败", nextIP.ToString()));
                return null;
            }
            string sysDescr = equipInfo[0].Value.ToString();
            string sysObjectID = equipInfo[1].Value.ToString();
            byte sysServices = Convert.ToByte(equipInfo[2].Value.ToString());//可以根据此值尝试获取设备类型，当数据库中没有值的时候使用此猜测值。

            strIP = nextIP.ToString();
            Equipment equip = GetEquipment(strIP, sysDescr);
            //equip.Step = 1;
            equip.EquipBrand = SnmpHelper.GetBrandFromObjectID(sysObjectID);
            if (equip == null)
                AddMessage(string.Format("！！！SNMP获取初始设备ip:{0}设备信息失败", strIP));
            return equip;
        }

        private void AddLineInfo2TopoData(Equipment srcEquip, Equipment dstEquip)
        {
            int i = topoData.GetIndexForV(srcEquip);
            int j = topoData.GetIndexForV(dstEquip);
            if (topoData.GetE(i, j) != null)
                return;
            LineInfo lInfo = new LineInfo();
            lInfo.EquipA = srcEquip;
            lInfo.EquipB = dstEquip;
            topoData.AddE(i, j, lInfo);
        }

        bool isDrawing;
        #region 网格型拓扑绘制参数
        bool[,] grid22; //根据canvas的长宽 分割成一个二维的矩形矩阵，每个矩阵初始值为false，代表没有东西占用，当canvas添加设备的时候，根据一定的算法来填充，同时在对应格子里面设置为true
        Dictionary<int, int> stepNumberList;
        #endregion

        #region 射线型拓扑绘制参数

        #endregion

        #region 力导向型拓扑绘制参数
        double tensionThresholdLength = 150d; //线的张力阈值长度，当线实际长度大于此值时，就会产生张力
        double forceWhileSuperposition = 100d;   //计算斥力时两点若重合距离为0，其斥力模为此值。
        double g = 500000d;  // 计算两点之间斥力时的常量g
        double maxPx = 20;  //每次迭代每个元素最多移动20像素
        #endregion

        private void btnStartDrawTopo_Click(object sender, RoutedEventArgs e)
        {
            if (isDetecting)
            {
                MessageBox.Show("拓扑发现过程还未结束");
                return;
            }
            loadingGrid.Visibility = Visibility.Hidden;
            ClearHistoryItem();
            isDrawing = true;
            if (rbGridTopo.IsChecked == true)
            {
                ThreadPool.QueueUserWorkItem(DrawGridTopo);
            }
            else if (rbRayTopo.IsChecked == true)
            {
                DrawRayTopo();
            }
            else
            {
                try
                {
                    tensionThresholdLength = Double.Parse(tbLineTensionThresholdLength.Text.Trim());
                    g = Double.Parse(tbG.Text.Trim());
                    maxPx = Double.Parse(tbMaxPx.Text.Trim());
                }
                catch
                {
                    MessageBox.Show("力导向拓扑参数设置错误");
                    isDrawing = false;
                    return;
                }
                ThreadPool.QueueUserWorkItem(DrawPullAndRepulsionTopo);
            }
        }
        
        /// <summary>
        /// 若是在拓扑界面内更改拓扑形式，则需要清除上一个拓扑发现的内容
        /// 需要再补！！！
        /// </summary>
        private void ClearHistoryItem()
        {
            //清除可能存在的网格拓扑
            canvas.Children.Clear();
            if (topoData != null)
            {
                for (int i = 0; i < topoData.Number; i++)
                    topoData.GetV(i).UCEquipIcon = null;
            }
        }

        private void DrawGridTopo(object value)
        {
            SetTextBlockAsync(tbStatusMessage, "使用网格模式绘制拓扑开始");
            //得到每一行有几个设备的dictionary，并初始化bool[]
            GetGridInfo();
            int rows = stepNumberList.Count + 2;
            int colums = stepNumberList.Values.Max() + 2;
            for (int i = 0; i < topoData.Number; i++)
            {
                //绘制矩阵列左边的每一行的设备
                Equipment equipRow = topoData.GetV(i);
                GetEquipIconAndPosition(rows, colums, equipRow);
                //根据此行设备，绘制与其相连的设备和连线
                for (int j = i + 1; j < topoData.Number; j++)
                {
                    if (topoData.GetE(i,j) == null)
                        continue;
                    Equipment equipColum = topoData.GetV(j);
                    GetEquipIconAndPosition(rows, colums, equipColum);
                    DrawLine(equipRow, equipColum);
                    Thread.Sleep(200);
                }
            }
            isDrawing = false;
            SetTextBlockAsync(tbStatusMessage, "绘制网格模式拓扑完毕");
        }

        private void GetEquipIconAndPosition(int rows, int colums, Equipment equip)
        {
            if (equip.UCEquipIcon == null)
                GetUCEquipIcon(equip);
            bool hasIcon = false;
            this.Dispatcher.Invoke(new Action(() =>
            {
                hasIcon = canvas.Children.Contains(equip.UCEquipIcon);
            }));
            if (!hasIcon)
            {
                if (equip.Index < 0)
                    SetEquipPositionInGrid(equip, rows, colums);
                this.Dispatcher.Invoke(new Action(() =>
                {
                    canvas.Children.Add(equip.UCEquipIcon);
                }));
                Thread.Sleep(200);
            }
        }

                
        /// <summary>
        /// 通过topodata中的矩阵和每个equip实例中的step计算出grid中应划分几行几列
        /// </summary>
        private void GetGridInfo()
        {
            //通过topodata计算grid需要几行几列
            int number = topoData.Number;
            stepNumberList = new Dictionary<int, int>();// 记录每个step有几个设备 dic[1] 代表step1
            
            int theMaxStepNumber = 0; //拥有最多equip 的step
            for (int i = 0; i < number; i++)
            {
                int step = topoData.GetV(i).Step;
                if (stepNumberList.ContainsKey(step))
                    stepNumberList[step]++;
                else
                    stepNumberList.Add(step, 1);
                theMaxStepNumber = stepNumberList.Values.Max();
            }
            grid22 = new bool[stepNumberList.Count + 2, theMaxStepNumber +2];
        }

        private void SetEquipPositionInGrid(Equipment equip, int row, int colum)
        {
            int equipNumsAtStep = stepNumberList[equip.Step];
            int startPosition = (colum - equipNumsAtStep) / 2;
            for (int i = startPosition; i < colum; i++)
            {
                int stepRow = row - equip.Step - 1;
                if (!grid22[stepRow, i])
                {
                    equip.X = (canvas.ActualWidth / colum) * i;
                    equip.Y = (canvas.ActualHeight / row) * stepRow;
                    grid22[stepRow, i] = true;
                    return;
                }
            }
            //进行到这里说明没有位置了，随即在这一行分配坐标，不过根据之前的计算，这里应该执行不到
        }
        

        private void DrawPullAndRepulsionTopo(object o)
        {
            SetTextBlockAsync(tbStatusMessage, "使用力导向模式绘制拓扑开始");
            DeployEquipAndLineRandom();
            TopoAdjustment();


            SetTextBlockAsync(tbStatusMessage, "绘制力导向模式拓扑完毕");
        }
        /// <summary>
        /// 将设备随机放置在画布上并连线
        /// </summary>
        private void DeployEquipAndLineRandom()
        {
            Thread.Sleep(400);
            SetTextBlockAsync(tbStatusMessage, "开始随机部署拓扑");
            double canvasWidth = canvas.ActualWidth;
            double canvasHeight = canvas.ActualHeight;
            Random r = new Random();
            for (int i = 0; i < topoData.Number; i++)
            {
                //绘制矩阵列左边的每一行的设备
                Equipment equip = topoData.GetV(i);
                if (equip.UCEquipIcon != null)
                    continue;
                GetUCEquipIcon(equip);
                double x = r.NextDouble() * (canvasWidth - 80);
                double y = r.NextDouble() * (canvasHeight - 80);
                equip.X = x;
                equip.Y = y;
                this.Dispatcher.Invoke(new Action(() =>
                {
                    canvas.Children.Add(equip.UCEquipIcon);
                }));
                Thread.Sleep(200);
            }
            for (int i = 0; i < topoData.Number; i++)
            {
                for (int j = i + 1; j < topoData.Number; j++)
                {
                    if (topoData.GetE(i, j) == null)
                        continue;
                    DrawLine(topoData.GetV(i), topoData.GetV(j));
                    Thread.Sleep(200);
                }
            }
            SetTextBlockAsync(tbStatusMessage, "随机部署拓扑完毕");
        }

        /// <summary>
        /// 使用力导向算法对拓扑进行迭代
        /// </summary>
        private void TopoAdjustment()
        {
            Thread.Sleep(500);
            SetTextBlockAsync(tbStatusMessage, "使用力导向算法对拓扑进行迭代");
            int i = 0; 
            while (isDrawing)
            {
                CalculateDegree();
                CalculateTension();
                CalculateRepulsion();
                AdjustmentEquipIconPosition();
                SetTextBlockAsync(tbStatusMessage, "使用力导向算法对拓扑进行迭代，第" + ++i + "次");
                Thread.Sleep(10);
            }
        }
        /// <summary>
        /// 计算所有设备的度数/维度，每个设备连接几个对象，度数就是几。
        /// </summary>
        private void CalculateDegree()
        {
            for (int i = 0; i < topoData.Number; i++)
            {
                Equipment equip = topoData.GetV(i);
                equip.Degree = 0;
                for (int j = 0; j < topoData.Number; j++)
                {
                    if (topoData.GetE(i, j) == null)
                        continue;
                    equip.Degree++;
                }
            }
        }

        /// <summary>
        /// 计算所有线造成的张力
        /// </summary>
        private void CalculateTension()
        {
            for (int i = 0; i < topoData.Number; i++)
            {
                for (int j = i + 1; j < topoData.Number; j++)
                {
                    if (topoData.GetE(i, j) == null)
                        continue;
                    CalculateTensionForLine(topoData.GetE(i, j));
                }
            }
        }
        /// <summary>
        /// 计算指定线造成的张力
        /// </summary>
        /// <param name="linfo"></param>
        private void CalculateTensionForLine(LineInfo linfo)
        {
            double lineLength = 0;
            this.Dispatcher.Invoke(new Action(() =>
            {
                lineLength = linfo.LineLength();
            }));
            if (lineLength <= tensionThresholdLength)
                return;
            Equipment equipA = linfo.EquipA;
            Equipment equipB = linfo.EquipB;
            Vector pBA = equipB.EquipVector - equipA.EquipVector;
            Vector pAB = equipA.EquipVector - equipB.EquipVector;
            pBA.Normalize();
            pAB.Normalize();
            Vector vBA = (lineLength - tensionThresholdLength) * pBA;
            equipA.Force += vBA;
            vBA.Negate();
            equipB.Force += vBA;
        }

        /// <summary>
        /// 计算任意两个设备点之间的斥力
        /// </summary>
        private void CalculateRepulsion()
        {
            for (int i = 0; i < topoData.Number; i++)
                for (int j = i + 1; j < topoData.Number; j++)
                    CalculateRepulsionBetweenEquips(i, j);
        }

        /// <summary>
        /// 计算两个设备点之间的斥力
        /// </summary>
        private void CalculateRepulsionBetweenEquips(int i, int j)
        {
            Equipment equipA = topoData.GetV(i);
            Equipment equipB = topoData.GetV(j);
            double distance = Math.Sqrt(Math.Pow(equipA.X - equipB.X, 2) + Math.Pow(equipA.Y - equipB.Y, 2));
            double absRepulsion;
            Vector pBA = equipB.EquipVector - equipA.EquipVector;
            Vector pAB = equipA.EquipVector - equipB.EquipVector;
            pBA.Normalize();
            pAB.Normalize();
            if (distance == 0)
            {
                absRepulsion = forceWhileSuperposition;
                pAB.X = 1;
                pAB.Y = 0;
                pBA.X = 0;
                pBA.Y = 1;
            }
            else
                absRepulsion = g * equipA.Degree * equipB.Degree / Math.Pow(distance, 2);
            equipA.Force += absRepulsion * pAB;
            equipB.Force += absRepulsion * pBA;
        }
        //Vector lastTotalForce, nowTotalForce;


        /// <summary>
        /// 根据每个设备点的作用力对设备点控件进行移动,每轮移动后总受力清零
        /// </summary>
        private void AdjustmentEquipIconPosition()
        {
            if (!isDrawing)
                return;
            List<double> forceList = new List<double>();
            for (int i = 0; i < topoData.Number; i++)
            {
                Equipment equip = topoData.GetV(i);

                forceList.Add(equip.Force.Length / equip.Degree);
            }
            forceList.Sort();
            double max = forceList[forceList.Count - 1];
            double min = forceList[0];
            double step = (max - min) / (maxPx - 1);
            for (int i = 0; i < topoData.Number; i++)
            {
                Equipment equip = topoData.GetV(i);
                //nowTotalForce += equip.Force;
                double forceLength = equip.Force.Length / equip.Degree;
                Vector normalizeForce = equip.Force;
                normalizeForce.Normalize();
                Vector v = equip.EquipVector + ((forceLength - min) / step + 1) * normalizeForce;
                if (v.X <= 0)
                    equip.X = 0;
                else if(v.X >= canvas.ActualWidth - 100)
                    equip.X = canvas.ActualWidth - 100;
                else
                    equip.X = v.X;
                if (v.Y <= 10)
                    equip.Y = 10;
                else if (v.Y >= canvas.ActualHeight - 80)
                    v.Y = canvas.ActualHeight - 80;
                else
                    equip.Y = v.Y;

                equip.Force = new Vector(0, 0);
            }
            //Vector vv = lastTotalForce + nowTotalForce;
            //SetTextBlockAsync(tbStatusMessage, vv.Length.ToString() + " + " + vv.ToString());
        }





        private void DrawRayTopo()
        {
            SetTextBlockAsync(tbStatusMessage, "使用射线模式绘制拓扑开始");

            SetTextBlockAsync(tbStatusMessage, "绘制射线模式拓扑完毕");
        }

        private void DrawLine(Equipment equip, Equipment lastEquip)
        {
            Line l = null;
            LineInfo lInfo = topoData.GetE(equip, lastEquip);
            //lInfo.EquipA = equip;
            //lInfo.EquipB = lastEquip;

            #region  trap来的时候可以通过以下代码获取line两端的设备端口信息
            //try
            //{
            //    if (equip.IpFirstGet.ToString() == "3.245.1.1" && lastEquip.IpFirstGet.ToString() == "3.247.1.1")
            //    {
            //    }
            //    //随便获取一个对方设备一个地址所属的子网，这样才可以从路由表中得到路由信息和其中的ifIndex
            //    IpAddress subnetA = equip.IpFirstGet.GetSubnetAddress(equip.IPAndInfoList[equip.IpFirstGet].IpMask);
            //    if (lastEquip.IpDstAndRouteInfoLIst.ContainsKey(subnetA))
            //        lInfo.IfIDB = lastEquip.IpDstAndRouteInfoLIst[subnetA].IfIndex;
            //    else
            //    {
            //        //考虑默认路由聚合路由等情况，比如到3.11.1.0/24的路由，它用3.0.0.0/8囊括了
            //        //还需要考虑一种情况，使用IpFirstGet所在子网作为目的地址获取路由，万一这个是一个互联子网或者不重要的
            //        //且路由协议不是动态的，它不把此子网加入到路由表怎么办？
            //        bool isContainSubnet = false;
            //        foreach (KeyValuePair<IpAddress, RouteInfomation> pair in lastEquip.IpDstAndRouteInfoLIst)
            //        {
            //            if (pair.Value.RouteType != 4)
            //                continue;   //既然是默认路由，肯定是indirect(4)
            //            //寻找是否有路由包含目的子网
            //            if (pair.Key.CompareTo(subnetA.GetSubnetAddress(pair.Value.DstMask)) == 0)
            //            {
            //                isContainSubnet = true;
            //                lInfo.IfIDB = pair.Value.IfIndex;
            //                break;
            //            }
            //        }
            //        if (!isContainSubnet)
            //            lInfo.IfIDB = -1; //标注为-1 代表未获取到合适的端口id，在后面的链路判断中，需要进行区分
            //    }
            //    IpAddress subnetB = lastEquip.IpFirstGet.GetSubnetAddress(lastEquip.IPAndInfoList[lastEquip.IpFirstGet].IpMask);
            //    if (equip.IpDstAndRouteInfoLIst.ContainsKey(subnetB))
            //        lInfo.IfIDA = equip.IpDstAndRouteInfoLIst[subnetB].IfIndex;
            //    else
            //    {
            //        bool isContainSubnet = false;
            //        foreach (KeyValuePair<IpAddress, RouteInfomation> pair in equip.IpDstAndRouteInfoLIst)
            //        {
            //            if (pair.Value.RouteType != 4)
            //                continue;   //既然是默认路由，肯定是indirect(4)
            //            //寻找是否有路由包含目的子网
            //            if (pair.Key.CompareTo(subnetB.GetSubnetAddress(pair.Value.DstMask)) == 0)
            //            {
            //                isContainSubnet = true;
            //                lInfo.IfIDA = pair.Value.IfIndex;
            //                break;
            //            }
            //        }
            //        if (!isContainSubnet)
            //            lInfo.IfIDA = -1;
            //    }
            //    //将接口ID和连接线的信息加入到两边设备
            //    equip.IfIDandLineInfoList.Add(lInfo.IfIDA, lInfo);
            //    lastEquip.IfIDandLineInfoList.Add(lInfo.IfIDB, lInfo);
            //}
            //catch (System.Exception ex)
            //{
            //    //lInfo = null;
            //    AddMessage("为连接线设置附加信息出现问题 " + ex.Message);
            //}

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
                lInfo.L = l;
                l.MouseLeftButtonDown += new MouseButtonEventHandler(l_MouseLeftButtonDown);
            }));
            if (lInfo == null)
            {
                //出现null引用错误，调试用
            }
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
        /// 生成设备类信息，获取此设备的ip地址信息列表、接口信息列表、路由信息列表
        /// </summary>
        private Equipment GetEquipment(string strIP, string sysDescr)
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
                //SetEquipPosition(equip,lastEquip);
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
                if (ipAlreadyFindList.ContainsKey(pair.Key))
                {
                    AddMessage(string.Format("！！！已发现IP地址列表中存在相同的IP地址{0},请确保网内无重复地址！\n已存在设备首要地址为{1},新添加地址所属设备首要地址为{2}", pair.Key.ToString(), ipAlreadyFindList[pair.Key].Equip.AdminIPAddress.ToString(),equip.AdminIPAddress.ToString()));
                }
                else
                    ipAlreadyFindList.Add(pair.Key, pair.Value);
            }
            if (!equip.GetIFIDandIFInfoList(out tipMessage))
            {
                AddMessage(tipMessage);
                AddMessage(string.Format("！！！获取设备(ID:{0},Name:{1})接口信息列表时出现错误", equip.Index, equip.Name));
                return null;
            }
            AddMessage(string.Format("获取设备(ID:{0},Name:{1})接口信息列表", equip.Index, equip.Name));
            if (!equip.GetIPAndRouteInfoList(out tipMessage))
            {
                AddMessage(tipMessage);
                AddMessage(string.Format("！！！获取设备(ID:{0},Name:{1})路由信息列表时出现错误", equip.Index, equip.Name));
                return null;
            }
            AddMessage(string.Format("获取设备(ID:{0},Name:{1})路由信息列表", equip.Index, equip.Name));
            if (equip.Index < 0)
                GetEquipName(equip);
            return equip;
        }

        /// <summary>
        /// 根据接口表找出loopback的接口id，根据此id找出对应的IP地址，同时设置adminIP也为这个地址
        /// </summary>
        /// <param name="equip"></param>
        public void GetEquipName(Equipment equip)
        {
            foreach (IFInfomation ifInfo in equip.IfIDandIFinfoLIst.Values)
                if (ifInfo.IfDescr.ToLower().Contains("loopback"))
                    foreach (IPInformation ipInfo in equip.IPAndInfoList.Values)
                        if (ipInfo.IfIndex == ifInfo.IfIndex && !ipInfo.IP.ToString().Equals("127.0.0.1"))
                         {
                             equip.Name = ipInfo.IP.ToString();
                             equip.AdminIPAddress = ipInfo.IP;
                             ipInfo.IsDefaultIP = true;
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
                //canvas.Children.Add(ucEquipIcon); 不同的布局方式有不同的坐标和添加方法
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
                isDrawing = false;
                IsDetecting = true;
                InitParameters();
                ThreadPool.QueueUserWorkItem(StartDetecTopo);
            }
        }

        private void btnReDraw_Click(object sender, RoutedEventArgs e)
        {
            isDrawing = false;
            loadingGrid.Visibility = Visibility.Visible;
        }

        Storyboard storyOpacity;
        Storyboard storyMove;
        Ellipse el;
        
        private void btnTracert_Click(object sender, RoutedEventArgs e)
        {
            if (isDetecting || isDrawing)
                return;
            if (el != null)
            {
                storyOpacity.Stop();
                storyOpacity = null;
                storyMove.Stop();
                storyMove = null;
                canvas.Children.Remove(el);
                el = null;
                return;
            }

            Tracert t = new Tracert();
            //t.ShowDialog();
                
            storyOpacity = new Storyboard();

            double x1 = ipAlreadyFindList[new IpAddress("10.1.1.1")].Equip.UCEquipIcon.CenterPointX - 10;
            double y1 = ipAlreadyFindList[new IpAddress("10.1.1.1")].Equip.UCEquipIcon.CenterPointY - 10;
            double x2 = ipAlreadyFindList[new IpAddress("10.2.1.1")].Equip.UCEquipIcon.CenterPointX - 10;
            double y2 = ipAlreadyFindList[new IpAddress("10.2.1.1")].Equip.UCEquipIcon.CenterPointY - 10;
            double x3 = ipAlreadyFindList[new IpAddress("10.3.1.1")].Equip.UCEquipIcon.CenterPointX - 10;
            double y3 = ipAlreadyFindList[new IpAddress("10.3.1.1")].Equip.UCEquipIcon.CenterPointY - 10;
            double x4 = ipAlreadyFindList[new IpAddress("10.4.1.1")].Equip.UCEquipIcon.CenterPointX - 10;
            double y4 = ipAlreadyFindList[new IpAddress("10.4.1.1")].Equip.UCEquipIcon.CenterPointY - 10;




            el = new Ellipse();
            el.Width = 20;
            el.Height = 20;           
            RadialGradientBrush myRadialGradientBrush = new RadialGradientBrush();
            myRadialGradientBrush.GradientOrigin = new Point(0.7, 0.3);
            myRadialGradientBrush.Center = new Point(0.5, 0.5);
            myRadialGradientBrush.RadiusX = 1;
            myRadialGradientBrush.RadiusY = 1;
            myRadialGradientBrush.GradientStops.Add(
                new GradientStop(Colors.White, 0.0));
            myRadialGradientBrush.GradientStops.Add(
                new GradientStop(Colors.Blue, 0.5));
            el.Fill = myRadialGradientBrush;
            el.SetValue(Canvas.LeftProperty, x1);
            el.SetValue(Canvas.TopProperty, y1);
            el.SetValue(Canvas.ZIndexProperty, 200);
            canvas.Children.Add(el);
            
            DoubleAnimation opacityDA = new DoubleAnimation(0.1,new Duration(new TimeSpan(0, 0, 1)));
            Storyboard.SetTarget(opacityDA, el);
            Storyboard.SetTargetProperty(opacityDA,new PropertyPath("Opacity"));
            storyOpacity.RepeatBehavior = RepeatBehavior.Forever;
            opacityDA.AutoReverse = true;
            storyOpacity.Children.Add(opacityDA);
            storyOpacity.Begin();

            storyMove = new Storyboard();

            DoubleAnimationUsingKeyFrames moveDAx = new DoubleAnimationUsingKeyFrames();
            DoubleAnimationUsingKeyFrames moveDAy = new DoubleAnimationUsingKeyFrames();
            Storyboard.SetTarget(moveDAx, el);
            Storyboard.SetTarget(moveDAy, el);
            Storyboard.SetTargetProperty(moveDAx, new PropertyPath("(Canvas.Left)"));
            Storyboard.SetTargetProperty(moveDAy, new PropertyPath("(Canvas.Top)"));
            LinearDoubleKeyFrame keyX1 = new LinearDoubleKeyFrame();
            LinearDoubleKeyFrame keyX2 = new LinearDoubleKeyFrame();
            LinearDoubleKeyFrame keyX3 = new LinearDoubleKeyFrame();
            LinearDoubleKeyFrame keyX4 = new LinearDoubleKeyFrame();
            LinearDoubleKeyFrame keyY1 = new LinearDoubleKeyFrame();
            LinearDoubleKeyFrame keyY2 = new LinearDoubleKeyFrame();
            LinearDoubleKeyFrame keyY3 = new LinearDoubleKeyFrame();
            LinearDoubleKeyFrame keyY4 = new LinearDoubleKeyFrame();
            keyX1.KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(2));
            keyX1.Value = x1;
            keyX2.KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(4));
            keyX2.Value = x2;
            keyX3.KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(6));
            keyX3.Value = x3;
            keyX4.KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(8));
            keyX4.Value = x4;
            keyY1.KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(2));
            keyY1.Value = y1;
            keyY2.KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(4));
            keyY2.Value = y2;
            keyY3.KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(6));
            keyY3.Value = y3;
            keyY4.KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(8));
            keyY4.Value = y4;
            
            moveDAx.KeyFrames.Add(keyX1);
            moveDAx.KeyFrames.Add(keyX2);
            moveDAx.KeyFrames.Add(keyX3);
            moveDAx.KeyFrames.Add(keyX4);
            moveDAy.KeyFrames.Add(keyY1);
            moveDAy.KeyFrames.Add(keyY2);
            moveDAy.KeyFrames.Add(keyY3);
            moveDAy.KeyFrames.Add(keyY4);

            storyMove.Children.Add(moveDAx);
            storyMove.Children.Add(moveDAy);
            storyMove.RepeatBehavior = RepeatBehavior.Forever;
            storyMove.Begin();

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
//                 foreach (Equipment equip in equipAlreadyFindList.Values)
//                 {
//                      if (!equip.SaveInformation())
//                      {
//                          isSaveAllOK = false;
//                          sb.Append(equip.Name + ",");
//                      }
//                 }
                if (isSaveAllOK)
                    MessageBox.Show("全部设备保存成功");
                else
                    MessageBox.Show("设备 " + sb.ToString().TrimEnd(',') + " 保存失败");
            }
        }

        private void btnTestDrawMatrix_Click(object sender, RoutedEventArgs e)
        {
            DrawMatrix draw = new DrawMatrix(topoData);
            draw.ShowDialog();
        }


    }




}
