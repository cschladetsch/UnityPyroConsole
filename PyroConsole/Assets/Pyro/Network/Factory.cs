namespace Pyro.Network
{
    /// <summary>
    /// Factory for network objects.
    /// </summary>
    public static class Create
    {
        public static IPeer NewPeer(int port)
        {
            return new Impl.Peer(port);
        }
    }
}

