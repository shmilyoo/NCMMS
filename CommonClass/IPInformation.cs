using System;
using System.ComponentModel;
using SnmpSharpNet;
using System.Windows;
using NCMMS.Config;

namespace NCMMS.CommonClass
{
    public class IPInformation : INotifyPropertyChanged
    {
        int ipIndex, equipIndex;
        string ipName, equipName;
        IpAddress ip, ipMask, ipGateWay;
        bool isDefaultIP;
        Equipment equip;
        int ifIndex = -1; //设备中此IP所对应的接口ID。获取管理地址时候用到。


        /// <summary>
        /// 设置修改相关属性值的时候是否同时修改数据库,在设置页面使能此属性以便同时存储数据库
        /// 在新建类的时候，给这个值赋值，最好放在最后，避免误操作数据库
        /// </summary>
        public bool isDatabaseEnabled = false;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
        /// <summary>
        /// 设备中此IP所对应的接口ID。获取管理地址时候用到。
        /// </summary>
        public int IfIndex
        {
            get { return ifIndex; }
            set { ifIndex = value; }
        }
        /// <summary>
        /// 获取或设置此IP地址所属的设备，只在拓扑发现中用
        /// 因为所属设备ID可能变化，所以用id来判断设备不准确容易出错
        /// </summary>
        public Equipment Equip
        {
            get { return equip; }
            set { equip = value; }
        }
        public string StrIP
        {
            get { return ip.ToString(); }
            set
            {
                if (IpAddress.IsIP(value))
                {
                    //if (isDatabaseEnabled)
                    //{
                    //    string updateSql = string.Format("UPDATE IPAddress SET IP_Address = '{0}' where IP_Index = {1}", value, ipIndex);
                    //    if (!App.DBHelper.ExecuteReturnBool(updateSql))
                    //    {
                    //        MessageBox.Show("更新失败");
                    //        return;
                    //    }
                    //    //同时更新静态缓存列表中的值
                    //    IPInformation info = new IPInformation(this);
                    //    info.ip = new IpAddress(value);
                    //    App.ipAndIPinfoList.Remove(ip.ToString());
                    //    App.ipAndIPinfoList.Add(value, info);
                    //}
                    IP = new IpAddress(value);
                    NotifyPropertyChanged("StrIP");
                }
                else
                    MessageBox.Show("IP地址格式不正确");
            }
        }
        public int IpIndex
        {
            get { return ipIndex; }
            set { ipIndex = value; }
        }
        public string StrIPMask
        {
            get
            { return ipMask == null ? "" : ipMask.ToString(); }
            set
            {
                if (IpAddress.IsIP(value))
                {
                    IpAddress tempIP = new IpAddress(value);
                    if (!tempIP.IsValidMask())
                    {
                        MessageBox.Show("掩码格式不正确");
                        return;
                    }
                    //if (isDatabaseEnabled)
                    //{
                    //    string updateSql = string.Format("UPDATE IPAddress SET IP_Mask = '{0}' where IP_Index = {1}", value, ipIndex);
                    //    if (!App.DBHelper.ExecuteReturnBool(updateSql))
                    //    {
                    //        MessageBox.Show("更新失败");
                    //        return;
                    //    }
                    //    //同时更新静态缓存列表中的值
                    //    App.ipAndIPinfoList[StrIP].ipMask = tempIP;
                    //}
                    IpMask = tempIP;
                    NotifyPropertyChanged("StrIPMask");
                }
                else
                    MessageBox.Show("IP地址格式不正确");
            }
        }
        public string StrIPGateWay
        {
            get { return ipGateWay == null ? "" : ipGateWay.ToString(); }
            set
            {
                if (IpAddress.IsIP(value))
                {
                    //if (isDatabaseEnabled)
                    //{
                    //    string updateSql = string.Format("UPDATE IPAddress SET IP_GateWay = '{0}' where IP_Index = {1}", value, ipIndex);
                    //    if (!App.DBHelper.ExecuteReturnBool(updateSql))
                    //    {
                    //        MessageBox.Show("更新失败");
                    //        return;
                    //    }
                    //    //同时更新静态缓存列表中的值
                    //    App.ipAndIPinfoList[StrIP].ipGateWay = new IpAddress(value);
                    //}
                    IpGateWay = new IpAddress(value);
                    NotifyPropertyChanged("StrIPGateWay");
                }
                else
                    MessageBox.Show("IP地址格式不正确");
            }
        }

        public int EquipIndex
        {
            get { return equipIndex; }
            set { equipIndex = value; }
        }

        public string EquipName
        {
            get { return equipName; }
            set
            {
                //if (isDatabaseEnabled)
                //{
                //    string selectSql = string.Format("SELECT Equip_Index FROM  Equipments WHERE Equip_Name = '{0}'", value);
                //    try
                //    {
                //        EquipIndex = Convert.ToInt32(App.DBHelper.returnScalar(selectSql));
                //    }
                //    catch (Exception ex)
                //    {
                //        MessageBox.Show("更新IP信息中的设备ID出错，数据库相关错误\n" + ex.Message);
                //        return;
                //    }
                //    string updateSql = string.Format("update IPAddress set ip_equipid = {0} where ip_index = {1}", equipIndex, ipIndex);
                //    IsDefaultIP = false;
                //    if (!App.DBHelper.ExecuteReturnBool(updateSql))
                //    {
                //        MessageBox.Show("更新失败");
                //        return;
                //    }
                //    //同时更新静态缓存列表中的值
                //    App.ipAndIPinfoList[StrIP].equipName = value;
                //    App.ipAndIPinfoList[StrIP].equipIndex = equipIndex;
                //}
                equipName = value;
                NotifyPropertyChanged("EquipName");
            }
        }

        public IpAddress IpGateWay
        {
            get { return ipGateWay; }
            set { ipGateWay = value; }
        }

        public IpAddress IpMask
        {
            get { return ipMask; }
            set { ipMask = value; }
        }

        public bool IsDefaultIP
        {
            get { return isDefaultIP; }
            set
            {
                
                //if (isDatabaseEnabled)
                //{
                //    string updateSql2 = string.Format("UPDATE IPAddress SET IP_IsDefaultIP = '{0}' where IP_Index = {1}", value, ipIndex);
                //    if (!App.DBHelper.ExecuteReturnBool(updateSql2))
                //    {
                //        MessageBox.Show("更新失败");
                //        return;
                //    }
                //    //同时更新静态缓存列表中的值
                //    App.ipAndIPinfoList[StrIP].isDefaultIP = value;
                //}
                isDefaultIP = value;
                NotifyPropertyChanged("IsDefaultIP");
            }
        }

        public string IpName
        {
            get { return ipName; }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    if (isDatabaseEnabled)
                    {
                        string updateSql = string.Format("UPDATE IPAddress SET IP_Name = '{0}' where IP_Index = {1}", value, ipIndex);
                        if (!App.DBHelper.ExecuteReturnBool(updateSql))
                        {
                            MessageBox.Show("更新失败");
                            return;
                        }
                        //同时更新静态缓存列表中的值
                        App.ipAndIPinfoList[StrIP].ipName = value;
                    }
                    ipName = value;
                    NotifyPropertyChanged("IpName");
                }
                else
                    MessageBox.Show("字符串不能为空");
            }
        }

        public IpAddress IP
        {
            get { return ip; }
            set { ip = value; }
        }

        public IPInformation(IpAddress _ip, int _equipID, string _equipName, IpAddress _mask, IpAddress _gateway, string _ipName, bool _isDefault)
        {
            ip = _ip;
            equipIndex = _equipID;
            equipName = _equipName;
            ipMask = _mask;
            ipGateWay = _gateway;
            ipName = _ipName;
            isDefaultIP = _isDefault;
        }

        public IPInformation(IPInformation second)
        {
            ip = second.ip;
            ipIndex = second.ipIndex;
            equipIndex = second.equipIndex;
            equipName = second.equipName;
            ipMask = second.ipMask;
            ipGateWay = second.ipGateWay;
            ipName = second.ipName;
            isDefaultIP = second.isDefaultIP;
        }
        public IPInformation(IPInfor4DB second)
        {
            ip = second.IP;
            ipIndex = second.IpIndex;
            equipIndex = second.EquipIndex;
            equipName = second.EquipName;
            ipMask = second.IpMask;
            ipGateWay = second.IpGateWay;
            ipName = second.IpName;
            isDefaultIP = second.IsDefaultIP;
        }
    }
}
