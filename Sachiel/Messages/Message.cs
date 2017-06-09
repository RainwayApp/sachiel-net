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
    ///     The Message class is used to both serialize and deserialize MessageModels.
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
                var payloadBytes = new List<byte>();
                while (bReader.BaseStream.Position != bReader.BaseStream.Length)
                {
                    payloadBytes.Add(bReader.ReadByte());
                }
                Payload = payloadBytes.ToArray();
            }
        }
        /// <summary>
        /// Ensure inherited classes have the [ProtoContract] attribute and assigns some values
        /// </summary>
        public Message()
        {
            var hasDataContractAttribute = GetType()
                .GetCustomAttributes(typeof(ProtoContractAttribute), true).Any();
            if (!hasDataContractAttribute)
                throw new InvalidModelException("Please add the ProtoContract Attribute to your model.");
            if (GetType() == typeof(Message)) return;
            Model = this;
            var attributes = Model.GetType().GetCustomAttributes(typeof(SachielHeader), false);
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
        ///     This model instance is used during serialization
        /// </summary>
        public object Model { get; set; }
        
        /// <summary>
        ///     Serialized message buffer
        /// </summary>
        public byte[] Payload { get; set; }


        /// <summary>
        ///     Create and return a message in a static fashion
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="synckey"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        public static Message Instance(string endpoint, string synckey, object model)
        {
            return new Message {Header = new Header {Endpoint = endpoint, SyncKey = synckey}, Model = model};
        }

        /// <summary>
        ///     Create a message and return the serialized buffer
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="synckey"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        public static byte[] Serialized(string endpoint, string synckey, object model)
        {
            return new Message {Header = new Header{Endpoint = endpoint, SyncKey = synckey}, Model = model}
                .Serialize();
        }

        /// <summary>
        ///     Returns the schema for your Model
        /// </summary>
        /// <returns></returns>
        public string GetSchema()
        {
            return MessageUtils.GetSchema(Model.GetType());
        }

        /// <summary>
        ///     Deserializes a proto buffer to the MessageModel of your choice and sets the Model property to the instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public object Deserialize<T>()
        {
            if (!IsCompatibile<T>())
                throw new InvalidSerializationException($"{typeof(T)} is not based on Message.");
            using (var memoryStream = new MemoryStream(Payload))
            {
                Model = Serializer.Deserialize<T>(memoryStream);
            }
            return Model;
        }

        private Header GetHeader(byte[] data)
        {
            using (var memoryStream = new MemoryStream(data))
            {
                return Serializer.Deserialize<Header>(memoryStream);
            }
        }

        private bool ListContainsType(object o = null)
        {
            if (Model == null) return true;
            o = o ?? Model;
            return ((IEnumerable) o).Cast<object>().Any(item => item.GetType().IsSubclassOf(typeof(Message)));
        }

        private bool IsList(Type type)
        {
            return type.IsGenericType &&
                   type.GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>));
        }

        private bool IsDictionary(Type type)
        {
            return type.IsGenericType &&
                   type.GetGenericTypeDefinition().IsAssignableFrom(typeof(Dictionary<,>));
        }

        private bool IsCompatibile<T>()
        {
            return IsCompatibile(typeof(T));
        }

        private bool IsCompatibile(Type type)
        {
            if (type.IsSubclassOf(typeof(Message)))
                return true;
            if (IsList(type) && ListContainsType())
                return true;
            if (!IsDictionary(type)) return false;
            var list = ((IDictionary<object, object>) Model).Select(kvp => kvp.Value).ToList();
            return ListContainsType(list);
        }

     
        /// <summary>
        ///     Serializes a Message and sets the Payload property.
        /// </summary>
        /// <returns></returns>
        public byte[] Serialize()
        {
            if (!IsCompatibile(Model.GetType()))
                throw new InvalidSerializationException($"{Model.GetType()} is not based on Message.");
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
                    Serializer.Serialize(protoStream, Model);
                    var payload = protoStream.ToArray();
                    bWriter.Write(payload);
                }
                Payload = messageStream.ToArray();
            }
            return Payload;
        }
    }
}