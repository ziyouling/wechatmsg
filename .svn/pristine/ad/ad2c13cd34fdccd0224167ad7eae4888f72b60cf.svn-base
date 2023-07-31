using Newtonsoft.Json;
using StompLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace wechatmsg.rest
{
    class Program
    {
        private static string fiddlerPath;
        static void Main(string[] args)
        {
            int hour = 4;
            if (args.Length > 0)
            {
                string tag = "--fiddler=";
                string hourTag = "--hour=";
                foreach (string item in args)
                {
                    Console.WriteLine("arg:" + item);
                    if (item.StartsWith(tag))
                    {
                        fiddlerPath = item.Substring(tag.Length);
                    }
                    if (item.StartsWith(hourTag))
                    {
                        hour =int.Parse(item.Substring(hourTag.Length));
                    }
                }
            }

            Program program = new Program();
            program.Init("https://course.muketang.com", fiddlerPath, hour);
            //program.Init("http://localhost", fiddlerPath, hour);
            int lastDay = -1;
            while (true)
            {
                DateTime datetime = DateTime.Now;
                int day = datetime.DayOfYear;
                bool daychanged = false;
                if (lastDay >= 0 && day != lastDay)
                {
                    daychanged = true;
                }
                if (daychanged && hour == datetime.Hour)
                {
                    try
                    {
                        program.commandManager.Reset(2);
                    }catch(Exception ex)
                    { }
                }
                lastDay = day;
                Thread.Sleep(1000);
            }
        }

        private StompClient stompClient;

        private string server;

        private CommandManager commandManager;

        public void Init(string server, string path, int hour)
        {
            this.server = server;
            commandManager = new CommandManager(server, path);
            commandManager.Reset(0);
            listen(server);
        }

        private void listen(string server)
        {
            this.server = server;
            stompClient = new StompClient();
            stompClient.Connect(server + "/muke-ws", "/topic/msger", onTopicMsg,null);
        }

        private void onTopicMsg(string msg)
        {
            if(string.IsNullOrEmpty(msg))
            {
                return;
            }
            try
            {
                HostCommand command = JsonConvert.DeserializeObject<HostCommand>(msg);
                commandManager.DoCommand(command);
            }
            catch(Exception ex)
            {
                Console.WriteLine("command:" + msg + " excetption:" + ex.Message);
            }
        }

        public string getAbsolutePath(string path)
        {
            if (path.IndexOf(":") >= 0 || path.IndexOf("/") >= 0 || path.StartsWith("\\")) return path;
            var dir = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().CodeBase.Substring(8));
            return dir + "\\" + path;
        }
    }

    class HostCommand
    {
        public string cmd;
        public string value;
    }
}
