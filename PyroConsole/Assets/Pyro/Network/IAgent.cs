namespace Pyro.Network
{
    /// <summary>
    /// Local representation of a network-visible object.
    ///
    /// There is only ever one Agent with a given unique NetId.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IAgent<T>
        : IAgentBase
    {
    }
}
