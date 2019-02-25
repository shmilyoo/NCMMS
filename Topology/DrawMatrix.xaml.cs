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
    /// DrawMatrix.xaml 的交互逻辑
    /// </summary>
    public partial class DrawMatrix : Window
    {
        TopoData<Equipment, LineInfo> topo;
        public DrawMatrix(TopoData<Equipment,LineInfo> _topo)
        {
            InitializeComponent();
            topo = _topo;
            this.Loaded += DrawMatrix_Loaded;
        }

        void DrawMatrix_Loaded(object sender, RoutedEventArgs e)
        {
            int number = topo.Number;
            for (int i = 0; i < number; i++)
            {
                ColumnDefinition cd = new ColumnDefinition();
                cd.Width = new GridLength(80d);
                xGrid.ColumnDefinitions.Add(cd);
                TextBlock tbx = new TextBlock();
                tbx.Text = topo.GetV(i).Name;
                tbx.HorizontalAlignment = HorizontalAlignment.Center;
                tbx.VerticalAlignment = VerticalAlignment.Center;
                xGrid.Children.Add(tbx);
                tbx.SetValue(Grid.ColumnProperty, i);

                RowDefinition rd = new RowDefinition();
                rd.Height = new GridLength(50d);
                yGrid.RowDefinitions.Add(rd);
                TextBlock tby = new TextBlock();
                tby.Text = topo.GetV(i).Name + "\nStep: " + topo.GetV(i).Step;
                tby.HorizontalAlignment = HorizontalAlignment.Center;
                tby.VerticalAlignment = VerticalAlignment.Center;
                yGrid.Children.Add(tby);
                tby.SetValue(Grid.RowProperty, i);

                RowDefinition row = new RowDefinition();
                row.Height = new GridLength(50d);
                matrixGrid.RowDefinitions.Add(row);

                for (int j = 0; j < number; j++)
                {
                    ColumnDefinition col = new ColumnDefinition();
                    col.Width = new GridLength(80d);
                    matrixGrid.ColumnDefinitions.Add(col);
                    TextBlock tbm = new TextBlock();
                    if (topo.GetE(i,j) == null)
                    {
                        if (i == j)
                            tbm.Text = "\\";
                        else
                            tbm.Text = "×";
                        tbm.FontSize = 25;
                        tbm.HorizontalAlignment = HorizontalAlignment.Center;
                        tbm.VerticalAlignment = VerticalAlignment.Center;
                    }
                    else
                    {
                        tbm.Text = topo.GetV(i).Name + "\n" + topo.GetV(j).Name;
                        tbm.HorizontalAlignment = HorizontalAlignment.Center;
                        tbm.VerticalAlignment = VerticalAlignment.Center;
                    }
                    matrixGrid.Children.Add(tbm);
                    tbm.SetValue(Grid.RowProperty, i);
                    tbm.SetValue(Grid.ColumnProperty, j);
                }
            }
        }
    }
}
