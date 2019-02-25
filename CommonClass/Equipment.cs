using System.Collections.Generic;
using System.Data.SqlClient;
using System.ComponentModel;
using SnmpSharpNet;
using Swordfish.NET.Collections;
using System.Windows;
using System;
using NCMMS.UC;

namespace NCMMS.CommonClass
{
    public enum Brand
    {
        Other = 0, Cisco, Huawei, H3Com, MicroSoft, IBM, HP
    }
    public enum EquipType
    {
        Other=0,Router,Layer3Switch,Layer2Switch,Hub,PC,ServerInTable,Server,FireWall
    }

    public class Equipment : INotifyPropertyChanged
    {
        int index, typeIndex;
        string name, typeName, sysDescr, brandName;//设备名称，设备类型名称，snmp返回的sysdescr
        UCEquipIcon ucEquipIcon;

        double x, y;//坐标初始值为-1，代表没有设置坐标值
        /// <summary>
        /// 第一次探索到这个设备所连接的IP地址，当设备名称没有设置的时候，使用这个地址作为默认名称
        /// </summary>
        IpAddress ipFirstGet, adminIPAddress = null;
        EquipType type;
        Brand equipBrand;
        public int Step = -1;

        #region 力导向相关参数
        int degree;
        Vector force, equipVector;
        /// <summary>
        /// 此设备坐标的矢量表示
        /// </summary>
        public Vector EquipVector
        {
            get {
                equipVector.X = x;
                equipVector.Y = y;
                return equipVector; }
        }

        /// <summary>
        /// 此设备在拓扑中所受的合力
        /// </summary>
        public Vector Force
        {
            get { return force; }
            set { force = value; }
        }

        /// <summary>
        /// 度数、维数。此设备在拓扑中连接的设备数
        /// </summary>
        public int Degree
        {
            get { return degree; }
            set { degree = value; }
        }


        #endregion



        /// <summary>
        /// 设置修改相关属性值的时候是否同时修改数据库
        /// 在新建类的时候，给这个值赋值，最好放在最后，避免误操作数据库
        /// </summary>
        public bool isDatabaseEnabled = false;    //??? 重命名无法写入数据库的问题
        
        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
        /// <summary>
        /// 可从equipment返回获取其依附的图标控件
        /// </summary>
        public UCEquipIcon UCEquipIcon
        {
            get { return ucEquipIcon; }
            set { ucEquipIcon = value; }
        }

        public IpAddress AdminIPAddress
        {
            get { return adminIPAddress == null ? ipFirstGet : adminIPAddress; }
            set { adminIPAddress = value; }
        }
        public string SysDescr
        {
            get { return sysDescr; }
            set { sysDescr = value; NotifyPropertyChanged("SysDescr"); }
        }
        /// <summary>
        /// 坐标，初始值为-1，代表没有设置坐标值
        /// </summary>
        public double Y
        {
            get { return y; }
            set { y = value; NotifyPropertyChanged("Y"); }
        }

        /// <summary>
        /// 坐标，初始值为-1，代表没有设置坐标值
        /// </summary>
        public double X
        {
            get { return x; }
            set { x = value; NotifyPropertyChanged("X"); }
        }


        public IpAddress IpFirstGet
        {
            get { return ipFirstGet; }
            set { ipFirstGet = value; }
        }

        public string TypeName
        {
            get { 
                //return typeName;Other=0,Router,Layer3Switch,Layer2Switch,Hub,PC,ServerInTable,Server,FireWall
                return SnmpHelper.GetStrTypeNameFromEquipType(type);
            }
            set
            {
                if (isDatabaseEnabled && !string.IsNullOrEmpty(typeName) && !typeName.Equals(value))
                {
                    string updateSql = "UPDATE Equipments SET Equip_TypeIndex = (select type_index from equipmenttype where type_name = '" + value + "') where Equip_Index = " + index;
                    if (!App.DBHelper.ExecuteReturnBool(updateSql))
                    {
                        MessageBox.Show("更新失败");
                        return;
                    }
                    //同时更新静态缓存列表中的值
                    App.idAndEquipList[index].typeName = value;
                    App.idAndEquipList[index].typeIndex = (int)App.DBHelper.returnScalar(string.Format("select type_index from equipmenttype where type_name ='{0}'", value));
                }
                this.typeName = value;
                Type = SnmpHelper.GetEquipTypeFromStrTypeName(value);
                NotifyPropertyChanged("TypeName");
            }
        }

        public EquipType Type
        {
            get { return type; }
            set { 
                type = value;
                typeName = SnmpHelper.GetStrTypeNameFromEquipType(value);
                NotifyPropertyChanged("Type");
                NotifyPropertyChanged("TypeName");
            }
        }

        public Brand EquipBrand
        {
            get { return equipBrand; }
            set {
                equipBrand = value;
                brandName = SnmpHelper.GetBrandStringFromBrand(value);
                NotifyPropertyChanged("EquipBrand");
                NotifyPropertyChanged("BrandName");
            }
        }
        public string BrandName
        {
            get
            {
                return SnmpHelper.GetBrandStringFromBrand(equipBrand);
            }
            set
            {
                brandName = value;
                EquipBrand = SnmpHelper.GetBrandFromString(value);
                NotifyPropertyChanged("BrandName");
            }
        }

        public string Name
        {
            get
            {
                if (ipFirstGet != null && string.IsNullOrEmpty(name))
                    return ipFirstGet.ToString();
                else
                    return name;
            }
            set
            {
                if (isDatabaseEnabled && !string.IsNullOrEmpty(name) && !name.Equals(value))
                {
                    string updateSql = "UPDATE Equipments SET Equip_Name = '" + value + "' where Equip_Index = " + index;
                    if (!App.DBHelper.ExecuteReturnBool(updateSql))
                    {
                        MessageBox.Show("更新失败");
                        return;
                    }
                    //同时更新静态缓存列表中的值
                    App.idAndEquipList[index].name = value;
                }
                name = value;
                NotifyPropertyChanged("Name");
            }
        }

        public int TypeIndex
        {
            get { return typeIndex; }
            set { typeIndex = value; }
        }

        /// <summary>
        /// 在牵涉到数据库操作的页面中，id>0：数据库中有对应设备；id小于0：不在数据库中设备
        /// </summary>
        public int Index
        {
            get { return index; }
            set { index = value; }
        }
        /// <summary>
        /// ip地址和ip地址信息类列表
        /// </summary>
        Dictionary<IpAddress, IPInformation> ipAndInfoList = new Dictionary<IpAddress, IPInformation>();
        /// <summary>
        /// 接口id和接口连接线信息列表，只有当连线的时候才添加子项
        /// </summary>
        Dictionary<int,LineInfo> ifIDandLineInfoList = new Dictionary<int,LineInfo>();
        /// <summary>
        /// 接口id和接口信息列表
        /// </summary>
        Dictionary<int,IFInfomation> ifIDandIFinfoLIst = new Dictionary<int,IFInfomation>();
        /// <summary>
        /// 目的地址和路由信息列表
        /// </summary>
        Dictionary<IpAddress, RouteInfomation> ipDstAndRouteInfoLIst = new Dictionary<IpAddress, RouteInfomation>();

        List<ARPInfo> arpInfoList = new List<ARPInfo>();

        public List<ARPInfo> ArpInfoList
        {
            get { return arpInfoList; }
            set { arpInfoList = value; }
        }

        public Dictionary<IpAddress, RouteInfomation> IpDstAndRouteInfoLIst
        {
            get { return ipDstAndRouteInfoLIst; }
            set { ipDstAndRouteInfoLIst = value; }
        }

        /// <summary>
        /// 接口id和接口信息列表
        /// </summary>
        public Dictionary<int, IFInfomation> IfIDandIFinfoLIst
        {
          get { return ifIDandIFinfoLIst; }
          set { ifIDandIFinfoLIst = value; }
        }
        /// <summary>
        /// 接口id和接口连接线信息列表，只有当连线的时候才添加子项
        /// </summary>
        public Dictionary<int, LineInfo> IfIDandLineInfoList
        {
          get { return ifIDandLineInfoList; }
          set { ifIDandLineInfoList = value; }
        }

        /// <summary>
        /// ip地址和ip地址信息类列表
        /// </summary>
        public Dictionary<IpAddress,IPInformation> IPAndInfoList
        {
            get { return ipAndInfoList; }
            set { ipAndInfoList = value; }
        }

        public Equipment(int _index, string _name)
        {
            index = _index;
            name = _name;
        }
        public Equipment() { }

        /// <summary>
        /// 获取设置此设备的Dictionary (IpAddress,IPInformation) ipAndInfoList
        /// </summary>
        /// <param name="errorMessage">输出错误信息</param>
        /// <returns>是否成功获取</returns>
        public bool GetIPAndInfoListFromSNMP(out string errorMessage)
        {
            bool isSuccess = false;
            IpAddress agentIP = adminIPAddress == null ? ipFirstGet : adminIPAddress;
            List<string> strIPList = SnmpHelper.GetSingleColumnListFromTable(agentIP, new Oid("1.3.6.1.2.1.4.20.1.1"), out errorMessage);
            if (strIPList == null)
            {
                isSuccess = false;
                return isSuccess;
            }
            else
            {
                strIPList.Remove("127.0.0.1"); // 地址表中应该只有一个127.0.0.1吧，如果不是则需要修改
                VbCollection vbc;
                ipAndInfoList.Clear();
                IpAddress ip = null;
                foreach (string strip in strIPList)
                {
                    vbc = SnmpHelper.GetResultsFromOids(agentIP, new string[] { "1.3.6.1.2.1.4.20.1.2." + strip, "1.3.6.1.2.1.4.20.1.3." + strip }, out errorMessage);
                    if (vbc == null)
                    {
                        isSuccess = false;
                        return isSuccess;
                    }
                    IpAddress mask, gateWay;
                    string ipName;
                    int ifIndex;
                    bool isDefault = false;
                    ifIndex = Convert.ToInt32(vbc[0].Value.ToString());
                    mask = new IpAddress(vbc[1].Value.ToString());
                    ip = new IpAddress(strip);
                    IPInformation ipInfo;
                    //IpAddress _ip, int _equipID, string _equipName, IpAddress _mask, IpAddress _gateway, string _name, bool _isDefault
                    if (index > 0 && App.ipAndIPinfoList.ContainsKey(strip))
                    {  // 说明这个equip是在数据库中有记录的，那么这个IP应该也是有记录的
                        IPInformation tempIPInfo = App.ipAndIPinfoList[strip];
                        //mask = tempIPInfo.IpMask; 还是采用snmp采集到的信息，毕竟真实一些
                        gateWay = tempIPInfo.IpGateWay;
                        ipName = tempIPInfo.IpName;
                        if (isDefault = tempIPInfo.IsDefaultIP)
                            adminIPAddress = ip;
                        ipInfo = new IPInformation(ip, index, name, mask, gateWay, ipName, isDefault);
                    }
                    else
                    {
                        ipInfo = new IPInformation(ip, index, name, mask, null, "", isDefault);
                    }
                    ipInfo.Equip = this;
                    ipInfo.IfIndex = ifIndex;
                    ipAndInfoList.Add(ip, ipInfo);
                }
                isSuccess = true;
                if (ipAndInfoList.Count == 1 && ip != null)  //当设备只有一个ip的时候，直接设为管理地址
                {
                    this.adminIPAddress = ip;
                    ipAndInfoList[ip].IsDefaultIP = true;
                }
            }
            return isSuccess;
        }

        /// <summary>
        /// 获取设置此设备的Dictionary (Int,IFInfomation) ifIDandIFInfoList。
        /// </summary>
        /// <param name="errorMessage">输出错误信息</param>
        /// <returns>是否成功获取</returns>
        public bool GetIFIDandIFInfoList(out string errorMessage)
        {
            bool isSuccess = false;
            IpAddress agentIP = adminIPAddress == null ? ipFirstGet : adminIPAddress;
            List<string> strIFIndexList = SnmpHelper.GetSingleColumnListFromTable(agentIP, new Oid("1.3.6.1.2.1.2.2.1.1"), out errorMessage);
            if (strIFIndexList == null)
            {
                isSuccess = false;
                return isSuccess;
            }
            else
            {
                VbCollection vbc;
                ifIDandIFinfoLIst.Clear();
                foreach (string strIFIndex in strIFIndexList)
                {
                    //获取ifDescr，ifAdminStatus，ifOperStatus
                    vbc = SnmpHelper.GetResultsFromOids(agentIP, new string[] { "1.3.6.1.2.1.2.2.1.2." + strIFIndex, "1.3.6.1.2.1.2.2.1.7." + strIFIndex, "1.3.6.1.2.1.2.2.1.8." + strIFIndex }, out errorMessage);
                    if (vbc == null)
                    {
                        isSuccess = false;
                        return isSuccess;
                    }
                    //暂时没有做接口自定义名称，现在只从snmp中获取信息
                    string descr = vbc[0].Value.ToString();
                    int index = Convert.ToInt32(strIFIndex);
                    bool adminStatus = vbc[1].Value.ToString().Equals("1") ? true : false;
                    bool operStatus = vbc[2].Value.ToString().Equals("1") ? true : false;
                    ifIDandIFinfoLIst.Add(index, new IFInfomation(descr, "", index, adminStatus, operStatus));
                }
                isSuccess = true;
            }
            return isSuccess;
        }

        /// <summary>
        /// 获取设置此设备的Dictionary (IpAddress,RouteInformation) ipAndRouteInfoList。
        /// </summary>
        /// <param name="errorMessage">输出错误信息</param>
        /// <returns>是否成功获取</returns>
        public bool GetIPAndRouteInfoList(out string errorMessage)
        {
            bool isSuccess = false;
            IpAddress agentIP = adminIPAddress == null ? ipFirstGet : adminIPAddress;
            List<string> strIPDstList = SnmpHelper.GetSingleColumnListFromTable(agentIP, new Oid("1.3.6.1.2.1.4.21.1.1"), out errorMessage);
            if (strIPDstList == null)
            {
                isSuccess = false;
                return isSuccess;
            }
            else
            {
                VbCollection vbc;
                ipDstAndRouteInfoLIst.Clear();
                foreach (string strIPDst in strIPDstList)
                {
                    //获取ipRouteIfIndex，ipRouteNextHop，ipRouteType ,ipRouteProto ,ipRouteMask 
                    vbc = SnmpHelper.GetResultsFromOids(agentIP, new string[] { "1.3.6.1.2.1.4.21.1.2." + strIPDst, "1.3.6.1.2.1.4.21.1.7." + strIPDst, "1.3.6.1.2.1.4.21.1.8." + strIPDst, "1.3.6.1.2.1.4.21.1.9." + strIPDst, "1.3.6.1.2.1.4.21.1.11." + strIPDst }, out errorMessage);
                    if (vbc == null)
                    {
                        isSuccess = false;
                        return isSuccess;
                    }
                    //暂时没有做接口自定义名称，现在只从snmp中获取信息
                    IpAddress ipDst = new IpAddress(strIPDst);
                    int ifindex = Convert.ToInt32(vbc[0].Value.ToString());
                    IpAddress ipNextHop = new IpAddress(vbc[1].Value.ToString());
                    int routeType = Convert.ToInt32(vbc[2].Value.ToString());
                    int routeProto = Convert.ToInt32(vbc[3].Value.ToString());
                    IpAddress ipRouteMask = new IpAddress(vbc[4].Value.ToString());
                    RouteInfomation routeInfo = new RouteInfomation(ipDst, ipRouteMask, ipNextHop, ifindex, routeType, routeProto);
                    ipDstAndRouteInfoLIst.Add(ipDst, routeInfo);
                }
                isSuccess = true;
            }
            return isSuccess;
        }

        public void GetIPandInfomationFromDatabase()
        {
            string sql = "SELECT IP_Address, IP_EquipID, IP_Mask, IP_GateWay, IP_Name, IP_IsDefaultIP, Equip_Name FROM IPAddress INNER JOIN Equipments ON IP_EquipID = Equip_Index WHERE IP_EquipID = " + index;
            SqlDataReader dr = App.DBHelper.returnReader(sql);
            if (dr.HasRows)
            {
                ipAndInfoList.Clear();
                while (dr.Read())
                {
                    IpAddress ip = new IpAddress(dr["IP_Address"].ToString());
                    IpAddress mask = new IpAddress(dr["IP_Mask"].ToString());
                    IpAddress gateway = new IpAddress(dr["IP_GateWay"].ToString());
                    bool isDefault = bool.Parse(dr["IP_IsDefaultIP"].ToString());
                    IPInformation ipInfo = new IPInformation(ip, index, dr["Equip_Name"].ToString(), mask, gateway, dr["IP_Name"].ToString(), isDefault);
                    ipAndInfoList.Add(ip, ipInfo);
                }
                dr.Close();
            }
        }
        public bool GetARPInfoListFrom(out string errorMessage)
        {
            bool success = false;
            List<string> index1List = new List<string>();
            List<string> index2List = new List<string>();
            index1List = SnmpHelper.GetSingleColumnListFromTable(AdminIPAddress, new Oid("1.3.6.1.2.1.4.22.1.1"), out errorMessage);
            index2List = SnmpHelper.GetSingleColumnListFromTable(AdminIPAddress, new Oid("1.3.6.1.2.1.4.22.1.3"), out errorMessage);
            if (index1List == null || index2List == null || index1List.Count != index2List.Count)
            {
                success = false;
                return success;
            }
            else
            {
                VbCollection vbc;
                arpInfoList.Clear();
                for (int i = 0; i < index1List.Count;i++)
                {
                    vbc = SnmpHelper.GetResultsFromOids(AdminIPAddress, new string[] { "1.3.6.1.2.1.4.22.1.2." + index1List[i] + "." + index2List[i], "1.3.6.1.2.1.4.22.1.4." + index1List[i] + "." + index2List[i] }, out errorMessage);
                    if (vbc == null)
                    {
                        success = false;
                        return success;
                    }
                    int id = Convert.ToInt32(index1List[i]);
                    IpAddress ip = new IpAddress(index2List[i]);
                    string mac = vbc[0].Value.ToString();
                    int mediaType = Convert.ToInt32(vbc[1].Value.ToString());
                    ARPInfo arp = new ARPInfo(this,id,ip,mac,mediaType);
                    arpInfoList.Add(arp);
                }
                success = true;
            }
            return success;
        }

        public bool SaveInformation()
        {
            return this.SaveInformation(name, typeName);
        }

        /// <summary>
        /// 将设备的一般信息和IP地址信息保存入数据库
        /// </summary>
        /// <param name="_name">一般属性界面中文本框中的名称文字</param>
        /// <param name="_typeName">一般属性界面中类型下拉框中的名称</param>
        /// <returns></returns>
        public bool SaveInformation(string _name,string _typeName)
        {
            //可以尝试获取所有已发现IP，然后搜索数据库，将数据库中存在的已发现IP取出，并关联取出对应的equip，
            //数据库库中删除equip的数据，包含interface ip等
            //将矩阵中所有数据存入数据库
            //待修改！！！！！！！！！
            List<string> cmdList = new List<string>();
            string cmd;
            if (index > 0)
            {
                #region 设备id大于0，数据库中存在的设备（正确的说法为，能够根据第一个IP地址，在数据库中找到设备）
                //此设备在数据库中有保存，需要提取相关信息，覆盖保存。0、保存名称类型；
                //1、数据库中删除此id所属的所有IP；2、此设备列表中每一个IP，轮询缓存ip列表，如果IP相同且设备ID不同，则在数据库中删除此项
                //3、将设备列表中IP插入数据库
                cmd = string.Format("UPDATE Equipments SET Equip_Name ='{0}', Equip_TypeIndex = (SELECT Type_Index FROM EquipmentType WHERE  Type_Name = '{1}') WHERE Equip_Index = {2}", _name, _typeName, index);
                cmdList.Add(cmd);
                cmd = string.Format("DELETE FROM IPAddress WHERE IP_EquipID = {0}", index);
                cmdList.Add(cmd);
                foreach (IpAddress ip in this.IPAndInfoList.Keys)
                {
                    if (App.ipAndIPinfoList.ContainsKey(ip.ToString()))
                    {
                        if (App.ipAndIPinfoList[ip.ToString()].EquipIndex != index)
                        {   //这里应该执行不到，应为数据库中ip是唯一的
                            MessageBox.Show("这里应该执行不到，应为数据库中ip是唯一的");
                            cmd = string.Format("DELETE FROM IPAddress WHERE IP_Address = '{0}'", ip.ToString());
                            cmdList.Add(cmd);
                        }
                    }
                }
                foreach (IPInformation ipInfo in this.IPAndInfoList.Values)
                {
                    cmd = string.Format("INSERT INTO IPAddress (IP_Address, IP_EquipID, IP_Mask, IP_GateWay, IP_Name, IP_IsDefaultIP) VALUES ('{0}',{1},'{2}','{3}','{4}','{5}')", ipInfo.StrIP, index, ipInfo.StrIPMask, ipInfo.StrIPGateWay, ipInfo.IpName, ipInfo.IsDefaultIP);
                    cmdList.Add(cmd);
                }
                if (App.DBHelper.ExecuteTransaction(cmdList))
                {
                    //MessageBox.Show("保存成功");
                    App.InitInformation();
                }
                else
                {
                    //MessageBox.Show("保存失败");
                    return false;
                }
                #endregion
            }
            else if (index < 0)
            {
                #region 此设备在数据库中未保存，检索本设备IP，是否与数据库中的重复，让用户选择覆盖还是退出
                List<string> repeatedIP = new List<string>();
                foreach (IpAddress ip in this.IPAndInfoList.Keys)
                {
                    if (App.ipAndIPinfoList.ContainsKey(ip.ToString()))
                    {
                        repeatedIP.Add(ip.ToString());
                    }
                }
                if (repeatedIP.Count > 0)
                {
                    System.Text.StringBuilder ips = new System.Text.StringBuilder();
                    foreach (string s in repeatedIP)
                    {
                        ips.Append(s + ",");
                    }
                    if (MessageBox.Show(string.Format("IP地址：{0} 与数据库中已经存在的地址重复,\n是否覆盖并继续？", ips.ToString().TrimEnd(',')), "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {   //数据库中有重复的IP且用户选择覆盖
                        cmd = string.Format("INSERT INTO Equipments (Equip_Name, Equip_TypeIndex, Equip_X, Equip_Y) VALUES ('{0}',(SELECT Type_Index FROM EquipmentType WHERE  Type_Name = '{1}'),{2},{3})", _name, _typeName, this.X, this.Y);
                        cmdList.Add(cmd);
                        foreach (string strip in repeatedIP)
                        {
                            cmd = string.Format("DELETE FROM IPAddress WHERE IP_Address = '{0}'", strip);
                            cmdList.Add(cmd);
                        }
                        foreach (IPInformation ipInfo in this.IPAndInfoList.Values)
                        {  //设备存入后，id发生变化，存IP的时候需要取设备新id
                            cmd = string.Format("INSERT INTO IPAddress (IP_Address, IP_EquipID, IP_Mask, IP_GateWay, IP_Name, IP_IsDefaultIP) VALUES ('{0}',(SELECT Equip_Index FROM Equipments WHERE Equip_Name = '{1}'),'{2}','{3}','{4}','{5}')", ipInfo.StrIP, _name, ipInfo.StrIPMask, ipInfo.StrIPGateWay, ipInfo.IpName, ipInfo.IsDefaultIP);
                            cmdList.Add(cmd);
                        }
                        if (App.DBHelper.ExecuteTransaction(cmdList))
                        {
                            //MessageBox.Show("保存成功");
                            App.InitInformation();
                        }
                        else
                        {
                            //MessageBox.Show("保存失败");
                            return false;
                        }
                    }
                    else
                        return false;
                }
                else
                {   //数据库中没有重复的IP，直接保存
                    cmd = string.Format("INSERT INTO Equipments (Equip_Name, Equip_TypeIndex, Equip_X, Equip_Y) VALUES ('{0}',(SELECT Type_Index FROM EquipmentType WHERE  Type_Name = '{1}'),{2},{3})", _name, _typeName, this.X, this.Y);
                    cmdList.Add(cmd);
                    foreach (IPInformation ipInfo in this.IPAndInfoList.Values)
                    {  //设备存入后，id发生变化，存IP的时候需要取设备新id
                        cmd = string.Format("INSERT INTO IPAddress (IP_Address, IP_EquipID, IP_Mask, IP_GateWay, IP_Name, IP_IsDefaultIP) VALUES ('{0}',(SELECT Equip_Index FROM Equipments WHERE Equip_Name = '{1}'),'{2}','{3}','{4}','{5}')", ipInfo.StrIP, _name, ipInfo.StrIPMask, ipInfo.StrIPGateWay, ipInfo.IpName, ipInfo.IsDefaultIP);
                        cmdList.Add(cmd);
                    }
                    if (App.DBHelper.ExecuteTransaction(cmdList))
                    {
                        //MessageBox.Show("保存成功");
                        App.InitInformation();
                    }
                    else
                    {
                        //MessageBox.Show("保存失败");
                        return false;
                    }
                }
                //id为负的设备，插入数据库成功后，将设备类的id也一并更新，否则下次更新此设备时扔当做id为负的设备处理
                this.Index = (int)App.DBHelper.returnScalar(string.Format("SELECT Equip_Index FROM Equipments WHERE Equip_Name = '{0}'", _name));
                #endregion
            }
            else
            {
                //这里是用户手动添加的设备
                //如果想避免此处的处理逻辑，就让用户必须在手动添加设备的时候同时保存如数据库
                //此时，检测数据库和已发现拓扑中有无手动添加的设备，若无，则允许添加，同时保存入数据库
                return false;
            }
            return true;
        }
    }


    /// <summary>
    /// 设备的接口信息类
    /// </summary>
    public class IFInfomation : INotifyPropertyChanged
	{
        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
		string ifDescr,ifName;
        int ifIndex;
        bool ifAdminStatus,ifOperStatus;

        public string IfName
        {
          get { return ifName; }
          set { ifName = value; }
        }

        public string IfDescr
        {
          get { return ifDescr; }
          set { ifDescr = value; }
        }

        public bool IfOperStatus
        {
            get { return ifOperStatus; }
            set { ifOperStatus = value; NotifyPropertyChanged("IfOperStatus"); }
        }

        public bool IfAdminStatus
        {
            get { return ifAdminStatus; }
            set { ifAdminStatus = value; NotifyPropertyChanged("IfAdminStatus"); }
        }

        public int IfIndex
        {
          get { return ifIndex; }
          set { ifIndex = value; }
        }
        public IFInfomation(string _ifDescr,string _ifName,int _ifIndex,bool _adminStatus,bool _operStatus)
        {
            ifDescr = _ifDescr;
            ifName = _ifName;
            ifIndex = _ifIndex;
            ifAdminStatus = _adminStatus;
            ifOperStatus = _operStatus;
        }

	}
    public class RouteInfomation
    {
        IpAddress dstIpaddr,dstMask,ipNextHop;
        int ifIndex,routeType,routeProtocol;

        public IpAddress IpNextHop
        {
            get { return ipNextHop; }
            set { ipNextHop = value; }
        }
        public IpAddress DstMask
        {
            get { return dstMask; }
            set { dstMask = value; }
        }
        public IpAddress DstIpaddr
        {
            get { return dstIpaddr; }
            set { dstIpaddr = value; }
        }
        public int RouteProtocol
        {
            get { return routeProtocol; }
            set { routeProtocol = value; }
        }
        public int RouteType
        {
            get { return routeType; }
            set { routeType = value; }
        }
        public int IfIndex
        {
            get { return ifIndex; }
            set { ifIndex = value; }
        }
        /// <summary>
        /// 根据RouteType的id返回字符串
        /// </summary>
        public string strRouteType
        {
            get{ return SnmpHelper.GetRouteTypeFromNum(routeType); }
        }
        /// <summary>
        /// 根据RouteProtocol的id返回字符串
        /// </summary>
        public string strRouteProtocol
        {
            get{ return SnmpHelper.GetRouteProtocolFromNum(routeProtocol); }
        }

        public string DstIPName
        {
            get 
            {
                if (App.ipAndIPinfoList.ContainsKey(dstIpaddr.ToString()))
                    return App.ipAndIPinfoList[dstIpaddr.ToString()].IpName;
                else
                    return null;
            }
        }
        public string IPNextHopName
        {
            get
            {
                if (App.ipAndIPinfoList.ContainsKey(ipNextHop.ToString()))
                    return App.ipAndIPinfoList[ipNextHop.ToString()].IpName;
                else
                    return null;
            }
        }

        public RouteInfomation(IpAddress _dstIP,IpAddress _dstMask,IpAddress _ipNextHop,int _ifIndex,int _routeType,int _routeProtocol)
        {
            dstIpaddr = _dstIP;
            dstMask = _dstMask;
            ipNextHop = _ipNextHop;
            ifIndex = _ifIndex;
            routeType = _routeType;
            routeProtocol = _routeProtocol;
        }
    }
    
    /// <summary>
    /// 图中没条线中的信息，两端设备，设备的接口id
    /// </summary>
    public class LineInfo
    {
        Equipment equipA, equipB;
        int ifIDA, ifIDB;
        System.Windows.Shapes.Line l;

        public System.Windows.Shapes.Line L
        {
            get { return l; }
            set { l = value; }
        }

        public int IfIDA
        {
            get { return ifIDA; }
            set { ifIDA = value; }
        }

        public int IfIDB
        {
            get { return ifIDB; }
            set { ifIDB = value; }
        }

        public Equipment EquipB
        {
            get { return equipB; }
            set { equipB = value; }
        }

        public Equipment EquipA
        {
            get { return equipA; }
            set { equipA = value; }
        }

        public double LineLength()
        {
            return Math.Sqrt(Math.Pow(l.X1 - l.X2, 2) + Math.Pow(l.Y1 - l.Y2, 2));
        }
    }

    public class ARPInfo
    {
        Equipment equip;
        int ifID;
        IpAddress ip;
        string mac;
        int mediaType;

        public int MediaType
        {
            get { return mediaType; }
            set { mediaType = value; }
        }
        public string StrMediaType
        {
            get { return SnmpHelper.GetStrMediaTypeFromNum(mediaType); }
        }
        public Equipment Equip
        {
            get { return equip; }
            set { equip = value; }
        }
        public int IfID
        {
            get { return ifID; }
            set { ifID = value; }
        }
        public IpAddress IP
        {
            get { return ip; }
            set { ip = value; }
        }
        public string Mac
        {
            get { return mac; }
            set { mac = value; }
        }
        public string IFDescr
        {
            get { return equip.IfIDandIFinfoLIst[ifID].IfDescr; }
        }
        public string IPName
        {
            get 
            {
                if (App.ipAndIPinfoList.ContainsKey(ip.ToString()))
                    return App.ipAndIPinfoList[ip.ToString()].IpName;
                else
                    return null;
            } 
        }

        public ARPInfo(Equipment _equip,int _ifID,IpAddress _ip,string _mac,int _mediaType)
        {
            equip = _equip;
            ifID = _ifID;
            ip = _ip;
            mac = _mac;
            mediaType = _mediaType;
        }
    }
}
