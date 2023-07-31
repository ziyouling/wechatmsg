using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using wechatmsg.Services;

namespace wechatmsg
{
    public class LiveRoomMsger
    {
        private string appId;
        private int roomid;
        private string sessionId;
        private int receivedMsgId;

        private Stat lastStat;

        private bool isStopped;

        private bool running;

        private Action<CommentStatus> callback;
        public LiveRoomMsger(string appId, int roomId)
        {
            this.appId = appId;
            this.roomid = roomId;
        }

        public void Start(Action<CommentStatus> callback)
        {
            if (running)
            {
                return;
            }
            this.callback = callback;
            Task.Factory.StartNew(() => { runInThread(); });
        }

        public void ChangeSession(string session)
        {
            this.sessionId = session;
            Utils.Log("new session id: " + appId + " roomid:" + roomid  + " sessionid:" + session);
        }

        public void Stop()
        {
            isStopped = true;
        }

        private void runInThread()
        {
            running = true;
            Utils.Log("监听消息: " + appId + " roomid:" + roomid+ "--------");
            while (!isStopped)
            {

                if (string.IsNullOrEmpty(sessionId))
                {
                    Thread.Sleep(100);
                    continue;
                }
                try
                {
                    if (!send(sessionId))
                    {
                        break;
                    }
                }
                catch(Exception ex)
                {
                    Utils.Log("send exception:" + ex.Message);
                }
               
                Thread.Sleep(3000);
            }
            running = false;
        }


        private bool send(string sessionId)
        {
            string url = "https://servicewechat.com/wxaliveapp/live_route?pluginVersion=1.3.2&sessionid=" + sessionId;
            string result =  Utils.Post(url, getCommentReq(receivedMsgId));
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            Dictionary<string, object> item = serializer.DeserializeObject(result) as Dictionary<string, object>;
            if(item.ContainsKey("base"))
            {
                Dictionary<string, object> based = item["base"] as Dictionary<string, object>;
                int ret = int.Parse(based["ret"].ToString());
                string errmsg = based["err_msg"].ToString();
                if(ret == 0)
                {
                    RespondData respondData = serializer.Deserialize<RespondData>(item["data"].ToString());
                    RespondRawData rawData = respondData.resp;
                    RespondRawDataDetail detail = serializer.Deserialize<RespondRawDataDetail>(rawData.raw_data);

                    if(detail.comments != null && detail.comments.list.Count > 0)
                    {
                        List<Comment> comments = detail.comments.list;
                        foreach (Comment comment in comments)
                        {
                            this.receivedMsgId = comment.msg_id;
                            CommentContent content = serializer.Deserialize<CommentContent>(comment.msg_content);
                            Utils.Log(appId + " roomid:" + roomid + " msg:" + content.nickname + " " + content.content);
                            fireEvent(CommentError.OK, content, detail.stat, comment.msg_id);
                        }
                    }
                    else if(detail.live_info != null && (detail.live_info.status_code == 103 || detail.live_info.status_code == 107))
                    {
                        Utils.Log("退出: " + appId + " roomid:" + roomid + "status:" + detail.live_info.status_code);
                        fireEvent(CommentError.LIVE_END, null, detail.stat, 0);
                        return false;
                    }else if(lastStat != null && detail.stat != null &&( lastStat.online_uv != detail.stat.online_uv || lastStat.like != detail.stat.like || lastStat.watch_pv != detail.stat.watch_pv))
                    {
                        fireEvent(CommentError.OK, null, detail.stat, 0);
                    }
                    lastStat = detail.stat;
                }
                else
                {
                    Utils.Log(appId + " roomid:" + roomid + " msg:" + errmsg + " code: " + ret);
                    if(ret == 200003)
                    {
                        fireEvent(CommentError.SESSION_TIMEOUT, null, null, 0);
                    }
                    else
                    {
                        fireUnknown(CommentError.UNKNOWN, ret, errmsg);
                    }          
                }
            }
            return true;
        }

        private void fireUnknown(CommentError error,int errorCode, string errorMsg)
        {
            if (callback == null)
            {
                return;
            }
            CommentStatus status = new CommentStatus();
            status.appid = appId;
            status.roomid = roomid;
            status.errorMsg = errorMsg + "(" + errorCode + ")";
            status.code = error;

            callback(status);
        }

        private void fireEvent(CommentError error, CommentContent content, Stat stat, int msgId)
        {
            if (callback == null)
            {
                return;
            }
            CommentStatus status = new CommentStatus();
            status.msgId = msgId;
            status.appid = appId;
            status.roomid = roomid;

            status.code = error;

            if(content != null)
            {
                status.avatar = content.avatar;
                status.nickname = content.nickname;
                status.content = content.content;
            }

            if (stat != null)
            {
                status.online_uv = stat.online_uv;
                status.watch_uv = stat.watch_uv;
                status.watch_pv = stat.watch_pv;
                status.comment_pv = stat.comment_pv;
                status.like = stat.like;
            }
            callback(status);
        }

        private string getCommentReq(int lastCommentId)
        {
            CommentReq commentReq = new CommentReq();
            commentReq.last_comment_id = lastCommentId;

            JavaScriptSerializer serializer = new JavaScriptSerializer();

            Req req = new Req();
            ReqItem reqItem = new ReqItem();

            ReqData reqData = new ReqData();
            reqData.room_id = roomid;
            reqData.room_appid = appId;
            reqData.timestamp = (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000)  / 10000;

            reqData.content = serializer.Serialize(commentReq);
            reqItem.req_data = serializer.Serialize(reqData);
            req.req = reqItem;
            string str = serializer.Serialize(req);
            return str;
        }
    }

    class Req
    {
        public ReqItem req;
    }

    class ReqItem
    {
        public string api_name= "LiveRoute";
        public string req_data;


    }

    class ReqData
    {
        public string action= "get_message";
        public int room_id;
        public string room_appid;
        public string plugin_appid = "wx2b03c6e691cd7370";
        public long timestamp;
        public string plugin_version= "1.3.2";
        public string phone_model = "microsoft";
        public string client_version = "3.4.5";

        public string content;
    }

    class CommentReq
    {
        public int comment=1;
        public int last_comment_id;
    }

    class Respond
    {
        public string data;
    }

    class RespondData
    {
        public RespondRawData resp;
    }

    class RespondRawData
    {
        public string raw_data;
    }

    class RespondRawDataDetail
    {
        public Comments comments;
        public LiveInfo live_info;
        public Stat stat;
    }

    class Comments
    {
        public List<Comment> list;

    }

    class Stat
    {
        public int like;
        public int online_uv;
        public int watch_uv;
        public int comment_pv;
        public int watch_pv;

    }

    class Comment
    {
        public int msg_id;
        public int room_id;
        public int msg_type;
        public int flag;
        public string msg_content;
    }

    class CommentContent
    {
        public string avatar;
        public string nickname;
        public string openid;
        public string id;
        public string content;
    }

    class LiveInfo
    {
        public LiveTime  live_time;
        // //  101：直播中，102：未开始，103已结束，104禁播，105：暂停，106：异常，107：已过期.-1:没有获取
        public int status_code;
        public int flag;
        public long create_time;
        public long update_time;
    }

    class LiveTime
    {
        public long start_time;
        public long finish_time;
    }

}
