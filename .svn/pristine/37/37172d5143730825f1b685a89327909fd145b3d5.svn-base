using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace wechatmsg.rest
{
    class CommandManager
    {
        private string server;

        private Win32ServiceImpl win32;
        private string fiddlerPath;
        public CommandManager(string server, string fiddlerPath)
        {
            this.server = server;
            this.fiddlerPath = fiddlerPath;
        }

        public void DoCommand(HostCommand command)
        {
            Task.Factory.StartNew(() => {
                switch (command.cmd)
                {
                    case "status":
                        respondStatus();
                        break;
                    case "img":
                        captureScreenAndUpload();
                        break;
                    case "mouse":
                        click(command.value);
                        break;
                    case "reset":
                        Reset(int.Parse(command.value));
                        break;
                }
            });
        }



        public void Reset(int code)
        {
            Console.WriteLine(DateTime.Now.ToLongTimeString() + ": reset" + code);
           
         

            //kill
            if (code == 0)
            {
                kill("Fiddler");

                kill("WeChat");
                kill("WeChatApp");
                kill("WechatBrowser");
                kill("WeChatPlayer");
               
                kill("wechatmsg");
            }
            else if(code == 1)
            {

                kill("WeChat");
                kill("WeChatApp");
                kill("WechatBrowser");
                kill("WeChatPlayer");

                kill("wechatmsg");
            }
            else if(code == 2)
            {
                kill("wechatmsg");
            }

            //start
            if (code == 0)
            {
                //Fiddler，
                start("Fiddler", fiddlerPath, "", new Point(640,690));
                Thread.Sleep(10000);
                findWindowAndClose(null, "#32770 (对话框)");
                //启动微信，
                start("WeChat", @"C:\Program Files (x86)\Tencent\WeChat\WeChat.exe", "", null);
                //2,启动scanwpf
                start("wechatmsg", getAbsolutePath("wechatmsg.exe"), "", new Point(1000, 0));
            }
            else if (code == 1)
            {

                //启动微信，
                start("WeChat", @"C:\Program Files (x86)\Tencent\WeChat\WeChat.exe", "", null);
                //2,启动scanwpf
                start("wechatmsg", getAbsolutePath("wechatmsg.exe"), "", new Point(1000, 0));
            }
            else if (code == 2)
            {
                //2,启动scanwpf
                findWindowAndClose(null, "UpdateWnd");
                start("wechatmsg", getAbsolutePath("wechatmsg.exe"), "", new Point(1000, 0));
            }
        }


        private void respondStatus()
        {
            Status status = new Status();
            status.msgerExist = processExist("wechatmsg");
            status.wxLogout =!isWxLogin();
            string result = Utils.JsonPost(server + "/msger/status_set", status);
            Utils.Log("status_set：" + result);
        }

        private void click(string location)
        {
            System.Windows.Point p = System.Windows.Point.Parse(location);
            if (win32 == null)
            {
                win32 = new Win32ServiceImpl();
            }
            Utils.Log("click:" + location);
            win32.MouseClickGlobal((int)p.X, (int)p.Y);
        }

        private void captureScreenAndUpload()
        {
            //1,截图上传
            Rectangle rect = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
            Bitmap memoryImage = new Bitmap(rect.Width, rect.Height);
            Graphics memoryGraphics = Graphics.FromImage(memoryImage);
            // 拷贝屏幕对应区域为 memoryGraphics 的 BitMap  
            memoryGraphics.CopyFromScreen(0, 0, 0, 0, new System.Drawing.Size(rect.Width, rect.Height));
            string url = getAbsolutePath("screen_" + DateTime.Now.Ticks + ".jpg");
            memoryImage.Save(url, System.Drawing.Imaging.ImageFormat.Jpeg);
            string fileId = upload(url);
            IEnumerable<string> items = System.IO.Directory.EnumerateFiles(getAbsolutePath(""), "screen_*.jpg");
            foreach (string item in items)
            {
                System.IO.File.Delete(item);
            }
            if(string.IsNullOrEmpty(fileId))
            {
                return;
            }
            string result = Utils.Get(server + "/msger/desktop_img_set?fileId=" + fileId);
            Utils.Log("desktop_img_set：" + result);
        }

        private string upload(string file)
        {
            try
            {
                WebClient client = new WebClient();
                string url = server + "/file_upload";
                Utils.Log("begin to upload:" + url + " file:" + file);
                Byte[] result = client.UploadFile(url, file);
                string str2 = Encoding.UTF8.GetString(result);
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                UrlFileRespond status = serializer.Deserialize<UrlFileRespond>(str2);
                if (status.code != 0 || status.result == null || string.IsNullOrEmpty(status.result.id))
                {
                    Utils.Log("failed to upload：" + status.code + " " + status.errorMsg);
                    return null;
                }
                String id = status.result.id;
                Utils.Log("end to upload, file id:" + id);
                return id;
            }
            catch (Exception ex) { Utils.Log("error:" + ex.Message); }
            return null;
        }

        private bool processExist(string name)
        {
            Process[] processList = Process.GetProcessesByName(name);
            return processList.Length > 0;
        }

        private bool isWxLogin()
        {
            if (win32 == null)
            {
                win32 = new Win32ServiceImpl();
            }
            IntPtr hwnd = win32.FindWindow("WeChatMainWndForPC", null);
            if (hwnd.ToInt32() == 0)
            {
                return false;
            }
            return true;
        }

        public string getAbsolutePath(string path)
        {
            if (path.IndexOf(":") >= 0 || path.IndexOf("/") >= 0 || path.StartsWith("\\")) return path;
            var dir = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().CodeBase.Substring(8));
            return dir + "\\" + path;
        }

        public void start(string name, string ext, string url, Point? location)
        {
            if (string.IsNullOrEmpty(ext))
            {
                return;
            }
            Console.WriteLine("start :" + url);
            Process process = new Process();
            Process p = Process.Start(ext, url);
            Thread.Sleep(2000);
            if (location != null && p.MainWindowHandle.ToInt32() != 0)
            {
                Win32ServiceImpl win32 = new Win32ServiceImpl();
                Point locationPoint = (Point)location;
                win32.ChangePosition(p.MainWindowHandle, (int)locationPoint.X, (int)locationPoint.Y);
            }
            Process[] processList = Process.GetProcessesByName(name);
            if (processList == null || processList.Count() <= 0)
            {
                Console.WriteLine(name + " exe is not found! restart ");
                start(name, ext, url, location);
            }

        }

        private void findWindowAndClose(string title, string clazz)
        {
            Win32ServiceImpl win32 = new Win32ServiceImpl();

            IntPtr hwnd = win32.FindWindow(clazz, title);
            int sleepcount = 0;
            while (hwnd.ToInt32() != 0 && sleepcount < 5)
            {
                Thread.Sleep(1000);
                win32.CloseWindow(hwnd);
                hwnd = win32.FindWindow(clazz, title);
                sleepcount++;
            }
        }

        public void kill(string name)
        {
            Process[] processList = Process.GetProcessesByName(name);
            foreach (Process process in processList)
            {
                try
                {
                    process.Kill();
                }
                catch (Exception ex) { }
            }
            Console.WriteLine(" kill: " + name);
        }

        class Status
        {
            public bool wxLogout;

            public bool msgerExist;
        }

        class UrlFileRespond
        {
            public int code;
            public string errorMsg;
            public string errorField;
            public UrlFile result;
        }

        class UrlFile
        {
            public string id;
            public string url;
        }
    }
}
