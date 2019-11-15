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
        event ClientReceivedHandler OnRecieved;

        string HostName { get; }
        int HostPort { get; }

        IList<string> Results();
        void GetLatest();
        bool Continue(string script);
        void Close();
    }
}

