using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace RCmdC
{
    internal class Program
    {
        private static Socket clientSocket = null;

        private static Queue<string> cmdResultQueue = new Queue<string>();

        private static void Main(string[] args)
        {
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            // 客户端不需要绑定， 需要连接
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 10492);   //服务端IP和端口
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