using Sachiel.Messages.Packets;

namespace Sachiel.Messages
{
    /// <summary>
    ///     The Consumer class is used a way to uniquely manage handled packets.
    /// </summary>
    public abstract class Consumer
    {
        /// <summary>
        ///     The SyncObject should be set to an object that allows you to lookup or perform actions on a per message basis
        /// </summary>
        public abstract object SyncObject { get; set; }

        /// <summary>
        ///     The Reply function is used to handle completed messages.
        ///     If the message has come over wire, then you can use the SyncObject to set to send replies to a pre-defined object.
        ///     See the example application for more details.
        /// </summary>
        public abstract void Reply(PacketCallback packet);
    }
}