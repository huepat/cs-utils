using System;
using System.Collections.Generic;

namespace HuePat.Util.IO.Networking.HTTP {
    public interface IHttpServlet {
        IReadOnlyDictionary<string, Action<IHttpRequest>> RouteMapping { get; }
    }
}