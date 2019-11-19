namespace Pyro.Network
{
    using System;

    /// <summary>
    /// Identifies an object in a Domain
    /// </summary>
    public class NetId
    {
        public Guid DomainId => _domain;
        public Id Id => _id;

        private Guid _domain;
        private Id _id;

        public override string ToString()
        {
            return $"NetId: @{_domain}:{_id}";
        }
    }
}

