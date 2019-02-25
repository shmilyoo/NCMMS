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
using System.Windows.Threading;
using System.Net;
using PacketDotNet;

namespace NCMMS.PeerMap
{
    /// <summary>
    /// WindowShowEllipseInfo.xaml 的交互逻辑
    /// </summary>
    public partial class WindowShowEllipseInfo : UserControl
    {
        private Ellipse el;
        DispatcherTimer timer;
        EllipseInfo elInfo;
        PeerMapShow uc;
        IPAddress ip;
        PeerMapPeerInfo peerInfo;

        public WindowShowEllipseInfo(Ellipse e, PeerMapShow _uc)
        {
            InitializeComponent();
            //this.ShowInTaskbar = false;
            uc = _uc;
            el = e;
            timer = new DispatcherTimer();
            timer.Interval = new System.TimeSpan(0, 0, 1);
            timer.Tick += new EventHandler(timer_Tick);
            timer.Start();
            elInfo = (EllipseInfo)el.Tag;
            ip = elInfo.ip;
            peerInfo = uc.ipAndPeerInfos[ip];
            tbIP.Text = ip.ToString();
            sentPackets.Text = peerInfo.allSendPkNum.ToString();
            sentBytes.Text = peerInfo.allSendPkSize.ToString();
            sentPercent.Text = ((double)peerInfo.allSendPkSize * 100 / (double)uc.totalPkSize).ToString("0.000");
            rcvPackets.Text = peerInfo.allrcvPkNum.ToString();
            rcvBytes.Text = peerInfo.allrcvPkSize.ToString();
            rcvPercent.Text = ((double)peerInfo.allrcvPkSize * 100 / (double)uc.totalPkSize).ToString("0.000");

            peers.Text = peerInfo.peerToPeers.Count.ToString();
            protocols.Text = CountProtocols(peerInfo).ToString();
        }

//         ~WindowShowEllipseInfo()
//         {
//             MessageBox.Show("回收了");
//             //还是没有回收，程序完全关闭才回收，待解决
//         }

        private int CountProtocols(PeerMapPeerInfo peerInfo)
        {
            int i = 0;
            Dictionary<IPAddress, PeerToPeer> tempPeerToPeers = new Dictionary<IPAddress, PeerToPeer>(peerInfo.peerToPeers);
            //tempPeerToPeers = peerInfo.peerToPeers;
            List<IPProtocolType> list = new List<IPProtocolType>();
            foreach (PeerToPeer peertopeer in tempPeerToPeers.Values)
            {
                foreach (IPProtocolType type in peertopeer.protocolPkInfos.Keys)
                {
                    if (!list.Contains(type))
                    {
                        list.Add(type);
                        i++;
                    }
                }
            }
            return i;
        }

        void timer_Tick(object sender, EventArgs e)
        {
            sentPackets.Text = peerInfo.allSendPkNum.ToString();
            sentBytes.Text = peerInfo.allSendPkSize.ToString();
            sentPercent.Text = ((double)peerInfo.allSendPkSize * 100 / (double)uc.totalPkSize).ToString("0.000");
            rcvPackets.Text = peerInfo.allrcvPkNum.ToString();
            rcvBytes.Text = peerInfo.allrcvPkSize.ToString();
            rcvPercent.Text = ((double)peerInfo.allrcvPkSize * 100 / (double)uc.totalPkSize).ToString("0.000");
            peers.Text = peerInfo.peerToPeers.Count.ToString();
            protocols.Text = CountProtocols(peerInfo).ToString();
        }

        public void Stop_Timing()
        {
            timer.Stop();
        }
    }
}
