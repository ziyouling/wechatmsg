using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace wechatmsg.Services
{
    public class SessionServiceImpl : ISessionService
    {
        private Dictionary<string, LiveRoomSession> key2Sessions = new Dictionary<string, LiveRoomSession>();

        private Thread thread;

        private IWechatServivce wechat;

        private bool isStopped;

        public SessionServiceImpl()
        {
            thread = new Thread(new ThreadStart(runInThread));
            thread.IsBackground = true;
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            wechat = new WechatServiceImpl();
        }

        public void Stop()
        {
            isStopped = true;
        }


        public Task<string> GetSessionId(string appId, int roomId, string scanCode)
        {
            string key = getKey(appId, roomId);
            makeSessionIdInValid(appId, roomId, scanCode);
            return Task.Factory.StartNew<string>(() =>
            {
                while (true)
                {
                    LiveRoomSession session = null;
                    lock (key2Sessions)
                    {
                        if (key2Sessions.ContainsKey(key))
                        {
                            session = key2Sessions[key];
                        }
                    }
                    if (session != null && !string.IsNullOrEmpty(session.sessionId))
                    {
                        return session.sessionId;
                    }
                    Thread.Sleep(300);
                }
            });
        }

        private void runInThread()
        {
            long tick = DateTime.Now.Ticks;
            while(!isStopped)
            {
                long min = long.MaxValue;
                LiveRoomSession toGet = null;
                LiveRoomSession toGetTimeout = null;
                lock (key2Sessions)
                {
                    foreach (KeyValuePair<string, LiveRoomSession> pair in key2Sessions)
                    {
                        if (!string.IsNullOrEmpty(pair.Value.sessionId))
                        {
                            continue;
                        }
                        //如果重复不通过，不阻塞
                        if (pair.Value.reqTick < min)
                        {
                            min = pair.Value.reqTick;
                            if (pair.Value.errorCount >= 3)
                            {
                                toGetTimeout = pair.Value;
                            }
                            else
                            {
                                toGet = pair.Value;
                            }
                        }
                    }
                }
                if(toGet == null && toGetTimeout != null)
                {
                    toGet = toGetTimeout;
                }
             
                if (toGet == null)
                {
                    if(DateTime.Now.Ticks - tick >= 100000000)
                    {
                        Utils.Log("session server is running: queuecount: " + key2Sessions.Count);
                        tick = DateTime.Now.Ticks;
                    }
                    Thread.Sleep(100);
                    continue;
                }
                tick = DateTime.Now.Ticks;
                Utils.Log("begin to get session :" + toGet.appId + " room:" + toGet.roomId);
                Session session = wechat.GetSession(toGet.appId, toGet.roomId, toGet.scanCode);
                Utils.Log("end to get session :" + toGet.appId + " room:" + toGet.roomId + " result:" + session);
                if (session == null)
                {
                    Thread.Sleep(1000);
                    continue;
                }
                if(!string.IsNullOrEmpty(session.sessionId) && session.appId == toGet.appId && session.roomId == toGet.roomId)
                {
                    toGet.sessionId = session.sessionId;
                }
                else
                {
                    toGet.errorCount++;
                }
                Thread.Sleep(1000);
            }
        }

        private void makeSessionIdInValid(string appId, int roomId, string scanCode)
        {
            string key = getKey(appId, roomId);
            lock (key2Sessions)
            {
                key2Sessions.Remove(key);
                LiveRoomSession session = new LiveRoomSession();
                session.appId = appId;
                session.roomId = roomId;
                session.reqTick = DateTime.Now.Ticks;
                session.scanCode = scanCode;
                session.sessionId = null;
                session.errorCount = 0;
                key2Sessions[key] = session;

                Utils.Log("add session req to queue:" + appId + " roomId:" + roomId);
            }
        }

        private string getKey(string appId, int roomId)
        {
            return appId + "#" + roomId;
        }

        public void MarkInvalid(string appId, int roomId, string sessionId)
        {
           
        }

        public bool IsSessionLoading(string appId, int roomId)
        {
            throw new NotImplementedException();
        }
    }
}
