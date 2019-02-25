using System;
using System.Windows.Data;
using System.Windows.Controls;
using System.Windows;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace NCMMS.MultiPing
{
    /// <summary>
    /// MultiPingShowReply.xaml 的交互逻辑
    /// </summary>
    public partial class MultiPingShowReply : MyWindow
    {
        private PingTarget pt;
        List<string> messageList;
        System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
        
        public MultiPingShowReply(PingTarget _pt)
        {
            InitializeComponent();
            pt = _pt;
            WindowTitle = pt.StrIP + " 的回显列表";
            messageList = new List<string>(pt.recordMessageList);
            foreach (string s in messageList)
            {
                listBox.Items.Add(s);
            }
            pt.ShowReplyAddMessageEvent += new PingTarget.ShowReplyAddMessageEventHandler(pt_ShowReplyAddMessageEvent);
            this.Closing += new System.ComponentModel.CancelEventHandler(MultiPingShowReply_Closing);
            
        }

        void pt_ShowReplyAddMessageEvent(object sender, ShowReplyAddMessageEventArgs e)
        {
            this.Dispatcher.Invoke(new Action(() =>
                    {
                        listBox.Items.Add(e.Message);
                        if ((bool)isAutoScroll.IsChecked)
                            listBox.ScrollIntoView(listBox.Items[listBox.Items.Count - 1]);
                    }));
            
        }

        void MultiPingShowReply_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
             pt.ShowReplyAddMessageEvent -= new PingTarget.ShowReplyAddMessageEventHandler(pt_ShowReplyAddMessageEvent);
        }

    }
}
