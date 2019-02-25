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
using System.Windows.Controls.Primitives;
using NCMMS.CommonClass;
using NCMMS.Topology;
using System.Diagnostics;
using System.Threading;

namespace NCMMS.UC
{
    /// <summary>
    /// UCEquipIcon.xaml 的交互逻辑
    /// </summary>
    public partial class UCEquipIcon : UserControl
    {
        public delegate void DeleteEventHandler(object sender,EventArgs e);
        public event DeleteEventHandler DeleteEvent;

        #region 左下角厂商图标 BrandName
        public Brand BrandName
        {
            get { return (Brand)GetValue(BrandNameProperty); }
            set { SetValue(BrandNameProperty, value); }
        }
        public static readonly DependencyProperty BrandNameProperty =
            DependencyProperty.Register("BrandName", typeof(Brand), typeof(UCEquipIcon), new UIPropertyMetadata(Brand.Other, new PropertyChangedCallback(OnBrandNameChanged)));

        private static void OnBrandNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //当这个依赖属性是绑定到另一个值上的时候，值的变化不触发普通属性中的set
            UCEquipIcon me = d as UCEquipIcon;
            Brand brand = (Brand)e.NewValue;
            BitmapImage pic = new BitmapImage();
            pic.BeginInit();
            if (brand.Equals(Brand.Huawei))
                pic.UriSource = new Uri("/Images/huawei1.png", UriKind.Relative);
            else if (brand.Equals(Brand.Cisco))
                pic.UriSource = new Uri("/Images/cisco1.png", UriKind.Relative);
            else if (brand.Equals(Brand.H3Com))
                pic.UriSource = new Uri("/Images/H3C.png", UriKind.Relative);
            else if (brand.Equals(Brand.MicroSoft))
                pic.UriSource = new Uri("/Images/Microsoft64X64.png", UriKind.Relative);
            else
                pic.UriSource = new Uri("/Images/blank1X1.png", UriKind.Relative);
            pic.EndInit();
            me.brandImage.Source = pic;
        }
        #endregion

        #region 背景设备图标 EquipmentType
        public EquipType EquipmentType
        {
            get { return (EquipType)GetValue(EquipmentTypeProperty); }
            set { SetValue(EquipmentTypeProperty, value); }
        }
        public static readonly DependencyProperty EquipmentTypeProperty =
            DependencyProperty.Register("EquipmentType", typeof(EquipType), typeof(UCEquipIcon), new UIPropertyMetadata(EquipType.Other, new PropertyChangedCallback(OnEquipmentTypeChanged)));

        private static void OnEquipmentTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UCEquipIcon me = d as UCEquipIcon;
            EquipType equipType = (EquipType)e.NewValue;
            BitmapImage pic = new BitmapImage();
            pic.BeginInit();
            if (equipType == EquipType.FireWall)
            {
                pic.UriSource = new Uri("/Images/firewall.png", UriKind.Relative);
            }
            else if (equipType == EquipType.Hub)
            {
                pic.UriSource = new Uri("/Images/hub.png", UriKind.Relative);
            }
            else if (equipType == EquipType.Layer2Switch)
            {
                pic.UriSource = new Uri("/Images/layer2Switch.png", UriKind.Relative);
            }
            else if (equipType == EquipType.Layer3Switch)
            {
                pic.UriSource = new Uri("/Images/layer3Switch.png", UriKind.Relative);
            }
            else if (equipType == EquipType.PC)
            {
                pic.UriSource = new Uri("/Images/pc.png", UriKind.Relative);
            }
            else if (equipType == EquipType.Router)
            {
                pic.UriSource = new Uri("/Images/router.png", UriKind.Relative);
            }
            else if (equipType == EquipType.Server)
            {
                pic.UriSource = new Uri("/Images/server.png", UriKind.Relative);
            }
            else if (equipType == EquipType.ServerInTable)
            {
                pic.UriSource = new Uri("/Images/serverInTable.png", UriKind.Relative);
            }
            else if (equipType == EquipType.Other)
            {
                pic.UriSource = new Uri("/Images/router.png", UriKind.Relative);
            }
            pic.EndInit();
            me.equipImage.Source = pic;
        }
        #endregion

        public double CenterPointX
        {
            get { return (double)GetValue(CenterPointXProperty); }
            set { SetValue(CenterPointXProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CenterPointX.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CenterPointXProperty =
            DependencyProperty.Register("CenterPointX", typeof(double), typeof(UCEquipIcon));

        public double CenterPointY
        {
            get { return (double)GetValue(CenterPointYProperty); }
            set { SetValue(CenterPointYProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CenterPointY.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CenterPointYProperty =
            DependencyProperty.Register("CenterPointY", typeof(double), typeof(UCEquipIcon));

        bool isSelected;

        public bool IsSelected
        {
            get { return isSelected; }
            set 
            {
                border.BorderBrush = value ? Brushes.Black : null;
                isSelected = value;
            }
        }


        Equipment equip;
        /// <summary>
        /// 和这个设备图标关联的设备类
        /// </summary>
        public Equipment Equip
        {
            get { return equip; }
            set { equip = value; }
        }

        public UCEquipIcon()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(UCEquipIcon_Loaded);
        }

        void UCEquipIcon_Loaded(object sender, RoutedEventArgs e)
        {
            this.tbName.SetBinding(TextBlock.TextProperty, new Binding() { Path = new PropertyPath("Name"), Source = equip });
            this.SetBinding(UCEquipIcon.EquipmentTypeProperty, new Binding() { Path = new PropertyPath("Type"), Source = equip });
            this.SetBinding(UCEquipIcon.BrandNameProperty, new Binding() { Path = new PropertyPath("EquipBrand"), Source = equip });
        }

        private void ContextMenu_ReName(object sender, RoutedEventArgs e)
        {
            ModifyEquipName modify = new ModifyEquipName(equip);
            modify.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            modify.ShowDialog();
        }

        private void ContextMenu_ShowEquipInfo(object sender, RoutedEventArgs e)
        {
            if (equip == null)
                return;
            Thread t = new Thread(new ThreadStart(() =>
            {
                ShowEquipInfo sei = new ShowEquipInfo(equip);
                sei.ShowDialog();
            }));
            t.SetApartmentState(ApartmentState.STA);
            t.Name = equip.Name + "属性修改线程";
            t.Start();
        }
        
        private void ContextMenu_Ping(object sender, RoutedEventArgs e)
        {
            if (equip != null)
            {
                ProcessStartInfo psi = new ProcessStartInfo("Cmd");
                psi.RedirectStandardInput = true;
                psi.RedirectStandardOutput = true;
                psi.UseShellExecute = false;
                Process proPing = Process.Start(psi);
                proPing.StandardInput.WriteLine("cd\\");
            }
        }
        
        private void ContextMenu_Telnet(object sender, RoutedEventArgs e)
        {
            if (equip != null)
            {
                
            }
        }

        private void ContextMenu_Delete(object sender, RoutedEventArgs e)
        {
            if (DeleteEvent!=null)
                DeleteEvent(this, e);
        }
        
    }
}
