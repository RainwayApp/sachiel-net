using System;
using Sachiel.Messages;
using Sachiel.Messages.Packets;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace SachielExample.Services
{
    internal class ExampleConsumer : Consumer
    {
        public override object SyncObject { get; set; }

        public override void Reply(PacketCallback packet)
        {
            var session = (IWebSocketSession) SyncObject;
            if (session != null && session.State == WebSocketState.Open)
            {
                Console.WriteLine("Replied to endpoint: " + packet.Endpoint);
                session.Context.WebSocket.Send(packet.Response);
            }
        }
    }

    public class Test : WebSocketBehavior
    {
        private ExampleConsumer _consumer;

        protected override void OnOpen()
        {
            if (!Sessions.TryGetSession(ID, out IWebSocketSession session)) return;
            Console.WriteLine("Connection from " + ID);
            _consumer = new ExampleConsumer {SyncObject = session};
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            var data = e.RawData;
            if (!Sessions.TryGetSession(ID, out IWebSocketSession session)) return;
            var packet = Packet.GetPacket(data);
            packet?.HandlePacket(_consumer);
        }
    }
}