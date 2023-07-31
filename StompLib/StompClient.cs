using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StompLib
{
    public class StompClient
    {
        //private string server = "https://course.muketang.com/muke-device";

        private string server = "http://localhost/muke-device";

        private string topic;

        private List<string> cmdList = new List<string>();


        private syp.biz.SockJS.NET.Client.SockJS sockjs;
        private StompMessageSerializer serializer = new StompMessageSerializer();

        private long openTick;

        private Action<string> callback;

        private Action connectCallback;

        private bool connecting;

        public void Connect(string server, string topic, Action<string> callback, Action connectCallback)
        {
            this.server = server;
            this.topic = topic;
            this.callback = callback;
            this.connectCallback = connectCallback;
            connecting = true;
            Console.WriteLine("****************** sockjs connecting..........");
            // syp.biz.SockJS.NET.Client.SockJS.SetLogger(new ConsoleLogger());
            if(sockjs != null)
            {
                sockjs.RemoveEventListener("open", onSocketJsOpen);
                sockjs.RemoveEventListener("message", onSocketJsMsg);
                sockjs.RemoveEventListener("close", onSocketJsClosed);
            }
            sockjs = new syp.biz.SockJS.NET.Client.SockJS(server);
            sockjs.AddEventListener("open", onSocketJsOpen);
            sockjs.AddEventListener("message", onSocketJsMsg);
            sockjs.AddEventListener("close", onSocketJsClosed);
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(5000);
                checkConnected();
            });
        }

        private void onSocketJsOpen(object sender, object[] e)
        {
            Console.WriteLine("****************** sockjs opened");
            openTick = DateTime.Now.Ticks;
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(1000);
                var connect = new StompMessage(StompFrame.CONNECT);
                connect["accept-version"] = "1.2";
                connect["host"] = "";
                connect["heart-beat"] = "0,10000";
                sockjs.Send(serializer.Serialize(connect));
            });
        }


        private void onSocketJsMsg(object sender, object[] e)
        {
            if (e.Length > 0 && e[0] is syp.biz.SockJS.NET.Client.Event.TransportMessageEvent msg)
            {
                var dataString = msg.Data.ToString();
                onMsg(dataString);
            }
        }

        private void onSocketJsClosed(object sender, object[] args)
        {
            Console.WriteLine($"******************sockjs closed:  ");
            Thread.Sleep(1000);
            reconnect();
        }

        public void ReConnect()
        {
            this.reconnect();
        }

        public bool IsConnected()
        {
            return !connecting;
        }

        public bool Send(string destination, object msg)
        {
            if(connecting)
            {
                return false;
            }
            try
            {
                string body = JsonConvert.SerializeObject(msg);
                var broad = new StompMessage(StompFrame.SEND, body);
                broad["content-type"] = "application/json";
                broad["destination"] = destination;
                sockjs.Send(serializer.Serialize(broad));
                return true;
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
            return false;
        }

        public void Close()
        {
            cmdList.Clear();
            if(sockjs != null)
            {
                sockjs.Close();
            }
            sockjs = null;
        }

        private void onMsg(string msg)
        {
            Console.WriteLine($"******************msg:" + msg);
            if (msg == null)
            {
                return;
            }
            if (msg.StartsWith("CONNECTED"))
            {
                openTick = 0;
                connecting = false;
                subscrible(this.topic);
                Task.Factory.StartNew(() => {
                    if (connectCallback != null)
                    {
                        connectCallback();
                    }
                });
            }
            else if (msg.StartsWith("MESSAGE"))
            {
                int index = msg.IndexOf('{');
                if (index <= 0)
                {
                    return;
                }
                msg = msg.Substring(index);
                Task.Factory.StartNew(() => {
                    if (callback != null)
                    {
                        callback(msg);
                    }
                });
            }
        }


        private void subscrible(string destination)
        {
            var sub = new StompMessage(StompFrame.SUBSCRIBE);
            sub["id"] = "sub-" + DateTime.Now.Ticks;
            sub["destination"] = destination;
            sockjs.Send(serializer.Serialize(sub));
        }


        private void checkConnected()
        {
            if (openTick == 0)
            {
                return;
            }
            long delta = (DateTime.Now.Ticks - openTick) / 10000;
            Console.WriteLine($"checkConnected... delta..." + delta);
            reconnect();
        }
        private void reconnect()
        {
            Connect(this.server, this.topic, this.callback, this.connectCallback);
        }
    }
}
