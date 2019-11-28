namespace Pyro.Network
{
    using System.Net.Sockets;

    public delegate void ClientReceivedHandler(IClient client, Socket server);

    /// <inheritdoc />
    /// <summary>
    /// The Client interface.
    /// </summary>
    public interface IClient
        : INetCommon
    {
        event ClientReceivedHandler OnReceived;

        void GetLatest();
        bool Continue(string piScript);
        bool ContinueRho(string rhoScript);
        void Close();
    }
}

