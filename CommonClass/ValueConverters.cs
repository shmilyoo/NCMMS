using System;
using System.Windows.Data;
using System.Windows;
using System.Windows.Media;
using System.Globalization;
using NCMMS.MultiPing;
using System.Windows.Controls;

namespace NCMMS.CommonClass
{
    /// <summary>
    /// 界面banner背景color字符串转换到brush
    /// </summary>
    [ValueConversion(typeof(string), typeof(Brush))]
    public class ColorStringToBrushForWindowBannerBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                string s = value as string;
                Color c = Color.FromArgb(byte.Parse(s.Substring(1, 2), NumberStyles.HexNumber), byte.Parse(s.Substring(3, 2), NumberStyles.HexNumber), byte.Parse(s.Substring(5, 2), NumberStyles.HexNumber), byte.Parse(s.Substring(7, 2), NumberStyles.HexNumber));
                SolidColorBrush brush = new SolidColorBrush(c);
                return brush;
            }
            catch
            {
                return new SolidColorBrush(Color.FromArgb(0xFF,0x47,0x76,0xcc));
            }
        }
        public object ConvertBack(object value, Type targetType, object parameter,
                                    CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 主界面用来显示投影效果，投影比实物要挤压一些
    /// </summary>
    [ValueConversion(typeof(double), typeof(double))]
    public class DoubleToDoubleForMirrorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double scaleY = (double)value;

            return -scaleY*0.7;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
                                    CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// MultiPing.xaml中控制单栏还是双栏显示
    /// </summary>
    [ValueConversion(typeof(bool), typeof(bool))]
    public class BoolToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    /// <summary>
    /// MultiPing.xaml中控制grid分隔条和第二个listbox的显示
    /// </summary>
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class BoolToVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool IsSingleColumn = (bool)value;
            if (IsSingleColumn)
                return Visibility.Collapsed;
            else
                return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// PortMonitor.xaml中控制限速报警的stackpanel根据checkbox来是否显示
    /// </summary>
    [ValueConversion(typeof(bool?), typeof(double))]
    public class BoolQuestionToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isChecked = (bool)value;
            if (isChecked)
                return 1d;
            else
                return 0.5d;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// MultiPing.xaml中控制灯的颜色
    /// </summary>
    [ValueConversion(typeof(bool?), typeof(SolidColorBrush))]
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool? isPingOK = value as bool?;
            if (isPingOK == null)
                return Brushes.LightSlateGray;
            else if (isPingOK == true)
                return Brushes.Lime;
            else
                return Brushes.Red;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// MultiPing.xaml中控制灯的颜色
    /// </summary>
    [ValueConversion(typeof(bool), typeof(SolidColorBrush))]
    public class BoolToIPTextColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool? isPingOK = value as bool?;
            if (isPingOK == false)
                return Brushes.Red;
            else 
                return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// MultiPing.xaml中控制每一行开始暂停按钮的图形
    /// </summary>
    [ValueConversion(typeof(PingTargetState), typeof(string))]
    public class StateToImageUrlConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            PingTargetState state = (PingTargetState)value;
            if (state == PingTargetState.Run)
                return "/NCMMS;component/Images/icoPause.png";
            else
                return "/NCMMS;component/Images/icoStart.png";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    /// <summary>
    /// MultiPing.xaml中控制ListBox背景是否显示
    /// </summary>
    [ValueConversion(typeof(bool), typeof(string))]
    public class BoolToColorStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isSelected = (bool)value;
            if (isSelected)
                return "#EEEE";
            else
                return "#0000";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// MultiPing.xaml中控制延迟结果显示,-2代表无响应，-1代表停止ping
    /// </summary>
    [ValueConversion(typeof(int), typeof(string))]
    public class IntToDelayStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int delay = (int)value;
            if (delay == -1)
                return "0";
            else if (delay == -2)
                return "n/a";
            else if (delay < 1)
                return "<1ms";
            else
                return string.Format("{0}ms", delay.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 拓扑中把图标左上角定点坐标转换成中心点坐标
    /// </summary>
    [ValueConversion(typeof(double), typeof(double))]
    public class PointToCenterPointConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (double)value + (double)parameter/2 + 30d;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    /// <summary>
    /// 拓扑中把图标左上角定点坐标转换成中心点坐标
    /// </summary>
    [ValueConversion(typeof(double), typeof(double))]
    public class PointYToCenterPointYConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (double)value + (double)parameter / 2;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 拓扑中把图标控件的宽和UCEquipLength绑定
    /// </summary>
    [ValueConversion(typeof(double), typeof(double))]
    public class PicWidthToUCWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (double)value + 60d;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 拓扑中把图标控件的高和UCEquipLength绑定
    /// </summary>
    [ValueConversion(typeof(double), typeof(double))]
    public class PicHeightToUCHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (double)value + 20d;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 拓扑中把图标控件的高和UCEquipLength绑定
    /// </summary>
    [ValueConversion(typeof(bool), typeof(string))]
    public class BoolToUPDownStrConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? "UP" : "Down";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

//     /// <summary>
//     /// PeerMap 中转换ucpoint的坐标到横坐标
//     /// </summary>
//     [ValueConversion(typeof(bool), typeof(bool))]
//     public class DoubleToUCPointCenterXConverter : IValueConverter
//     {
//         public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
//         {
//             NCMMS.PeerMap.UCPoint uc = parameter as NCMMS.PeerMap.UCPoint;
//             if (uc.FlowDirect == FlowDirection.LeftToRight)
//                 return (double)value + uc.ActualHeight/2;
//             else
//                 return (double)value + uc.ActualWidth - uc.ActualHeight / 2;
//         }
//         public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
//         {
//             throw new NotImplementedException();
//         }
//     }
//     /// <summary>
//     /// PeerMap 中转换ucpoint的坐标到纵坐标
//     /// </summary>
//     [ValueConversion(typeof(bool), typeof(bool))]
//     public class DoubleToUCPointCenterYConverter : IValueConverter
//     {
//         public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
//         {
//             return (double)value + (parameter as NCMMS.PeerMap.UCPoint).ActualHeight / 2;
//         }
//         public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
//         {
//             throw new NotImplementedException();
//         }
//     }


    /// <summary>
    /// PortMonitor.xaml时间间隔绑定，控制大小
    /// </summary>
    //[ValueConversion(typeof(double), typeof(int))]
    //public class DoubleToIntConverter : IValueConverter
    //{
    //    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        int interval = System.Convert.ToInt32(value);
    //        if (interval > 0)
    //            return interval;
    //        else
    //            return 1;
    //    }

    //    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        double interval = System.Convert.ToDouble(value);
    //        return interval;
    //    }
    //}
}
