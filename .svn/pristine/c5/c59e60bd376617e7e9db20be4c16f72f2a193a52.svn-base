using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Forms;

namespace wechatmsg.Services
{
    class WechatServiceImpl : IWechatServivce
    {
        private Thread thread;
        private Win32ServiceImpl win32;

        private IntPtr wechatHwnd;

        private Scaner scaner;


        public WechatServiceImpl()
        {
            scaner = new Scaner();
            thread = new Thread(new ThreadStart(runInThread));
            //thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();
        }

        private void runInThread()
        {
          
            //2,
        }

  
        private void prepareWxEnv()
        {
            //1,移动微信到指定位置；//
            Utils.Log("获取微信窗口...");
            if (win32 == null)
            {
                win32 = new Win32ServiceImpl();
            }
            wechatHwnd = win32.FindWindow("WeChatMainWndForPC", null);
            while (wechatHwnd.ToInt32() == 0)
            {
                Thread.Sleep(1000);
                wechatHwnd = win32.FindWindow("WeChatMainWndForPC", null);
            }
            Utils.Log("Got 微信窗口");
            win32.SwitchToThisWindow(wechatHwnd, true);
            win32.ChangePosition(wechatHwnd, 0, 0);
            win32.ResizeWindow(wechatHwnd, 720, 800);
            Thread.Sleep(1000);
            //置顶文件传输助手，然后点击
            win32.MouseClick(wechatHwnd, 150, 100);
        }

        public Session GetSession(string appId, int roomId, string scanCode)
        {
            try
            {
                if (!IsWxLogin())
                {
                    Utils.Log("wechat is logout and can't get session: " + appId + " roomid:" + roomId);
                    return null;
                }
                Utils.Log("wechat to get session: " + appId + " roomid:" + roomId);
                prepareWxEnv();
                return downloadAndScan(appId, roomId, scanCode);
            }
            catch(Exception ex)
            {
                Utils.Log("webchat getsession exception:" + ex.Message);
            }
            return null;
        }

        public bool IsWxLogin()
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
        #region download and scan


        private long imgIndex;

        /**
       * <summary>返回是否扫码行为进行ok，不代表结果</summary>
       */
        public Session downloadAndScan(string appId, int roomId, string sancode)
        {
            imgIndex = imgIndex % 10;
            string filepath = Utils.GetAbsolutePath(imgIndex + ".jfif");
            imgIndex++;

            string error = Utils.DownloadImage(sancode, filepath);
            if (!string.IsNullOrEmpty(error))
            {
                Utils.Log("下载图片失败!!!");
                return null;
            }
            System.Windows.Clipboard.SetImage(new System.Windows.Media.Imaging.BitmapImage(new Uri(filepath)));
            //定位到对话框
            win32.MouseClickRight(wechatHwnd, 400, 700);
            Thread.Sleep(500);
            //粘贴
            win32.MouseClick(wechatHwnd, 400 + 10, 700 + 10);
            Thread.Sleep(500);
            //发送
            //service.MouseMove(wechatHwnd, 650, 750);
            win32.MouseClick(wechatHwnd, 650, 750);
            Thread.Sleep(1500);

            //点击图片
            win32.MouseClick(wechatHwnd, 560, 500);
            //图片被打开
            IntPtr imgHwnd = win32.FindWindow("ImagePreviewWnd", null);
            int sleepcount = 0;
            while (imgHwnd.ToInt32() == 0 && sleepcount < 10)
            {
                Thread.Sleep(1000);
                imgHwnd = win32.FindWindow("ImagePreviewWnd", null);
                sleepcount++;
            }
            if (imgHwnd.ToInt32() <= 0)
            {
                Utils.Log("图片窗口获取超时!!!");
                File.Delete(filepath);
                closeAllWebAndImgWindow();
                return null;
            }
            Utils.Log("Got 图片窗口");
            Thread.Sleep(1000);

      
            //TODO 检查二维码菜单
            IntPtr destktop = new IntPtr(Win32NativeUtil.GetDesktopWindow());
            Rect bounds = Rect.Empty;
            int retryCount = 3;
            bool got = false;
            while(!got && --retryCount >= 0)
            {
                //识别二维码
                got = false;
                Utils.Log("开始检查2码菜单....");
                win32.SetForegroundWindow(imgHwnd);
                Thread.Sleep(300);
                win32.MouseClick(imgHwnd, 300, 300+240);
                win32.MouseClickRight(imgHwnd, 300, 300 + 240);
                Thread.Sleep(500);
                bounds = scaner.Scan(330, 160, 960, 750, "二维码", 5000, ref got);
                Utils.Log("结束检查2码:" + got + " bounds:" +  bounds);
            }
            if(!got)
            {
                Utils.Log("没有二维码扫码选项，返回...");
                File.Delete(filepath);
                closeAllWebAndImgWindow();
                return null;
            }

            Thread.Sleep(500);
            win32.MouseClick(imgHwnd, 300 + 50, 300 +240 + 85);

            sleepcount = 0;
            IntPtr webHwnd = win32.FindWindow("Chrome_WidgetWin_0", null);
            while (webHwnd.ToInt32() == 0 && sleepcount < 10)
            {
                Thread.Sleep(1000);
                webHwnd = win32.FindWindow("Chrome_WidgetWin_0", null);
                sleepcount++;
            }
            if (webHwnd.ToInt32() == 0)
            {
                Utils.Log("直播间窗口获取超时!!!");
                closeAllWebAndImgWindow();
                return null;
            }
            Thread.Sleep(1000);
            string tile = win32.GetTitle(webHwnd);
            string controlText = win32.GetControlText(webHwnd.ToInt32());

            Win32NativeUtil.Win32RECT rect;
            Win32NativeUtil.GetWindowRect(webHwnd, out rect);
            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;
            Utils.Log("GOT直播间窗口, title:" + tile + " controlText:" + controlText + " size:" + width + "," + height);

            Session readed = readSession();
            int count = 0;
            while(readed == null ||  readed.appId != appId || readed.roomId != roomId)
            {
                Thread.Sleep(1000);
                readed = readSession();
                count++;
                if(count >= 60)
                {
                    break;
                }
            }
            Session resultSession = null;
            if (readed != null && readed.appId == appId && readed.roomId == roomId && !string.IsNullOrEmpty(readed.sessionId))
            {
                resultSession = readed;
            }
            closeAllWebAndImgWindow();


            Utils.Log("获取到session:" + (resultSession != null));
            Thread.Sleep(1000);

            IEnumerable<string> reqList = System.IO.Directory.EnumerateFiles("C:\\mukewx", "wxmsg_req_*.txt");
            IEnumerable<string> respList = System.IO.Directory.EnumerateFiles("C:\\mukewx", "wxmsg_rep_*.txt");

            foreach(string item in reqList)
            {
                System.IO.File.Delete(item);
            }

            foreach (string item in respList)
            {
                System.IO.File.Delete(item);
            }

            return resultSession;
        }

        private void closeAllWebAndImgWindow()
        {
            IntPtr imgHwnd = win32.FindWindow("ImagePreviewWnd", null);
            int count = 10;
            while (imgHwnd.ToInt32() != 0 && --count >= 0)
            {
                win32.CloseWindow(imgHwnd);
                Thread.Sleep(100);
                imgHwnd = win32.FindWindow("ImagePreviewWnd", null);
            }
            Utils.Log("ImagePreviewWnd:" + (imgHwnd.ToInt32()));
            count = 10;
            imgHwnd = win32.FindWindow("Chrome_WidgetWin_0", null);
            while (imgHwnd.ToInt32() != 0 && --count >= 0)
            {
                win32.CloseWindow(imgHwnd);
                Thread.Sleep(100);
                imgHwnd = win32.FindWindow("Chrome_WidgetWin_0", null);
            }
            Utils.Log("Chrome_WidgetWin_0:" + (imgHwnd.ToInt32()));
        }

        private Session readSession()
        {
            IEnumerable<string> reqList = System.IO.Directory.EnumerateFiles("C:\\mukewx", "wxmsg_req_*.txt");
            IEnumerable<string> respList = System.IO.Directory.EnumerateFiles("C:\\mukewx", "wxmsg_rep_*.txt");
            if(reqList.Count() < 2)
            {
                return null;
            }
            long maxId = 0;
            long secondMaxId = 0;
            int prefixLength = "wxmsg_req_".Length;
            foreach(string item in reqList)
            {
                string name = System.IO.Path.GetFileName(item);
                long id = long.Parse(name.Substring(prefixLength, name.Length - 4-prefixLength));
                if(id > maxId)
                {
                    secondMaxId = maxId;
                    maxId = id;
                }
            }
            if(secondMaxId <= 0)
            {
                return null;
            }
            string maxReqPath = "C:\\mukewx\\wxmsg_req_" + secondMaxId + ".txt";
            string respondFile = "C:\\mukewx\\wxmsg_rep_" + secondMaxId + ".txt";
            if(!System.IO.File.Exists(respondFile))
            {
                return null;
            }
            StreamReader reader = new StreamReader(maxReqPath);
            string reqText = reader.ReadToEnd();
            reader.Close();

            StreamReader reader2 = new StreamReader(respondFile);
            string respondText = reader2.ReadToEnd();
            reader2.Close();


            JavaScriptSerializer serializer = new JavaScriptSerializer();
            Dictionary<string, object> dict = serializer.DeserializeObject(respondText) as Dictionary<string, object>;
            string sessionId = null;
            if (dict.ContainsKey("base"))
            {
                Dictionary<string, object> based = dict["base"] as Dictionary<string, object>;
                int ret = int.Parse(based["ret"].ToString());
                string errmsg = based["err_msg"].ToString();
                if (ret == 0)
                {
                    sessionId = based["sessionid"].ToString();
                }
            }
            Req req = serializer.Deserialize<Req>(reqText);
            string reqDataText = req.req.req_data;
            ReqData reqData = serializer.Deserialize<ReqData>(reqDataText);
            Session session = null;
            if (!string.IsNullOrEmpty(reqData.room_appid) && reqData.room_id != 0 && !string.IsNullOrEmpty(sessionId))
            {
                session = new Session();
                session.sessionId = sessionId;
                session.appId = reqData.room_appid;
                session.roomId = reqData.room_id;
            }
            return session;
        }

        #endregion



    }

    class LiveRoomSession
    {
        public string appId;
        public int roomId;
        public string sessionId;
        public long reqTick;
        public string scanCode;
        public int errorCount;
    }
}
