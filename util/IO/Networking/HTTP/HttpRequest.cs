using Grapevine.Interfaces.Server;
using Grapevine.Server;
using Grapevine.Shared;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Web;

namespace HuePat.Util.IO.Networking.HTTP {
    class HttpRequest: IHttpRequest {
        private IHttpContext httpContext;

        public HttpRequest(IHttpContext httpContext) {
            this.httpContext = httpContext;
        }

        public void RespondOk() {
            httpContext.Response.SendResponse(HttpStatusCode.Ok);
        }

        public void Respond(string response) {
            httpContext.Response.SendResponse(response);
        }

        public void Respond(Exception e) {
            httpContext.Response.SendResponse(HttpStatusCode.InternalServerError, e.Message);
            httpContext.Response.Abort();
        }

        public void Respond<T>(T response) {
            httpContext.Response.SendResponse(
                JsonConvert.SerializeObject(response));
        }

        public string GetHeader(string key) {
            if (!httpContext.Request.Headers.AllKeys.Contains(key)) {
                throw new ArgumentException(
                    $"Request doesn't contain header key '{key}'");
            }
            return httpContext.Request.Headers[key];
        }

        public string GetPayload() {
            return HttpUtility.UrlDecode(httpContext.Request.Payload);
        }

        public T GetPayload<T>() {
            return JsonConvert.DeserializeObject<T>(GetPayload());
        }
    }
}