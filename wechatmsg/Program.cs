using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wechatmsg.Services;

namespace wechatmsg
{
    class Program
    {
        static void Main(string[] args)
        {
            //if(args != null && args.Length < 3)
            //{
            //    Console.WriteLine("Usage: webchatmsg.exe app_id room_id session_id");
            //    return;
            //}
            //string appId = args[0];
            //string roomId = args[1];
            //string sessionId = args[2];
            //Console.WriteLine("got appid:" + appId);
            //Console.WriteLine("got roomId:" + roomId);
            //Console.WriteLine("got sessionId:" + sessionId);
            ////LiveRoomMsger liveRoomMsger = new LiveRoomMsger("wx3823924b2804b672", 2809);
            //////14:12
            ////liveRoomMsger.ChangeSession("TzxEQo_pnWiagCyWq0uXv1Cn4u5v2w5idvWoQM_0zjICAkblqUSTAQVnV1O5SSkVPeY-qP-4cS4xOyJs8gsM05udG02pAirsErb6I3G0fYtkabP32sRXlzx-lOLG3e5XyuRZj3xEanZMm2djSg2lb2PjoERSbY3aSwatVNpenAW-93xAQF7pPBbP7W_VKah_8OpgU78jWqyKiM04yaq_pA1hx6NcwNccyA21Ph4lyko");
            ////liveRoomMsger.Start();

            //LiveRoomMsger liveRoomMsger = new LiveRoomMsger(appId, int.Parse(roomId));
            ////14:12
            //liveRoomMsger.ChangeSession(sessionId);
            //liveRoomMsger.Start();
            ICommentService comment = new CommentServiceImpl();
            ISessionService session = new SessionServiceImpl();
            CommentManager manager = new CommentManager("https://course.muketang.com",comment, session);
            manager.Start();
            Console.ReadLine();
        }
    }
}
