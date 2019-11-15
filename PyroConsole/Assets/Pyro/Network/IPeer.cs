namespace Pyro.Network
{
    using System;
    using System.Collections.Generic;
    using Flow;
    using Exec;

    /// <summary>
    /// General message passed from client to server.
    /// </summary>
    public delegate void MessageHandler(IClient client, string text);
    public delegate void ConnectedHandler(IPeer peer, IClient client);
    public delegate void PeerWriteHandler(EDebugLevel log, string text);

    /// <inheritdoc />
    /// <summary>
    /// A Peer listens to incoming connections, and can connect to other peers.
    /// </summary>
    public interface IPeer
        : IProcess
    {
        IServer Local { get; }
        IClient Remote { get; }
        IList<IClient> Clients { get; }
        string LocalHostName { get; }

        event PeerWriteHandler OnWrite;

        /// <summary>
        /// Fires when we connect to a new client.
        /// </summary>
        event ConnectedHandler OnConnected;

        /// <summary>
        /// Fires when we receive a new request to do some work.
        /// </summary>
        event MessageHandler OnReceivedRequest;

        /// <summary>
        /// Fires when we receive a response to some previous work.
        /// </summary>
        event MessageHandler OnReceivedResponse;

        bool SelfHost();

        bool Connect(string hostName, int port);
        bool EnterClient(IClient client);
        bool Enter(int clientNumber);
        bool Execute(string pi);
        void Leave();

        void Stop();

        /// <summary>
        /// Make a new local Agent
        /// </summary>
        TIAgent NewAgent<TIAgent>();

        /// <summary>
        /// Make a local proxy to a remote Agent
        /// </summary>
        IFuture<TIProxy> NewProxy<TIProxy>(Guid agentNetId);

        IFuture<TR> RemoteCall<TR>(NetId agentId, string methodName);
        IFuture<TR> RemoteCall<TR, T0>(NetId agentId, string methodName, T0 t0);
        IFuture<TR> RemoteCall<TR, T0, T1>(NetId agentId, string methodName, T0 t0, T1 t1);
    }
}

