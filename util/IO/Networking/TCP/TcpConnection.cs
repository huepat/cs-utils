using Ceras;
using HuePat.Util.IO.Bytes;
using System;
using System.IO;
using System.Net.Sockets;

namespace HuePat.Util.IO.Networking.TCP {
    class TcpConnection : IDisposable {
        private const int PACKAGE_LENGTH_BYTE_SIZE = 4;

        private byte[] lengthBuffer = new byte[PACKAGE_LENGTH_BYTE_SIZE];
        private byte[] buffer = new byte[1];
        private IByteSerializer serializer;
        private System.Net.Sockets.TcpClient client;
        private NetworkStream stream;

        public TcpConnection(
                System.Net.Sockets.TcpClient client,
                IByteSerializer serializer) {
            this.client = client;
            this.serializer = serializer;
            stream = client.GetStream();
        }

        public void Dispose() {
            client.Dispose();
            stream.Dispose();
        }

        public void Send(object @object) {
            int packageSize = serializer.Serialize(@object, ref buffer);
            int offset = 0;
            SerializerBinary.WriteInt32Fixed(ref lengthBuffer, ref offset, packageSize);
            try {
                stream.Write(lengthBuffer, 0, offset);
                stream.Write(buffer, 0, packageSize);
            }
            catch (IOException e) {
                throw new TcpException(e);
            }
            catch(ObjectDisposedException e) {
                throw new TcpException(e);
            }
        }

        public byte[] GetPackage() {
            int offset = 0;
            byte[] buffer;
            try {
                Read(PACKAGE_LENGTH_BYTE_SIZE, ref lengthBuffer);
                int packageSize = SerializerBinary.ReadInt32Fixed(lengthBuffer, ref offset);
                buffer = new byte[packageSize];
                Read(packageSize, ref buffer);
            }
            catch (IOException e) {
                throw new TcpException(
                    $"TCP network stream terminated within message: '{e.Message}'");
            }
            return buffer;
        }

        public virtual void Close() {
            client.Close();
            stream.Close();
            Dispose();
        }

        private void Read(int length, ref byte[] buffer) {
            int totalRead = 0;
            while (totalRead < length) {
                int leftToRead = length - totalRead;
                int read = stream.Read(buffer, totalRead, leftToRead);
                if (read <= 0) {
                    throw new TcpException(
                        "TCP network stream terminated within message.");
                }
                totalRead += read;
            }
        }
    }
}