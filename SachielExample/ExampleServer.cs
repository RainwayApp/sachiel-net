using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using SachielExample.Services;
using WebSocketSharp.Server;

namespace SachielExample
{
    public class ExampleServer
    {
        private HttpServer _wssv;

        public void Start()
        {
            _wssv = new HttpServer(IPAddress.Any, 9999, false) { KeepClean = true };
            _wssv.AddWebSocketService("/Test", () => new Test());
            _wssv.Start();
            if (!_wssv.IsListening) throw new Exception("Server failed to start.");
            Console.WriteLine($"Listening on {_wssv.Address}:{_wssv.Port}, and providing WebSocket services:");
            foreach (var path in _wssv.WebSocketServices.Paths)
                Console.WriteLine($"- {path}");
        }
    }
}
