using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wechatmsg.Services
{
    public interface ISessionService
    {
        bool IsSessionLoading(string appId, int roomId);

        Task<string> GetSessionId(string appId, int roomId, string scanCode);

        void MarkInvalid(string appId, int roomId, string sessionId);

        void Stop();
    }
}
