using System;
using Flow;

namespace Pyro.Network
{
    using System.Collections.Generic;
    using System.Net.Sockets;
    using Exec;

    public delegate void ClientReceivedHandler(IClient client, Socket server);

    /// <inheritdoc />
    /// <summary>
    /// The Client interface.
    /// </summary>
    public interface IClient
        : INetCommon
    {
        event ClientReceivedHandler OnReceived;

        string HostName { get; }
        int HostPort { get; }

        IEnumerable<string> Results();
        void GetLatest();
        bool Continue(string piScript);
        bool ContinueRho(string rhoScript);
        void Close();
    }
}

