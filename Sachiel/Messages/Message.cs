using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
using Sachiel.Extensions.Arrays;
using Sachiel.Extensions.Binary;
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
            unsafe
            {
                var segmentReader = new UnsafeBinaryReader(Encoding.UTF8);
                fixed (byte* mData = messageData)
                {
                    var segment = new BufferSegment(mData);
                    segmentReader.SetBuffer(segment.Data, messageData.Length);
                    var length = segmentReader.ReadVariableLengthQuantity();
                    fixed (byte* headerData = segmentReader.ReadBytes(length))
                    {
                        Header = GetHeader(headerData, length);
                    }
                    Payload = segmentReader.ReadBytes(segmentReader.AvailableData());
                }
            }
        }

        /// <summary>
        /// Contains the payload of a Sachiel message.
        /// </summary>
        internal byte[] Payload { get; set; }


        /// <summary>
        /// Initiate a sachiel message and auto assign values if a SachielHeader attribute is present. 
        /// </summary>
        public Message()
        {
            var hasDataContractAttribute = GetType().GetTypeInfo()
                .GetCustomAttributes(typeof(ProtoContractAttribute), true).Any();
            if (!hasDataContractAttribute)
                throw new InvalidModelException("Please add the ProtoContract Attribute to your source.");
            if (GetType() == typeof(Message)) return;
            Source = this;
            var attributes = Source.GetType().GetTypeInfo().GetCustomAttributes(typeof(SachielHeader), false).ToList();
            if (!attributes.Any() || attributes.ElementAt(0) == null) return;
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
        internal object Source { get; set; }


        /// <summary>
        ///     Deserializes a Sachiel payload into its original model, if this method was already called it returns a cached copy of the payload.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Deserialize<T>()
        {
            if (Source != null)
            {
                return (T) Source;
            }
            unsafe
            {
                if (!IsCompatibile<T>())
                    throw new InvalidSerializationException($"{typeof(T)} is not based on Message.");
                fixed (byte* payloadData = Payload)
                {
                    Source = MessageUtils.Deserialize<T>(payloadData, Payload.Length);
                    return (T) Source;
                }
            }
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
        /// Deserializes a message header from a message block
        /// </summary>
        /// <param name="data"></param>
        /// <param name="length"></param>
        /// <returns>A message header containing the Endpoint name and Synckkey</returns>
        private unsafe Header GetHeader(byte* data, int length)
        {
            return MessageUtils.Deserialize<Header>(data, length);
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
            return ((IEnumerable) o).Cast<object>()
                .Any(item => item.GetType().GetTypeInfo().IsSubclassOf(typeof(Message)));
        }

        /// <summary>
        /// Checks if a type is a List
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        private bool IsList(Type type)
        {
            return type.GetTypeInfo().IsGenericType &&
                   type.GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>));
        }

        /// <summary>
        /// Checks if a type is a Dictionary
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        private bool IsDictionary(Type type)
        {
            return type.GetTypeInfo().IsGenericType &&
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
            if (type.GetTypeInfo().IsSubclassOf(typeof(Message)))
                return true;
            if (IsList(type) && ListContainsType())
                return true;
            if (!IsDictionary(type)) return false;
            var list = ((IDictionary<object, object>) Source).Select(kvp => kvp.Value).ToList();
            return ListContainsType(list);
        }


        private void Serialize(MemoryStream stream, object value)
        {
            if (PacketLoader.Serializer != null)
            {
                PacketLoader.Serializer.Serialize(stream, value);
            }
            else
            {
                Serializer.Serialize(stream, value);
            }
        }

        /// <summary>
        ///   Serializes a Message to a Sachiel buffer and sets the Raw property.
        ///   If you are attempting to serialize a request endpoint, set includeEndpointAsHeader to true to automatically set the header. 
        /// </summary>
        /// <returns></returns>
        public async Task<byte[]> Serialize(bool includeEndpointAsHeader = false)
        {
            if (!IsCompatibile(Source.GetType()))
                throw new InvalidSerializationException($"{Source.GetType()} is not based on Message.");

            // For IPC purposes people might wish to use request both ways, this should allow them to do that. 
            if (includeEndpointAsHeader)
            {
                var attribute = Source.GetType().GetTypeInfo().GetCustomAttribute<SachielEndpoint>(false);
                if (attribute == null)
                    throw new InvalidOperationException("No SachielEndpoint attribute is present on message.");
                Header.Endpoint = attribute?.Name;
            }
            //Throw is the header is null.
            if (string.IsNullOrEmpty(Header?.Endpoint))
            {
                throw new InvalidOperationException("Message headers cannot be null.");
            }
            using (var messageStream = new MemoryStream())
            {
                using (var headerStream = new MemoryStream())
                {
                    Serialize(headerStream, Header);
                    await UnsafeArrayIo.WriteArray(messageStream, BinaryExtensions.EncodeVariableLengthQuantity((ulong) headerStream.Length), true);
                    await UnsafeArrayIo.WriteArray(messageStream, headerStream.ToArray(), true);
                    Serialize(messageStream, Source);
                }
                return messageStream.ToArray();
            }
        }
    }
}