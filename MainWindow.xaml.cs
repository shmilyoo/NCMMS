using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using NCMMS.CommonClass;
using NCMMS.Topology;
using NCMMS.Config;
using NCMMS.Help;
using NCMMS.PeerMap;
using NCMMS.PortMonitor;
using System.Windows.Media.Animation;
using System.Threading;

namespace NCMMS
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : MyWindow
    {
        List<Button> buttons = new List<Button>();
        int btnClickedOldIndex = -1;
        UserControl OldUCInCanvas;
        UserControl ucInCanvas = null;

        //Dictionary<String, UserControl> btnToUserControl = new Dictionary<String, UserControl>();

        public MainWindow()
        {
            InitializeComponent();
            
            ucInCanvas = new StartPage();
            //ucInCanvas.Margin = new Thickness(10);
            canvas.Children.Add(ucInCanvas);
            OldUCInCanvas = ucInCanvas;

            this.Loaded += new RoutedEventHandler(MainWindow_Loaded);       
            
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            double i = canvas.ActualHeight;
            double ii = canvas.ActualWidth;
            SetButtonsAtBottom();
            //canvas.SizeChanged += new SizeChangedEventHandler(canvas_SizeChanged);
        }

        /// <summary>
        /// 提取底部的按钮，添加按钮名和倒影，将倒影的变换绑定到按钮的变换上
        /// </summary>
        private void SetButtonsAtBottom()
        {
            foreach (var item in bottomGrid.Children)
            {
                if (item is Button)
                    buttons.Add(item as Button);
            }
            foreach (Button btn in buttons)
            {
                double width = btn.ActualWidth;
                double height = btn.ActualHeight;
                Rectangle rect = new Rectangle();
                rect.Height = height;
                rect.Width = width;
                rect.Opacity = 0.7d;
                rect.Fill = new VisualBrush(btn as Visual);
                ScaleTransform scale = new ScaleTransform();
                Binding bindX = new Binding();
                bindX.Source = btn;
                bindX.Path = new PropertyPath("RenderTransform.(ScaleTransform.ScaleX)");
                BindingOperations.SetBinding(scale, ScaleTransform.ScaleXProperty, bindX);
                
                Binding bindY = new Binding();
                bindY.Source = btn;
                bindY.Path = new PropertyPath("RenderTransform.(ScaleTransform.ScaleY)");
                bindY.Converter = new DoubleToDoubleForMirrorConverter();
                BindingOperations.SetBinding(scale, ScaleTransform.ScaleYProperty, bindY);
                rect.LayoutTransform = scale;
                
                LinearGradientBrush opacityBrush = new LinearGradientBrush();
                opacityBrush.EndPoint = new Point(0, 1);
                opacityBrush.GradientStops.Add(new GradientStop(Colors.Transparent, 0));
                opacityBrush.GradientStops.Add(new GradientStop(Colors.Black, 2));
                rect.OpacityMask = opacityBrush;
                bottomGrid.Children.Add(rect);
                rect.VerticalAlignment = VerticalAlignment.Top;
                int col = Grid.GetColumn(btn as Button);
                Grid.SetRow(rect, 1);
                Grid.SetColumn(rect, col);
                rect.MouseDown += new MouseButtonEventHandler(rect_MouseDown);

                TextBlock tbBtnName = new TextBlock();
                tbBtnName.Text = btn.Content.ToString();
                bottomGrid.Children.Add(tbBtnName);
                Grid.SetRow(tbBtnName, 1);
                Grid.SetColumn(tbBtnName, col);
                tbBtnName.Margin = new Thickness(0, 5, 0, 0);
                tbBtnName.HorizontalAlignment = HorizontalAlignment.Center;
                tbBtnName.Foreground = Brushes.White;
                tbBtnName.FontFamily = new FontFamily("宋体");
            }
        }

        void rect_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Rectangle rect = sender as Rectangle;
            double i = rect.ActualHeight;
            double ii = rect.ActualWidth;
            double iii = rect.Height;
            double iiii = rect.Width;
        }

        void BottomButton_OnClick(object sender, RoutedEventArgs e)
        {
            OldUCInCanvas = ucInCanvas;
            Button btn = sender as Button;
            int btnIndex = Grid.GetColumn(btn);
            if (btnIndex == btnClickedOldIndex)
                return;
            string btnContent = btn.Content.ToString();

            if (btnContent.Equals(this.FindResource("topo").ToString()))
            {
                ucInCanvas = new TopoStart();
            }
            else if (btnContent.Equals(this.FindResource("trap").ToString()))
            {

            }
            else if (btnContent.Equals(this.FindResource("portMonitor").ToString()))
            {
                ucInCanvas = new PortMonitorStart();
            }
            else if (btnContent.Equals(this.FindResource("fiberPowerMonitor").ToString()))
            {

            }
            else if (btnContent.Equals(this.FindResource("peerMap").ToString()))
            {
                ucInCanvas = new PeerMapStart();
            }
            else if (btnContent.Equals(this.FindResource("multiPing").ToString()))
            {
//                 Process p = new Process();
//                 p.StartInfo.UseShellExecute = false;
//                 // You can start any process, HelloWorld is a do-nothing example.
//                 p.StartInfo.FileName = "MultiPing/MultiPing.exe";
//                 p.Start();
               // ucInCanvas = new MultiPingStart();
                Thread threadMultiPingPage = new Thread(delegate()
                {
                    MultiPing.MultiPing MultiPingShow = new MultiPing.MultiPing();
                    MultiPingShow.ShowDialog();
                });
                threadMultiPingPage.SetApartmentState(ApartmentState.STA);
                threadMultiPingPage.IsBackground = true;
                threadMultiPingPage.Start();
            
            }
            else if (btnContent.Equals(this.FindResource("multiBroadcast").ToString()))
            {

            }
            else if (btnContent.Equals(this.FindResource("config").ToString()))
            {
                ucInCanvas = new ConfigStart();
                (ucInCanvas as ConfigStart).RefreshEvent += new ConfigStart.RefreshEventHandler(ConfigStart_RefreshEvent);
            }
            else if (btnContent.Equals(this.FindResource("help").ToString()))
            {
                ucInCanvas = new HelpStart();
            }
            if (ucInCanvas != null && ucInCanvas != OldUCInCanvas)
            {
                ucInCanvas.Height = canvas.ActualHeight;
                ucInCanvas.Width = canvas.ActualWidth;
                Storyboard sbBtnClick = new Storyboard();
                sbBtnClick.Completed += new EventHandler(sbBtnClick_Completed);
                
                DoubleAnimation daXOld = new DoubleAnimation();
                DoubleAnimation daXNew = new DoubleAnimation();
                DoubleAnimation daOpacityOld = new DoubleAnimation();
                daXOld.Duration = daXNew.Duration = daOpacityOld.Duration = new Duration(TimeSpan.FromSeconds(0.5));
                daOpacityOld.From = 1d;
                daOpacityOld.To = 0d;
                Storyboard.SetTarget(daOpacityOld, OldUCInCanvas);
                Storyboard.SetTargetProperty(daOpacityOld, new PropertyPath("Opacity"));
                sbBtnClick.Children.Add(daOpacityOld);

                if (btnClickedOldIndex < btnIndex)
                {
                    //老的向左滑动，新的从右边进来
                    daXOld.From = 0d;
                    daXOld.To = -1100d;
                    daXNew.From = 1080d;
                    daXNew.To = 0d;
                }
                else
                {
                    //老的向右滑动，新的从左边进来
                    daXOld.From = 0d;
                    daXOld.To = 1100d;
                    daXNew.From = -1080d;
                    daXNew.To = 0d; 
                }
                //daXOld.DecelerationRatio = 0.5d;
                Storyboard.SetTarget(daXOld, OldUCInCanvas);
                Storyboard.SetTargetProperty(daXOld, new PropertyPath("(Canvas.Left)"));
                sbBtnClick.Children.Add(daXOld);
                //daXNew.BeginTime = TimeSpan.FromSeconds(25d); 当设置这个之后，老的原位置有拖影
//                 BounceEase be = new BounceEase();
//                 be.EasingMode = EasingMode.EaseOut;
//                 be.Bounciness = 5;
//                 be.Bounces = 10;
//                 daXNew.EasingFunction = be;

//                 ElasticEase ee = new ElasticEase();
//                 ee.Oscillations = 2;
//                 ee.Springiness = 3;
//                 ee.EasingMode = EasingMode.EaseOut;
//                 daXNew.EasingFunction = ee;

                Storyboard.SetTarget(daXNew, ucInCanvas);
                Storyboard.SetTargetProperty(daXNew, new PropertyPath("(Canvas.Left)"));
                sbBtnClick.Children.Add(daXNew);
                canvas.Children.Add(ucInCanvas);
                btnClickedOldIndex = btnIndex;
                sbBtnClick.Begin();
            }

        }

        void sbBtnClick_Completed(object sender, EventArgs e)
        {
            canvas.Children.Remove(OldUCInCanvas);
        }

        private void canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
             ucInCanvas.Height = canvas.ActualHeight;
             ucInCanvas.Width = canvas.ActualWidth;
        }

        private void ConfigStart_RefreshEvent(object sender, EventArgs e)
        {
            canvas.Children.Remove(ucInCanvas);
            ucInCanvas = new ConfigStart(); 
            (ucInCanvas as ConfigStart).RefreshEvent += new ConfigStart.RefreshEventHandler(ConfigStart_RefreshEvent);
            canvas.Children.Add(ucInCanvas);
            (ucInCanvas as ConfigStart).SetTabIDtoShow(2);
        }
    }
}
