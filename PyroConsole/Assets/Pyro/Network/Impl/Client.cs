using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;

namespace Pyro.Network.Impl
{
    using Exec;

    /// <inheritdoc cref="IClient" />
    /// <inheritdoc cref="NetCommon" />
    /// <summary>
    /// A connection to a remote server. Can send executable script, and receive
    /// results that are also executable scripts.
    /// </summary>
    public class Client
        : NetCommon
        , IClient
    {
        public event ClientReceivedHandler OnRecieved;

        // TODO: Move to NetCommon
        public string HostName => GetHostName();
        public int HostPort => GetHostPort();
        public override Socket Socket { get => _socket; set => _socket = value; }

        private Socket _socket;
        private IList<string> _results = new List<string>();

        public Client(Peer peer)
            : base(peer)
        {
        }

        public override string ToString()
            => $"Client: connected to {HostName}:{HostPort}";

        public IList<string> Results()
        {
            return _results;
        }

        public void CompleteConnect(Socket socket)
            => _socket = socket;

        //public bool Continue(Continuation cont)
            //=> Send(cont?.ToText());

        public bool Continue(string script)
        {
            return Send(script);
        }

        public bool Connect(string hostName, int port)
        {
            var address = GetAddress(hostName);
            if (address == null)
                return Fail($"Couldn't find address for {hostName}");

            var endPoint = new IPEndPoint(address, port);
            var client = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            client.BeginConnect(endPoint, Connected, client);

            return true;
        }

        public bool Send(Continuation continuation)
            => Send(continuation.ToText());

        public void Close()
        {
            _Stopping = true;
            _socket.Close();
            _socket = null;
        }

        private void Connected(IAsyncResult ar)
        {
            try
            {
                _socket = (Socket)ar.AsyncState;
                _socket.EndConnect(ar);
                if (!_socket.Connected)
                {
                    Warn($"Failed to connect to {_socket.RemoteEndPoint}");
                    return;
                }

                WriteLine($"Client: connected to {_socket.RemoteEndPoint} using {_socket.LocalEndPoint}");

                Receive(_socket);
            }
            catch (Exception e)
            {
                Error($"{e.Message}");
            }
        }

        public void GetLatest()
        {
            // hacks
            Send(" ");
        }

        protected override bool ProcessReceived(Socket sender, string pi)
        {
            try
            {
                if (!_Context.Translate(pi, out var cont))
                    return Error($"Failed to translate {pi}");

                cont.Scope = _Exec.Scope;
                _Exec.Continue(cont);
                _results.Clear();
                foreach (var elem in _Exec.Pop<IList<object>>())
                    _results.Add(_Context.Registry.ToPiScript(elem));

                OnRecieved?.Invoke(this, sender);
            }
            catch (Exception e)
            {
                Error(e.Message);
                return false;
            }

            return true;
        }

        private int GetHostPort()
        {
            var address = Socket?.RemoteEndPoint as IPEndPoint;
            return address?.Port ?? 0;
        }

        private string GetHostName()
        {
            var address = Socket?.RemoteEndPoint as IPEndPoint;
            return address?.Address.ToString() ?? "none";
        }
    }
}
