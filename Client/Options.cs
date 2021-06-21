using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;

namespace Client
{
    internal class Options
    {
        internal static bool flag = false;  //指示是否进入写管道逻辑
        internal static string md5 = "";
        internal static List<string> sendData_List = new List<string> { };
        internal static List<string> sendData_Queue = new List<string> { };
        internal static byte[] data = Encoding.UTF8.GetBytes("EDF");
        internal static SortedList<Int32, string> fileDate = new SortedList<Int32, string> { }; //存储下发文件
        internal static Int32 dataCount = 0;  //指示下载文件需要的请求数
        internal static Int32 key = 0;
        internal static Queue request_number = new Queue { };
        internal static SortedList<Int32, string> dataList = new SortedList<Int32, string>();
    }
}
