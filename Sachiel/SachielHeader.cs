using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Framework;

namespace Sachiel
{
    /// <summary>
    /// Indicates a model is used as an endpoint for packets
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class SachielHeader : Attribute
    {
        /// <summary>
        /// The endpoint name
        /// </summary>
        [Required]
        public string Endpoint { get; set; }
    }
}
