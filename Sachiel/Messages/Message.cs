using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProtoBuf;
using Sachiel.Extensions;
using Sachiel.Messages.Exceptions;
using Sachiel.Messages.Packets;

namespace Sachiel.Messages
{

    /// <summary>
    ///     The Message class is used to both serialize and deserialize Messages.
    /// </summary>
    [ProtoContract]
    public class Message
    {
        /// <summary>
        ///     Take a raw message buffer and extract the endpoint name and serialized proto buffer.
        /// </summary>
        /// <param name="messageData"></param>
        public Message(byte[] messageData)
        {
            using (var messageStream = new MemoryStream(messageData))
            using (var bReader = new BinaryReader(messageStream))
            {
                var length = (int) bReader.ReadVariableLengthQuantity();
                Header = GetHeader(bReader.ReadBytes(length));
                var rawBytes = new List<byte>();
                while (bReader.BaseStream.Position != bReader.BaseStream.Length)
                {
                    rawBytes.Add(bReader.ReadByte());
                }
                Raw = rawBytes.ToArray();
            }
        }

        /// <summary>
        /// Initiate a sachiel message and auto assign values if a SachielHeader attribute is present. 
        /// </summary>
        public Message()
        {
            var hasDataContractAttribute = GetType()
                .GetCustomAttributes(typeof(ProtoContractAttribute), true).Any();
            if (!hasDataContractAttribute)
                throw new InvalidModelException("Please add the ProtoContract Attribute to your source.");
            if (GetType() == typeof(Message)) return;
            Source = this;
            var attributes = Source.GetType().GetCustomAttributes(typeof(SachielHeader), false);
            if (attributes.Length <= 0 || attributes.ElementAt(0) == null) return;
            var info = (SachielHeader) attributes[0];
            Header.Endpoint = info.Endpoint;
        }


        /// <summary>
        ///     Endpoint and Synk key information are stored here and used to define each message so they can be identfied for
        ///     proper deserialization on other platforms.
        /// </summary>
        public Header Header { get; set; } = new Header();

        /// <summary>
        ///     This source instance is used during serialization
        /// </summary>
        public object Source { get; set; }
        
        /// <summary>
        ///     Serialized message buffer
        /// </summary>
        public byte[] Raw { get; set; }


        /// <summary>
        ///     Create and return a message in a static fashion
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="synckey"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        public static Message Instance(string endpoint, string synckey, object source)
        {
            return new Message {Header = new Header {Endpoint = endpoint, SyncKey = synckey}, Source = source};
        }

        /// <summary>
        ///     Create a message and return the serialized buffer
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="synckey"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        public static byte[] Serialized(string endpoint, string synckey, object source)
        {
            return new Message {Header = new Header{Endpoint = endpoint, SyncKey = synckey}, Source = source}
                .Serialize();
        }

        /// <summary>
        ///     Returns the schema for your Source
        /// </summary>
        /// <returns></returns>
        public string GetSchema()
        {
            return MessageUtils.GetSchema(Source.GetType());
        }

        /// <summary>
        ///     Deserializes a proto buffer to the Message based class of your choice and sets the Source property to the instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public object Deserialize<T>()
        {
            if (!IsCompatibile<T>())
                throw new InvalidSerializationException($"{typeof(T)} is not based on Message.");
            using (var memoryStream = new MemoryStream(Raw))
            {
                Source = Serializer.Deserialize<T>(memoryStream);
            }
            return Source;
        }
        /// <summary>
        /// Deserializes a message header from a message block
        /// </summary>
        /// <param name="data"></param>
        /// <returns>A message header containing the Endpoint name and Synckkey</returns>
        private Header GetHeader(byte[] data)
        {
            using (var memoryStream = new MemoryStream(data))
            {
                return Serializer.Deserialize<Header>(memoryStream);
            }
        }
        /// <summary>
        /// Check if a list contains a valid Message type object 
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        private bool ListContainsType(object o = null)
        {
            if (Source == null) return true;
            o = o ?? Source;
            return ((IEnumerable) o).Cast<object>().Any(item => item.GetType().IsSubclassOf(typeof(Message)));
        }
        /// <summary>
        /// Checks if a type is a List
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        private bool IsList(Type type)
        {
            return type.IsGenericType &&
                   type.GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>));
        }
        /// <summary>
        /// Checks if a type is a Dictionary
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        private bool IsDictionary(Type type)
        {
            return type.IsGenericType &&
                   type.GetGenericTypeDefinition().IsAssignableFrom(typeof(Dictionary<,>));
        }
        /// <summary>
        /// Determins if a given type can be serialized 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private bool IsCompatibile<T>()
        {
            return IsCompatibile(typeof(T));
        }
        /// <summary>
        /// Determins if a given type can be serialized 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private bool IsCompatibile(Type type)
        {
            if (type.IsSubclassOf(typeof(Message)))
                return true;
            if (IsList(type) && ListContainsType())
                return true;
            if (!IsDictionary(type)) return false;
            var list = ((IDictionary<object, object>) Source).Select(kvp => kvp.Value).ToList();
            return ListContainsType(list);
        }

     
        /// <summary>
        ///   Serializes a Message and sets the Raw property.
        /// </summary>
        /// <returns></returns>
        public byte[] Serialize()
        {
            if (!IsCompatibile(Source.GetType()))
                throw new InvalidSerializationException($"{Source.GetType()} is not based on Message.");
            using (var messageStream = new MemoryStream())
            using (var bWriter = new BinaryWriter(messageStream))
            {
                using (var headerStream = new MemoryStream())
                {
                    Serializer.Serialize(headerStream, Header);
                    var header = headerStream.ToArray();
                    bWriter.WriteVariableLengthQuantity((uint)header.Length);
                    bWriter.Write(header);
                }
                using (var protoStream = new MemoryStream())
                {
                    Serializer.Serialize(protoStream, Source);
                    bWriter.Write(protoStream.ToArray());
                }
                Raw = messageStream.ToArray();
            }
            return Raw;
        }
    }
}