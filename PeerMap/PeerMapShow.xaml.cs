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
using SharpPcap;
using PacketDotNet;
using System.Net;
using System.Threading;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using System.Collections;

namespace NCMMS.PeerMap
{
    /// <summary>
    /// PeerMapShow.xaml 的交互逻辑
    /// </summary>
    public partial class PeerMapShow : MyWindow
    {
        public bool BackgroundThreadStop = false;
        private object QueueLock = new object();
        private object tempEllipseLineLock = new object();
        //private object DrawLineLock = new object();
        private List<RawCapture> PacketQueue = new List<RawCapture>();
        //private DateTime LastStatisticsOutput = DateTime.Now;
        //private TimeSpan LastStatisticsInterval = new TimeSpan(0, 0, 2);
        public ICaptureDevice dev;
        public int pkSize, totalPkNum, totalPkSize;
        IPAddress srcIP, dstIP;
        IPProtocolType ipProtocol;
        Packet pk;
        IpPacket ipPacket;
        public Thread backgroundThread;
        Dictionary<IPAddress, Ellipse> ipAndEllipses = new Dictionary<IPAddress, Ellipse>();
        public Dictionary<IPAddress, PeerMapPeerInfo> ipAndPeerInfos = new Dictionary<IPAddress, PeerMapPeerInfo>();
        List<String> drawedLine = new List<String>();
        List<LineSrcDst> drawLine = new List<LineSrcDst>();
        List<LineSrcDst> listIP = new List<LineSrcDst>();
        List<Rectangle> animationRects = new List<Rectangle>();
        bool hasSrcIP, hasDstIP, putPointAtLeft;
        double canvasWidth, canvasHeight;
        Random r;
        PeerMapPeerInfo peerInfo;
        DispatcherTimer timer;
        //鼠标拖动点相关参数
        Point pBefore = new Point();//鼠标点击前坐标
        Point eBefore = new Point();//圆移动前坐标
        bool isMove = false;//是否需要移动
        WindowShowEllipseInfo windowShowEllipseInfo;
        WindowShowLineInfo windowShowLineInfo;
        Storyboard storyBoard = new Storyboard();
        List<int> leftPositionList = new List<int>();
        List<int> rightPositionList = new List<int>();
        int PointNumbersAtEachSide = 50;  //每边放置多少个点
        double step; //点距
        IPAddress[] localIPs;

        public class LineSrcDst
        {
            public IPAddress _srcip;
            public IPAddress _dstip;
            public DoubleAnimation daSrcDstX;
            public DoubleAnimation daSrcDstY;
            public DoubleAnimation daDsSrctX;
            public DoubleAnimation daDsSrctY;
            public Rectangle raSrcDst;
            public Rectangle raDstSrc;
            public int oldAllPKNumInLineSrcToDst;
            public int oldAllPKNumInLineDstToSrc;
        }

        public PeerMapShow(ICaptureDevice d)
        {
            InitializeComponent();
            SetWindowSizeAndPosition();
            r = new Random();
            dev = d;
            Storyboard.SetDesiredFrameRate(storyBoard, 30);
            //storyBoard.SetValue(Timeline.DesiredFrameRateProperty, 30);
            localIPs = Dns.GetHostAddresses(Dns.GetHostName());
        }
        private void SetWindowSizeAndPosition()
        {
            int w = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Width;
            int h = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Height;
            this.Left = w / 2 - Width / 2;
            //this.Top = 50d;
            this.Height = h - 100;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            backgroundThread = new Thread(BackgroundThread);
            backgroundThread.Start();
            dev.OnPacketArrival += new PacketArrivalEventHandler(dev_OnPacketArrival);
            dev.Open(DeviceMode.Promiscuous, 100);
            dev.Filter = "ip";
            dev.StartCapture();
            timer = new DispatcherTimer();
            timer.Tick += new EventHandler(timer_Tick);
            timer.Interval = new TimeSpan(0, 0, 2);
            timer.Start();

            canvasHeight = canvas1.ActualHeight;
            canvasWidth = canvas1.ActualWidth;
            step = (canvasHeight - 60) / PointNumbersAtEachSide;
            for (int i = 0; i < PointNumbersAtEachSide; i++)
            {
                leftPositionList.Add(i);
                rightPositionList.Add(i);
            }
        }

        private void BackgroundThread()
        {
            while (!BackgroundThreadStop)
            {
                bool shouldSleep = true;

                lock (QueueLock)
                {
                    if (PacketQueue.Count != 0)
                        shouldSleep = false;
                }                                                                         
                if (shouldSleep)
                {
                    System.Threading.Thread.Sleep(250);
                }
                else // should process the queue
                {
                    List<RawCapture> ourQueue;
                    lock (QueueLock)
                    {
                        // swap queues, giving the capture callback a new one
                        ourQueue = PacketQueue;
                        PacketQueue = new List<RawCapture>();
                    }

                    foreach (var packet in ourQueue)
                    {
                        pkSize = packet.Data.Length + 4;//数据帧的大小
                        pk = Packet.ParsePacket(packet.LinkLayerType, packet.Data);
                        totalPkSize += pkSize;//所有捕获到的数据包大小
                        totalPkNum++; //所有捕获到的数据包数目

                        ipPacket = pk.Extract(typeof(IpPacket)) as IpPacket;
                        ipProtocol = ipPacket.Protocol;
                        srcIP = ipPacket.SourceAddress;
                        dstIP = ipPacket.DestinationAddress;
                        hasSrcIP = ipAndPeerInfos.ContainsKey(srcIP);
                        hasDstIP = ipAndPeerInfos.ContainsKey(dstIP);

                        if (hasSrcIP && hasDstIP)     // 窗口已经有这两个点了
                        {
                            #region Linq
                            //                 var peerInfo = from PeerMapPeerInfo info in peerMapPeerInfos
                            //                                where info.localIP == srcIP
                            //                                select info;
                            //                 int ii = peerInfo.Count();
                            #endregion
                            //通过把已经划过的线的源目的地址字符串加入drawedLine,来代表已经划过此线。
                            // 当 A 连接 到B，B 连接到C ，一旦出现A 连接到C，就要进行此判断，否则不会再画线。
                            if (!drawedLine.Contains(srcIP.ToString() + dstIP.ToString()))
                            {
                                lock (tempEllipseLineLock)
                                {
                                    drawLine.Add(new LineSrcDst() { _srcip = srcIP, _dstip = dstIP });
                                }
                            }
                        }
                        else if (!hasSrcIP && !hasDstIP)          // 窗口没有这两个点，需要添加，同时要在pointIPs中添加
                        {
                            peerInfo = new PeerMapPeerInfo(srcIP);
                            ipAndPeerInfos.Add(srcIP, peerInfo);
                            peerInfo = new PeerMapPeerInfo(dstIP);
                            ipAndPeerInfos.Add(dstIP, peerInfo);
                            drawedLine.Add(srcIP.ToString() + dstIP.ToString());
                            drawedLine.Add(dstIP.ToString() + srcIP.ToString());
                            lock (tempEllipseLineLock)
                            {
                                listIP.Add(new LineSrcDst() { _srcip = srcIP, _dstip = dstIP });
                                listIP.Add(new LineSrcDst() { _srcip = dstIP, _dstip = srcIP });
                                drawLine.Add(new LineSrcDst() { _srcip = srcIP, _dstip = dstIP });
                            }
                        }
                        else if (hasSrcIP && !hasDstIP)    // 有srcip 没有 dstip
                        {
                            peerInfo = new PeerMapPeerInfo(dstIP);
                            ipAndPeerInfos.Add(dstIP, peerInfo);
                            drawedLine.Add(srcIP.ToString() + dstIP.ToString());
                            drawedLine.Add(dstIP.ToString() + srcIP.ToString());
                            lock (tempEllipseLineLock)
                            {
                                listIP.Add(new LineSrcDst() { _srcip = dstIP, _dstip = srcIP });
                                drawLine.Add(new LineSrcDst() { _srcip = srcIP, _dstip = dstIP });
                            }
                        }
                        else                               // 有dstip没有 srcip 
                        {
                            peerInfo = new PeerMapPeerInfo(srcIP);
                            ipAndPeerInfos.Add(srcIP, peerInfo);
                            drawedLine.Add(srcIP.ToString() + dstIP.ToString());
                            drawedLine.Add(dstIP.ToString() + srcIP.ToString());
                            lock (tempEllipseLineLock)
                            {
                                listIP.Add(new LineSrcDst() { _srcip = srcIP, _dstip = dstIP });
                                drawLine.Add(new LineSrcDst() { _srcip = srcIP, _dstip = dstIP });
                            }
                        }
                        ipAndPeerInfos[srcIP].PkFromHere(dstIP, ipProtocol, pkSize);
                        ipAndPeerInfos[dstIP].PkFromHere(srcIP, ipProtocol, 0);
                        ipAndPeerInfos[dstIP].allrcvPkNum++;
                        ipAndPeerInfos[dstIP].allrcvPkSize += pkSize;
                    }
                }
            }
        }

        //计算圆点的半径，本节点发送的数据包数决定
        public double PointSize(int allSendPkNum)
        {
            int i = allSendPkNum;
            double pointSize;
            if (i >= 360)
            {
                pointSize = 16;
            }
            else if (i >= 280)
            {
                pointSize = 15;
            }
            else if (i >= 210)
            {
                pointSize = 14;
            }
            else if (i >= 150)
            {
                pointSize = 13;
            }
            else if (i >= 100)
            {
                pointSize = 12;
            }
            else if (i >= 60)
            {
                pointSize = 11;
            }
            else if (i >= 30)
            {
                pointSize = 10;
            }
            else if (i >= 10)
            {
                pointSize = 9;
            }
            else
            {
                pointSize = 8;
            }
            return pointSize;
        }

        //计算线的宽度，由线上双向数据包和决定
        public double LineThickness(int allPkNum)
        {
            int i = allPkNum;
            double thickness;
            if (i >= 200)
            {
                thickness = 5;
            }
            else if (i >= 90)
            {
                thickness = 4;
            }
            else if (i >= 30)
            {
                thickness = 3;
            }
            else if (i >= 10)
            {
                thickness = 2;
            }
            else
            {
                thickness = 1;
            }
            return thickness;
        }

        double pointSize;
        EllipseInfo tempEllipseInfo;
        Line tempLine;
        LineSrcDst tempLineSrcDst;
        EllipseInfo dstEllipseInfo, srcEllipseInfo;

        void timer_Tick(object sender, EventArgs e)
        {
            #region 每次计时器跳时把队列中的点和线画到屏幕上
            List<LineSrcDst> tempListIP;
            List<LineSrcDst> tempDrawLine;
            lock (tempEllipseLineLock)
            {
                tempListIP = listIP;
                listIP = new List<LineSrcDst>();
                tempDrawLine = drawLine;
                drawLine = new List<LineSrcDst>();
            }
            foreach (var ip in tempListIP)
                AddEllipseToPage(ip._srcip, ip._dstip);

            foreach (var lineIP in tempDrawLine)
                AddLineToPage(lineIP);
            #endregion

            storyBoard.Children.Clear();

            //每次跳时根据控件的类型来更新点线的参数，大小，宽度等
            foreach (var element in canvas1.Children)
            {
                if (element is Ellipse)
                {
                    tempEllipseInfo = (EllipseInfo)((Ellipse)element).Tag;
                    pointSize = PointSize(ipAndPeerInfos[tempEllipseInfo.ip].allSendPkNum);
                    ipAndEllipses[tempEllipseInfo.ip].Height = pointSize;
                    ipAndEllipses[tempEllipseInfo.ip].Width = pointSize;
                    tempEllipseInfo.TextBlockLeft = tempEllipseInfo.left;
                    tempEllipseInfo.TextBlockTop = tempEllipseInfo.top;

                    //下面两行随便赋值，只是为了激活line端点坐标的重新绑定
//                     tempEllipseInfo.CenterPointX = 100;
//                     tempEllipseInfo.CenterPointY = 100;
                    tempEllipseInfo.RaiseNotifyPropertyChanged("CenterPointX");
                    tempEllipseInfo.RaiseNotifyPropertyChanged("CenterPointY");
                }

                if (element is Line)
                {
                    int allPKNumInLineSrcToDst = 0;
                    int allPKNumInLineDstToSrc = 0;
                    tempLine = (Line)element;
                    tempLineSrcDst = (LineSrcDst)tempLine.Tag;
                    if (ipAndPeerInfos[tempLineSrcDst._srcip].peerToPeers.ContainsKey(tempLineSrcDst._dstip))
                        allPKNumInLineSrcToDst = ipAndPeerInfos[tempLineSrcDst._srcip].peerToPeers[tempLineSrcDst._dstip].totalSendPkNum;
                    if (ipAndPeerInfos[tempLineSrcDst._dstip].peerToPeers.ContainsKey(tempLineSrcDst._srcip))
                        allPKNumInLineDstToSrc = ipAndPeerInfos[tempLineSrcDst._dstip].peerToPeers[tempLineSrcDst._srcip].totalSendPkNum;
                    tempLine.StrokeThickness = LineThickness(allPKNumInLineSrcToDst + allPKNumInLineDstToSrc);

                    if ((bool)cbShowAnimation.IsChecked)
                    {
                        srcEllipseInfo = (EllipseInfo)ipAndEllipses[tempLineSrcDst._srcip].Tag;
                        dstEllipseInfo = (EllipseInfo)ipAndEllipses[tempLineSrcDst._dstip].Tag;
                        if (allPKNumInLineSrcToDst > tempLineSrcDst.oldAllPKNumInLineSrcToDst)
                        {
                            tempLineSrcDst.raSrcDst.Visibility = Visibility.Visible;
                            tempLineSrcDst.daSrcDstX.From = srcEllipseInfo.CenterPointX - 3;
                            tempLineSrcDst.daSrcDstY.From = srcEllipseInfo.CenterPointY - 3;
                            tempLineSrcDst.daSrcDstX.To = dstEllipseInfo.CenterPointX - 3;
                            tempLineSrcDst.daSrcDstY.To = dstEllipseInfo.CenterPointY - 3;
                            //Storyboard.SetTarget(tempLineSrcDst.daSrcDstX, tempLineSrcDst.raSrcDst);
                            //Storyboard.SetTarget(tempLineSrcDst.daSrcDstY, tempLineSrcDst.raSrcDst);
                            //Storyboard.SetTargetProperty(tempLineSrcDst.daSrcDstX, new PropertyPath("(Canvas.Left)"));
                            //Storyboard.SetTargetProperty(tempLineSrcDst.daSrcDstY, new PropertyPath("(Canvas.Top)"));
                            storyBoard.Children.Add(tempLineSrcDst.daSrcDstX);
                            storyBoard.Children.Add(tempLineSrcDst.daSrcDstY);
                        }
                        if (allPKNumInLineDstToSrc > tempLineSrcDst.oldAllPKNumInLineDstToSrc)
                        {
                            tempLineSrcDst.raDstSrc.Visibility = Visibility.Visible;
                            tempLineSrcDst.daDsSrctX.From = dstEllipseInfo.CenterPointX - 3;
                            tempLineSrcDst.daDsSrctY.From = dstEllipseInfo.CenterPointY - 3;
                            tempLineSrcDst.daDsSrctX.To = srcEllipseInfo.CenterPointX - 3;
                            tempLineSrcDst.daDsSrctY.To = srcEllipseInfo.CenterPointY - 3;
                            //Storyboard.SetTarget(tempLineSrcDst.daDsSrctX, tempLineSrcDst.raDstSrc);
                            //Storyboard.SetTarget(tempLineSrcDst.daDsSrctY, tempLineSrcDst.raDstSrc);
                            //Storyboard.SetTargetProperty(tempLineSrcDst.daDsSrctX, new PropertyPath("(Canvas.Left)"));
                            //Storyboard.SetTargetProperty(tempLineSrcDst.daDsSrctY, new PropertyPath("(Canvas.Top)"));
                            storyBoard.Children.Add(tempLineSrcDst.daDsSrctX);
                            storyBoard.Children.Add(tempLineSrcDst.daDsSrctY);
                        }
                        
                    }
                    tempLineSrcDst.oldAllPKNumInLineSrcToDst = allPKNumInLineSrcToDst;
                    tempLineSrcDst.oldAllPKNumInLineDstToSrc = allPKNumInLineDstToSrc;
                }
            } 
            storyBoard.Begin();
        }

        private void AddEllipseToPage(IPAddress ip, IPAddress anotherIP)
        {  //参数中添加 源地址目的地址，在本点是目的地址且没有向源地址发数据时，也添加相应的peertopeer
            Point thisPosition = new Point();
            if (((IList)localIPs).Contains(ip))
            {
                thisPosition.X = canvasWidth / 2;
                thisPosition.Y = 120 + (canvasHeight - 240)*r.NextDouble();
            }
            else
                thisPosition = countPosition(anotherIP);

            Ellipse el = new Ellipse();
            el.SetValue(Canvas.LeftProperty, thisPosition.X);
            el.SetValue(Canvas.TopProperty, thisPosition.Y);
            el.Width = 10;
            el.Height = 10;
            EllipseInfo elInfo = new EllipseInfo(el, ip);
            RadialGradientBrush myRadialGradientBrush = new RadialGradientBrush();
            myRadialGradientBrush.GradientOrigin = new Point(0.7, 0.3);
            myRadialGradientBrush.Center = new Point(0.5, 0.5);
            myRadialGradientBrush.RadiusX = 1;
            myRadialGradientBrush.RadiusY = 1;
            myRadialGradientBrush.GradientStops.Add(
                new GradientStop(Colors.White, 0.0));
            myRadialGradientBrush.GradientStops.Add(
                new GradientStop(Colors.Black, 0.5));
            el.Fill = myRadialGradientBrush;
            el.MouseEnter += new MouseEventHandler(el_MouseEnter);
            el.MouseLeave += new MouseEventHandler(el_MouseLeave);


            canvas1.Children.Add(el);
            el.Tag = elInfo;
            el.SetValue(Canvas.ZIndexProperty, 300);
            el.SetValue(ToolTipService.InitialShowDelayProperty, 0);
            el.SetValue(ToolTipService.ShowDurationProperty, 100000);
            ipAndEllipses.Add(ip, el);

            TextBlock strIP = new TextBlock();
            strIP.Text = ip.ToString();
            strIP.SetValue(Canvas.ZIndexProperty, 400);
            canvas1.Children.Add(strIP);
            strIP.SetBinding(Canvas.LeftProperty, new Binding() { Path = new PropertyPath("TextBlockLeft"), Source = elInfo });
            strIP.SetBinding(Canvas.TopProperty, new Binding() { Path = new PropertyPath("TextBlockTop"), Source = elInfo });
        }

        private void AddLineToPage(LineSrcDst _lineIP)
        {
            //Vector vector = VisualTreeHelper.GetOffset(ipAndEllipses[srcIP]);
            Ellipse srcEllipse, dstEllipse;
            _lineIP.daSrcDstX = new DoubleAnimation();
            _lineIP.daSrcDstY = new DoubleAnimation();
            _lineIP.daDsSrctX = new DoubleAnimation();
            _lineIP.daDsSrctY = new DoubleAnimation();

            if (ipAndEllipses.ContainsKey(_lineIP._srcip))
            {
                srcEllipse = ipAndEllipses[_lineIP._srcip];
            }
            else
            {
                srcEllipse = null;
            }
            if (ipAndEllipses.ContainsKey(_lineIP._dstip))
            {
                dstEllipse = ipAndEllipses[_lineIP._dstip];
            }
            else
            {
                dstEllipse = null;
            }

            EllipseInfo srcEllipseInfo = (EllipseInfo)srcEllipse.Tag;
            EllipseInfo dstEllipseInfo = (EllipseInfo)dstEllipse.Tag;

            Duration duration = new Duration(new TimeSpan(0, 0, 1));

            _lineIP.daSrcDstX.Duration = duration;
            _lineIP.daSrcDstY.Duration = duration;
            _lineIP.daDsSrctX.Duration = duration;
            _lineIP.daDsSrctY.Duration = duration;

            _lineIP.raSrcDst = new Rectangle();
            _lineIP.raSrcDst.Fill = Brushes.Red;
            _lineIP.raSrcDst.Width = 6;
            _lineIP.raSrcDst.Height = 6;
            _lineIP.raSrcDst.RadiusX = 3;
            _lineIP.raSrcDst.RadiusY = 3;

            _lineIP.raDstSrc = new Rectangle();
            _lineIP.raDstSrc.Fill = Brushes.Red;
            _lineIP.raDstSrc.Width = 6;
            _lineIP.raDstSrc.Height = 6;
            _lineIP.raDstSrc.RadiusX = 3;
            _lineIP.raDstSrc.RadiusY = 3;

            //EllipseInfo tempElinfoSrc = (EllipseInfo)ipAndEllipses[_lineIP._srcip].Tag;
            //EllipseInfo tempElinfoDst = (EllipseInfo)ipAndEllipses[_lineIP._dstip].Tag;

            //_lineIP.daSrcDstX.From = tempElinfoSrc.CenterPointX - 3;
            //_lineIP.daSrcDstY.From = tempElinfoSrc.CenterPointY - 3;
            //_lineIP.daSrcDstX.To = tempElinfoDst.CenterPointX - 3;
            //_lineIP.daSrcDstY.To = tempElinfoDst.CenterPointY - 3;
            //_lineIP.daDsSrctX.From = tempElinfoDst.CenterPointX - 3;
            //_lineIP.daDsSrctY.From = tempElinfoDst.CenterPointY - 3;
            //_lineIP.daDsSrctX.To = tempElinfoSrc.CenterPointX - 3;
            //_lineIP.daDsSrctY.To = tempElinfoSrc.CenterPointY - 3;

            Storyboard.SetTarget(_lineIP.daSrcDstX, _lineIP.raSrcDst);
            Storyboard.SetTarget(_lineIP.daSrcDstY, _lineIP.raSrcDst);
            Storyboard.SetTargetProperty(_lineIP.daSrcDstX, new PropertyPath("(Canvas.Left)"));
            Storyboard.SetTargetProperty(_lineIP.daSrcDstY, new PropertyPath("(Canvas.Top)"));

            Storyboard.SetTarget(_lineIP.daDsSrctX, _lineIP.raDstSrc);
            Storyboard.SetTarget(_lineIP.daDsSrctY, _lineIP.raDstSrc);
            Storyboard.SetTargetProperty(_lineIP.daDsSrctX, new PropertyPath("(Canvas.Left)"));
            Storyboard.SetTargetProperty(_lineIP.daDsSrctY, new PropertyPath("(Canvas.Top)"));

            animationRects.Add(_lineIP.raSrcDst);
            animationRects.Add(_lineIP.raDstSrc);

            _lineIP.raSrcDst.SetValue(Canvas.ZIndexProperty, 200);
            _lineIP.raSrcDst.Visibility = Visibility.Hidden;
            _lineIP.raSrcDst.IsHitTestVisible = false;
            _lineIP.raDstSrc.SetValue(Canvas.ZIndexProperty, 200);
            _lineIP.raDstSrc.Visibility = Visibility.Hidden;
            _lineIP.raDstSrc.IsHitTestVisible = false;

            canvas1.Children.Add(_lineIP.raSrcDst);
            canvas1.Children.Add(_lineIP.raDstSrc);

            Line l = new Line();
            l.SnapsToDevicePixels = true;
            l.Stroke = new SolidColorBrush(Colors.Blue);
            l.StrokeThickness = 2;
            l.Tag = _lineIP;
            l.SetBinding(Line.X1Property, new Binding() { Path = new PropertyPath("CenterPointX"), Source = srcEllipseInfo });
            l.SetBinding(Line.Y1Property, new Binding() { Path = new PropertyPath("CenterPointY"), Source = srcEllipseInfo });
            l.SetBinding(Line.X2Property, new Binding() { Path = new PropertyPath("CenterPointX"), Source = dstEllipseInfo });
            l.SetBinding(Line.Y2Property, new Binding() { Path = new PropertyPath("CenterPointY"), Source = dstEllipseInfo });
            l.MouseEnter += new MouseEventHandler(l_MouseEnter);
            l.MouseLeave += new MouseEventHandler(l_MouseLeave);
            l.SetValue(ToolTipService.InitialShowDelayProperty, 0);
            l.SetValue(ToolTipService.ShowDurationProperty, 100000);
            canvas1.Children.Add(l);
        }

        void el_MouseEnter(object sender, MouseEventArgs e)
        {
            windowShowEllipseInfo = new WindowShowEllipseInfo((Ellipse)sender, this);
            ((Ellipse)sender).ToolTip = windowShowEllipseInfo;
            ToolTip tt = new ToolTip();
            tt.Content = windowShowEllipseInfo;
            ((Ellipse)sender).ToolTip = tt;
        }

        void el_MouseLeave(object sender, MouseEventArgs e)
        {
            if (((Ellipse)sender).ToolTip != null)
            {
                ((Ellipse)sender).ToolTip = null;
                windowShowEllipseInfo.Stop_Timing();
                windowShowEllipseInfo = null;
            }
        }

        void l_MouseEnter(object sender, MouseEventArgs e)
        {
            windowShowLineInfo = new WindowShowLineInfo((Line)sender, this);
            ((Line)sender).ToolTip = windowShowLineInfo;
            ToolTip tt = new ToolTip();
            tt.Content = windowShowLineInfo;
            ((Line)sender).ToolTip = tt;
        }

        void l_MouseLeave(object sender, MouseEventArgs e)
        {
            if (((Line)sender).ToolTip != null)
            {
                ((Line)sender).ToolTip = null;
                windowShowLineInfo.Stop_Timing();
                windowShowLineInfo = null;
            }
        }

        // 鼠标左键按下事件
        private void canvas1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource.GetType() == typeof(Ellipse))
            {
                timer.Stop();
                this.pBefore = e.GetPosition(this);//获取点击前鼠标坐标
                Ellipse el = (Ellipse)e.OriginalSource;
                this.eBefore = new Point(Canvas.GetLeft(el), Canvas.GetTop(el));//获取点击前圆的坐标
                isMove = true;//开始移动了
                el.CaptureMouse();//鼠标捕获此圆

                foreach (Rectangle rect in animationRects)
                {
                    rect.Visibility = Visibility.Hidden;
                }

            }
        }

        //鼠标左键放开事件
        private void canvas1_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource.GetType() == typeof(Ellipse))
            {
                Ellipse el = (Ellipse)e.OriginalSource;
                isMove = false;//结束移动了
                el.ReleaseMouseCapture();//鼠标释放此圆
                timer.Start();
            }
        }

        //鼠标移动事件
        private void canvas1_MouseMove(object sender, MouseEventArgs e)
        {
            if (isMove && e.OriginalSource != null && e.OriginalSource.GetType() == typeof(Ellipse))
            {
                Ellipse el = (Ellipse)e.OriginalSource;
                //el.SetValue(Canvas.ZIndexProperty, 200);
                Point p = e.GetPosition(this);//获取鼠标移动中的坐标
                double x = eBefore.X + (p.X - pBefore.X);
                double y = eBefore.Y + (p.Y - pBefore.Y);
                Canvas.SetLeft(el, x);
                Canvas.SetTop(el, y);
                ((EllipseInfo)el.Tag).TextBlockLeft = x;
                ((EllipseInfo)el.Tag).TextBlockTop = y;
                ((EllipseInfo)el.Tag).CenterPointX = x;
                ((EllipseInfo)el.Tag).CenterPointY = y;
            }
        }

        /// <summary>
        /// 当第一次添加一个点到窗口时，计算此点的坐标，left和top，和圆心有一个半径的差距。
        /// 当需要连线的对方点存在时，判断对方点在左侧还是右侧，然后选定另外一侧。
        /// 当对方点不存在时，按照一左一右顺序放置。放置位置遵循一个曲线。
        /// </summary>
        /// <param name="_anotherIP">两点钟另外一点代表的IP地址</param>
        /// <returns>放置的坐标</returns>
        private Point countPosition(IPAddress _anotherIP)
        {
            Point ellipsePosition = new Point();
            if (ipAndEllipses.ContainsKey(_anotherIP))
            {
                //Vector vector = VisualTreeHelper.GetOffset(ipAndEllipses[_anotherIP]);
                double x = (double)ipAndEllipses[_anotherIP].GetValue(Canvas.LeftProperty);
                //Point anotherIPPoint = new Point(vector.X, vector.Y);
                if (x > canvasWidth / 2)
                {
                    if (leftPositionList.Count > 0 || rightPositionList.Count == 0)
                    {
                        ellipsePosition.Y = PutElAtLeft();
                        ellipsePosition.X = CountXPosition(ellipsePosition.Y, true);
                    }
                    else
                    {
                        ellipsePosition.Y = PutElAtRight();
                        ellipsePosition.X = CountXPosition(ellipsePosition.Y, false);
                    }
                }
                else
                {
                    if (rightPositionList.Count > 0 || leftPositionList.Count == 0)
                    {
                        ellipsePosition.Y = PutElAtRight();
                        ellipsePosition.X = CountXPosition(ellipsePosition.Y, false);
                    }
                    else
                    {
                        ellipsePosition.Y = PutElAtLeft();
                        ellipsePosition.X = CountXPosition(ellipsePosition.Y, true);
                    }
                }
            }
            else
            {
                if (putPointAtLeft)
                {
                    if (rightPositionList.Count > 0)
                    {
                        putPointAtLeft = false;
                        ellipsePosition.Y = PutElAtRight();
                        ellipsePosition.X = CountXPosition(ellipsePosition.Y, false);
                    }
                    else
                    {
                        putPointAtLeft = true;
                        ellipsePosition.Y = PutElAtLeft();
                        ellipsePosition.X = CountXPosition(ellipsePosition.Y, true);
                    }
                }
                else
                {
                    if (leftPositionList.Count > 0)
                    {
                        putPointAtLeft = true;
                        ellipsePosition.Y = PutElAtLeft();
                        ellipsePosition.X = CountXPosition(ellipsePosition.Y, true);
                    }
                    else
                    {
                        putPointAtLeft = false;
                        ellipsePosition.Y = PutElAtRight();
                        ellipsePosition.X = CountXPosition(ellipsePosition.Y, false);
                    }
                }
            }
            return ellipsePosition;
        }

        private double PutElAtLeft()
        {
            putPointAtLeft = true;
            if (leftPositionList.Count==0)
                return 30 + step * r.Next(PointNumbersAtEachSide);
            int ran = r.Next(leftPositionList.Count);
            double y = 30 + step * leftPositionList[ran];
            leftPositionList.RemoveAt(ran);
            return y;
        }

        private double PutElAtRight()
        {
            putPointAtLeft = false;
            if (rightPositionList.Count == 0)
                return 30 + step * r.Next(PointNumbersAtEachSide);
            int ran = r.Next(rightPositionList.Count);
            double y = 30 + step * rightPositionList[ran];
            rightPositionList.RemoveAt(ran);
            return y;
        }

        /// <summary>
        /// 根据纵坐标，以及放在屏幕左边还是右边来计算横坐标
        /// </summary>
        /// <param name="ellipsePositionY">纵坐标</param>
        /// <param name="IsLeft">屏幕左边？</param>
        /// <returns></returns>
        private double CountXPosition(double ellipsePositionY, bool IsLeft)
        {
            if (IsLeft)
                return canvasWidth / 2 - canvasWidth * Math.Sqrt(5 * canvasHeight * canvasHeight + 16 * canvasHeight * ellipsePositionY - 16 * ellipsePositionY * ellipsePositionY) / (9 * canvasHeight);
            else
                return canvasWidth / 2 + canvasWidth * Math.Sqrt(5 * canvasHeight * canvasHeight + 16 * canvasHeight * ellipsePositionY - 16 * ellipsePositionY * ellipsePositionY) / (9 * canvasHeight);
        }

        private void dev_OnPacketArrival(object sender, CaptureEventArgs e)
        {
            if (e.Packet.LinkLayerType != PacketDotNet.LinkLayers.Ethernet)
                return;//只支持以太网

            // lock QueueLock to prevent multiple threads accessing PacketQueue at
            // the same time
            lock (QueueLock)
            {
                PacketQueue.Add(e.Packet);
            }
        }



        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            BackgroundThreadStop = true;
            if (dev.Started)
            {
                dev.StopCapture();
                dev.Close();
            }
        }



    }
}
