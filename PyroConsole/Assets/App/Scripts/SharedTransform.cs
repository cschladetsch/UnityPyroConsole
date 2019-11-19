using System.Collections.Generic;

namespace App
{
    using System;
    using System.Collections;
    using UnityEngine;
    using Flow;
    using Pyro.Network;

    /// <summary>
    /// A Transform that sends or receives state updates to a remote client.
    /// </summary>
    public class SharedTransform : MonoBehaviour
    {
        /// <summary>
        /// The remote client to send updates to.
        /// </summary>
        public Pyro.Network.IClient Remote;

        /// <summary>
        /// Unique across the app.
        /// </summary>
        public int NetworkId;

        /// <summary>
        /// The local object to update if we are the client.
        /// </summary>
        public GameObject Local;
        public string FullName;
        public int UpdateMillis = 100;

        private ICoroutine _coro;
        private static int _nextNetworkId = 1;

        /// <summary>
        /// Send updates to a remote client
        /// </summary>
        private void SetRemote(IClient remote)
        {
            IEnumerator SendUpdate(IGenerator self)
            {
                while (true)
                {
                    yield return self.ResumeAfter(TimeSpan.FromMilliseconds(UpdateMillis));
                    var p = transform.position;
                    Debug.Log($"Updating {FullName}");
                    Remote?.Continue($"remote.UpdateTransform({NetworkId}, {p.x}, {p.y}, {p.z})");
                }
            }

            _coro = Main.Instance.Kernel.Factory.Coroutine(SendUpdate);
            _coro.Name = $"Update {name}";
            Main.Instance.Kernel.Root.Add(_coro);

            Remote = remote;
            NetworkId = _nextNetworkId++;
            FullName = Utility.GetFullName(this);
            remote.Continue($"remote.AddRemote({FullName}, {NetworkId})");
        }

        public void UpdatePosition(Vector3 pos)
        {
            transform.position = pos;
        }

        /// <summary>
        /// Act as a proxy to a remote.
        /// </summary>
        /// <param name="fullName">The full name, from root, of the object to update.</param>
        /// <param name="id">The network id of the object.</param>
        public void AddRemote(string fullName, int id)
        {
            name = $"Proxy for {fullName}";
            FullName = fullName;
            Local = GameObject.Find(fullName);
            NetworkId = id;
        }

        private void Update()
        {
            if (!Main.Instance.IsServer)
                return;

            if (!_hooked)
            {
                Main.Instance.Peer.OnConnected += Peer_OnConnected;
                _hooked = true;
            }

            if (_connect)
            {
                SetRemote(_client);
                _connect = false;
            }
        }

        private bool _hooked;
        private bool _connect;
        private IClient _client;

        private void Peer_OnConnected(IPeer peer, IClient client)
        {
            Main.Instance.Peer.OnConnected -= Peer_OnConnected;
            _connect = true;
            _client = client;
        }

        private void OnDestroy()
        {
            Remote?.Continue($"DestroyObject({NetworkId})");
            _coro?.Complete();
        }
    }
}

