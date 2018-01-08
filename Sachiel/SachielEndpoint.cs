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

        /// <summary>
        /// If set to true, we will spawn a dedicated thread to prevent behavior
        /// which could be considered an abuse of the thread pool
        /// </summary>
        public bool Expensive { get; set; }
    }
}
