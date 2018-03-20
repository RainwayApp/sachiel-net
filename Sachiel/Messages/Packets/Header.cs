using ProtoBuf;
using ProtoBuf.Meta;

namespace Sachiel.Messages.Packets
{
    [ProtoContract]
    public class Header
    {
        [ProtoMember(1, IsRequired = true),]
        public string Endpoint { get; set; }
        [ProtoMember(2, IsRequired = true)]
        public string SyncKey { get; set; }

        /// <summary>
        /// Returns the .proto schema for your model  
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return MessageUtils.GetSchema(GetType(), ProtoSyntax.Proto2);
        }
    }
}
