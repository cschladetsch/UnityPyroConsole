using System;
using System.Linq;
using System.Net.Sockets;

using Pyro.Exec;

namespace Pyro.Network.Impl
{
    /// <inheritdoc cref="IServer" />
    /// <summary>
    /// A server on the network executes incoming scripts and returns Executor data-stack
    /// to sender.
    /// </summary>
    public class Server
        : NetCommon
        , IServer
    {
        public override Socket Socket
        {
            get => _listener;
            set => _listener = value;
        }

        public int ListenPort => _port;
        public event MessageHandler ReceivedRequest;

        private const int RequestBacklogCount = 50;
        private Socket _listener;
        private readonly int _port;

        public Server(Peer peer, int port)
            : base(peer)
        {
            RegisterTypes.Register(_Context.Registry);

            _port = port;

            var scope = _Exec.Scope;

            _Context.Language = Language.ELanguage.Rho;
            scope["peer"] = _Peer;
            scope["server"] = this;
            scope["connect"] = TranslateRho("peer.Connect(\"192.168.3.146\", 9999)");
            scope["enter"] = TranslateRho("peer.Enter(2)");
            scope["join"] = TranslateRho("assert(connect() && enter())");
            scope["leave"] = TranslateRho("peer.Leave()");
            _Context.Language = Language.ELanguage.Pi;
        }

        private Exec.Continuation TranslateRho(string text)
        {
            if (!_Context.Translate(text, out var cont))
            {
                Error(_Context.Error);
                return null;
            }

            return cont;
        }

        public override string ToString()
        {
            return $"Server: listening on {ListenPort}, connected={_listener?.Connected}";
        }

        public bool Start()
        {
            var endPoint = GetLocalEndPoint(_port);
            if (endPoint == null)
                return Fail($"Couldn't find suitable local endpoint using {_port}");

            var address = endPoint.Address;
            _listener = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                _listener.Bind(endPoint);
                _listener.Listen(RequestBacklogCount);

                WriteLine($"Listening on {address}:{_port}");
                Listen();
            }
            catch (Exception e)
            {
                Error(e.Message);
                Stop();
                return false;
            }

            return true;
        }

        public void Stop()
        {
            _Stopping = true;
            _listener?.Close();
            _listener = null;
        }

        public bool Execute(string script)
        {
            return _Context.Exec(script);
        }

        protected override bool ProcessReceived(Socket sender, string pi)
        {
            try
            {
                ReceivedRequest?.Invoke(this, _Peer.GetClient(sender), pi);
                RunLocally(pi);
                return SendResponse(sender);
            }
            catch (Exception e)
            {
                var msg = $"{e.Message} {e.InnerException?.Message}";
                _Exec.Push($"Error: {msg}");
                SendResponse(sender);
                return Error(msg);
            }
        }

        private void RunLocally(string pi)
        {
            if (TranslatePi(pi).Code[0] is Continuation cont)
            {
                cont.Scope = _Exec.Scope;
                _Exec.Continue(cont);
            }
            else
            {
                _Exec.Push($"Error: {_Context.Error}");
            }
        }

        private bool SendResponse(Socket sender)
        {
            var response = _Registry.ToPiScript(_Exec.DataStack.ToList());
            return Send(sender, response);
        }
        private void Listen()
        {
            _listener.BeginAccept(ConnectRequest, null);
        }

        private void ConnectRequest(IAsyncResult ar)
        {
            if (_Stopping)
                return;

            var socket = _listener.EndAccept(ar);
            WriteLine($"Serving {socket.RemoteEndPoint}");
            _Peer.NewConnection(socket);
            Receive(socket);
            Listen();
        }
    }
}

