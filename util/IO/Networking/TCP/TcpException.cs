using System;

namespace HuePat.Util.IO.Networking.TCP {
    public class TcpException: Exception {
        public TcpException(string message) :
            base(message) {
        }

        public TcpException(Exception e):
            base("An error occured in TCP networking.", e) {
        }
    }
}