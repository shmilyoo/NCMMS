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
    public partial class WindowShowLineInfo : UserControl
    {
        private Line line;
        DispatcherTimer timer;
        EllipseInfo srcElInfo,dstElInfo;
        PeerMapShow uc;
        IPAddress srcIP,dstIP;
        //PeerMapPeerInfo srcPeerInfo,dstPeerInfo;
        PeerToPeer p2pS2D, p2pD2S;
        int sentPacketsNum, sentBytesNum, oldSentBytesNum, sentSpeedNum, rcvPacketsNum, rcvBytesNum,oldRcvBytesNum, rcvSpeedNum;
        double sentPercentNum,rcvPercentNum;

        public WindowShowLineInfo(Line l, PeerMapShow _uc)
        {
            InitializeComponent();
            uc = _uc;
            line = l;
            timer = new DispatcherTimer();
            timer.Interval = new System.TimeSpan(0, 0, 1);
            timer.Tick += new EventHandler(timer_Tick);
            timer.Start();
            srcIP = ((PeerMapShow.LineSrcDst)line.Tag)._srcip;
            dstIP = ((PeerMapShow.LineSrcDst)line.Tag)._dstip;
            tbSrcIP.Text = srcIP.ToString();
            tbDstIP.Text = dstIP.ToString();
            //srcPeerInfo = uc.ipAndPeerInfos[srcIP];
            //dstPeerInfo = uc.ipAndPeerInfos[dstIP];
            p2pS2D = uc.ipAndPeerInfos[srcIP].peerToPeers[dstIP];
            p2pD2S = uc.ipAndPeerInfos[dstIP].peerToPeers[srcIP];

            tbIP.Text = srcIP.ToString() + " - " + dstIP.ToString();

            sentPacketsNum = p2pS2D.totalSendPkNum;
            oldSentBytesNum = sentBytesNum = p2pS2D.totalSendPkSize;
            sentPercentNum = (double)p2pS2D.totalSendPkSize * 100 / (double)uc.totalPkSize;
            rcvPacketsNum = p2pD2S.totalSendPkNum;
            oldRcvBytesNum = rcvBytesNum = p2pD2S.totalSendPkSize;
            rcvPercentNum = (double)p2pD2S.totalSendPkSize * 100 / (double)uc.totalPkSize;

            sentPackets.Text = sentPacketsNum.ToString();
            sentBytes.Text = sentBytesNum.ToString();
            sentSpeed.Text = "";
            sentPercent.Text = sentPercentNum.ToString("0.000");

            rcvPackets.Text = rcvPacketsNum.ToString();
            rcvBytes.Text = rcvBytesNum.ToString(); 
            rcvSpeed.Text = "";
            rcvPercent.Text = rcvPercentNum.ToString("0.000");

            sumPackets.Text = (sentPacketsNum + rcvPacketsNum).ToString();
            sumBytes.Text = (sentBytesNum + rcvBytesNum).ToString();
            sumSpeed.Text = "";
            sumPercent.Text = (sentPercentNum + rcvPercentNum).ToString("0.000");

            protocols.Text = CountProtocols(p2pS2D, p2pD2S);

        }

        private string CountProtocols(PeerToPeer s2d, PeerToPeer d2s)
        {
            int totalProtocolNumInPeer = 0;
            int s2dTCP = 0, s2dUDP = 0, d2sTCP = 0, d2sUDP = 0, s2dTCPSize = 0,
                s2dUDPSize = 0, d2sTCPSize = 0, d2sUDPSize = 0;
            List<IPProtocolType> list = new List<IPProtocolType>();

            foreach (IPProtocolType type in s2d.protocolPkInfos.Keys)
            {
                if (!list.Contains(type))
                {
                    list.Add(type);
                    totalProtocolNumInPeer++;
                }
            }
            foreach (IPProtocolType type in d2s.protocolPkInfos.Keys)
            {
                if (!list.Contains(type))
                {
                    list.Add(type);
                    totalProtocolNumInPeer++;
                }
            }

            if (list.Count >=2)
            {
            }

            if (s2d.protocolPkInfos.ContainsKey(IPProtocolType.TCP))
            {
                
                s2dTCP = s2d.protocolPkInfos[IPProtocolType.TCP][0];
                s2dTCPSize = s2d.protocolPkInfos[IPProtocolType.TCP][1];
            }

            if (s2d.protocolPkInfos.ContainsKey(IPProtocolType.UDP))
            {
                s2dUDP = s2d.protocolPkInfos[IPProtocolType.UDP][0];
                s2dUDPSize = s2d.protocolPkInfos[IPProtocolType.UDP][1];
            }

            if (d2s.protocolPkInfos.ContainsKey(IPProtocolType.TCP))
            {
                d2sTCP = d2s.protocolPkInfos[IPProtocolType.TCP][0];
                d2sTCPSize = d2s.protocolPkInfos[IPProtocolType.TCP][1];
            }

            if (d2s.protocolPkInfos.ContainsKey(IPProtocolType.UDP))
            {
                d2sUDP = d2s.protocolPkInfos[IPProtocolType.UDP][0];
                d2sUDPSize = d2s.protocolPkInfos[IPProtocolType.UDP][1];
            }
            string protocolText = string.Format("协议:{0}; TCP:{1}个{2}字节; UDP:{3}个{4}字节",
                totalProtocolNumInPeer, s2dTCP + d2sTCP,s2dTCPSize + d2sTCPSize,s2dUDP + d2sUDP,s2dUDPSize + d2sUDPSize);

            return protocolText;
        }

        void timer_Tick(object sender, EventArgs e)
        {
            sentPacketsNum = p2pS2D.totalSendPkNum;
            sentBytesNum = p2pS2D.totalSendPkSize;
            sentPercentNum = (double)p2pS2D.totalSendPkSize * 100 / (double)uc.totalPkSize;
            rcvPacketsNum = p2pD2S.totalSendPkNum;
            rcvBytesNum = p2pD2S.totalSendPkSize;
            rcvPercentNum = (double)p2pD2S.totalSendPkSize * 100 / (double)uc.totalPkSize;

            sentPackets.Text = sentPacketsNum.ToString();
            sentBytes.Text = sentBytesNum.ToString();
            sentSpeed.Text = ((sentBytesNum - oldSentBytesNum) / (double)125).ToString("0.000");
            sentPercent.Text = sentPercentNum.ToString("0.000");

            rcvPackets.Text = rcvPacketsNum.ToString();
            rcvBytes.Text = rcvBytesNum.ToString();
            rcvSpeed.Text = ((rcvBytesNum - oldRcvBytesNum) / (double)125).ToString("0.000");
            rcvPercent.Text = rcvPercentNum.ToString("0.000");

            sumPackets.Text = (sentPacketsNum + rcvPacketsNum).ToString();
            sumBytes.Text = (sentBytesNum + rcvBytesNum).ToString();
            sumSpeed.Text = ((sentBytesNum - oldSentBytesNum + rcvBytesNum - oldRcvBytesNum) / (double)125).ToString("0.000");
            sumPercent.Text = (sentPercentNum + rcvPercentNum).ToString("0.000");

            protocols.Text = CountProtocols(p2pS2D, p2pD2S);

            oldSentBytesNum = sentBytesNum;
            oldRcvBytesNum = rcvBytesNum;
        }



        public void Stop_Timing()
        {
            timer.Stop();
        }
    }
}
