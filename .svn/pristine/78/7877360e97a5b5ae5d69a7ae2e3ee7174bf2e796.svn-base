using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wechatmsg.Services
{
    public interface ICommentService
    {
        void GetComment(string appId, string sessionId, int roomId, Action<CommentStatus> callback);

        void ChangeSession(string appId, int roomId, string sessionId);

        void Stop(string appId, int roomId);
    }

    public class CommentStatus
    {
        public int msgId;

        public string appid;
        public int roomid;

        public CommentError code;
        public string errorMsg;
       
        public string avatar;
        public string nickname;
        public string content;

        public int like;
        public int online_uv;
        public int watch_uv;
        public int comment_pv;
        public int watch_pv;
    }

    public enum CommentError:int
    {
        OK = 0,
        SESSION_TIMEOUT,
        LIVE_END,
        UNKNOWN,
    }
}
