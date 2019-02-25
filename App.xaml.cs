using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using NCMMS.CommonClass;
using System.Threading;
using System.Data.SqlClient;
using SnmpSharpNet;
using System.Diagnostics;

namespace NCMMS
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        public static Dictionary<string, IPInformation> ipAndIPinfoList = new Dictionary<string, IPInformation>();
        public static Dictionary<int, Equipment> idAndEquipList = new Dictionary<int, Equipment>();
        public static bool multiPingIsPlayAlarm = false;

        #region snmp相关参数
        public static string snmpCommunity;
        public static int snmpPort, snmpTrapPort, snmpTimeout, snmpRetry;
        public static bool snmpCheckSrcFlag;
        #endregion

        /// <summary>
        ///判断数据库连接是否正常
        /// </summary>
        public static bool? databaseConState = false; 
        private static DBHelp dbHelper = new DBHelp();
        //public MyWindow thisWindow = null;
        public static DBHelp DBHelper
        {
            get { return dbHelper; }
        }
        App()
        {

            snmpCommunity = NCMMS.Properties.Settings.Default["SNMP_Community"].ToString();
            snmpPort = (int)NCMMS.Properties.Settings.Default["SNMP_Port"];
            snmpTrapPort = (int)NCMMS.Properties.Settings.Default["SNMP_Trap_Port"];
            snmpTimeout = (int)NCMMS.Properties.Settings.Default["SNMP_Timeout"];
            snmpRetry = (int)NCMMS.Properties.Settings.Default["SNMP_Retry"];
            snmpCheckSrcFlag = (bool)NCMMS.Properties.Settings.Default["SNMP_CheckSrcFlag"];

            //在这里取出数据库中的ip地址和注释等数据，存储为一个静态的dictionary，放在以后调用。用dictionary还是类取决于数据是否复杂
            Thread t = new Thread(InitInformation);
            t.Name = "应用程序初始化获取数据库数据线程";
            t.Start();

            //ProcessStartInfo psi = new ProcessStartInfo("Cmd");
            //psi.UseShellExecute = false;
            //psi.RedirectStandardInput = true;
            //psi.RedirectStandardOutput = true;
            //Process proPing = Process.Start(psi);
            //Console.WriteLine("ipconfig");
        }

        public static void InitInformation()
        {
            if (dbHelper.Test())
            {
                InitIPAndIPinfoList();
                InitIDAndEquipList();
            }
            else
            {
                MessageBox.Show("数据库无法连接，一些与数据库有关的功能无法实现","提示");
            }
        }

        //InitIPAndNameList
        private static void InitIPAndIPinfoList()
        {
            ipAndIPinfoList.Clear();
            try
            {
                SqlDataReader dr = App.DBHelper.returnReader("SELECT IP_Index, IP_Address, IP_EquipID, IP_Mask, IP_GateWay, IP_Name, IP_IsDefaultIP FROM IPAddress");
                while (dr.Read())
                {
                    //ipAndIPinfoList.Add(dr[0] as string, dr[1] as string);
                    string strIP = dr["IP_Address"].ToString();
                    IpAddress mask = string.IsNullOrEmpty(dr["IP_Mask"].ToString()) ? null : new IpAddress(dr["IP_Mask"].ToString());
                    IpAddress gateway = string.IsNullOrEmpty(dr["IP_GateWay"].ToString()) ? null : new IpAddress(dr["IP_Mask"].ToString());
                    IPInformation info = new IPInformation(new IpAddress(strIP), Convert.ToInt32(dr["IP_EquipID"]), "", mask, gateway, dr["IP_Name"].ToString(), Convert.ToBoolean(dr["IP_IsDefaultIP"]));
                    info.IpIndex = Convert.ToInt32(dr["IP_Index"]);
                    ipAndIPinfoList.Add(strIP, info);
                }
                dr.Close();
            }
            catch (Exception e)
            {
                MessageBox.Show("系统启动时初始化IP地址与IP信息列表出错\n" + e.Message);
            }
        }

        private static void InitIDAndEquipList()
        {
            idAndEquipList.Clear();
            try
            {
                SqlDataReader dr = App.DBHelper.returnReader("SELECT Equip_Index, Equip_Name, Equip_TypeIndex, Equip_X, Equip_Y, Type_Name FROM Equipments INNER JOIN EquipmentType ON Equip_TypeIndex = Type_Index");
                while (dr.Read())
                {
                    int id = Convert.ToInt32(dr["Equip_Index"]);
                    Equipment equip = new Equipment(id, dr["Equip_Name"].ToString());
                    equip.TypeIndex = Convert.ToInt32(dr["Equip_TypeIndex"]);
                    equip.TypeName = dr["Type_Name"].ToString();
                    equip.X = Convert.ToInt32(dr["Equip_X"]);
                    equip.Y = Convert.ToInt32(dr["Equip_Y"]);
                    idAndEquipList.Add(id, equip);
                }
                dr.Close();
            }
            catch (Exception e)
            {
                MessageBox.Show("系统启动时初始化IP地址与IP信息列表出错\n" + e.Message);
            }
        }
    }
}
