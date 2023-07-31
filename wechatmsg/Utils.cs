using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;

namespace wechatmsg
{
    public static class Utils
    {

        private static Thread logThread;

        private static long tick;

        private static List<string> msgLst = new List<string>();

        public static string Get(string url, int timeout = 5000)
        {
            long tick = DateTime.Now.Ticks;
            try
            {
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                request.Method = "GET";
                request.Timeout = timeout;
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                StreamReader reader = new StreamReader(response.GetResponseStream());
                string line = reader.ReadToEnd();
               // Console.WriteLine("it take " + (DateTime.Now.Ticks - tick) / 10000 + " ms to request " + url);
                return line;
            }
            catch (Exception ex)
            {
                string ksg = ex.Message;
                Console.WriteLine(" exception: " + ksg);
            }
           // Console.WriteLine("it take " + (DateTime.Now.Ticks - tick) / 10000 + " ms to request " + url);
            return null;
        }

        public static string Post(string url, string body, int timeout = 5000)
        {
            long tick = DateTime.Now.Ticks;
            try
            {
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                request.Method = "POST";
                request.ContentType = "application/json";
                request.Timeout = timeout;
                using (StreamWriter dataStream = new StreamWriter(request.GetRequestStream()))
                {
                    dataStream.Write(body);
                    dataStream.Flush();
                    dataStream.Close();
                }
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                StreamReader reader = new StreamReader(response.GetResponseStream());
                string line = reader.ReadToEnd();
                // Console.WriteLine("it take " + (DateTime.Now.Ticks - tick) / 10000 + " ms to request " + url);
                return line;
            }
            catch (Exception ex)
            {
                string ksg = ex.Message;
                Console.WriteLine(" exception: " + ksg);
            }
            // Console.WriteLine("it take " + (DateTime.Now.Ticks - tick) / 10000 + " ms to request " + url);
            return null;
        }

        public static string JsonPost(string url, object body, int timeout = 5000)
        {
            long tick = DateTime.Now.Ticks;
            try
            {
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                request.Method = "POST";
                request.ContentType = "application/json";
                request.Timeout = timeout;
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                using (StreamWriter dataStream = new StreamWriter(request.GetRequestStream()))
                {
                    string json = serializer.Serialize(body);
                    dataStream.Write(json);
                    dataStream.Flush();
                    dataStream.Close();
                }
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                StreamReader reader = new StreamReader(response.GetResponseStream());
                string line = reader.ReadToEnd();
                // Console.WriteLine("it take " + (DateTime.Now.Ticks - tick) / 10000 + " ms to request " + url);
                return line;
            }
            catch (Exception ex)
            {
                string ksg = ex.Message;
                Console.WriteLine(" exception: " + ksg);
            }
            // Console.WriteLine("it take " + (DateTime.Now.Ticks - tick) / 10000 + " ms to request " + url);
            return null;
        }

        public static void Log(string msg)
        {
            if(logThread == null)
            {
                tick = DateTime.Now.Ticks;
                logThread = new Thread(new ThreadStart(logInThread));
                logThread.IsBackground = true;
                logThread.Start();
            }
            lock(msgLst)
            {
                string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss fff");
                msgLst.Add(time);
                msgLst.Add(msg);
            }
            Console.WriteLine(msg);
        }

        private static void logInThread()
        {
            while(true)
            {
                string msg = null;
                lock(msgLst)
                {
                    if(msgLst.Count > 0)
                    {
                        msg = msgLst[0];
                        msgLst.RemoveAt(0);
                    }
                }
                if(string.IsNullOrEmpty(msg))
                {
                    Thread.Sleep(10);
                    continue;
                }
                logFile(msg);
                Console.WriteLine(msg);
            }
        }

        private static void logFile(string msg)
        {
            try
            {

                StreamWriter writer = new StreamWriter(GetAbsolutePath("log_" + tick + ".txt"), true);
                writer.WriteLine(msg);
                writer.Flush();
                writer.Close();
            }
            catch(Exception ex)
            {

            }
        }

        public static string GetAbsolutePath(string path)
        {
            if (path.IndexOf(":") >= 0 || path.IndexOf("/") >= 0 || path.StartsWith("\\")) return path;
            var dir = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().CodeBase.Substring(8));
            return dir + "\\" + path;
        }

        public static async void Delay(int timeoutIsMs, Action callbak)
        {
            await Task.Factory.StartNew(() => {
                Thread.Sleep(timeoutIsMs);
            });
            callbak();
        }

        public static System.Drawing.Bitmap GetBitmapFromScreen(int x, int y, int width, int height)
        {
            Rectangle rect = System.Windows.Forms.Screen.PrimaryScreen.Bounds;;
            var bitmap = new System.Drawing.Bitmap(width, height);

            Graphics gfxScreenshot = Graphics.FromImage(bitmap);
            gfxScreenshot.CopyFromScreen(rect.Left + x, rect.Top + y, 0, 0, new Size(width, height));
            return bitmap;
        }

        public static string DownloadImage(string url, string dest)
        {
            string tmpPath = dest + ".tmp" + DateTime.Now.Ticks;
            string error = null;
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.ContentType = "	image/jpeg";
                request.Timeout = 10000;
                
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                HttpStatusCode code = response.StatusCode;
                Stream stream = response.GetResponseStream();

                if (code == HttpStatusCode.OK)
                {
                    FileStream file = File.Create(tmpPath);
                    byte[] bt = new byte[1024];
                    do
                    {
                        int length = stream.Read(bt, 0, 1024);
                        file.Write(bt, 0, length);
                        if (length <= 0)
                        {
                            break;
                        }
                    } while (true);
                    file.Close();
                }
                error = response.Headers.Get("error");
                if (!string.IsNullOrEmpty(error))
                {
                    error = HttpUtility.UrlDecode(error);
                }
            }
            catch (Exception e)
            {
                WebException we = e as WebException;
                if (we != null && we.Response != null)
                {
                    error = we.Response.Headers.Get("error");
                    if (!string.IsNullOrEmpty(error))
                    {
                        error = HttpUtility.UrlDecode(error);
                    }
                }
                if (string.IsNullOrEmpty(error))
                {
                    error = e.Message;
                }
            }
            if (string.IsNullOrEmpty(error))
            {
                if (File.Exists(dest))
                {
                    File.Delete(dest);
                }
                File.Move(tmpPath, dest);
            }
            else
            {
                File.Delete(tmpPath);
            }
            return error;
        }


    }
}
