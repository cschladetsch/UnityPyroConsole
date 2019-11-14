namespace Pyro.Network
{
    /// <inheritdoc />
    /// <summary>
    /// Interface for a general server.
    /// </summary>
    public interface IServer
        : INetCommon
    {
        int ListenPort { get; }

        bool Start();
        bool Execute(string pi);
        void Stop();
    }
}

