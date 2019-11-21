using System;
using System.Net;
using System.Linq;
using System.Net.Sockets;
using System.Collections.Generic;

using Flow;

namespace Pyro.Network.Impl
{
    using Exec;

    /// <inheritdoc cref="IPeer" />
    /// <summary>
    /// A network peer. Contains a client and a server.
    /// </summary>
    public class Peer
        : NetworkConsoleWriter
        , IPeer
    {
        public string LocalHostName => GetLocalHostname();

        public event MessageHandler OnReceivedRequest;

        // We connected to a new remote server (and made a new local client for it)
        public event ConnectedHandler OnConnected;

        // a local client (and there could be many different local clients) received a response.
        public event MessageHandler OnReceivedResponse;

        public IList<object> Stack { get; }

        public IList<IClient> Clients => _clients.Cast<IClient>().ToList();
        public IServer Local => _server;
        public IClient Remote => _remote;
        public string HostName => GetHostName();
        public int HostPort => GetHostPort();

        private Server _server;
        private IClient _remote;
        private readonly List<IClient> _clients = new List<IClient>();

        public Peer()
        {
        }

        public Peer(int listenPort)
        {
            StartServer(listenPort);
        }

        public override string ToString()
        {
            var text = $"Peer: {Clients.Count} clients, ";
            if (_server != null)
                text += $"{_server}";
            else
                text += "no server";

            return $"\"{text}\"";
        }

        public static void Register(IRegistry reg)
        {
            reg.Register(new ClassBuilder<Peer>(reg)
                .Methods
                    .Add<string, int, bool>("Connect", (q, s, p) => q.Connect(s, p))
                    .Add<int>("StartServer", (q, s) => q.StartServer(s))
                    .Add<int, bool>("Remote", (q, s) => q.Enter(s))
                    .Add("Leave", (q) => q.Leave())
                .Class);
        }

        public void StartServer(int listenPort)
        {
            _server = new Server(this, listenPort);
            _server.ReceivedRequest += (client, text) => 
            {
                OnReceivedRequest?.Invoke(client, text);
            };
        }

        public bool Execute(string script)
            => _remote?.Continue(script) ?? Fail("Not connected");

        public IClient GetClient(Socket sender)
            => _clients.FirstOrDefault(c => c.Socket == sender);

        public string GetHostName()
            => GetRemoteEndPoint()?.Address.ToString();

        public bool SelfHost()
            => !_server.Start() ? Fail("Couldn't start server") : SelfHost(_server.ListenPort);

        public bool Enter(int index)
            => index >= _clients.Count ? Fail($"No such client id={index}") : EnterRemote(_clients[index]);

        private IPEndPoint GetRemoteEndPoint()
            => Remote?.Socket?.RemoteEndPoint as IPEndPoint;

        private int GetHostPort()
            => GetRemoteEndPoint()?.Port ?? 0;

        public bool EnterRemote(IClient client)
        {
            if (client.Socket == null)
                return false;

            WriteLine($"Remoting into {client.Socket.RemoteEndPoint}");
            if (!_clients.Contains(client))
                return false;

            _remote = client;
            return true;
        }

        /// <summary>
        /// Connect to local loopback address.
        /// </summary>
        /// <param name="port">The port to connect to.</param>
        /// <returns>True if connection made,</returns>
        public bool SelfHost(int port)
        {
            if (!Connect(GetLocalHostname(), port))
                return Error("Couldn't connect to localhost");

            // This Sleep is to give a little time for local
            // client to connect to local server via loopback Tcp.
            System.Threading.Thread.Sleep(TimeSpan.FromMilliseconds(250));

            return EnterClient(Clients[0]) || Error("Couldn't shell to localhost");
        }

        public bool Listen()
            =>  _server.Start();

        public void Received(Socket socket, string text)
            => OnReceivedResponse?.Invoke(FindClient(socket), text);

        private IClient FindClient(Socket socket)
            => _clients.FirstOrDefault(c => c.Socket == socket);

        public void Leave()
        {
            if (_remote == _clients[0])
            {
                Error("Cannot leave self");
                return;
            }

            _remote.Close();
            _clients.Remove(_remote);

            _remote = _clients[0];  // self-host
        }

        public bool Connect(string hostName, int port)
        {
            var client = new Client(this);
            if (!client.Connect(hostName, port))
                return false;

            _clients.Add(client);
            return true;
        }

        public bool EnterClient(IClient client)
        {
            if (client == null)
                return Fail("Null client");

            if (client.Socket == null || !client.Socket.Connected)
                return Fail("Client not connected");

            _remote = client;
            OnConnected?.Invoke(this, client);
            return true;
        }

        public void Stop()
        {
            WriteLine($"Closing {_clients.Count} connections");
            foreach (var client in _clients)
                client.Close();

            _clients.Clear();
            _server.Stop();
            _server = null;
        }

        public TIAgent NewAgent<TIAgent>()
        {
            // TODO NEXT
            //var agent = _server.NewAgent<TIAgent>();
            //agent.Bind(this);
            //
            //return null;
            throw new NotImplementedException();
        }

        public IFuture<TIProxy> NewProxy<TIProxy>(Guid agentNetId)
        {
            throw new NotImplementedException();
        }

        public IFuture<TR> RemoteCall<TR>(NetId agentId, string methodName)
        {
            throw new NotImplementedException();
        }

        public IFuture<TR> RemoteCall<TR, T0>(NetId agentId, string methodName, T0 t0)
        {
            throw new NotImplementedException();
        }

        public IFuture<TR> RemoteCall<TR, T0, T1>(NetId agentId, string methodName, T0 t0, T1 t1)
        {
            throw new NotImplementedException();
        }

        public void Update()
        {
        }

        public void NewConnection(Socket socket)
        {
            var client = new Client(this) {  Socket = socket};
            _clients.Add(client);
            OnConnected?.Invoke(this, client);

            WriteLine($"Connected to {socket.RemoteEndPoint}");
        }

        public string GetLocalHostname()
        {
            var address = Dns.GetHostAddresses(Dns.GetHostName()).FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
            return address?.ToString() ?? "localhost";
        }
    }
}

