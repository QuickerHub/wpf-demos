using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Cea.Utils.Extension
{
    public static class ObjectExtensions
    {
        public static Dictionary<string, object> ConvertToDictionary(this object anonymousObject)
        {
            return anonymousObject
                .GetType()
                .GetProperties()
                .ToDictionary(property => property.Name, property => property.GetValue(anonymousObject));
        }
    }
}

