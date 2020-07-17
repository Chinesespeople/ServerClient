using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Client
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Thread th = new Thread(BeginTo);
            th.IsBackground = true;
            th.Start();
        }

        public byte[] res = new byte[1024 * 1024];
        int port = 7689;
        string localIP = "192.168.1.5";

        public Socket Csocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        string nowdateTime= DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
        string CreateMsg="";
        public string createMsg
        {
            get
            {
                return CreateMsg;
            }
            set
            {
                nowdateTime = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
                CreateMsg = nowdateTime + "  来自[" + Control_Username.Text + "]\n" + value;
            }
        }
        private void Control_Send_Click(object sender, RoutedEventArgs e)
        {
            createMsg = Control_SendMsg.Text;
            sendMsg(createMsg);
            Trace.WriteLine(createMsg);
            Control_SendMsg.Text = null;
        }

        private void Control_SendMsg_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                createMsg = Control_SendMsg.Text;
                sendMsg(createMsg);
                Trace.WriteLine(createMsg);
                Control_SendMsg.Text = null;
            }
        }
        /// <summary>
        /// 发送消息
        /// </summary>
        public void sendMsg(string msg)
        {
            try
            {
                Socket socket = (Socket)Csocket;
                Csocket.Send(Encoding.UTF8.GetBytes(msg));

            }
            catch
            {
                Msg_box.Text = "服务器可以已经被关闭，连接被断开";
            }
        }

        private void Msg_box_TextChanged(object sender, TextChangedEventArgs e)
        {
            Msg_box.ScrollToEnd();
        }

        /// <summary>
        /// 开始连接服务器
        /// </summary>
        public void BeginTo()
        {
            IPAddress ipaddress = IPAddress.Parse(localIP);
            try
            {
                CreateMsg_box("正在寻找服务器……\n");
                Csocket.Connect(new IPEndPoint(ipaddress, port));
                CreateMsg_box("连接服务器成功，现在你可以任意的发送信息。\n");
                GetMsg();
            }
            catch
            {
                CreateMsg_box("与服务器连接时出现了错误\n");
            }
        }
        /// <summary>
        /// 循环接受服务器的信息
        /// </summary>
        public void GetMsg()
        {
            while(true)
            {
                try
                {
                    int MsgLength = Csocket.Receive(res);
                    CreateMsg_box(Encoding.UTF8.GetString(res,0,MsgLength),true);
                }
                catch
                {
                    CreateMsg_box("服务器已被关闭",true);
                }
            }
        }
        public void CreateMsg_box(string text,bool bo=false)
        {
            Action action = () =>
            {
                if (bo == true)
                    Msg_box.Text = text;
                else
                    Msg_box.Text += text;
            };
            Msg_box.Dispatcher.BeginInvoke(action);
        }

    }
}
