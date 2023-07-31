using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wechatmsg.Services
{
    public interface IWechatServivce
    {
        Session GetSession(string appId, int roomId, string scanCode);

        bool IsWxLogin();
    }

    public class Session
    {
        public string appId;
        public int roomId;
        public string sessionId;
    }
}
