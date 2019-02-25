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

namespace NCMMS.Topology
{
    /// <summary>
    /// ModifyEquipName.xaml 的交互逻辑
    /// </summary>
    public partial class ModifyEquipName : Window
    {
        Equipment equip;
        public ModifyEquipName(Equipment _e)
        {
            InitializeComponent();
            equip = _e;
            if (equip != null)
                textBox.Text = equip.Name;
            else
                textBox.Text = "没有给设备图标控件赋值设备类";
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            equip.Name = textBox.Text;
            this.Close();
        }
    }
}
