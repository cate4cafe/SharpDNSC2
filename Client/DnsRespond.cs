using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Client
{
    public class DnsRespond
    {
        public static void respond(byte[] rec)
        {           
            List<string> temp = new List<string>();
            List<string> file = new List<string>();
            List<string> flag = new List<string>();

            int bitnumber = 12; //DNS 头
            //byte sss = rec.Skip(36).Take(1).ToArray()[0];
            while (rec.Skip(bitnumber).Take(1).ToArray()[0].ToString() != "0") //判断域名结束
                bitnumber += 1;
            bitnumber += 1;  //跳过域名结束标志位 0
            bitnumber += 4;  //跳过请求中的Type,Class In 
            bitnumber += 2;  //跳过C00C
            if (Convert.ToInt16(rec.Skip(bitnumber).Take(2).ToArray()[1]) == 6)
            {
                bitnumber += 10;
                //Console.WriteLine(bitnumber);
                while (rec.Skip(bitnumber).Take(1).ToArray()[0].ToString() != "0")   //获取primary server name  指示返回的是数据还是需要请求的次数
                {
                    int length = Convert.ToInt16(rec.Skip(bitnumber).Take(1).ToArray()[0]);
                    //Console.WriteLine(length);
                    flag.Add(Encoding.UTF8.GetString(rec.Skip(bitnumber + 1).Take(length).ToArray()));
                    bitnumber += 1;
                    bitnumber += length;
                }
                if (string.Join("", flag) == "data")  // 数据传输
                {
                    bitnumber += 1;
                    while (rec.Skip(bitnumber).Take(1).ToArray()[0].ToString() != "0") //获取点分域名
                    {
                        int length = Convert.ToInt16(rec.Skip(bitnumber).Take(1).ToArray()[0]);
                        //Console.WriteLine(length);
                        temp.Add(Encoding.UTF8.GetString(rec.Skip(bitnumber + 1).Take(length).ToArray()));
                        bitnumber += 1;
                        bitnumber += length;
                    }
                    bitnumber += 1;
                   // bitnumber += 4;
                    Console.WriteLine("当前返回的键是 " + BitConverter.ToInt32(rec.Skip(bitnumber).Take(4).Reverse().ToArray(), 0).ToString());
                    if(!Options.dataList.ContainsKey(BitConverter.ToInt32(rec.Skip(bitnumber).Take(4).Reverse().ToArray(), 0)))
                        Options.dataList.Add(BitConverter.ToInt32(rec.Skip(bitnumber).Take(4).Reverse().ToArray(), 0), string.Join("", temp));
                    temp.Clear();
                    //Console.WriteLine("当前返回数据   " + string.Join("", Options.dataList.SelectMany(v => v.Value).ToArray()));
                    Console.WriteLine("当前Options.dataList大小 " + Options.dataList.Count.ToString() + "   " + Options.dataCount.ToString());
                    if (Options.dataList.Count == Options.dataCount)
                    {
                        Console.WriteLine("dddddddddddddddd");              
                        Options.data = Encryption.HexStringToByteArray(string.Join("", Options.dataList.SelectMany(v => v.Value).ToArray()));
                        Console.WriteLine("接收到的CS返回数据长度" + Options.data.Length);
                        Options.dataList.Clear();
                    }
                }
                if (string.Join("", flag) == "reccount")  //获取要请求的次数
                {
                    //Console.WriteLine("aaaaa");
                    bitnumber += 1;
                    while (rec.Skip(bitnumber).Take(1).ToArray()[0].ToString() != "0") //获取点分域名
                    {
                        int length = Convert.ToInt16(rec.Skip(bitnumber).Take(1).ToArray()[0]);
                        file.Add(Encoding.UTF8.GetString(rec.Skip(bitnumber + 1).Take(length).ToArray()));
                        bitnumber += 1;
                        bitnumber += length;
                    }
                    //Console.WriteLine(string.Join("",file));
                    bitnumber += 1;
                    Options.dataCount = BitConverter.ToInt32(rec.Skip(bitnumber).Take(4).Reverse().ToArray(), 0);
                    Console.WriteLine("需要请求的数据次数  " + Options.dataCount.ToString());
                    Options.flag = true; //指示进行get请求
                   
                }


            }
            //if (Convert.ToInt16(rec.Skip(bitnumber).Take(2).ToArray()[1]) == 1) //判断返回
            //{
            //    Console.WriteLine("A请求应答");
            //    bitnumber += 10; //跳过TYPE、CLASS、DATA Length、ttl
            //    Console.WriteLine("{0}.{1}.{2}.{3}", rec[bitnumber].ToString(), rec[bitnumber + 1].ToString(), rec[bitnumber + 2].ToString(), rec[bitnumber + 3].ToString());

            //}
            //if (Convert.ToInt16(rec.Skip(bitnumber).Take(2).ToArray()[1]) == 16)
            //{
            //    Console.WriteLine("TXT请求应答");
            //    bitnumber += 10; //跳过TYPE、CLASS、DATA Length、ttl
            //    int txt_length = Convert.ToInt16(rec.Skip(bitnumber).Take(2).ToArray()[1]);
            //    bitnumber += 1; // 跳过txt length
            //    Console.WriteLine(Encoding.UTF8.GetString(rec.Skip(bitnumber).Take(txt_length).ToArray()));

            //}
            //if (Convert.ToInt16(rec.Skip(bitnumber).Take(2).ToArray()[1]) == 5)
            //{
            //    Console.WriteLine("Cname请求应答");
            //    bitnumber += 10; //跳过TYPE、CLASS、DATA Length、ttl
            //    int Cname_length = Convert.ToInt16(rec.Skip(bitnumber).Take(2).ToArray()[1]);
            //    bitnumber += 1; // 跳过txt length
            //    Console.WriteLine(Encoding.UTF8.GetString(rec.Skip(bitnumber).Take(Cname_length).ToArray()));

            //}

        }
    }
}

