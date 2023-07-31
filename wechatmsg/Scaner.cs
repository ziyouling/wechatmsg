using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using wechatmsg;
using Windows.Globalization;
using Windows.Media.Ocr;
using Windows.Storage.Streams;
using Windows.System.UserProfile;

namespace wechatmsg
{
    public class Scaner 
    {

        private bool got;

        public Scaner()
        {
        }

        public Rect Scan(int x, int y, int width, int height, string destText, int timeoutInMs, ref bool gotResult)
        {
            got = false;
            DestBounds = Rect.Empty;
            long now = DateTime.Now.Ticks;
            long timeoutinTick = timeoutInMs * 10000;
            scanOne(x,y,width,height, destText);
            while (DateTime.Now.Ticks - now <= timeoutinTick)
            {
                if (InProcess)
                {
                    Thread.Sleep(100);
                    continue;
                }
                if (!got)
                {
                    scanOne(x, y, width, height, destText);
                }
                else
                {
                    gotResult = got;
                    return DestBounds;
                }
            }
            gotResult = got;
            return DestBounds;
        }

        private void scanOne(int x, int y, int width, int height, string destText)
        {
            InProcess = true;
            string path = Utils.GetAbsolutePath("wx_" +DateTime.Now.Ticks + ".bmp");
            try
            {
                Bitmap bitmap = Utils.GetBitmapFromScreen(x, y, width, height);

                bitmap.Save(path);
                extractText(path, "zh-Hans-CN", destText);
                bitmap.Dispose();

                Utils.Log("scan bitmap:" + path);
            }
            catch(Exception ex)
            {
                Utils.Log("scan :" + ex.Message);
            }
            IEnumerable<string> files = System.IO.Directory.EnumerateFiles(Utils.GetAbsolutePath(""), "wx_*.bmp");
            //保存最近1天的
            long tick = DateTime.Now.Ticks;
            long onedayTick = 24 * 60 * 60 * 10000000L;
            foreach (string file in files)
            {
                try
                {
                    DateTime date = System.IO.File.GetCreationTime(file);
                    if(tick - date.Ticks >= onedayTick)
                    {
                        File.Delete(file);
                    }
                   
                }
                catch (Exception ex)
                {

                }
            }
        }

        public  void extractText(string image, string languageCode, string destText)
        {
            if (!GlobalizationPreferences.Languages.Contains(languageCode))
            {
                foreach (string item in GlobalizationPreferences.Languages)
                {
                    Utils.Log("valid languageCode:" + item);
                }
                done(false);
                return;
            }
            StringBuilder text = new StringBuilder();
            Rect result = Rect.Empty;
            StringBuilder sb = new StringBuilder();
            using (var fileStream = File.OpenRead(image))
            {
                result = Rect.Empty;
                Windows.Foundation.IAsyncOperation<Windows.Graphics.Imaging.BitmapDecoder> asyncOperation = Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(fileStream.AsRandomAccessStream());
                while(asyncOperation.Status != Windows.Foundation.AsyncStatus.Completed)
                {
                    Thread.Sleep(10);
                }
                Windows.Graphics.Imaging.BitmapDecoder bmpDecoder = asyncOperation.GetResults();
                Windows.Foundation.IAsyncOperation<Windows.Graphics.Imaging.SoftwareBitmap> bitmaptAsync = bmpDecoder.GetSoftwareBitmapAsync();
                while (bitmaptAsync.Status != Windows.Foundation.AsyncStatus.Completed)
                {
                    Thread.Sleep(10);
                }
                var softwareBmp = bitmaptAsync.GetResults();

                var ocrEngine = OcrEngine.TryCreateFromLanguage(new Language(languageCode));

                Windows.Foundation.IAsyncOperation<OcrResult> orcAysc = ocrEngine.RecognizeAsync(softwareBmp);
                while (orcAysc.Status != Windows.Foundation.AsyncStatus.Completed)
                {
                    Thread.Sleep(10);
                }
                var ocrResult = orcAysc.GetResults();

                foreach (var line in ocrResult.Lines)
                {
                    // text.AppendLine(line.Text);
                    Utils.Log("got line:" + line.Text);
                    IReadOnlyList<OcrWord> words = line.Words;
                    sb.Clear();
                    foreach (OcrWord word in words)
                    {
                        string word1 = word.Text.Trim();
                        if(!string.IsNullOrEmpty(word1))
                        {
                            sb.Append(word1);
                        }
                        result.Union(new Rect(word.BoundingRect.X, word.BoundingRect.Y, word.BoundingRect.Width, word.BoundingRect.Height));
                        //log("word:" + word.Text + " rect:" + word.BoundingRect);
                    }
                    string line2 = sb.ToString();
                    //Utils.Log("got line:" + line2);
                    if (match(line2, destText))//line2 == destText || line2.Contains(destText))
                    {
                        this.DestBounds = result;
                        Utils.Log("found:" + line2 + " match:" + destText + " result:" + result);
                        done(true);
                        return;
                    }
                    result = Rect.Empty;

                }
            }
            done(false);
        }

        private bool match(string src, string destFull)
        {
            if(src == destFull || src.Contains(destFull))
            {
                return true;
            }
            //dest可能不全，占一定比例，就认为ok.
            int k = 0;
            int length = destFull.Length;
            int count = 0;
            for(int i = 0; i < length; i++)
            {
                string item = destFull.Substring(i, 1);

                for(int j = k; j < src.Length; j++)
                {
                    string jitem = src.Substring(j, 1);
                    if(jitem.Equals(item))
                    {
                        k = j;
                        count++;
                        break;
                    }
                }
            }
            return (destFull.Length -count ) <= 1;
        }

        private void done(bool ok)
        {
            InProcess = false;
            this.got = ok;
        }


        private Rect DestBounds { get; set; }

        private bool InProcess
        { get; set; }
    }
}
