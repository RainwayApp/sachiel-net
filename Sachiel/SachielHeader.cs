using System;
using System.ComponentModel.DataAnnotations;


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
