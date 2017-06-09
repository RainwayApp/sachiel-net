using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace Sachiel.Messages.Exceptions
{
    [Serializable]
    public class InvalidSerializationException : Exception
    {
        public InvalidSerializationException()
        {
        }

        public InvalidSerializationException(string message)
            : base(message)
        {
        }

        public InvalidSerializationException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected InvalidSerializationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            ResourceReferenceProperty = info.GetString("ResourceReferenceProperty");
        }

        public string ResourceReferenceProperty { get; set; }

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));
            info.AddValue("ResourceReferenceProperty", ResourceReferenceProperty);
            base.GetObjectData(info, context);
        }
    }
}
