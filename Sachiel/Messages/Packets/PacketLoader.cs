using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProtoBuf;

namespace Sachiel.Messages.Packets
{
    /// <summary>
    ///     Stores the handler and type of a pack.
    /// </summary>
    public class PacketInfo
    {
        public Type Handler { get; set; }
        public Type Type { get; set; }
    }

    /// <summary>
    ///     A model used to store cached packet information that is loaded at run-time.
    /// </summary>
    [ProtoContract]
    internal class LoaderModel : Message
    {
        [ProtoMember(1)]
        public string Handler { get; set; }

        [ProtoMember(2)]
        public string Type { get; set; }

        [ProtoMember(3)]
        public string Endpoint { get; set; }
        
    }
  
    public static class PacketLoader
    {
        /// <summary>
        ///     A dictionary containing packet information keyed by endpoint name.
        /// </summary>
        public static Dictionary<string, PacketInfo> Packets = new Dictionary<string, PacketInfo>();

        /// <summary>
        ///     Turns a string into a class Type
        /// </summary>
        private static Type GetType(string v)
        {
            if (Type.GetType(v) != null)
                return Type.GetType(v);
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            foreach (var t in a.GetTypes())
            {
                if (t.ToString().Equals(v))
                    return t;
                if (t.FullName.Equals(v))
                    return t;
                if (t.Name.Equals(v))
                    return t;
            }
            return null;
        }


        /// <summary>
        ///     Get each class with a defined SachielEndpoint
        /// </summary>
        /// <returns></returns>
        private static IEnumerable<Type> GetTypesWithSachielAttribute()
        {
            return from assembly in AppDomain.CurrentDomain.GetAssemblies()
                from type in assembly.GetTypes()
                where type.GetCustomAttributes(typeof(SachielEndpoint), true).Length > 0
                select type;
        }

        /// <summary>
        ///     Loop over each class with a SachielEndpoint and serialize them to a stream.
        /// </summary>
        /// <param name="stream"></param>
        public static void SavePackets(Stream stream)
        {
            var models = GetTypesWithSachielAttribute();
            var loaders = (from model in models
                let sachielInfo = (SachielEndpoint) model.GetCustomAttributes(typeof(SachielEndpoint), false)[0]
                select new LoaderModel
                {
                    Type = model.FullName,
                    Handler = sachielInfo.Handler.FullName,
                    Endpoint = sachielInfo.Name
                }).ToList();
            var message = new Message
            {
                Model = loaders,
                Header = new Header
                {
                    Endpoint = "Packets"
                }
            }.Serialize();
           stream.Write(message, 0, message.Length);
        }

        /// <summary>
        ///     Load packets from the serialized packet buffer
        /// </summary>
        /// <param name="packetBuffer"></param>
        public static void LoadPackets(byte[] packetBuffer)
        {
            var packets = (List<LoaderModel>) new Message(packetBuffer).Deserialize<List<LoaderModel>>();
            foreach (var packet in packets)
            {
                var endpointName = packet.Endpoint;
                var handlerName = packet.Handler;
                var type = GetType(packet.Type);
                if (type == null)
                    throw new InvalidCastException(
                        $"\"{packet.Type}\" could not be found as a valid type.");
                var handler = GetType(handlerName);
                if (handler == null)
                    throw new InvalidCastException(
                        $"\"{packet.Handler}\" could not be found as a valid type");
                Packets.Add(endpointName, new PacketInfo
                {
                    Type = type,
                    Handler = handler
                });
            }
        }
    }
}