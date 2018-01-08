using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using Sachiel.Messages;
using Sachiel.Messages.Packets;
using SachielExample.Handlers;
using SachielExample.Models;

namespace SachielExample
{

    internal class ConsumerExample : Consumer
    {
        //Can be any object you wish, depends on how you plan to handle replies.
        public override object SyncObject { get; set; }

        public override void Reply(PacketCallback packet)
        {
            //Usually you would send your network reply here, but for this example we will just deserialize our buffer
            //And display its contents.
            var response = new Message(packet.Response);
            var tree = response.Deserialize<TreeResponse>();
            foreach (var folder in tree.Tree.RootFolder.ChildFolders)
            {
                foreach (var file in folder.Files)
                {
                    Console.WriteLine(file.Path);
                }
            }
        }
    }

    internal class Program
    {

        private static async void RunExample()
        {



            //Loads all the packets in the application
            PacketLoader.LoadPackets();
            PacketLoader.CompileInPlace();
            Console.WriteLine($"{PacketLoader.Packets.Count} packets loaded");

            //Saves the schemas of all packets/responses
            PacketLoader.DumpSchemas("D:/test");
            Console.WriteLine("Saved schemas");


            var request = new RequestFileTree
            {
                Path = "D:/test",
                DeepScan = true
            };
            //Create a consume which will handle replies from messages
            var consomer = new ConsumerExample();

            //serialize your model to a Sachiel buffer. 
            var data = await request.Serialize(true);

            //Load a packet from a buffer
            var packet = Packet.GetPacket(data);

            //Handle the packet
            packet.HandlePacket(consomer);

        }

        private static void Main(string[] args)
        {
            RunExample();
            Console.Read();
        }
    }
}