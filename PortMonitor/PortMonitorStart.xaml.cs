using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Threading;
using System.Data.SqlClient;
using NCMMS.CommonClass;
using SnmpSharpNet;
using System.Net;
using System.Collections.ObjectModel;
using Swordfish.NET.Collections;
using System.Xml;

namespace NCMMS.PortMonitor
{
    /// <summary>
    /// PortMonitorStart.xaml 的交互逻辑
    /// </summary>
    public partial class PortMonitorStart : UserControl
    {
        List<Equipment> equipmentList = new List<Equipment>();
        ObservableDictionary<Integer32, Interface> ocInterfaces = new ObservableDictionary<Integer32, Interface>();
        ObservableCollection<Interface> selectedIFs = new ObservableCollection<Interface>();

        public PortMonitorStart()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(PortMonitorStart_Loaded);
        }

        void PortMonitorStart_Loaded(object sender, RoutedEventArgs e)
        {
            ifListByEquip.ItemsSource = ocInterfaces;
            ifSelectedList.ItemsSource = selectedIFs;
            if (App.databaseConState == true)
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
                MessageBox.Show("读取数据库出现错误\n" + e.Message);
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
            catch(Exception ex)
            {
                MessageBox.Show("读取数据库出现错误\n" + ex.Message);
                return;
            }
            ConstructListByIP(agentIP, equipName);
        }

        private void btnSearchIP_Click(object sender, RoutedEventArgs e)
        {
            if (tbSearchIP.IP == null)
            {
                MessageBox.Show("不是有效的IP地址");
                return;
            }
            else
            {
                IpAddress agentIP = new IpAddress(tbSearchIP.IP);
                ConstructListByIP(agentIP,null);
            }
        }

        private void ConstructListByIP(IpAddress agentIP,string equipName)
        {
            if (string.IsNullOrEmpty(equipName))
            {
                try
                {
                    equipName = App.idAndEquipList[App.ipAndIPinfoList[agentIP.ToString()].EquipIndex].Name;
                }
                catch { }
            }
            ocInterfaces.Clear();
            OctetString community = new OctetString(App.snmpCommunity);
            AgentParameters param = new AgentParameters(community);
            param.DisableReplySourceCheck = !App.snmpCheckSrcFlag;
            // Set SNMP version to 2 (GET-BULK only works with SNMP ver 2 and 3)
            param.Version = SnmpVersion.Ver2;
            // Construct target

            UdpTarget target = new UdpTarget((IPAddress)agentIP, App.snmpPort, App.snmpTimeout, App.snmpRetry);

            // Define Oid that is the root of the MIB
            //  tree you wish to retrieve
            Oid rootOid = new Oid("1.3.6.1.2.1.2.2.1.1"); // ifIndex

            // This Oid represents last Oid returned by
            //  the SNMP agent
            Oid lastOid = (Oid)rootOid.Clone();

            // Pdu class used for all requests
            Pdu pdu = new Pdu(PduType.GetBulk);

            // In this example, set NonRepeaters value to 0
            pdu.NonRepeaters = 0;
            // MaxRepetitions tells the agent how many Oid/Value pairs to return
            // in the response.
            pdu.MaxRepetitions = 5;
            
            // Loop through results
            while (lastOid != null)
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
                pdu.VbList.Add(lastOid);
                // Make SNMP request
                SnmpV2Packet result = null;
                try
                {
                    result = (SnmpV2Packet)target.Request(pdu, param);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("获取SNMP应答出现错误\n" + ex.Message);
                    target.Close();
                    return;
                }
                // You should catch exceptions in the Request if using in real application.

                // If result is null then agent didn't reply or we couldn't parse the reply.
                if (result != null)
                {
                    // ErrorStatus other then 0 is an error returned by 
                    // the Agent - see SnmpConstants for error definitions
                    if (result.Pdu.ErrorStatus != 0)
                    {
                        // agent reported an error with the request
                        MessageBox.Show(string.Format("SNMP应答数据包中有错误。 Error {0} index {1}", result.Pdu.ErrorStatus, result.Pdu.ErrorIndex));
                        lastOid = null;
                        break;
                    }
                    else
                    {
                        // Walk through returned variable bindings
                        foreach (Vb v in result.Pdu.VbList)
                        {
                            // Check that retrieved Oid is "child" of the root OID
                            if (rootOid.IsRootOf(v.Oid))
                            {
                                if (v.Value.Type == SnmpConstants.SMI_ENDOFMIBVIEW)
                                    lastOid = null;
                                else
                                    lastOid = v.Oid;
                                Integer32 f = v.Value as Integer32;
                                Interface intf = new Interface(agentIP);
                                intf.EquipName = equipName;
                                //intf.TimerInteral = double.Parse(cbbTimerInterval.Text);
                                ocInterfaces.Add(f, intf);
                            }
                            else
                            {
                                // we have reached the end of the requested
                                // MIB tree. Set lastOid to null and exit loop
                                lastOid = null;
                                break; // 每个数据包获取5个值，一旦有一个不是这一列的数据，后面的应该都不是了
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show("指定网管代理未返回有效信息");
                }
            }
            target.Close();
            string errorMessage;
            foreach (Integer32 i in ocInterfaces.Keys)
            {
                string strOid = "1.3.6.1.2.1.2.2.1.2." + i.ToString();
                VbCollection vbc = SnmpHelper.GetResultsFromOids(agentIP,new string[] { strOid },out errorMessage);
                ocInterfaces[i].Descr = vbc[0].Value.ToString();
                ocInterfaces[i].IfIndex = i;
            }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            foreach (KeyValuePair<Integer32, Interface> intf in ifListByEquip.SelectedItems)
            {
                double interval = double.Parse(cbbTimerInterval.Text);
                Interface i = intf.Value;
                if (!selectedIFs.Contains(i))
                {
                    i.TimerInteral = interval;
                    selectedIFs.Add(i);
                }
            }
        }
        private void TextBlock_MouseLeftButtonDown_Left(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                double interval = double.Parse(cbbTimerInterval.Text);
                Interface i = ((KeyValuePair<Integer32, Interface>)ifListByEquip.SelectedItem).Value;
                if (!selectedIFs.Contains(i))
                {
                    i.TimerInteral = interval;
                    selectedIFs.Add(i);
                }
            }
        }

        private void TextBlock_MouseLeftButtonDown_Right(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                Interface i = ifSelectedList.SelectedItem as Interface;
                if (selectedIFs.Contains(i))
                    selectedIFs.Remove(i);
            }
        }

        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            List<Interface> removeList = new List<Interface>();
            foreach (Interface item in ifSelectedList.SelectedItems)
                removeList.Add(item);
            foreach (Interface intf in removeList)
            {
                if (selectedIFs.Contains(intf))
                    selectedIFs.Remove(intf);
            }
        }

        private void btnAddAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (KeyValuePair<Integer32, Interface> intf in ifListByEquip.Items)
            {
                double interval = double.Parse(cbbTimerInterval.Text);
                Interface i = intf.Value;
                if (!selectedIFs.Contains(i))
                {
                    i.TimerInteral = interval;
                    selectedIFs.Add(i);
                }
            }
        }

        private void btnRemoveAll_Click(object sender, RoutedEventArgs e)
        {
            selectedIFs.Clear();
        }

        private void cbStartWithFile_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)cbStartWithFile.IsChecked)
                ucFileBrowse.Visibility = Visibility.Visible;
            else
                ucFileBrowse.Visibility = Visibility.Collapsed;            
        }

        private void btnStartMonitor_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)cbStartWithFile.IsChecked)
            {
                //读取xml保存的interface列表来生成监视
                string fileFullName = ucFileBrowse.FileUrl;
                XmlDocument myXml = new XmlDocument();
                myXml.Load(fileFullName);
                XmlNode rootNode = myXml.DocumentElement;
                ObservableCollection<Interface> ifList = new ObservableCollection<Interface>();
                foreach (XmlNode nodeIf in rootNode.ChildNodes)
                {
                    int ifIndex = int.Parse(nodeIf.ChildNodes.Item(0).InnerText);
                    string ifDescr = nodeIf.ChildNodes.Item(1).InnerText;
                    IpAddress ip = new IpAddress(nodeIf.ChildNodes.Item(2).InnerText);
                    double timerInteral = double.Parse(nodeIf.ChildNodes.Item(3).InnerText);
                    bool isShowSpeedAlarm = nodeIf.ChildNodes.Item(4).InnerText.Equals("True") ? true : false;
                    Interface i = new Interface(ip);
                    i.IfIndex = ifIndex;
                    i.Descr = ifDescr;
                    i.IP = ip;
                    i.TimerInteral = timerInteral;
                    i.IsShowSpeedAlarm = isShowSpeedAlarm;
                    if (isShowSpeedAlarm)
                    {
                        double maxOutSpeed = double.Parse(nodeIf.ChildNodes.Item(5).InnerText);
                        double maxInSpeed = double.Parse(nodeIf.ChildNodes.Item(6).InnerText);
                        i.MaxInSpeed = maxInSpeed;
                        i.MaxOutSpeed = maxOutSpeed;
                    }
                    ifList.Add(i);
                }
                Thread threadPortMonitorPage = new Thread(() =>
                {
                    PortMonitor portMonitor = new PortMonitor(ifList);
                    portMonitor.Show();
                });
                threadPortMonitorPage.SetApartmentState(ApartmentState.STA);
                threadPortMonitorPage.IsBackground = true;
                threadPortMonitorPage.Start();
            }
            else
            {
                //使用listbox中的列表来打开
                if (selectedIFs.Count < 1)
                {
                    MessageBox.Show("至少要选择一个接口！");
                    return;
                }
                ObservableCollection<Interface> ifList = new ObservableCollection<Interface>(selectedIFs);
                Thread threadPortMonitorPage = new Thread(()=>
                {
                    PortMonitor portMonitor = new PortMonitor(ifList);
                    portMonitor.ShowDialog();
                });
                threadPortMonitorPage.SetApartmentState(ApartmentState.STA);
                threadPortMonitorPage.IsBackground = true;
                threadPortMonitorPage.Start();
                selectedIFs.Clear();
            }
        }

        private void cbbTimerInterval_MouseLeave(object sender, MouseEventArgs e)
        {
            int interval;
            bool isOK = int.TryParse(cbbTimerInterval.Text, out interval);
            if (!isOK)
            {
                MessageBox.Show("输入不合法，应为正整数");
            }
            if (selectedIFs.Count > 0 && isOK)
            {
                foreach (Interface inf in selectedIFs)
                    inf.TimerInteral = interval;
            }
        }

        private void cbbTimerInterval_KeyDown(object sender, KeyEventArgs e)
        {
            if (!((e.Key <= Key.D9 && e.Key >= Key.D0) || (e.Key <= Key.NumPad9 && e.Key >= Key.NumPad0)))
            {
                e.Handled = true;
            }
        }




    }
}
