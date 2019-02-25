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
using System.Data.SqlClient;
using NCMMS.CommonClass;
using System.Net;
using System.Threading;

namespace NCMMS.UC
{
    /// <summary>
    /// UCIPSelectorFormEquip.xaml 的交互逻辑
    /// </summary>
    public partial class UCIPSelectorFormEquip : UserControl
    {
        List<Equipment> equipmentList = new List<Equipment>();
        public string SelectedIP = "";

        public UCIPSelectorFormEquip()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(UCIPSelectorFormEquip_Loaded);
        }

        void UCIPSelectorFormEquip_Loaded(object sender, RoutedEventArgs e)
        {
            if (App.databaseConState == true)
                ThreadPool.QueueUserWorkItem(new WaitCallback(InitComboList));
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
                        coBoxEquips.ItemsSource = equipmentList;
                        coBoxEquips.DisplayMemberPath = "Name";
                    }));
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("读取数据库出现错误\n" + e.Message);
            }
        }


        private void coBoxEquips_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            List<string> ipList = new List<string>();
            int equipID = (coBoxEquips.SelectedItem as Equipment).Index;
            SqlDataReader dr = App.DBHelper.returnReader("SELECT IP_Address FROM IPAddress WHERE(IP_EquipID = " + equipID + ")");
            while (dr.Read())
            {
                ipList.Add(dr["IP_Address"].ToString());
            }
            dr.Close();
            coBoxIPs.ItemsSource = ipList;
            if (coBoxIPs.Items.Count > 0)
                coBoxIPs.SelectedIndex = 0;
        }

        private void coBoxIPs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (coBoxIPs.SelectedItem != null)
            {
                SelectedIP = coBoxIPs.SelectedValue.ToString();
            }
            else
            {
                SelectedIP = "";
            }

        }



    }
}
