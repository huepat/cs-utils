using HuePat.Util.Object.Properties;
using System.Collections.Generic;

namespace HuePat.Util.Object {
    public interface IObject {
        Dictionary<string, IProperty> Properties { get; set; }
    }
}