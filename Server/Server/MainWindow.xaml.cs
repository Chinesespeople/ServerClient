using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Server
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        State fr = new State();
        public MainWindow()
        {
            InitializeComponent();
            fr.Show();
            ListenTCPstart();
        }
        public byte[] res = new byte[1024 * 1024];//创建字节集
        public Socket Ssocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);//实例化Socket对象
        public Dictionary<string, Socket> ClientINFO = new Dictionary<string, Socket> { };//定义一个用户字典
        public string nowdateTime = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
        public string CreateMsg = "";
        public int localport = 7689;
        public string localIP = "192.168.1.5";
        /// <summary>
        /// 创建访问器，给需要发送的数据打标
        /// </summary>
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
            if(Control_SendMsg.Text==null)
            {
                return;
            }
            createMsg = Control_SendMsg.Text;
            Msg_box.Text = Msg_box.Text + Environment.NewLine + Environment.NewLine + createMsg;
            createMsg = Control_SendMsg.Text;
            Trace.WriteLine(createMsg);
            sendMsg();
            Control_SendMsg.Text = null;
        }
        private void Control_SendMsg_KeyDown(object sender, KeyEventArgs e)
        {
            if (Control_SendMsg.Text == null)
            {
                return;
            }
            if (e.Key == Key.Enter)
            {
                createMsg = Control_SendMsg.Text;
                Msg_box.Text = Msg_box.Text + Environment.NewLine + Environment.NewLine + createMsg;
                createMsg = Control_SendMsg.Text;
                Trace.WriteLine(createMsg);
                sendMsg();
                Control_SendMsg.Text = null;
            }
        }
        private void Msg_box_TextChanged(object sender, TextChangedEventArgs e)
        {
            Msg_box.ScrollToEnd();
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("确定是退出吗？", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if(result==MessageBoxResult.Yes)
            {
                e.Cancel = false;
            }
            if (result == MessageBoxResult.No)
            {
                e.Cancel = true;
            }
        }

        /// <summary>
        /// 给所有人发送同步消息
        /// </summary>
        void sendMsg()
        {
            int i=0;
            for (i = 0; i < fr.Control_hostlist.Items.Count; i++)
            {
                try
                {
                    Trace.WriteLine(fr.Control_hostlist.Items[i].ToString());
                    Socket socket = ClientINFO[fr.Control_hostlist.Items[i].ToString()];
                    socket.Send(Encoding.UTF8.GetBytes(Msg_box.Text));
                }
                catch
                {
                    //因为无法直接访问到目标对象，所以将这些代码封装委托
                    Action action = () =>
                      {
                          fr.Control_stateMsg.Text = fr.Control_stateMsg.Text + "\n地址:" + fr.Control_hostlist.Items[i - 1].ToString() + "可能已经失效，现已被移出群聊";
                          fr.Control_hostlist.Items.RemoveAt(i - 1);
                      };
                    fr.Dispatcher.BeginInvoke(action);
                }
            }
        }

        /// <summary>
        /// 创建套接字
        /// </summary>
        public void ListenTCPstart()
        {
            try
            {
                IPAddress ipaddress = IPAddress.Parse(localIP);
                Ssocket.Bind(new IPEndPoint(ipaddress,localport));
                Ssocket.Listen(10);
                fr.Control_stateMsg.Text = fr.Control_stateMsg.Text + "\n服务器" + Ssocket.LocalEndPoint.ToString() + "启动成功，等待用户接入……";
                Thread th = new Thread(Listen);
                th.IsBackground = true;
                th.Start();
                Thread.Sleep(500);
            }
            catch
            {
                fr.Control_stateMsg.Text += "\n" + "发生异常";
                Ssocket.Close();
            }
        }


        /// <summary>
        /// 侦听请求的连接
        /// </summary>
        public void Listen()
        {
            Socket Csocket = null;//临时存储连接到的客户端
            IPAddress C_ip;
            int C_port;
            while (true)
            {
                try
                {
                    Csocket = Ssocket.Accept();
                }
                catch
                {
                    fr.Control_stateMsg.Text += "\n" + "侦听异常";
                }
                C_ip = (Csocket.RemoteEndPoint as IPEndPoint).Address;
                C_port = (Csocket.RemoteEndPoint as IPEndPoint).Port;
                Action action = () =>
                  {
                      fr.Control_stateMsg.Text += "\n" + "IP:" + C_ip.ToString() + ":" + C_port.ToString()+"已连接";
                      fr.Control_hostlist.Items.Add(C_ip.ToString() + ":" + C_port.ToString());
                      ClientINFO.Add(Csocket.RemoteEndPoint.ToString(),Csocket);
                  };
                fr.Dispatcher.BeginInvoke(action);
                Thread thread = new Thread(ListenClientDate);
                thread.Start(Csocket);
            }
        }
        /// <summary>
        /// 给每一个客户分配一个构造函数，在此函数实现循环接受客户端发送的数据
        /// </summary>
        /// <param name="s"></param>
        public void ListenClientDate(object s)
        {
            Socket socket = (Socket)s;//分配新的对象
            while(true)
            {
                try
                {
                    int reslen = socket.Receive(res);
                    string resMsg = Encoding.UTF8.GetString(res, 0, reslen);
                    Action action1 = () =>
                    {
                        Msg_box.Text = Msg_box.Text + Environment.NewLine + Environment.NewLine + resMsg;
                        sendMsg();
                    };
                    this.Dispatcher.BeginInvoke(action1);
                    Trace.WriteLine(resMsg);
                }
                catch
                {
                    Action action = () =>
                      {
                          fr.Control_stateMsg.Text = fr.Control_stateMsg.Text + "\n客户端出现问题！已与" + socket.RemoteEndPoint + "强制断开连接，此会话已退出。";
                          for (int i = 0; i < fr.Control_hostlist.Items.Count; i++)
                          {
                              if(fr.Control_hostlist.Items[i].ToString()== socket.RemoteEndPoint.ToString())
                              {
                                  Trace.WriteLine(fr.Control_hostlist.Items[i].ToString());
                                  ClientINFO.Remove(fr.Control_hostlist.Items[i].ToString());
                                  fr.Control_hostlist.Items.RemoveAt(i);
                                  break;
                              }
                          }
                          socket.Close();
                      };
                    fr.Control_stateMsg.Dispatcher.BeginInvoke(action);
                    break;
                }
            }
        }

    }
}