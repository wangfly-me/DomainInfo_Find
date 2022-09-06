using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.IO;
using System.Net.NetworkInformation;
using System.Text;
using NDesk.Options;

namespace DomainInfo_Find
{
    public static class tools
    {
        public static DirectoryEntry coon = null;
        public static DirectorySearcher search = null;

        public static void Machine()
        {
            string url = "LDAP://" + GetArgsValue.domain;
            //域外
            if (GetArgsValue.user != "" && GetArgsValue.pass != "")
            {
                string username = GetArgsValue.user;
                string password = GetArgsValue.pass;
                coon = new DirectoryEntry(url, username, password);
                search = new DirectorySearcher(coon);
            }
            //域内
            else
            {
                coon = new DirectoryEntry(url);
                search = new DirectorySearcher(coon);
            }
            search.Filter = "(&(objectclass=computer))";
            using (StreamWriter file = new StreamWriter(@"machine.txt", true))
            {
                foreach (SearchResult r in search.FindAll())
                {
                    string computername = "";
                    computername = r.Properties["cn"][0].ToString();
                    //Console.WriteLine("===========All Computers===========");
                    //Console.WriteLine(computername);
                    file.WriteLine(computername);
                }
            }
        }
        public static bool IsMachineUp(string hostName)
        {
            bool retVal = false;
            try
            {
                Ping pingSender = new Ping();
                PingOptions options = new PingOptions();
                options.DontFragment = true;
                string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
                byte[] buffer = Encoding.ASCII.GetBytes(data);
                int timeout = 120;

                PingReply reply = pingSender.Send(hostName, timeout, buffer, options);
                if (reply.Status == IPStatus.Success)
                {
                    retVal = true;
                }
            }
            catch (Exception ex)
            {
                retVal = false;
            }
            return retVal;
        }
        public static void C()
        {
            try
            {
                string CFiles = "";
                StreamReader machine_name = new StreamReader(@"machine.txt");
                while (!machine_name.EndOfStream)
                {
                    try
                    {
                        string machine = machine_name.ReadLine();
                        if (IsMachineUp(machine))
                        {
                            string currentpath = Directory.GetCurrentDirectory();
                            CFiles = currentpath + "\\CInfos";
                            Directory.CreateDirectory(CFiles);

                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("[*]" + machine);
                            Console.ForegroundColor = ConsoleColor.White;

                            //获取users目录
                            string dpath = @"\\" + machine + @"\c$";
                            var d_list = Directory.EnumerateDirectories(dpath);
                            if (Directory.Exists(dpath))
                            {
                                //创建机器名文件夹
                                string MachineFolder = CFiles + "\\" + machine;
                                Directory.CreateDirectory(MachineFolder);
                                //创建输出文本
                                string E_txt = MachineFolder + "\\cFiles.txt";
                                StreamWriter sw = File.CreateText(E_txt);
                                sw.Close();
                                try
                                {
                                    var files = Directory.GetFiles(dpath);
                                    foreach (string file in files)
                                    {
                                        Console.WriteLine(file);
                                        string create_time = Directory.GetCreationTime(file).ToString();
                                        string writeFileTo = "create time:" + create_time + "  " + file + "\r\n";
                                        File.AppendAllText(E_txt, writeFileTo);
                                    }

                                    var directorys = Directory.EnumerateDirectories(dpath);
                                    foreach (string directory in directorys)
                                    {
                                        if (!directory.Contains("System Volume Information"))
                                        {
                                            string[] AllFiles = Directory.GetFileSystemEntries(directory, "*", SearchOption.AllDirectories);
                                            foreach (string file in AllFiles)
                                            {
                                                string create_time = Directory.GetCreationTime(file).ToString();
                                                Console.WriteLine(file);
                                                string writeFileTo = "create time:" + create_time + "  " + file + "\r\n";
                                                File.AppendAllText(E_txt, writeFileTo);
                                            }
                                        }
                                    }
                                }
                                catch (UnauthorizedAccessException ex)
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine(ex.Message);
                                    Console.ForegroundColor = ConsoleColor.White;
                                    continue;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(ex.Message);
                        Console.ForegroundColor = ConsoleColor.White;
                        continue;
                    }
                }
                machine_name.Close();
                Console.WriteLine("[+]out put to:" + CFiles);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[-] error");
                Console.WriteLine("[-] Exception: " + ex.Message);
                Console.ForegroundColor = ConsoleColor.White;
                return;
            }
        }
    }

    public static class GetArgsValue
    {
        public static string domain = "";
        public static string user = "";
        public static string pass = "";
        public static void GetDomainValue(List<string> param1 = null)
        {
            foreach (string p in param1)
            {
                domain = p;
            }
        }

        public static void GetUserValue(List<string> param2 = null)
        {
            foreach (string p in param2)
            {
                user = p;
            }
        }

        public static void GetPassValue(List<string> param3 = null)
        {
            foreach (string p in param3)
            {
                pass = p;
            }
        }
    }

    class Program
    {
        public static void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Usage:");
            p.WriteOptionDescriptions(Console.Out);
        }
        static void Main(string[] args)
        {
            List<string> domains = new List<string>();
            List<string> users = new List<string>();
            List<string> passes = new List<string>();
            bool show_help = false;

            OptionSet options = new OptionSet()
            {
                { "d|domain=", "the {IP} of the DC target",v => domains.Add (v) },
                { "u|user=", "the {user} of the DC target",v => users.Add (v) },
                { "p|pass=", "the {pass} of the DC target",v => passes.Add (v) },
                { "h|help",  "show this message and exit",v => show_help = v != null },
            };
            options.Parse(args);
            if (show_help)
            {
                ShowHelp(options);
                return;
            }

            GetArgsValue.GetDomainValue(domains);
            GetArgsValue.GetUserValue(users);
            GetArgsValue.GetPassValue(passes);

            tools.Machine();
            tools.C();
        }
    }
}
