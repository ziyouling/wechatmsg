using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace wechatmsg.Services
{
    class AppSessionServiceImpl : ISessionService
    {
        private ISessionService session;
        private Dictionary<string, AppSession> appid2SessionIds = new Dictionary<string, AppSession>();

        public AppSessionServiceImpl(ISessionService session)
        {
            this.session = session;
        }

        public Task<string> GetSessionId(string appId, int roomId, string scanCode)
        {
            AppSession appSession = null;
            lock (this)
            {
                if (!appid2SessionIds.ContainsKey(appId))
                {
                    appid2SessionIds[appId] = new AppSession(session, appId);
                }
                appSession = appid2SessionIds[appId];
            }
            return appSession.GetSessionId(roomId, scanCode);
        }

        public bool IsSessionLoading(string appId, int roomId)
        {
            AppSession appSession = null;
            lock (this)
            {
                if (!appid2SessionIds.ContainsKey(appId))
                {
                    appid2SessionIds[appId] = new AppSession(session, appId);
                }
                appSession = appid2SessionIds[appId];
            }
            return appSession.IsSessionLoading();
        }

        public void MarkInvalid(string appId, int roomId, string sessionId)
        {
            lock (this)
            {
                if (!appid2SessionIds.ContainsKey(appId))
                {
                    return;
                }
                appid2SessionIds[appId].MarkInvalid(roomId, sessionId);
            }
        }
 
        public void Stop()
        {
           
        }



        //private Task<string> GetAppSessionId(string appId, int roomId, string scanCode)
        //{
        //    return Task.Factory.StartNew<string>(() => {
        //        AppSession session = null;
        //        lock (appid2SessionIds)
        //        {
        //            if (!appid2SessionIds.ContainsKey(appId))
        //            {
        //                appid2SessionIds[appId] = new AppSession();
        //            }
        //            session = appid2SessionIds[appId];
        //        }
        //        if (!session.loading && string.IsNullOrEmpty(session.sessionId))
        //        {
        //            loadAppSessionId(session, appId, roomId, scanCode);
        //        }
        //        while (session.loading)
        //        {
        //            Thread.Sleep(100);
        //        }
        //        return session.sessionId;
        //    });
        //}

        //private void loadAppSessionId(AppSession session, string appId, int roomId, string scanCode)
        //{
        //    session.loading = true;
        //    int count = 3;
        //    while (--count >= 0)
        //    {
        //        Task<string> task = wechatService.GetSessionId(appId, roomId, scanCode);
        //        task.Wait();
        //        string result = task.Result;
        //        if (!string.IsNullOrEmpty(result))
        //        {
        //            session.sessionId = result;
        //            break;
        //        }

        //    }
        //    session.loading = false;
        //}
    }


    class AppSession
    {
        public string appid;
        public bool loading;
        public string sessionId;

        private List<RoomReq> rooms = new List<RoomReq>();

        private ISessionService session;

        private bool sessionLoading;

        public AppSession(ISessionService session,string appid)
        {
            this.session = session;
            this.appid = appid;
            Utils.Log("create app session:" + this.appid );
            Task.Factory.StartNew(runInThread);
        }

        public bool IsSessionLoading()
        {
            return string.IsNullOrEmpty(sessionId) && sessionLoading;
        }

        public Task<string> GetSessionId(int roomId, string scanCode)
        {
            int index = indexOf(roomId);
            if(index < 0)
            {
                RoomReq req = new RoomReq();
                req.roomId = roomId;
                req.scanCode = scanCode;
                lock (rooms)
                {
                    rooms.Add(req);
                    //保存一些老直播间，同时也有一些新直播间
                    while(rooms.Count > 10)
                    {
                        rooms.RemoveAt(5);
                    }
                    Utils.Log("add room to app session load queue:" + this.appid + " room id:" + req.roomId);
                }  
            }
            return Task.Factory.StartNew<string>(() => {
                while(string.IsNullOrEmpty(sessionId))
                {
                    Thread.Sleep(100);
                }
                return sessionId;
            }) ;
        }

        private void runInThread()
        {
            int reqIndex = 0;
            long tick = DateTime.Now.Ticks;
            sessionLoading = false;
            while (true)
            {
                try
                {

                    if (!string.IsNullOrEmpty(this.sessionId))
                    {
                        if (DateTime.Now.Ticks - tick >= 100000000)
                        {
                            Utils.Log("app session is running: " + this.appid + " room count:" + rooms.Count + " sessionid:" + this.sessionId);
                            tick = DateTime.Now.Ticks;
                        }
                        Thread.Sleep(100);
                        continue;
                    }
                    RoomReq room = null;
                    lock (rooms)
                    {
                        int count = rooms.Count;
                        if(count != 0)
                        {
                            reqIndex = reqIndex % count;
                            if (reqIndex < count)
                            {
                                room = rooms[reqIndex];
                            }
                        }
                    }
                    if (room == null)
                    {
                        if (DateTime.Now.Ticks - tick >= 100000000)
                        {
                            Utils.Log("app session is running: " + this.appid + " room count:" + rooms.Count);
                            tick = DateTime.Now.Ticks;
                        }
                        Thread.Sleep(100);
                        continue;
                    }
                    tick = DateTime.Now.Ticks;
                    sessionLoading = true;
                    Utils.Log("begin to app session to get session id:" + this.appid + " roomId:" + room.roomId);
                    Task<string> task = session.GetSessionId(this.appid, room.roomId, room.scanCode);
                    task.Wait();
                    string result = task.Result;
                    Utils.Log("end to app session to get session id " + this.appid + " roomId:" + room.roomId + " result:" + result);
                    if (!string.IsNullOrEmpty(result))
                    {
                        this.sessionId = result;
                        sessionLoading = false;
                    }
                    reqIndex++;
                }
                catch (Exception ex)
                {
                    Utils.Log("app session error:" + this.appid + " exception:" + ex.Message);
                }

            }
        }

        public void MarkInvalid(int roomId, string sessionId)
        {
            if (this.sessionId == null || !this.sessionId.Equals(sessionId))
            {
                return;
            }
            this.sessionId = null;
        }

        private int indexOf(int roomId)
        {
            lock (rooms)
            {
                for (int i = 0; i < rooms.Count; i++)
                {
                    if (rooms[i].roomId == roomId)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }
    }

    class RoomReq
    {
        public int roomId;
        public string scanCode;
    }
}
