namespace Pyro.Network
{
    /// <summary>
    /// A collection of objects that spatially coherent across the network.
    ///
    /// Network Domains are like local Registries, but contain network-aware objects
    /// and have spatial awareness.
    /// </summary>
    public interface IDomain
       : IRegistry
    {
    }
}

