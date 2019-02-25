using System;
using System.Collections.Generic;
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
    /// UCRepeatNum.xaml 的交互逻辑
    /// </summary>
    public partial class UCRepeatNum : UserControl
    {
        //int result;
        /// <summary>
        /// 文本框中显示的值
        /// </summary>
        //public int Result
        //{
        //    set
        //    {
        //        result = value;
        //        textbox.Text = result.ToString();
        //    }
        //    get { return result; }
        //}
        bool hasMinResult = false;
        int minResult;
        public int MinResult
        {
            get { return minResult; }
            set { minResult = value; }
        }
        public bool HasMinResult
        {
            get { return hasMinResult; }
            set { hasMinResult = value; }
        }
        public string Unit
        {
            set
            {
                tbUnit.Text = value;
                tbUnit.Visibility = Visibility.Visible;
            }
        }
        public double TBoxWidth
        {
            set { textbox.Width = value; }
        }
        public string InputTip
        {
            set { tbTip.Text = value; }
        }

        public int Result
        {
            get { return (int)GetValue(ResultProperty); }
            set
            {
                if (hasMinResult && value < minResult)
                {
                    textbox.Text = minResult.ToString();
                    SetValue(ResultProperty, minResult);
                }
                else
                {
                    textbox.Text = value.ToString();
                    SetValue(ResultProperty, value);
                }
            }
        }
        public static readonly DependencyProperty ResultProperty =
            DependencyProperty.Register("Result", typeof(int), typeof(UCRepeatNum),new PropertyMetadata(9));

        public UCRepeatNum()
        {
            this.InitializeComponent();
            this.Loaded += new RoutedEventHandler(UCRepeatNum_Loaded);
            this.MouseEnter += new MouseEventHandler(UCRepeatNum_MouseEnter);
        }

        void UCRepeatNum_MouseEnter(object sender, MouseEventArgs e)
        {
            tbTip.Visibility = Visibility.Hidden;
        }

        void UCRepeatNum_Loaded(object sender, RoutedEventArgs e)
        {
            if (Result != -1)
            {
                textbox.Text = Result.ToString();
                tbTip.Visibility = Visibility.Hidden;
            }
        }


        private void btnIncr_Click(object sender, RoutedEventArgs e)
        {
            Result++;
        }

        private void btnDecr_Click(object sender, RoutedEventArgs e)
        {
            Result--;
        }

        private void textbox_KeyDown(object sender, KeyEventArgs e)
        {
            if (!((e.Key <= Key.D9 && e.Key >= Key.D0) || (e.Key <= Key.NumPad9 && e.Key >= Key.NumPad0)))
            {
                e.Handled = true;
            }
        }

        private void textbox_KeyUp(object sender, KeyEventArgs e)
        {
            string str = textbox.Text;
            //result = int.Parse(str);
            try
            {
                Result = int.Parse(str);
            }
            catch
            {
                Result = minResult;
            }
        }
        public delegate void ResultChangedEventHandler(object sender, ResultChangedEventArgs e);
        public event ResultChangedEventHandler ResultChangedEvent;

        private void textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ResultChangedEvent != null)
            {
                ResultChangedEvent(this, new ResultChangedEventArgs(Result));
            }
        }

    }
    public class ResultChangedEventArgs
    {
        public ResultChangedEventArgs(int num) { Result = num; }
        public int Result { get; private set; } // readonly
    }
}