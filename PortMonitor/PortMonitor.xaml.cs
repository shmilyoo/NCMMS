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
using System.Collections.ObjectModel;
using Visifire.Charts;
using NCMMS.UC;
using System.Xml;

namespace NCMMS.PortMonitor
{
    /// <summary>
    /// PortMonitor.xaml 的交互逻辑
    /// </summary>
    public partial class PortMonitor : MyWindow
    {
        ObservableCollection<Interface> ifList;
        public PortMonitor(ObservableCollection<Interface> _IfList)
        {
            InitializeComponent();
            SetWindowSizeAndPosition();
            ifList = _IfList;
            foreach (Interface itf in ifList)
            {
                itf.uiDispatcher = this.Dispatcher;
            }
            this.Closing += new System.ComponentModel.CancelEventHandler(PortMonitor_Closing);
            listBox.ItemsSource = ifList; 
            AdjustIsShowLightOnTop();
        }

        private void SetWindowSizeAndPosition()
        {
            int w = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Width;
            int h = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Height;
            this.Left = w / 2 - Width / 2;
            this.Top = -10d;
            this.Height = h + 20;
        }

        private void btnStartAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (Interface inf in ifList)
                inf.IsRunning = true;
        }

        private void btnStopAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (Interface inf in ifList)
                inf.IsRunning = false;
        }

        void PortMonitor_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            foreach (Interface inf in ifList)
                inf.IsRunning = false;
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            Interface i = (sender as Button).DataContext as Interface;
            i.IsRunning = true;
        }

        private void btnPause_Click(object sender, RoutedEventArgs e)
        {
            Interface i = (sender as Button).DataContext as Interface;
            i.IsRunning = null;
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            Interface i = (sender as Button).DataContext as Interface;
            i.IsRunning = false;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.Filter = "XML文件(xml)|*.xml";
            dialog.RestoreDirectory = true;
            if ((bool)dialog.ShowDialog())
            {
                string fileFullName = dialog.FileName;
                //string fileName = fileFullName.Substring(fileFullName.LastIndexOf("\\" + 1));
                //string path = fileFullName.Substring(0, fileFullName.LastIndexOf("\\"));
                XmlDocument xmlDoc = new XmlDocument();
                XmlElement xmlRoot = xmlDoc.CreateElement("Interfaces");
                xmlDoc.AppendChild(xmlRoot);
                foreach (Interface i in ifList)
                {
                    XmlElement iface = xmlDoc.CreateElement("Interface");
                    xmlRoot.AppendChild(iface);
                    XmlElement index = xmlDoc.CreateElement("IfIndex");
                    index.InnerText = i.IfIndex.ToString();
                    XmlElement descr = xmlDoc.CreateElement("IfDescr");
                    descr.InnerText = i.Descr.ToString();
                    XmlElement ip = xmlDoc.CreateElement("IfIP");
                    ip.InnerText = i.IP.ToString();
                    XmlElement timerInteral = xmlDoc.CreateElement("TimerInteral");
                    timerInteral.InnerText = i.TimerInteral.ToString();
                    XmlElement isShowSpeedAlarm = xmlDoc.CreateElement("IsShowSpeedAlarm");
                    isShowSpeedAlarm.InnerText = i.IsShowSpeedAlarm.ToString();
                    XmlElement maxOutSpeed = xmlDoc.CreateElement("MaxOutSpeed");
                    maxOutSpeed.InnerText = i.MaxOutSpeed.ToString();
                    XmlElement maxInSpeed = xmlDoc.CreateElement("MaxInSpeed");
                    maxInSpeed.InnerText = i.MaxInSpeed.ToString();
                    iface.AppendChild(index);
                    iface.AppendChild(descr);
                    iface.AppendChild(ip);
                    iface.AppendChild(timerInteral);
                    iface.AppendChild(maxOutSpeed);
                    iface.AppendChild(maxInSpeed);
                    iface.AppendChild(isShowSpeedAlarm);
                }
                try
                {
                    xmlDoc.Save(fileFullName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("保存失败\n" + ex.Message);
                }
            }

        }

        private void tbTipMessage_MouseEnter(object sender, MouseEventArgs e)
        {
            try
            {
                TextBlock tb = sender as TextBlock;
                Interface i = tb.DataContext as Interface;
                if (i.LogList.Count > 0)
                {
                    System.Windows.Controls.ToolTip tt = new System.Windows.Controls.ToolTip();
                    ListBox lb = new ListBox();
                    lb.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
                    lb.BorderThickness = new System.Windows.Thickness(0);
                    lb.ItemsSource = i.LogList;
                    tt.Content = lb;
                    tb.ToolTip = tt;
                }
            }
            catch (System.Exception ex)
            {
            	
            }
        }

        private void tbTipMessage_MouseLeave(object sender, MouseEventArgs e)
        {
            TextBlock tb = sender as TextBlock;
            if (tb.ToolTip != null)
            {
                tb.ToolTip = null;
            }
        }

        private void btnMoveUp_Click(object sender, RoutedEventArgs e)
        {
            Interface intf = listBox.SelectedItem as Interface;
            if (intf == null)
            {
                MessageBox.Show("需选择一行");
                return;
            }
            int oldIndex = ifList.IndexOf(intf);
            if (oldIndex == 0)
                return;
            ifList.Move(oldIndex, oldIndex - 1);
        }

        private void btnMoveDown_Click(object sender, RoutedEventArgs e)
        {
            Interface intf = listBox.SelectedItem as Interface;
            if (intf == null)
            {
                MessageBox.Show("需选择一行");
                return;
            }
            int oldIndex = ifList.IndexOf(intf);
            if (oldIndex == ifList.Count - 1)
                return;
            ifList.Move(oldIndex, oldIndex + 1);

        }

        private void btnMoveTop_Click(object sender, RoutedEventArgs e)
        {
            Interface intf = listBox.SelectedItem as Interface;
            if (intf == null)
            {
                MessageBox.Show("需选择一行");
                return;
            }
            int oldIndex = ifList.IndexOf(intf);
            if (oldIndex == 0)
                return;
            ifList.Move(oldIndex, 0);

        }

        private void btnMoveBottom_Click(object sender, RoutedEventArgs e)
        {
            Interface intf = listBox.SelectedItem as Interface;
            if (intf == null)
            {
                MessageBox.Show("需选择一行");
                return;
            }
            int oldIndex = ifList.IndexOf(intf);
            if (oldIndex == ifList.Count - 1)
                return;
            ifList.Move(oldIndex, ifList.Count - 1);

        }

        private void btnDel_Click(object sender, RoutedEventArgs e)
        {
            Interface intf = listBox.SelectedItem as Interface;
            if (intf == null)
            {
                MessageBox.Show("需选择一行");
                return;
            }
            intf.IsRunning = false;
            ifList.Remove(intf);
        }

        private void elTopLight_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Interface inf = (sender as Ellipse).DataContext as Interface;
            int oldIndex = ifList.IndexOf(inf);
            if (oldIndex == 0)
                return;
            ifList.Move(oldIndex, 0);
        }

        private void cbShowLightOnTop_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)cbShowLightOnTop.IsChecked)
            {
                gridShowLightOnTop.Visibility = Visibility.Visible;
                lbLightOnTop.ItemsSource = ifList;
            }
            else
            {
                gridShowLightOnTop.Visibility = Visibility.Collapsed;
                lbLightOnTop.ItemsSource = null;
            }
        }

        private void AdjustIsShowLightOnTop()
        {
            if (ifList.Count > 4)
            {
                cbShowLightOnTop.IsChecked = true;
                gridShowLightOnTop.Visibility = Visibility.Visible;
                lbLightOnTop.ItemsSource = ifList;
            }
            else
            {
                cbShowLightOnTop.IsChecked = false;
                gridShowLightOnTop.Visibility = Visibility.Collapsed;
                lbLightOnTop.ItemsSource = null;
            }
        }
    }
}
