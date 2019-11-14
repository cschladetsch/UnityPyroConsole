using System.Net.Sockets;
using System.Text;

namespace Pyro.Network.Impl
{
    internal class StateObject
    {
        public Socket workSocket = null;
        public const int BufferSize = 1024;
        public byte[] buffer = new byte[BufferSize];
        public StringBuilder sb = new StringBuilder();
    }
}
