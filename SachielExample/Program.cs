using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using ProtoBuf;
using Sachiel;
using Sachiel.Messages;
using Sachiel.Messages.Packets;
using SachielExample.Handlers;
using SachielExample.Models;
using WebSocketSharp.Server;
using File = System.IO.File;

namespace SachielExample
{
    internal class Program
    {

        public static HttpServer Wssv { get; set; }
        private static void SetupPackets()
        {
            var packetFileName = "Packets.bin";
            using (var fileStream = new FileStream(packetFileName, FileMode.Create))
            {
                PacketLoader.SavePackets(fileStream);
            }
            PacketLoader.LoadPackets(File.ReadAllBytes(packetFileName));
            Console.WriteLine($"{PacketLoader.Packets.Count} packets loaded");
        }

        private static void Main(string[] args)
        {
            SetupPackets();
            Console.Read();
            //new ExampleServer().Start();
        }
    }
}