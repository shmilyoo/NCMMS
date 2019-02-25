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
using System.ComponentModel;

namespace NCMMS.PeerMap
{
    class EllipseInfo : INotifyPropertyChanged
    {
        public  double left, top;
        //public double r;
        private Ellipse el;
        string strIP;
        public IPAddress ip;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }

        public EllipseInfo(Ellipse e, IPAddress _ip)
        {
            el = e;
            left = Canvas.GetLeft(el);
            top = Canvas.GetTop(el);
            //r = el.Width / 2;
            ip = _ip;
            strIP = ip.ToString();

        }

        //设置时是点的位置，获取时是文本的位置，为了简便
        public double TextBlockLeft
        {
            get 
            {
                if (left*2 > ((Canvas)el.Parent).ActualWidth)
                    return left + el.Width + 5; 
                else
                    return left - strIP.Length * 6.2; 
            }
            set
            {
                left = value;
                NotifyPropertyChanged("TextBlockLeft");
            }
        }

        //设置时是点的位置，获取时是文本的位置，为了简便
        public double TextBlockTop
        {
            get { return top - 2; }
            set
            {
                top = value;
                NotifyPropertyChanged("TextBlockTop");
            }
        }

        public double CenterPointX
        {
            get { return left + el.Width / 2; }
            set
            {
                //top = value;
                NotifyPropertyChanged("CenterPointX");
            }
        }

        public double CenterPointY
        {
            get { return top + el.Width / 2; }
            set
            {
                //top = value;
                NotifyPropertyChanged("CenterPointY");
            }
        }

        public void RaiseNotifyPropertyChanged(string pro)
        {
            NotifyPropertyChanged(pro);
        }
    }
}
