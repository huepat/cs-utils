using HuePat.Util.IO.Bytes;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HuePat.Util.IO.Networking.TCP {
    class TcpServerConnection: TcpConnection {
        public delegate void Callback(byte[] packet, Action<object> respondCallback);

        private bool keepAlive;
        private bool notYetResponded;
        private CancellationTokenSource listening;
        private Callback callback;

        public TcpServerConnection(
                System.Net.Sockets.TcpClient client,
                IByteSerializer serializer,
                Callback callback) : 
                    base(client, serializer) {
            keepAlive = true;
            this.callback = callback;
            listening = new CancellationTokenSource();
            Task.Run((Action)Listen, listening.Token);
        }

        public override void Close() {
            keepAlive = false;
            base.Close();
        }

        private void Listen() {
            while (keepAlive) {
                try {
                    byte[] package = GetPackage();
                    notYetResponded = true;
                    callback(package, Respond);
                    Respond(null);
                }
                catch (TcpException) {
                    keepAlive = false;
                }
            }
        }

        private void Respond(object @object) {
            if (notYetResponded) {
                Send(@object);
                notYetResponded = false;
            }
        }
    }
}