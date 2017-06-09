using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
