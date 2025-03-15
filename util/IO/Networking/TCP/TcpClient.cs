using HuePat.Util.IO.Bytes;
using System;
using System.Net;

namespace HuePat.Util.IO.Networking.TCP {
    public class TcpClient {
        private int port;
        private string iPAddress;
        private TcpConnection connection;

        public IByteSerializer ByteSerializer { private get; set; }

        public TcpClient(
                string iPAddress, 
                int port) {
            this.iPAddress = iPAddress;
            this.port = port;
            ByteSerializer = CerasSerializer.ForNetworking();
        }

        public void Connect() {
            System.Net.Sockets.TcpClient client = new System.Net.Sockets.TcpClient();
            client.Connect(IPAddress.Parse(iPAddress), port);
            connection = new TcpConnection(client, ByteSerializer.Clone());
        }

        public void Send(object @object) {
            Send(
                @object,
                response => { });
        }

        public void Send(
                object @object,
                Action<object> responseCallback) {
            try {
                connection.Send(@object);
                byte[] package = connection.GetPackage();
                responseCallback(
                    ByteSerializer.Deserialize<object>(package));
            }
            catch (NullReferenceException e) {
                if (connection == null) {
                    throw new TcpException("TcpClient must connect before it can send.");
                }
                throw new TcpException(e);
            }
        }

        public void Disconnect() {
            connection.Close();
        }
    }
}