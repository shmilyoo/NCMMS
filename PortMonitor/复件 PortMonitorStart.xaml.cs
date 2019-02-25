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
using System.Threading;
using System.Data.SqlClient;
using NCMMS.CommonClass;
using SnmpSharpNet;
using System.Net;
using System.Collections.ObjectModel;

namespace NCMMS.PortMonitor
{
    /// <summary>
    /// PortMonitorStart.xaml 的交互逻辑
    /// </summary>
    public partial class PortMonitorStart : UserControl
    {
        List<Equipment> equipmentList = new List<Equipment>();
        ObservableCollection<Interface> interfaces = new ObservableCollection<Interface>();
        ObservableCollection<Interface> selectedIFs = new ObservableCollection<Interface>();

        public PortMonitorStart()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(PortMonitorStart_Loaded);
        }

        void PortMonitorStart_Loaded(object sender, RoutedEventArgs e)
        {
            ifListByEquip.ItemsSource = interfaces;
            ifSelectedList.ItemsSource = selectedIFs;
            ThreadPool.QueueUserWorkItem(new WaitCallback(InitComboList));
            //ThreadPool.QueueUserWorkItem(new WaitCallback(填充注释列表));
        }

        private void InitComboList(Object stateInfo)
        {
            try
            {
                SqlDataReader dr = App.DBHelper.returnReader("SELECT Equip_Name, Equip_Index FROM Equipments");
                while (dr.Read())
                {
                    Equipment equip = new Equipment((int)dr["Equip_Index"], (string)dr["Equip_Name"]);
                    equipmentList.Add(equip);
                }
                dr.Close();
                if (equipmentList.Count > 0)
                {
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        cbbSelectEquip.ItemsSource = equipmentList;
                        cbbSelectEquip.DisplayMemberPath = "Name";
                    }));
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("读取数据库出现错误\n" + e.ToString());
            }
        }

        private void cbbSelectEquip_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Equipment equip = (sender as ComboBox).SelectedItem as Equipment;
            string equipName = equip.Name;
            int equipID = equip.Index;
            IpAddress agentIP = null;
            try
            {
                object strIP = App.DBHelper.returnScalar(string.Format("SELECT IP_Address FROM IPAddress WHERE(IP_EquipID = {0}) AND (IP_IsDefaultIP = 1)",equipID));
                if (strIP == null)
                {
                    MessageBox.Show("数据库中没有" + equipName + "的默认管理IP地址");
                    return;
                }
                if (IpAddress.IsIP(strIP.ToString()))
                {
                    agentIP = new IpAddress(strIP.ToString());
                }
                else
                {
                    MessageBox.Show("地址格式错误");
                    return;
                }
            }
            catch
            {
                MessageBox.Show("读取数据库出现错误\n" + e.ToString());
            }

            OctetString community = new OctetString("public");
            AgentParameters param = new AgentParameters(community);
            // Set SNMP version to 2 (GET-BULK only works with SNMP ver 2 and 3)
            param.Version = SnmpVersion.Ver2;
            // Construct target
            UdpTarget target = new UdpTarget((IPAddress)agentIP, 161, 2000, 1);

            // Define Oid that is the root of the MIB
            //  tree you wish to retrieve
            Oid rootOid1 = new Oid("1.3.6.1.2.1.2.2.1.1"); // ifIndex
            Oid rootOid2 = new Oid("1.3.6.1.2.1.2.2.1.2"); // ifDescr

            // This Oid represents last Oid returned by
            //  the SNMP agent
            Oid lastOid1 = (Oid)rootOid1.Clone();
            Oid lastOid2 = (Oid)rootOid2.Clone();

            // Pdu class used for all requests
            Pdu pdu = new Pdu(PduType.GetBulk);

            // In this example, set NonRepeaters value to 0
            pdu.NonRepeaters = 0;
            // MaxRepetitions tells the agent how many Oid/Value pairs to return
            // in the response.
            pdu.MaxRepetitions = 5;

            // Loop through results
            while (lastOid1 != null && lastOid2 != null)
            {
                // When Pdu class is first constructed, RequestId is set to 0
                // and during encoding id will be set to the random value
                // for subsequent requests, id will be set to a value that
                // needs to be incremented to have unique request ids for each
                // packet
                if (pdu.RequestId != 0)
                {
                    pdu.RequestId += 1;
                }
                // Clear Oids from the Pdu class.
                pdu.VbList.Clear();
                // Initialize request PDU with the last retrieved Oid
                pdu.VbList.Add(lastOid1);
                pdu.VbList.Add(lastOid2);
                // Make SNMP request
                SnmpV2Packet result = (SnmpV2Packet)target.Request(pdu, param);
                // You should catch exceptions in the Request if using in real application.

                // If result is null then agent didn't reply or we couldn't parse the reply.
                if (result != null)
                {
                    // ErrorStatus other then 0 is an error returned by 
                    // the Agent - see SnmpConstants for error definitions
                    if (result.Pdu.ErrorStatus != 0)
                    {
                        // agent reported an error with the request
                        Console.WriteLine("Error in SNMP reply. Error {0} index {1}",
                            result.Pdu.ErrorStatus,
                            result.Pdu.ErrorIndex);
                        MessageBox.Show(string.Format("SNMP应答数据包中有错误。 Error {0} index {1}", result.Pdu.ErrorStatus, result.Pdu.ErrorIndex));
                        lastOid1 = null;
                        lastOid2 = null;
                        break;
                    }
                    else
                    {
                        // Walk through returned variable bindings
                        foreach (Vb v in result.Pdu.VbList)
                        {
                            // Check that retrieved Oid is "child" of the root OID
                            if (rootOid1.IsRootOf(v.Oid))
                            {
                                if (v.Value.Type == SnmpConstants.SMI_ENDOFMIBVIEW)
                                    lastOid1 = null;
                                else
                                    lastOid1 = v.Oid;
                                var f = v.Value;
                                //Interface interface = new Interface(v.Value.,)
                            }
                            else if (rootOid2.IsRootOf(v.Oid))
                            {
                                if (v.Value.Type == SnmpConstants.SMI_ENDOFMIBVIEW)
                                    lastOid2 = null;
                                else
                                    lastOid2 = v.Oid;
                                var f = v.Value;
                            }
                            else
                            {
                                // we have reached the end of the requested
                                // MIB tree. Set lastOid to null and exit loop
                                lastOid1 = null;
                                lastOid2 = null;
                            }



//                             if (rootOid.IsRootOf(v.Oid))
//                             {
//                                 Console.WriteLine("{0} ({1}): {2}",
//                                     v.Oid.ToString(),
//                                     SnmpConstants.GetTypeName(v.Value.Type),
//                                     v.Value.ToString());
//                                 if (v.Value.Type == SnmpConstants.SMI_ENDOFMIBVIEW)
//                                     lastOid = null;
//                                 else
//                                     lastOid = v.Oid;
//                             }
//                             else
//                             {
//                                 // we have reached the end of the requested
//                                 // MIB tree. Set lastOid to null and exit loop
//                                 lastOid = null;
//                           }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("No response received from SNMP agent.");
                }
            }
            target.Close();
        }

    }
}
