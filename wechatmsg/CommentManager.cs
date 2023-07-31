using StompLib;
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
    public class CommentManager
    {
        private string server;

        private ICommentService commentService;

        private ISessionService wechatService;

   
        private StompClient stompClient;

        private Dictionary<string, LiveRoomItem> liveRooms = new Dictionary<string, LiveRoomItem>();

        private List<LiveRoomComment> comments = new List<LiveRoomComment>();

        public CommentManager(string server, ICommentService commentService, ISessionService wechatService)
        {
            this.server = server;
            this.commentService = commentService;
            //this.wechatService = wechatService;
            this.wechatService = new AppSessionServiceImpl(wechatService);
            //onTopicMsg("{\"id\":3741,\"updateTime\":\"2021 -12-07T06:37:50.030+00:00\", \"schoolId\":5,\"liveRoomId\":1347,\"living\":true,\"liveReal\":null,\"wxLiveState\":0,\"appId\":\"wxb45082dbda7c55fe\"}");
        }

        public void Start()
        {
            getLivingRoomListAndGetComments();
            listenLiveRoomStart();
            Task.Factory.StartNew(sendCommentsInThread);
        }

        private  void getLivingRoomListAndGetComments()
        {
            Utils.Log("getLivingRoomListAndGetComments");
            //1,获取正在开播的房间
            string result = Utils.Get(server + "/living_room_list");
            if (string.IsNullOrEmpty(result))
            {
                Utils.Delay(1000, getLivingRoomListAndGetComments);
                return;
            }
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            LivingRoomResp respond = serializer.Deserialize<LivingRoomResp>(result);
            //2,获取开播评论
            if(respond.code != 0)
            {
                Utils.Delay(1000, getLivingRoomListAndGetComments);
                return;
            }
            List<LiveRoomItem>  list = respond.result;
            if(list == null || list.Count <= 0)
            {
                return;
            }
            foreach(LiveRoomItem item in list)
            {
                loadComment(item);
            }
            //延迟30分钟
            Utils.Delay(30 * 60*1000, getLivingRoomListAndGetComments);
        }

        private void listenLiveRoomStart()
        {
            stompClient = new StompClient();
            stompClient.Connect(server + "/muke-ws", "/topic/live", onTopicMsg, onReConnect);
        }

        private void onReConnect()
        {
            getLivingRoomListAndGetComments();
        }

        /**
         * <summary>收到直播间开始的信息</summary>
         */
        private void onTopicMsg(string msg)
        {
            if(string.IsNullOrEmpty(msg))
            {
                return;
            }
            msg=msg.Replace("\0","");
            Utils.Log("on topic msg:" + msg);
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            LiveRoomState liveRoomState = null;
            try
            {
                liveRoomState = serializer.Deserialize<LiveRoomState>(msg);
            }
            catch(Exception ex)
            {
                Utils.Log("topic msg exception:" + ex.Message);
            }
            if(liveRoomState == null)
            {
                return;
            }
            if(!liveRoomState.living)
            {
                return;
            }

            loadComment(liveRoomState.schoolId, liveRoomState.appId, liveRoomState.liveRoomId);
        }

        private void loadComment(int schoolId, string appId, int roomId)
        {
            if(string.IsNullOrEmpty(appId) || roomId <= 0)
            {
                return;
            }
            string key = appId + "#" + roomId;
            LiveRoomItem item = getLiveRoom(key);
            if (item != null)
            {
                sendSessionGot(item);
                return;
            }
            item = new LiveRoomItem();
            item.appId = appId;
            item.roomId = roomId;
            item.schoolId = schoolId;
            loadComment(item);
        }

        private LiveRoomItem getLiveRoom(string key)
        {
            LiveRoomItem result = null;
            try
            {
                lock (liveRooms)
                {
                    if (liveRooms.ContainsKey(key))
                    {
                        result = liveRooms[key];
                    }
                }
            }
            catch(Exception ex)
            {

            }
            return result;
        }

        private async void loadComment(LiveRoomItem item)
        {
            string key = item.appId + "#" + item.roomId;
            Utils.Log("to load comment: " + item.appId + " roomid:" + item.roomId + "--------");
            string sessionId = null;
            //1,获取session
            lock(liveRooms)
            {
                if (!liveRooms.ContainsKey(key))
                {
                    liveRooms[key] = item;
                }
                sessionId = liveRooms[key].sessionId;
            }
            if(string.IsNullOrEmpty(sessionId))
            {
                if (string.IsNullOrEmpty(item.scancode))
                {
                    item.scancode = getScancode(item);
                }
                if(string.IsNullOrEmpty(item.scancode))
                {
                    //Utils.Delay(1000, () => { loadComment(item); });
                    Utils.Log("找不到scancode,school:" + item.schoolId + " room: " + item.roomId);
                    return;
                }
                Utils.Log("got scancode:" + item.schoolId + " room: " + item.roomId);
                sessionId = await wechatService.GetSessionId(item.appId, item.roomId, item.scancode);
            }
            if(string.IsNullOrEmpty(sessionId))
            {
                Utils.Log("找不到sessionid,延迟获取,school:" + item.schoolId + " room: " + item.roomId);
                Utils.Delay(1000, () => { loadComment(item); });
                return;
            }
            Utils.Log("got sessionid:" + item.schoolId + " room: " + item.roomId);

            //2，获取评论
            commentService.GetComment(item.appId, sessionId, item.roomId, onCommentGot);
            LiveRoomItem existItem = getLiveRoom(key);
            if (existItem != null)
            {
                existItem.sessionId = sessionId;
                sendSessionGot(existItem);
            }
        }


        private string getScancode(LiveRoomItem item)
        {
            string result = Utils.Get(server + "/sharecode_url?school_id=" + item.schoolId + "&room_id=" + item.roomId);
            if(string.IsNullOrEmpty(result))
            {
                return result;
            }
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            ScancodeResp respond = serializer.Deserialize<ScancodeResp>(result);
            if(string.IsNullOrEmpty(respond.result))
            {
                return respond.result;
            }
            return respond.result;
        }

        private void onCommentGot(CommentStatus status)
        {
            try
            {
                string key = status.appid + "#" + status.roomid;
                LiveRoomItem roomItem = getLiveRoom(key);
                switch (status.code)
                {
                    case CommentError.SESSION_TIMEOUT:
                        if (roomItem != null)
                        {
                            reloadSessionId(roomItem);
                        }
                        sendSessionTimeoutToServer(roomItem, status);
                        break;
                    case CommentError.LIVE_END:
                        commentService.Stop(status.appid, status.roomid);
                        lock (liveRooms)
                        {
                            liveRooms.Remove(key);
                        }
                        sendLiveStopToServer(roomItem, status);
                        break;
                    case CommentError.OK:
                        if (roomItem != null)
                        {
                            sendCommentToServer(roomItem, status);
                        }
                        break;
                    default:
                        if (roomItem != null)
                        {
                            sendCommentToServer(roomItem, status);
                        }
                        break;
                }
            }
            catch (Exception ex) { }
        }

        private void sendCommentToServer(LiveRoomItem roomItem, CommentStatus status)
        {
            if (!string.IsNullOrEmpty(status.content))
            {
                Utils.Log("消息 " + roomItem.schoolId + "(" + status.roomid + ")," + status.nickname + ":" + status.content);
            }
            else
            {
                Utils.Log("状态 " + roomItem.schoolId + "(" + status.roomid + "),online: " + status.online_uv + " like:" + status.like + " wathchuv:" + status.watch_uv + " watchpv:" + status.watch_pv);
            }
            LiveRoomComment comment = new LiveRoomComment();
            comment.msgId = status.msgId;
            comment.schoolId = roomItem.schoolId;
            comment.roomId = status.roomid;
            comment.appId = roomItem.appId;
            comment.sessionId = roomItem.sessionId;

            comment.errorMsg = status.errorMsg;

            comment.avatar = status.avatar;
            comment.nickname = status.nickname;
            comment.content = status.content;

            comment.onlineUV = status.online_uv;
            comment.watchUV = status.watch_uv;
            comment.watchPV = status.watch_pv;
            comment.commentPV = status.comment_pv;
            comment.like = status.like;

            comment.scancode = roomItem.scancode;
            sendComment(comment);
        }


        private void sendSessionTimeoutToServer(LiveRoomItem roomItem, CommentStatus status)
        {
            LiveRoomComment comment = new LiveRoomComment();
            comment.msgId = status.msgId;
            comment.schoolId = roomItem.schoolId;
            comment.roomId = status.roomid;
            comment.appId = roomItem.appId;
            comment.sessionId = roomItem.sessionId;
            comment.scancode = roomItem.scancode;

            comment.sessionTimeout = true;
            sendComment(comment);
        }

        private void sendLiveStopToServer(LiveRoomItem roomItem, CommentStatus status)
        {
            LiveRoomComment comment = new LiveRoomComment();
            comment.msgId = status.msgId;
            comment.schoolId = roomItem.schoolId;
            comment.roomId = status.roomid;
            comment.appId = roomItem.appId;
            comment.sessionId = roomItem.sessionId;
            comment.scancode = roomItem.scancode;

            comment.liveStop = true;

            sendComment(comment);
        }

        private void sendSessionGot(LiveRoomItem roomItem)
        {
            LiveRoomComment comment = new LiveRoomComment();
            comment.schoolId = roomItem.schoolId;
            comment.roomId = roomItem.roomId;
            comment.appId = roomItem.appId;
            comment.sessionId = roomItem.sessionId;
            comment.scancode = roomItem.scancode;

            comment.sessionTimeout = false;

            sendComment(comment);
        }

        private void sendComment(LiveRoomComment comment)
        {
            lock (comments)
            {
                while (comments.Count > 10000)
                {
                    comments.RemoveAt(0);
                }
                comments.Add(comment);
            }
        }

        private void sendCommentsInThread()
        {
            //string server1 = "http://localhost";
            while (true)
            {
                LiveRoomComment comment = null;
                lock (comments)
                {
                    if (comments.Count > 0)
                    {
                        comment = comments[0];
                    }
                }
                findWindowAndClose(null, "UpdateWnd");
                if (comment == null)
                {
                    Thread.Sleep(10);
                    continue;
                }
                string result = Utils.JsonPost(server + "/live_room_comment", comment, 5000);
                if(!string.IsNullOrEmpty(result))
                {
                    lock (comments)
                    {
                        comments.RemoveAt(0);
                    }
                }
                Thread.Sleep(10);
            }
        }

        private async void reloadSessionId(LiveRoomItem item)
        {
            string appId = item.appId;
            if(wechatService.IsSessionLoading(appId, item.roomId))
            {
                return;
            }
            wechatService.MarkInvalid(item.appId, item.roomId, item.sessionId);
            string sessionId = await wechatService.GetSessionId(item.appId, item.roomId, item.scancode);
            if(string.IsNullOrEmpty(sessionId))
            {
                return;
            }
            List<LiveRoomItem> rooms = new List<LiveRoomItem>();
            lock(liveRooms)
            {
                foreach(KeyValuePair<string, LiveRoomItem> pair in liveRooms)
                {
                    if(pair.Value.appId == appId)
                    {
                        rooms.Add(pair.Value);
                    }
                }
            }
            Utils.Log("Got new session id:" + item.appId + " session:" + sessionId);
            foreach(LiveRoomItem room in rooms)
            {
                room.sessionId = sessionId;
                commentService.ChangeSession(room.appId, room.roomId, sessionId);
                sendSessionGot(room);
            }
        }

        private void findWindowAndClose(string title, string clazz)
        {
            Win32ServiceImpl win32 = new Win32ServiceImpl();
            IntPtr hwnd = win32.FindWindow(clazz, title);
            if (hwnd.ToInt32() ==0)
            {
                return;
            }
            win32.CloseWindow(hwnd);
        }

    }

    class LivingRoomResp
    {
        public int code;
        public string errorMsg;
        public List<LiveRoomItem> result;
    }

    class ScancodeResp
    {
        public int code;
        public string errorMsg;
        public string result;
    }

    class LiveRoomItem
    {
        public int schoolId;
        public string appId;
        public string scancode;
        public int roomId;
        public string sessionId;
    }



    class LiveRoomState
    {
        //{"id":3741,"updateTime":"2021-12-07T06:37:50.030+00:00","schoolId":5,"liveRoomId":1347,"living":true,"liveReal":null,"wxLiveState":0,"appId":"wxb45082dbda7c55fe"}
        public int schoolId;

        public int liveRoomId;

        public bool living;

        public int wxLiveState;

        public string appId;
    }

    class LiveRoomComment
    {
        public int msgId;
        public int schoolId;
        public int roomId;

        public string avatar;
        public string nickname;
        public string content;

        public int like;
        public int onlineUV;
        public int watchUV;
        public int commentPV;
        public int watchPV;

        public string appId;
        public string sessionId;

        public bool liveStop;
        public bool sessionTimeout;
        public string errorMsg;
        public string scancode;
    }
}
