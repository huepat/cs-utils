using Grapevine.Exceptions.Server;
using Grapevine.Server;
using System;

namespace HuePat.Util.IO.Networking.HTTP {
    public class HttpServer: IDisposable {
        private ServerSettings settings = new ServerSettings();
        private RestServer server;

        public bool LogToConsole { private get; set; }

        protected string HostIp {
            get {
                return settings.Host;
            }
        }

        protected int Port {
            get {
                return int.Parse(settings.Port);
            }
        }

        protected HttpServer() {
            settings = new ServerSettings();
            settings.Router = new Router();
            LogToConsole = true;
        }

        public HttpServer(string hostIp, int port): this() {
            Configure(hostIp, port);
        }

        public virtual void Dispose() {
            server.Dispose();
        }

        public HttpServer Register(IHttpServlet servlet) {
            foreach (string route in servlet.RouteMapping.Keys) {
                settings.Router.Register(
                    context => {
                        servlet.RouteMapping[route](new HttpRequest(context));
                        return context;
                    }, 
                    route);
            }
            return this;
        }

        public virtual void Start() {
            server = new RestServer(settings);
            try {
                if (LogToConsole) {
                    server.LogToConsole();
                }
                server.Start();
            }
            catch (UnableToStartHostException e) {
                throw new ApplicationException(
                    $"If an exception occurs here, you most probably have to run program with admin rights " +
                    $"when you use ip '+' as host: {e.Message}");
            }
        }

        public virtual void Stop() {
            server.Stop();
        }

        protected void Configure(string hostIp, int port) {
            settings.Host = hostIp;
            settings.Port = port.ToString();
        }
    }
}