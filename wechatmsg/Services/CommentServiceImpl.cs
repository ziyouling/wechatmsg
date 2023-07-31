using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wechatmsg.Services
{
    public class CommentServiceImpl : ICommentService
    {
        private Dictionary<string, LiveRoomMsgerTag> tags = new Dictionary<string, LiveRoomMsgerTag>();

        public void ChangeSession(string appId, int roomId, string sessionId)
        {
            LiveRoomMsgerTag tag = getTag(appId, roomId);
            tag.msger.ChangeSession(sessionId);
        }

        public void GetComment(string appId, string sessionId, int roomId, Action<CommentStatus> callback)
        {
            LiveRoomMsgerTag tag = getTag(appId, roomId);
            tag.msger.ChangeSession(sessionId);
            tag.callback = callback;
            tag.msger.Start(callback);
        }

        public void Stop(string appId, int roomId)
        {
            LiveRoomMsgerTag tag = getTag(appId, roomId);
            tag.msger.Stop();
            string key = appId + "#" + roomId;
            lock(tags)
            {
                tags.Remove(key);
            }
        }

        private LiveRoomMsgerTag getTag(string appId, int roomId)
        {
            lock(tags)
            {
                string key = appId + "#" + roomId;
                if (!tags.ContainsKey(key))
                {
                    LiveRoomMsgerTag tag = new LiveRoomMsgerTag();
                    tag.appId = appId;
                    tag.roomId = roomId;
                    tag.msger = new LiveRoomMsger(appId, roomId);
                    tags[key] = tag;
                }
                return tags[key];
            }
        }
    }

    class LiveRoomMsgerTag
    {
        public string appId;
        public int roomId;
        public LiveRoomMsger msger;

        public Action<CommentStatus> callback;
    }
}
