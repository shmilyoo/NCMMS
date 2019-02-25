using System.Collections.Generic;
//using System.Linq;
using System.Net;
using PacketDotNet;

namespace NCMMS.PeerMap
{
    public class PeerToPeer
    {
        //以下数据都是单向的数据！！！
        public IPAddress srcIP;
        public  IPAddress dstIP;
        //List<int[]> protocolPkInfos;
        public  Dictionary<IPProtocolType, int[]> protocolPkInfos; //根据协议、数据包数、总大小 保存数据，每个数组两个数，为单向发送的数据包数和总大小。
        public int totalSendPkNum;
        public int totalSendPkSize;
 //       int[] 数组中为 每对节点对中 的每一个协议的单向数据包数量和总大小，totalPkNum和totalSize为每一对节点中间的单向数据包数量和总大小

        public PeerToPeer(IPAddress _SrcIP, IPAddress _DstIP, IPProtocolType protocolType, int _PkSize)
        {
            //当数据包到达时，若判断是第一个从_SrcIP到_DstIP的数据包，则生成此结构，并初始化，后续到来的包执行AddData函数
            protocolPkInfos = new Dictionary<IPProtocolType, int[]>();
            srcIP = _SrcIP;
            dstIP = _DstIP;
            if (_PkSize != 0)
            {
                protocolPkInfos.Add(protocolType, new int[] { 1, _PkSize });
                totalSendPkNum++;
                totalSendPkSize += _PkSize;
            }
            else
            {
                protocolPkInfos.Add(protocolType, new int[] { 0, _PkSize });
            }
        }

        public void AddData(IPProtocolType protocolType,int _PkSize)
        {
            //若是后续到达数据包，SrcIP到DstIP已经存在了，只要增加相应数据即可，不过要判断一下协议
            //if (protocolPkInfos.Keys.Contains(protocolType)) //本协议数据存在
            if (protocolPkInfos.ContainsKey(protocolType)) //本协议数据存在
            {
                if (_PkSize != 0)
                {
                    protocolPkInfos[protocolType][0]++;
                    protocolPkInfos[protocolType][1] += _PkSize;

                    totalSendPkNum++;
                    totalSendPkSize += _PkSize;
                }
            }
            else //本协议数据不存在，建立这个协议的值
            {
                if (_PkSize != 0)
                {
                    protocolPkInfos.Add(protocolType, new int[] { 1, _PkSize });

                    totalSendPkNum++;
                    totalSendPkSize += _PkSize;
                }
                else
                {
                    protocolPkInfos.Add(protocolType, new int[] { 0, _PkSize });
                }
            }
        }

    }

    public class PeerMapPeerInfo
    {
        //此类用来存储peermap每一个点的信息，如IP地址、对端的点、发送的数据等
        public IPAddress localIP;
        public List<IPAddress> linkedIPs;//和本IP链接的所有对端节点地址
        //List<PeerToPeer> PeerToPeers;
        public Dictionary<IPAddress, PeerToPeer> peerToPeers;  //所连接的对端点的地址以及点对点类信息
        
        public int allSendPkNum;  //本节点发送的所有数据包数
        public int allSendPkSize;   //本节点发送的所有数据包大小
        public int allrcvPkNum; 
        public int allrcvPkSize;  


        // 本地地址，对方地址，发送协议（icmp，udp，tcp，else），包数目，本协议总大小

        /// <summary>
        /// 代表本IP的圆点第一次出现的时候，进行初始化,不管对方节点是否存在,判断节点是否存在并添加的工作在主页面上进行
        /// </summary>
        /// <param name="_LocalIP">本点地址</param>
        /// <param name="_DstIP">对端地址</param>
        public PeerMapPeerInfo(IPAddress _LocalIP)
        {
            //构造函数只负责赋值本节点ip
            localIP = _LocalIP;

            linkedIPs = new List<IPAddress>();
            peerToPeers = new Dictionary<IPAddress, PeerToPeer>();
        }


        public void PkFromHere(IPAddress dstIP, IPProtocolType protocolType, int pkSize)
        {
//             if (dstIP.ToString().Equals("192.168.2.1"))
//             {
//             }
            //当到dstIP的数据包是第一个时
            if (!peerToPeers.ContainsKey(dstIP))
            {
                peerToPeers.Add(dstIP, new PeerToPeer(localIP, dstIP, protocolType, pkSize));
                linkedIPs.Add(dstIP);
            }
            else   //当到dstIP的数据包是后续的时
            {
                peerToPeers[dstIP].AddData(protocolType, pkSize);
            }
            if (pkSize != 0)
            {
                allSendPkNum++;
                allSendPkSize += pkSize;
            }

        }


    }
}
