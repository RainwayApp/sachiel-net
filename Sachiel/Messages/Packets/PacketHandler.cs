namespace Sachiel.Messages.Packets
{
    /// <summary>
    /// A base class used for inheriting the PacketHandler 
    /// </summary>
    public abstract class PacketHandler
    {
        /// <summary>
        ///     Using the HandlePacket void we are able to handle each endpoint in their own functions
        /// </summary>
        public abstract void HandlePacket(Consumer consumer, Packet packet);

        protected abstract string SyncKey { get; set; }
    }
}