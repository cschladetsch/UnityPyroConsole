namespace Pyro.Network
{
    using System.Collections.Generic;
    using Exec;

    /// <inheritdoc />
    /// <summary>
    /// The Client interface.
    /// </summary>
    public interface IClient
        : INetCommon
    {
        string HostName { get; }
        int HostPort { get; }

        IEnumerable<string> Results();
        bool Continue(Continuation continuation);
        bool Continue(string script);
        void Close();
    }
}

