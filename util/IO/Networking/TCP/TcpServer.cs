using HuePat.Util.IO.Bytes;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace HuePat.Util.IO.Networking.TCP {
    public abstract class TcpServer : IDisposable {
        private bool isStopping;
        private int port;
        private object @object;
        private TcpListener listener;
        private CancellationTokenSource listeningForConnections;
        private List<TcpServerConnection> connections;

        public bool SupportsMultipleClients { private get; set; }
        public bool RecycleObjectReference { private get; set; }
        public IPAddress IPAddress { private get; set; }
        public IByteSerializer ByteSerializer { private get; set; }

        public TcpServer(int port) {
            this.port = port;
            SupportsMultipleClients = false;
            RecycleObjectReference = false;
            IPAddress = IPAddress.Any;
            ByteSerializer = CerasSerializer.ForNetworking();
        }

        public void Dispose() {
            listeningForConnections.Dispose();
            connections.Dispose();
        }

        public virtual void Start() {
            isStopping = false;
            listener = new TcpListener(IPAddress, port);
            listener.Start();
            listeningForConnections = new CancellationTokenSource();
            connections = new List<TcpServerConnection>();
            Task.Run((Action)Listen, listeningForConnections.Token);
        }

        public virtual void Stop() {
            isStopping = true;
            listener.Stop();
            if (SupportsMultipleClients) {
                listeningForConnections.Cancel();
            }
            foreach (TcpServerConnection connection in connections) {
                connection.Close();
            }
        }

        private void Listen() {
            bool clientConnected = false;
            while (SupportsMultipleClients || !clientConnected) {
                System.Net.Sockets.TcpClient client;
                try {
                    client = listener.AcceptTcpClient();
                }
                catch (SocketException e) {
                    if (isStopping) {
                        return;
                    }
                    throw new TcpException(e);
                }
                OnNewConnection();
                connections.Add(
                    new TcpServerConnection(
                        client, 
                        ByteSerializer.Clone(), 
                        React));
                clientConnected = true;
            }
        }

        protected virtual void OnNewConnection() {
        }

        protected virtual void React(byte[] packet, Action<object> respondCallback) {
            if (RecycleObjectReference) {
                ByteSerializer.Deserialize(ref @object, packet);
                Receive(@object, respondCallback);
            }
            else {
                Receive(
                    ByteSerializer.Deserialize<object>(packet),
                    respondCallback);
            }
        }

        protected abstract void Receive(object @object, Action<object> respondCallback);
    }
}