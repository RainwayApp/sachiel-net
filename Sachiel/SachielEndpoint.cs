using System;

namespace Sachiel
{
    /// <summary>
    /// Indicates a model is used as an endpoint for packets
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class SachielEndpoint : Attribute
    {
        /// <summary>
        /// The endpoint name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The endpoint handler
        /// </summary>
        public Type Handler { get; set; }
    }
}
