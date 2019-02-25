using System;
using System.Windows;
using System.Windows.Controls;
using System.Data;
using System.Collections.ObjectModel;
using NCMMS.CommonClass;
using System.Data.SqlClient;
using System.Threading;
using System.Collections.Generic;
using SnmpSharpNet;

namespace NCMMS.Config
{
    /// <summary>
    /// ConfigStart.xaml 的交互逻辑
    /// </summary>
    public partial class ConfigStart : UserControl
    {
        ObservableCollection<Equip4DB> equipList = new ObservableCollection<Equip4DB>();
        ObservableCollection<string> typeList = new ObservableCollection<string>();
        ObservableCollection<string> equipNameList = new ObservableCollection<string>();
        ObservableCollection<IPInfor4DB> ipInformationList = new ObservableCollection<IPInfor4DB>();

        //操作数据比较多后，直接把usercontrol删除然后重建，这样还比较快些，在主页面调用
        public delegate void RefreshEventHandler(object sender, EventArgs e);
        public event RefreshEventHandler RefreshEvent;

        public ConfigStart()
        {
            InitializeComponent();
            InitDatabaseConfig();
            InitGeneralConfig();
            InitSNMPConfig();
            if (App.databaseConState == true)
            {
                InitTypeList();
                InitEquipNameList();
                InitDataGridEquipments();
                InitDataGridIPinfo();
            }
        }
        #region 数据库设置tab相关

        private void InitDatabaseConfig()
        {
            string connectionString = Properties.Settings.Default["ConnectionString"] as string;
            string[] csConfigs = connectionString.Trim(' ', ';').Split(';');
            dataBaseIP.InsertIP(csConfigs[0].Substring(csConfigs[0].IndexOf('=') + 1));
            dataBaseName.Text = csConfigs[1].Substring(csConfigs[1].IndexOf('=') + 1);
            username.Text = csConfigs[2].Substring(csConfigs[2].IndexOf('=') + 1);
            password.Password = csConfigs[3].Substring(csConfigs[3].IndexOf('=') + 1);
        }
        private void btnTestDataBase_Click(object sender, RoutedEventArgs e)
        {
            if (App.DBHelper.Con.State != ConnectionState.Closed)
            {
                MessageBox.Show("有别的进程正在连接数据库，请稍候");
                return;
            }
            if (dataBaseIP.IP != null)
            {
                string oldConnectionString = Properties.Settings.Default["ConnectionString"] as string;
                try
                {
                    App.DBHelper.Con.ConnectionString = string.Format("server={0};database={1};uid={2};pwd={3};", dataBaseIP.IP.ToString(), dataBaseName.Text, username.Text, password.Password);
                    App.DBHelper.Open();
                    MessageBox.Show("连接数据库成功");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("连接数据库失败\n" + ex.Message);
                }
                finally
                {
                    App.DBHelper.Close();
                    if (App.DBHelper.Con.State == ConnectionState.Closed)
                        App.DBHelper.Con.ConnectionString = oldConnectionString;
                }
            }
            else
            {
                MessageBox.Show("不是有效的IP地址");
            }
        }

        private void saveDataBaseConfig_Click(object sender, RoutedEventArgs e)
        {
            if (App.DBHelper.Con.State == ConnectionState.Closed)
            {
                App.DBHelper.Con.ConnectionString = string.Format("server={0};database={1};uid={2};pwd={3};", dataBaseIP.IP.ToString(), dataBaseName.Text, username.Text, password.Password);
                Properties.Settings.Default["ConnectionString"] = App.DBHelper.Con.ConnectionString;
                Properties.Settings.Default.Save();
                MessageBox.Show("保存成功");
            }
            else
                MessageBox.Show("数据库连接不是关闭状态，无法保存连接字符串");
            if (App.databaseConState == true)
                App.InitInformation();
        }

        #endregion

        #region 一般设置tab相关

        private void InitGeneralConfig()
        {
            backgroundPicUrl.Text = Properties.Settings.Default["BackgroundPicUrl"].ToString();
            tbWindowBorderColor.Text = Properties.Settings.Default["WindowBannerColor"].ToString();
        }



        private void btnOpenBgPic_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "图像文件(jpg,gif,png,bmp)|*.jpg;*.gif;*.png;*.bmp";
            if ((bool)openFileDialog.ShowDialog())
            {
                backgroundPicUrl.Text = openFileDialog.FileName;
            }
        }

        private void btnConfigGeneralOK_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default["BackgroundPicUrl"] = backgroundPicUrl.Text;
            Properties.Settings.Default["WindowBannerColor"] = tbWindowBorderColor.Text;
            Properties.Settings.Default.Save();
        }

        #endregion

        #region 数据库数据tab相关

        private void InitDataGridEquipments()
        {
            SqlDataReader dr = App.DBHelper.returnReader("SELECT Equip_Index, Equip_Name, Type_Index, Type_Name FROM Equipments INNER JOIN EquipmentType ON Equip_TypeIndex = Type_Index");
            while (dr.Read())
            {
                Equip4DB equip = new Equip4DB(Convert.ToInt32(dr["Equip_Index"]), dr["Equip_Name"] as string);
                equip.TypeIndex = Convert.ToInt32(dr["Type_Index"]);
                equip.TypeName = dr["Type_Name"] as string;
                equipList.Add(equip);
            }
            dr.Close();
            dgcEquipType.ItemsSource = typeList;
            dgEquip.ItemsSource = equipList;
            dgcEquipName.ItemsSource = equipNameList;
            cbbAddEquipType.ItemsSource = typeList;
            cbbAddIPEquipName.ItemsSource = equipNameList;
            
           // dgEquip.SelectedItem
        }

        private void InitDataGridIPinfo()
        {
            string sql = "SELECT IP_Index, IP_Address, IP_EquipID, IP_Mask, IP_GateWay, IP_Name, IP_IsDefaultIP, Equip_Name FROM IPAddress INNER JOIN Equipments ON IP_EquipID = Equip_Index";
            SqlDataReader dr = App.DBHelper.returnReader(sql);
            if (dr.HasRows)
            {
                ipInformationList.Clear();
                while (dr.Read())
                {
                    string ipAddr = dr["IP_Address"].ToString();
                    string ipMask = dr["IP_Mask"].ToString();
                    string ipGateway = dr["IP_GateWay"].ToString();
                    IpAddress ip = new IpAddress(ipAddr);
                    IpAddress mask = string.IsNullOrEmpty(ipMask)?null:new IpAddress(ipMask);
                    IpAddress gateway = string.IsNullOrEmpty(ipGateway) ? null : new IpAddress(ipGateway);
                    bool isDefault = bool.Parse(dr["IP_IsDefaultIP"].ToString());
                    int equipID = Convert.ToInt32(dr["IP_EquipID"]);
                    IPInfor4DB ipInfo = new IPInfor4DB(ip, equipID, dr["Equip_Name"].ToString(), mask, gateway, dr["IP_Name"].ToString(), isDefault);
                    ipInfo.IpIndex = Convert.ToInt32(dr["IP_Index"]);
                    ipInformationList.Add(ipInfo);
                }
                dr.Close();
            }
            dgIPInfo.ItemsSource = ipInformationList;
        }

        private void InitTypeList()
        {
            typeList.Clear();
            SqlDataReader drType = App.DBHelper.returnReader("SELECT Type_Index,Type_Name FROM EquipmentType ORDER BY Type_Order");
            while (drType.Read())
            {
                string type = drType["Type_Name"] as string;
                typeList.Add(type);
            }
            drType.Close();
        }

        private void InitEquipNameList()
        {
            equipNameList.Clear();
            SqlDataReader drEquipName = App.DBHelper.returnReader("SELECT Equip_Name FROM Equipments");
            while (drEquipName.Read())
            {
                string type = drEquipName["Equip_Name"] as string;
                equipNameList.Add(type);
            }
            drEquipName.Close();
        }

        private void dgEquip_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgEquip.SelectedItem != null)
            {
                Equip4DB selectedItem = dgEquip.SelectedItem as Equip4DB;
                if (selectedItem != null)
                {
                    string equipName = (dgEquip.SelectedItem as Equip4DB).Name;
                    int n = dgIPInfo.Items.Count;
                    for (int i = 0; i < n; i++)
                    {
                        DataGridRow row = dgIPInfo.ItemContainerGenerator.ContainerFromIndex(i) as DataGridRow;
                        if ((dgIPInfo.Items[i] as IPInfor4DB).EquipName.Equals(equipName))
                        {
                            row.Visibility = Visibility.Visible;
                        }
                        else
                            row.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        private void btnAllIP_Click(object sender, RoutedEventArgs e)
        {
            int n = dgIPInfo.Items.Count;
            for (int i = 0; i < n; i++)
            {
                DataGridRow row = dgIPInfo.ItemContainerGenerator.ContainerFromIndex(i) as DataGridRow;
                row.Visibility = Visibility.Visible;
            }
        }
        
        /// <summary>
        /// 添加设备，在equipNameList、数据库、equipList中同时添加
        /// </summary>
        private void btnAddEquip_Click(object sender, RoutedEventArgs e)
        {
            string equipName = tbAddEquipName.Text;
            string equipType = cbbAddEquipType.Text;
            if (equipNameList.Contains(equipName))
            {
                MessageBox.Show("已经有这个设备了");
                return;
            }
            if (string.IsNullOrEmpty(equipName))
            {
                MessageBox.Show("请输入设备名称");
                return;
            }
            if (equipType.Equals("选择类型"))
            {
                MessageBox.Show("请选择设备类型");
                return;
            }
            int typeIndex = Convert.ToInt32(App.DBHelper.returnScalar("SELECT Type_Index FROM EquipmentType WHERE Type_Name = '" + equipType + "'"));
            string insertSql = string.Format("INSERT INTO Equipments(Equip_Name, Equip_TypeIndex) values('{0}',{1})", equipName, typeIndex);
            if (App.DBHelper.ExecuteReturnBool(insertSql))
            {
                equipNameList.Add(equipName);
                int equipIndex = Convert.ToInt32(App.DBHelper.returnScalar("SELECT Equip_Index FROM Equipments WHERE Equip_Name = '" + equipName + "'"));
                Equip4DB equip = new Equip4DB(equipIndex, equipName);
                equip.TypeIndex = typeIndex;
                equip.TypeName = equipType;
                equipList.Add(equip);
            }
        }

        private void btnDelEquip_Click(object sender, RoutedEventArgs e)
        {
            var fdf = equipList;
            Equip4DB equip = dgEquip.SelectedItem as Equip4DB;
            if (equip == null)
            {
                MessageBox.Show("请选择一个设备");
                return;
            }
            string name = equip.Name;
            if (MessageBox.Show("同时将删除" + name + "相关的IP地址，继续么？", "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                string delIPSql = string.Format("delete from IPAddress where IP_EquipID = {0}", equip.Index);
                string delEquipSql = string.Format("delete from Equipments where Equip_Index = {0}", equip.Index);
                if (App.DBHelper.ExecuteTransaction(new List<string>() { delIPSql, delEquipSql }))
                {
                    App.InitInformation();  //不初始化的话，如果删除掉一个设备，同时拓扑等程序打开的话，会调用
                    if (RefreshEvent != null)
                        RefreshEvent(null, null);
                }
                else
                    MessageBox.Show("删除失败");
            }
        }

        private void btnAddIP_Click(object sender, RoutedEventArgs e)
        {
            string strIpAddr = tbAddIP.Text.Trim();
            IpAddress ipAddr, ipMask, ipGateWay;
            string strIpMask = tbAddMask.Text.Trim();
            string strIpGateWay = tbAddGateway.Text.Trim();
            string ipName = tbAddIPName.Text.Trim();
            string ipEquipName = cbbAddIPEquipName.Text;
            if (ipEquipName.Equals("选择设备"))
            {
                MessageBox.Show("请选择一个设备");
                return;
            }
            if (!IpAddress.IsIP(strIpAddr))
            {
                MessageBox.Show("地址格式不正确");
                return;
            }
            ipAddr = new IpAddress(strIpAddr);
            if (string.IsNullOrEmpty(strIpMask))
            {
                ipMask = null;
            }
            else
            {
                if (!IpAddress.IsIP(strIpMask))
                {
                    MessageBox.Show("掩码应是一个IP地址格式");
                    return;
                }
                ipMask = new IpAddress(strIpMask);
                if (!ipMask.IsValidMask())
                {
                    MessageBox.Show("掩码格式不正确");
                    return;
                }
            }
            if (string.IsNullOrEmpty(strIpGateWay))
            {
                ipGateWay = null;
            }
            else
            {
                if (!IpAddress.IsIP(strIpGateWay))
                {
                    MessageBox.Show("网关格式错误");
                    return;
                }
                ipGateWay = new IpAddress(strIpGateWay);
            }
            string getIDSql = "SELECT Equip_Index FROM Equipments WHERE Equip_Name = '" + ipEquipName + "'";
            int equipID = Convert.ToInt32(App.DBHelper.returnScalar(getIDSql));
            if (equipID <= 0)
            {
                MessageBox.Show("数据库中未找到相应的设备号");
                return;
            }
            string insertSql = string.Format("INSERT INTO IPAddress(IP_Address, IP_EquipID, IP_Mask, IP_GateWay, IP_Name) VALUES('{0}',{1},'{2}','{3}','{4}')", strIpAddr, equipID, strIpMask, strIpGateWay, ipName);
            if (!App.DBHelper.ExecuteReturnBool(insertSql))
            {
                MessageBox.Show("插入IP失败");
                return;
            }
            IPInfor4DB ipInfo = new IPInfor4DB(ipAddr, equipID, ipEquipName, ipMask, ipGateWay, ipName, false);
            string getipIDSql = string.Format("SELECT IP_Index FROM IPAddress WHERE  IP_EquipID = {0} AND IP_Address = '{1}'", equipID, strIpAddr);
            ipInfo.IpIndex = Convert.ToInt32(App.DBHelper.returnScalar(getipIDSql));
            ipInformationList.Add(ipInfo);
            tbAddIP.Text = "";
            tbAddIPName.Text = "";
            tbAddMask.Text = "";
            tbAddGateway.Text = "";
        }

        private void btnDelIP_Click(object sender, RoutedEventArgs e)
        {
            int count = dgIPInfo.SelectedItems.Count;
            if (count == 0)
            {
                MessageBox.Show("请在表格中选择需要删除的IP地址");
                return;
            }
            for (int i = count - 1; i >= 0; i--)
            {
                App.DBHelper.ExecuteReturnBool(string.Format("delete from IPAddress where IP_Index = {0}", (dgIPInfo.SelectedItems[i] as IPInfor4DB).IpIndex));
                ipInformationList.Remove(dgIPInfo.SelectedItems[i] as IPInfor4DB);
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (RefreshEvent != null)
                RefreshEvent(null, null);
        }
        #endregion

#region  SNMP设置tab相关

        private void InitSNMPConfig()
        {
            tbSNMPCommunity.Text = Properties.Settings.Default["SNMP_Community"].ToString();
            tbSNMPPort.Text = Properties.Settings.Default["SNMP_Port"].ToString();
            tbSNMPTrapPort.Text = Properties.Settings.Default["SNMP_Trap_Port"].ToString();
            tbSNMPTimeout.Text = Properties.Settings.Default["SNMP_Timeout"].ToString();
            tbSNMPRetry.Text = Properties.Settings.Default["SNMP_Retry"].ToString();
            cbSNMPSourceCheckFlag.IsChecked = (bool)Properties.Settings.Default["SNMP_CheckSrcFlag"];
        }

        private void btnSNMPSave_Click(object sender, RoutedEventArgs e)
        {
            tbWindowBorderColor.Text = Properties.Settings.Default["WindowBannerColor"].ToString();
            string community;
            int snmpPort, snmpTrapPort, snmpTimeout, snmpRetry;
            bool snmpCheckSrcFlag;
            try
            {
                community = tbSNMPCommunity.Text.Trim();
                snmpPort = Convert.ToInt32(tbSNMPPort.Text.Trim());
                snmpTrapPort = Convert.ToInt32(tbSNMPTrapPort.Text.Trim());
                snmpTimeout = Convert.ToInt32(tbSNMPTimeout.Text.Trim());
                snmpRetry = Convert.ToInt32(tbSNMPRetry.Text.Trim());
                snmpCheckSrcFlag = (bool)cbSNMPSourceCheckFlag.IsChecked;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("输入格式不正确\n" + ex.Message);
                return;
            }
            if (snmpPort < 1 || snmpPort > 65535 || snmpTrapPort < 1 || snmpTrapPort > 65535)
            {
                MessageBox.Show("端口号需是1到65535之间的整数");
                return;
            }
            if (community.Length < 1)
            {
                MessageBox.Show("请填写共同体名");
                return;
            }
            if (snmpTimeout < 1 || snmpRetry < 0)
            {
                MessageBox.Show("超时或重试次数不正确");
                return;
            }
            Properties.Settings.Default["SNMP_Community"] = community;
            Properties.Settings.Default["SNMP_Port"] = snmpPort;
            Properties.Settings.Default["SNMP_Trap_Port"] = snmpTrapPort;
            Properties.Settings.Default["SNMP_Timeout"] = snmpTimeout;
            Properties.Settings.Default["SNMP_Retry"] = snmpRetry;
            Properties.Settings.Default["SNMP_CheckSrcFlag"] = snmpCheckSrcFlag;
            Properties.Settings.Default.Save();
            MessageBox.Show("保存成功");
        }



#endregion











        public void SetTabIDtoShow(int i)
        {
            tab.SelectedIndex = i;
        }


    }
}
