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

namespace NCMMS.UC
{
    /// <summary>
    /// UCFileBrowse.xaml 的交互逻辑
    /// </summary>
    public partial class UCFileBrowse : UserControl
    {
        string fileFilter = "XML文件(xml)|*.xml";
        public string FileFilter
        {
            get { return fileFilter; }
            set { fileFilter = value; }
        }
        public double TextBoxWidth
        {
            set { tbFileUrl.Width = value; }
        }
        public double TextBoxHeight
        {
            set { tbFileUrl.Height = value; }
        }
        public double ButtonWidth
        {
            set { btnOpenFile.Width = value; }
        }
        public double ButtonHeight
        {
            set { btnOpenFile.Height = value; }
        }
        public double UCHeight
        {
            set { this.Height = value; }
        }
        /// <summary>
        /// 选取文件的全路径名
        /// </summary>
        public string FileUrl
        {
            get { return tbFileUrl.Text; }
        }

        public UCFileBrowse()
        {
            InitializeComponent();
        }

        private void btnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = fileFilter;
            if ((bool)openFileDialog.ShowDialog())
            {
                tbFileUrl.Text = openFileDialog.FileName;
            }
        }
    }
}
