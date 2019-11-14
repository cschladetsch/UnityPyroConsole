namespace Pyro.Network
{
    using Impl;

    /// <summary>
    /// Add network-specific types to given registry.
    /// </summary>
    public static class RegisterTypes
    {
        public static void Register(IRegistry registry)
        {
            Exec.RegisterTypes.Register(registry);
            Peer.Register(registry);
        }
    }
}

