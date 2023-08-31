using System.Net.Sockets;
using System.Net;
using System.Text;

namespace RCmdC
{

    internal class Program
    {
        private static Socket clientSocket = null;

        private static Queue<string> cmdResultQueue = new Queue<string>();

        private static void Main(string[] args)
        {
            string ip = null;
            int port = 0;

            bool flag1 = false;
            do
            {
                Console.WriteLine("输入目标IP或域名");
                string param = Console.ReadLine().Trim();
                if (!string.IsNullOrEmpty(param))
                {
                    ip = param;
                    flag1 = true;
                }
                else
                {
                    Console.WriteLine("请正确输入");
                }
            } while(!flag1);

            bool flag2 = false;
            do
            {
                Console.WriteLine("输入目标端口");
                string param = Console.ReadLine().Trim();
                if (int.TryParse(param, out int _port))
                {
                    port = _port;
                    flag2 = true;
                }
                else
                {
                    Console.WriteLine("请正确输入");
                }
            } while (!flag2);

            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            // 客户端不需要绑定， 需要连接
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(ip), port);   //服务端IP和端口
            clientSocket.Connect(endPoint);
            Console.WriteLine("连接到服务器");

            ThreadPool.QueueUserWorkItem(new WaitCallback(delegate
            {
                byte[] buffer = new byte[1024];
                while (true)
                {
                    Array.Clear(buffer, 0, buffer.Length);
                    clientSocket.Receive(buffer);
                    string msg = Encoding.Default.GetString(buffer);
                    Console.WriteLine("收到消息： " + msg.Replace("\0", ""));
                    //Thread.Sleep(10);
                }
            }));


            ThreadPool.QueueUserWorkItem(new WaitCallback(delegate
            {
                while (true)
                {
                    if (cmdResultQueue.Count > 0)
                    {
                        string cmd = cmdResultQueue.Dequeue();
                        Console.WriteLine("上传命令:" + cmd);
                        clientSocket.Send(Encoding.Default.GetBytes(cmd));
                    }
                    Thread.Sleep(10);
                }
            }));


            while (true)
            {
                string cmdstr = Console.ReadLine();

                if (string.IsNullOrEmpty(cmdstr))
                {
                    Console.WriteLine("CMD命令不能为空:");
                    continue;
                }
                else
                    cmdResultQueue.Enqueue(cmdstr);
            }
        }




    }
}