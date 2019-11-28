namespace Pyro.Network
{
    using System;
    using System.Net.Sockets;
    using Flow;
    using ExecutionContext;

    /// <summary>
    /// Common interface to clients and servers.
    /// </summary>
    public interface INetCommon
        : IProcess
    {
        Context Context { get; }
        Socket Socket { get; }
        
        IFuture<DateTime> Ping();
    }
}
