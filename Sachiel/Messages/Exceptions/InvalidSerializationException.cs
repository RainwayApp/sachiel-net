using System;

namespace Sachiel.Messages.Exceptions
{
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

    }
}
