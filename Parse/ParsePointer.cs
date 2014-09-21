using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Parse
{
    /// <summary>
    /// Parse data type for an object reference to a ParseObject
    /// </summary>
    public class ParsePointer
    {
        public ParsePointer()
        {
        }
        public ParsePointer(ParseObject obj)
        {
            if (obj != null)
            {
                ObjectId = obj.objectId;
                ClassName = obj["Class"] as string;// GetClassName(obj.GetType());
            }
        }

        internal const string PARSE_TYPE = "Pointer";

        [JsonProperty(ParseObject.TYPE_PROPERTY)]
        internal readonly string Type = PARSE_TYPE;

        [JsonProperty("className")]
        public string ClassName { get; set; }

        [JsonProperty("objectId")]
        public string ObjectId { get; set; }

        public string GetClassName(Type type)
        {
            //if (typeof(ParseUser).IsAssignableFrom(type))
            //{
            //    return "_User";
            //}

            return type.Name;
        }
    }
}
