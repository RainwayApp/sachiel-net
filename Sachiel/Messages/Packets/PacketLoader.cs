using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using ProtoBuf;
using ProtoBuf.Meta;
using Sachiel.Extensions;

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

        internal static TypeModel Serializer;

        /// <summary>
        ///     Turns a string into a class Type
        /// </summary>
        private static Type GetType(string v)
        {
            if (Type.GetType(v) != null)
            {
                return Type.GetType(v);
            }
            foreach (var a in SachielAppDomain.CurrentDomain.GetAssemblies())
            foreach (var t in a.GetTypes())
            {
                if (t.ToString().Equals(v))
                {
                    return t;
                }
                if (t.FullName.Equals(v))
                {
                    return t;
                }
                if (t.Name.Equals(v))
                {
                    return t;
                }
            }
            return null;
        }


        /// <summary>
        ///     Get each class with a defined SachielEndpoint
        /// </summary>
        /// <returns></returns>
        private static List<Type> GetTypesWithSachielAttribute()
        {
            return (from assembly in SachielAppDomain.CurrentDomain.GetAssemblies()
                from type in assembly.GetTypes()
                where !string.IsNullOrWhiteSpace(type.GetTypeInfo().GetCustomAttribute<SachielEndpoint>(true)?.Name)
                select type).ToList();
        }

       

        /// <summary>
        ///     Loop over each class with a SachielEndpoint and serialize them to a stream.
        /// </summary>
        /// <param name="stream"></param>
        private static void SavePackets(Stream stream)
        {
            var models = GetTypesWithSachielAttribute();
            var loaders = (from model in models
                let sachielInfo = model.GetTypeInfo().GetCustomAttribute<SachielEndpoint>(false)
                select new LoaderModel
                {
                    Type = model.FullName,
                    Handler = sachielInfo.Handler.FullName,
                    Endpoint = sachielInfo.Name
                }).ToList();
            var message = new Message
            {
                Source = loaders,
                Header = new Header
                {
                    Endpoint = "Packets"
                }
            }.Serialize();
            stream.Write(message, 0, message.Length);
        }

        /// <summary>
        /// Allows for ahead of time compiling and analysis of messages.
        /// This method will analyse from the root-types, adding in any additional types needed as it goes, setting the compiled generated instance Serializer instance.
        /// </summary>
        public static void Compile()
        {
            var model = TypeModel.Create();
            foreach (var packet in Packets.Values)
            {
                model.Add(packet.Type, true);
            }
            foreach (var type in GetTypesWithSachielHeader())
            {
                model.Add(type, true);
            }
            Serializer = model.Compile();
        }

        /// <summary>
        /// Allows for partial ahead of time compiling and analysis of messages.
        /// This method will not fully expand the models
        /// </summary>
        public static void CompileInPlace(int iterations = 5)
        {
            foreach (var packet in Packets.Values)
            {
                RuntimeTypeModel.Default.Add(packet.Type, true);
            }
            foreach (var type in GetTypesWithSachielHeader())
            {
                RuntimeTypeModel.Default.Add(type, true);
            }
            for (var i = 0; i < iterations; i++)
            {
                RuntimeTypeModel.Default.CompileInPlace();
            }
        }



        /// <summary>
        ///     Saves all response/request models to raw schemas.
        ///     Call after loading all your packets.
        /// </summary>
        /// <param name="path"></param>
        public static void DumpSchemas(string path, bool removePackage = true)
        {
            var requestPath = Path.Combine(path, "Request");
            var responsePath = Path.Combine(path, "Responses");
            Directory.CreateDirectory(requestPath);
            Directory.CreateDirectory(responsePath);
            //save our packets
            foreach (var packet in Packets.ToList())
            {
                var requestName = Path.Combine(requestPath, $"{packet.Key}.schema");
                File.WriteAllText(requestName, GetSchemaForType(packet.Value.Type, removePackage));
            }
            foreach (var type in GetTypesWithSachielHeader())
            {
                var endpoint = type.GetTypeInfo().GetCustomAttribute<SachielHeader>(false).Endpoint;
                var responseName = Path.Combine(responsePath, $"{endpoint}.schema");
                File.WriteAllText(responseName, GetSchemaForType(type, removePackage));
            }
        }

        private static string GetSchemaForType(Type type, bool removePackage)
        {
            var schema = MessageUtils.GetSchema(type);
            if (removePackage)
            {
                var builder = new StringBuilder();
                foreach (var myString in schema.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (myString.Contains("package"))
                    {
                        continue;
                    }
                    builder.AppendLine(myString);
                }
                schema = builder.ToString();
            }
            return schema;
        }

        /// <summary>
        ///     Get each class with a defined SachielEndpoint
        /// </summary>
        /// <returns></returns>
        private static List<Type> GetTypesWithSachielHeader()
        {
            return (from assembly in SachielAppDomain.CurrentDomain.GetAssemblies()
                from type in assembly.GetTypes()
                where type.GetTypeInfo().GetCustomAttributes(typeof(SachielHeader), true).Any()
                select type).ToList();
        }

        /// <summary>
        ///     Load packets from the serialized packet buffer
        /// </summary>
        /// <param name="packetBuffer"></param>
        public static void LoadPackets()
        {
            byte[] packetBuffer;
            using (var memoryStream = new MemoryStream())
            {
                SavePackets(memoryStream);
                packetBuffer = memoryStream.ToArray();
            }
            var packets = new Message(packetBuffer).Deserialize<List<LoaderModel>>();
            foreach (var packet in packets)
            {
                var endpointName = packet.Endpoint;
                var handlerName = packet.Handler;
                var type = GetType(packet.Type);
                if (type == null)
                {
                    throw new InvalidCastException(
                        $"\"{packet.Type}\" could not be found as a valid type.");
                }
                var handler = GetType(handlerName);
                if (handler == null)
                {
                    throw new InvalidCastException(
                        $"\"{packet.Handler}\" could not be found as a valid type");
                }
                Packets.Add(endpointName, new PacketInfo
                {
                    Type = type,
                    Handler = handler
                });
            }
        }
    }
}