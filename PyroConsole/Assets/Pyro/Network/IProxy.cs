namespace Pyro.Network
{
    /// <inheritdoc />
    ///  <summary>
    ///  Local representation of a remote network-aware object.
    ///  There may be many proxies with different NetId's that all map
    ///  onto the same, unique, network Agent.
    ///  </summary>
    ///  <typeparam name="T"></typeparam>
    public interface IProxy<T> : IProxyBase
    {
    }
}

