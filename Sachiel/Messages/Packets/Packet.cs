using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sachiel.Messages.Packets
{
    /// <summary>
    ///     Stores responses from handled packets
    /// </summary>
    public class PacketCallback
    {
        /// <summary>
        ///     The name of the completed endpoint
        /// </summary>
        public string Endpoint { get; set; }

        /// <summary>
        ///     The seralized response
        /// </summary>
        public byte[] Response { get; set; }
    }

    /// <summary>
    ///     Handles generating and storing Packet instances from serialized messages
    /// </summary>
    public class Packet
    {
        /// <summary>
        ///     The packet handler defined in the packet schema
        /// </summary>
        public Type Handler { get; set; }

        /// <summary>
        ///     The Message instance from the deserialized buffer
        /// </summary>
        public Message Message { get; set; }

        /// <summary>
        ///     If set to true, we will spawn a dedicated thread to prevent behavior
        ///     which could be considered an abuse of the thread pool
        /// </summary>
        public bool Expensive { get; set; }

        /// <summary>
        ///     Checks the loaded packets for the endpoint name and attempts to find handler information.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        private static PacketInfo GetPacketInfo(string endpoint)
        {
            return PacketLoader.Packets.ContainsKey(endpoint) ? PacketLoader.Packets[endpoint] : null;
        }

        /// <summary>
        ///     Accepts a serialized message buffer and attempts to deserialize and return a valid Packet object.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Packet GetPacket(byte[] data)
        {
            if (data.Length == 0) return null;
            var message = new Message(data);
            var packetInfo = GetPacketInfo(message.Header.Endpoint);
            if (packetInfo == null) return null;
            var method = message.GetType().GetMethod("Deserialize")?.MakeGenericMethod(packetInfo.Type);
            if (method != null)
            {
                method.Invoke(message, null);
                return new Packet
                {
                    Message = message,
                    Handler = packetInfo.Handler,
                    Expensive = packetInfo.Expensive
                };
            }

            return null;
        }

        /// <summary>
        ///     Executes a packet in an asynchronous manner
        /// </summary>
        /// <param name="consumer"></param>
        public void HandlePacket(Consumer consumer)
        {
            if (Handler == null) throw new InvalidOperationException("No packet handler has been set");
            if (Message == null) throw new InvalidOperationException("No message has been set");
            dynamic handler = Activator.CreateInstance(Handler);
            Task.Factory.StartNew(() => { handler.HandlePacket(consumer, this); }, CancellationToken.None,
                Expensive ? TaskCreationOptions.LongRunning : TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default);
        }
    }
}