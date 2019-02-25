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
using NCMMS.CommonClass;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using SnmpSharpNet;
using System.Threading;
using NCMMS.PortMonitor;
using System.Collections.Specialized;

namespace NCMMS.Topology
{
    /// <summary>
    /// ShowEquipInfo.xaml 的交互逻辑
    /// </summary>
    public partial class ShowEquipInfo : MyWindow
    {
        Equipment equip;
        string oldEquipName,errorMessage;
        ObservableCollection<string> typeList = new ObservableCollection<string>();
        public ShowEquipInfo(Equipment _equip)
        {
            InitializeComponent();
            equip = _equip;
            this.WindowTitle = equip.Name + "的相关信息";
            oldEquipName = equip.Name;
            if (equip.Index < 0)
                tbkIsInDB.Visibility = Visibility.Visible;
            this.Loaded += new RoutedEventHandler(ShowEquipInfo_Loaded);
            
        }

        void ShowEquipInfo_Loaded(object sender, RoutedEventArgs e)
        {
            tbEquipName.SetBinding(TextBox.TextProperty, new Binding { Path = new PropertyPath("Name"), Source = equip, Mode = BindingMode.TwoWay });
            if (App.databaseConState == true)
            {
                InitTypeList();
                cbbEquipType.ItemsSource = typeList;
            }
            else
                cbbEquipType.ItemsSource = SnmpHelper.TypeNames;
            cbbEquipType.SetBinding(ComboBox.SelectedItemProperty, new Binding { Path = new PropertyPath("TypeName"), Source = equip, Mode = BindingMode.TwoWay,UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            cbbEquipBrand.ItemsSource = SnmpHelper.BrandNames;
            cbbEquipBrand.SetBinding(ComboBox.SelectedItemProperty, new Binding { Path = new PropertyPath("BrandName"), Source = equip, Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            dgIPInfo.ItemsSource = equip.IPAndInfoList.Values.ToList<IPInformation>();
            dgRouteInfo.ItemsSource = equip.IpDstAndRouteInfoLIst.Values.ToList<RouteInfomation>();
            dgIFInfo.ItemsSource = equip.IfIDandIFinfoLIst.Values.ToList<IFInfomation>();
            equip.GetARPInfoListFrom(out errorMessage);
            dgARPInfo.ItemsSource = equip.ArpInfoList;
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

        private void btnSaveToDB_Click(object sender, RoutedEventArgs e)
        {
            Thread t = new Thread(UpdateDB);
            t.Name = "更新数据库线程";
            t.Start();
        }

        private void UpdateDB()
        {
            if (App.databaseConState != true)
            {
                MessageBox.Show("数据库连接异常，请检查数据库连接");
                return;
            }
            string name = null;
            string typeName = null;
            this.Dispatcher.Invoke(new Action(() => 
            {
                name = tbEquipName.Text.Trim(); 
                typeName = cbbEquipType.Text; 
            }));
            int index = equip.Index;
            foreach (Equipment eq in App.idAndEquipList.Values)
            {
                if (index< 0 && eq.Name.Equals(name))
                {
                    MessageBox.Show("数据库中已经有这个设备名称了");
                    return;
                }
                if (index > 0 && eq.Name.Equals(name) && !name.Equals(oldEquipName))
                {
                    MessageBox.Show("数据库中已经有这个设备名称了");
                    return;
                }
            }
            if (equip.SaveInformation(name, typeName))
                MessageBox.Show("保存成功");
            if (tbkIsInDB.Visibility == Visibility.Visible)
            {
                this.Dispatcher.Invoke(new Action(() =>
                {
                    tbkIsInDB.Visibility = Visibility.Hidden;
                }));
            }
            //如果用户停留在此页面，第二次更新数据库，若不更新oldname会触发数据库中有同名的检测
            oldEquipName = name;
        }

        private void btnStartPortMonitor_Click(object sender, RoutedEventArgs e)
        {
            if (dgIFInfo.SelectedItems.Count < 1)
            {
                MessageBox.Show("请至少选择一个接口");
                return;
            }
            ObservableCollection<Interface> ifList = new ObservableCollection<Interface>();
            foreach (var item in dgIFInfo.SelectedItems)
            {
                IFInfomation ifInfo = item as IFInfomation;
                Interface inf = new Interface(equip.AdminIPAddress);
                inf.IfIndex = ifInfo.IfIndex;
                inf.Descr = ifInfo.IfDescr;
                inf.TimerInteral = 1d;
                inf.MaxInSpeed = -1d;
                inf.MaxOutSpeed = -1d;
                inf.IsShowSpeedAlarm = false;
                ifList.Add(inf);
            }
            Thread t = new Thread(new ThreadStart(() =>
            {
                NCMMS.PortMonitor.PortMonitor portMonitor = new NCMMS.PortMonitor.PortMonitor(ifList);
                portMonitor.ShowDialog();
            }));
            t.SetApartmentState(ApartmentState.STA);
            t.IsBackground = true;
            t.Start();
        }

        private void RadioButton_Click(object sender, RoutedEventArgs e)
        {
            RadioButton rb = e.Source as RadioButton;
            if (rb.IsChecked == true)
            {
                IPInformation ipInfo = rb.DataContext as IPInformation;
                equip.AdminIPAddress = ipInfo.IP;
            } 
        }

        private void btnPing_Click(object sender, RoutedEventArgs e)
        {
            if (dgIPInfo.SelectedItems.Count < 1)
            {
                MessageBox.Show("请至少选择一个地址");
                return;
            }
            StringCollection ifList = new StringCollection();
            foreach (var item in dgIPInfo.SelectedItems)
                ifList.Add((item as IPInformation).StrIP);
            Thread t = new Thread(new ThreadStart(() =>
            {
                MultiPing.MultiPing multiPing = new MultiPing.MultiPing(ifList);
                multiPing.ShowDialog();
            }));
            t.SetApartmentState(ApartmentState.STA);
            t.IsBackground = true;
            t.Start();
        }

        private void btnRefreshARP_Click(object sender, RoutedEventArgs e)
        {
            equip.GetARPInfoListFrom(out errorMessage);
            dgARPInfo.ItemsSource = null;
            dgARPInfo.ItemsSource = equip.ArpInfoList;
        }
    }
}
