using System;
using System.Collections.Generic;
using ProtoBuf;
using Sachiel;
using Sachiel.Messages;
using Sachiel.Messages.Packets;
using SachielExample.Models;

namespace SachielExample.Handlers
{
    [ProtoContract]
    [SachielHeader(Endpoint = "TreeResponse")]
    internal class TreeResponse : Message
    {
        [ProtoMember(1)]
        public FileTree Tree { get; set; }
    }

    internal class FilePacketHandler : PacketHandler
    {
        private PacketCallback _callback;
        private Consumer _consumer;
        private Message _message;
        private Packet _packet;

        public override void HandlePacket(Consumer consumer, Packet packet)
        {
            _consumer = consumer;
            _packet = packet;
            _message = _packet.Message;
            _callback = new PacketCallback {Endpoint = _message.Header.Endpoint };
            switch (_message.Header.Endpoint)
            {
                case "RequestFileTree":
                    RequestFileTree();
                    break;
            }
        }

        private void RequestFileTree()
        {
            var fileRequest = (RequestFileTree) _message.Source;
            Console.WriteLine($"Request for {fileRequest.Path} received");
            var response = new TreeResponse {Tree = new FileTree(fileRequest.Path, fileRequest.DeepScan)};
            _callback.Response = response.Serialize();
            _consumer.Reply(_callback);
        }
    }
}