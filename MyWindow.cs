using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Data;
using NCMMS.CommonClass;

namespace NCMMS
{
    public class MyWindow : Window
    {
        Button max;
        Grid windowGrid;
        Border resize;
        bool isResizeWork;
        Point oldPoint, newPoint;

        public Brush BannerBackground
        {
            get { return (Brush)GetValue(BannerBackgroundProperty); }
            set { SetValue(BannerBackgroundProperty, value); }
        }
        public static readonly DependencyProperty BannerBackgroundProperty =
            DependencyProperty.Register("BannerBackground", typeof(Brush), typeof(MyWindow));

        public bool BtnMaxFlag
        {
            get { return (bool)GetValue(BtnMaxFlagProperty); }
            set { SetValue(BtnMaxFlagProperty, value); }
        }
        public static readonly DependencyProperty BtnMaxFlagProperty =
            DependencyProperty.Register("BtnMaxFlag", typeof(bool), typeof(MyWindow), new UIPropertyMetadata(true));

        public string WindowTitle
        {
            get { return (string)GetValue(WindowTitleProperty); }
            set { SetValue(WindowTitleProperty, value); }
        }
        public static readonly DependencyProperty WindowTitleProperty =
            DependencyProperty.Register("WindowTitle", typeof(string), typeof(MyWindow));
        
        public MyWindow()
        {
            this.Loaded += new RoutedEventHandler(MyWindow_Loaded);
            Binding bind = new Binding() { Path = new PropertyPath("WindowBannerColor"), Source = Properties.Settings.Default };
            bind.Converter = new ColorStringToBrushForWindowBannerBackgroundConverter();
            this.SetBinding(BannerBackgroundProperty, bind);
        }

        void MyWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Title = WindowTitle;
            Button min = this.Template.FindName("btnMin", this) as Button;
            max = this.Template.FindName("btnMax", this) as Button;
            Button close = this.Template.FindName("btnClose", this) as Button;
            Grid titleGrid = this.Template.FindName("titleGrid", this) as Grid;
            windowGrid = this.Template.FindName("windowGrid", this) as Grid;
            min.Click += new RoutedEventHandler(btnMin_Click);
            max.Click += new RoutedEventHandler(btnMax_Click);
            close.Click += new RoutedEventHandler(btnClose_Click);
            titleGrid.MouseLeftButtonDown += new MouseButtonEventHandler(titleGrid_MouseLeftButtonDown);
            if (BtnMaxFlag)
            {
                resize = this.Template.FindName("resize", this) as Border;
                resize.Visibility = Visibility.Visible;
                resize.Cursor = Cursors.SizeNWSE;
                resize.MouseDown += new MouseButtonEventHandler(resize_MouseDown);
                resize.MouseUp += new MouseButtonEventHandler(resize_MouseUp);
                resize.MouseMove += new MouseEventHandler(resize_MouseMove);
            }
        }

        void resize_MouseDown(object sender, MouseButtonEventArgs e)
        {
            resize.CaptureMouse();
            isResizeWork = true;
            oldPoint = e.GetPosition(this);
        }
        void resize_MouseUp(object sender, MouseButtonEventArgs e)
        {
            resize.ReleaseMouseCapture();
            isResizeWork = false;
        }
        void resize_MouseMove(object sender, MouseEventArgs e)
        {
            if (isResizeWork)
            {
                newPoint = e.GetPosition(this);
                this.Height += newPoint.Y - oldPoint.Y;
                this.Width += newPoint.X - oldPoint.X;
                oldPoint = newPoint;
            }
        }

        void titleGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
            //oldPosition = e.GetPosition(this);

            if (BtnMaxFlag && e.ClickCount == 2)
            {
                //btnMax_Click(max, null);
                max.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
        }

        private void btnMin_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void btnMax_Click(object sender, RoutedEventArgs e)
        {
            Button btnMax = sender as Button;
            Image _imageMaximize = btnMax.Template.FindName("imageMaximize", btnMax) as Image;
            Image _imageRestore = btnMax.Template.FindName("imageRestore", btnMax) as Image;
            if (this.WindowState == WindowState.Maximized)
            {
                windowGrid.Margin = new Thickness(10, 0, 10, 10);
                this.WindowState = WindowState.Normal;
                _imageMaximize.Visibility = Visibility.Visible;
                _imageRestore.Visibility = Visibility.Hidden;
            }
            else
            {
                int taskbarHeight = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height - System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Height;
                windowGrid.Margin = new Thickness(0, 0, 0, taskbarHeight);
                this.WindowState = WindowState.Maximized;
                _imageMaximize.Visibility = Visibility.Hidden;
                _imageRestore.Visibility = Visibility.Visible;
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
