using System;
using System.Collections.Generic;

namespace HuePat.Util.IO.Networking.HTTP {
    public class PingServlet : IHttpServlet {
        private string route;

        public IReadOnlyDictionary<string, Action<IHttpRequest>> RouteMapping {
            get {
                return new Dictionary<string, Action<IHttpRequest>> {
                    { route, AnswerPing }
                };
            }
        }

        public PingServlet(string route) {
            this.route = route;
        }

        private void AnswerPing(IHttpRequest request) {
            request.Respond("ok");
        }
    }
}