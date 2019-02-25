using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using SnmpSharpNet;

namespace NCMMS.CommonClass
{
    public class SubnetErrorException : Exception
    {
        public SubnetErrorException() { }
        public SubnetErrorException(string message) : base(message) { }
        public SubnetErrorException(string message, Exception inner) : base(message, inner) { }
        protected SubnetErrorException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }

    public class Subnet
    {
        IpAddress mask, ip;
        int maskBitsNum;
        public IpAddress Mask
        {
            get { return mask; }
        }
        public IpAddress IP
        {
            get { return ip; }
        }
        public int MaskBitsNum
        {
            get { return mask.GetMaskBits(); }
        }

        /// <summary>
        /// 根据子网地址和掩码位数初始化子网类
        /// </summary>
        /// <param name="_ip">子网地址</param>
        /// <param name="_maskBitsNum">掩码位数</param>
        public Subnet(IpAddress _ip, int _maskBitsNum)
        {
            mask = IpAddress.BuildMaskFromBits(_maskBitsNum);
            if (_ip.GetSubnetAddress(mask).CompareTo(_ip) != 0)
                throw new SubnetErrorException("子网地址和掩码不搭配");
            ip = _ip;
        }
        /// <summary>
        /// 根据子网地址和掩码地址初始化子网类
        /// </summary>
        /// <param name="_ip">子网地址</param>
        /// <param name="_mask">掩码地址</param>
        public Subnet(IpAddress _ip, IpAddress _mask)
        {
            if (!_mask.IsValidMask())
                throw new SubnetErrorException("不是正确的掩码格式");
            if (_ip.GetSubnetAddress(_mask).CompareTo(_ip) != 0)
                throw new SubnetErrorException("子网地址和掩码不搭配");
            ip = _ip;
            mask = _mask;
            maskBitsNum = _mask.GetMaskBits();
        }

        public static bool IsSubnet(IpAddress _ip, IpAddress _mask)
        {
            if (_ip.GetSubnetAddress(_mask).CompareTo(_ip) != 0)
                return false;
            return true;
        }
        public static bool IsSubnet(IpAddress _ip, int _maskBitsNum)
        {
            if (_ip.GetSubnetAddress(IpAddress.BuildMaskFromBits(_maskBitsNum)).CompareTo(_ip) != 0)
                return false;
            return true;
        }

        /// <summary>
        /// 比较两个子网是否相同
        /// </summary>
        public bool Equals(Subnet secondSubnet)
        {
            if (this.ip.CompareTo(secondSubnet.ip) == 0 && this.mask.CompareTo(secondSubnet.mask) == 0)
                return true;
            return false;
        }

        /// <summary>
        /// 判断指定ip地址是否在此子网范围内
        /// </summary>
        /// <param name="_ip">指定ip地址</param>
        public bool Contains(IpAddress _ip)
        {
            return ip.CompareTo(_ip.GetSubnetAddress(mask)) == 0 ? true : false;
        }
        public override string ToString()
        {
            return string.Format("子网地址:{0};掩码{1},{2}位",ip,mask,maskBitsNum);
        }
    }
}
