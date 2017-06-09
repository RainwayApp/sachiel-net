using System;
using ProtoBuf;

namespace Sachiel.Messages
{
    internal static class MessageUtils
    {
        /// <summary>
        ///  We use reflection to get the Proto Schema from protobuff-net
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetSchema(Type type)
        {
            var method = typeof(Serializer).GetMethod("GetProto").MakeGenericMethod(type);
            method.Invoke(null, null);
            return (string) method.Invoke(null, null);
        }
    }
}