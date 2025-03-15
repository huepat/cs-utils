using System;

namespace HuePat.Util.IO.Networking.HTTP {
    public interface IHttpRequest {
        void RespondOk();
        void Respond(string response);
        void Respond(Exception e);
        void Respond<T>(T response);
        string GetHeader(string key);
        string GetPayload();
        T GetPayload<T>();
    }
}