﻿namespace App
{
    using System;
    using System.Collections;
    using UnityEngine;
    using Flow;

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

        private ICoroutine _coro;
        private int _nextNetworkId = 1;

        /// <summary>
        /// Connect to a remote object.
        /// </summary>
        public void SetRemote(Pyro.Network.IClient remote)
        {
            IEnumerator SendUpdate(IGenerator self)
            {
                while (true)
                {
                    self.ResumeAfter(TimeSpan.FromMilliseconds(100));
                    var p = transform.position;
                    Remote?.Continue($"UpdateTransform({NetworkId}, {p.x}, {p.y}, {p.z})");
                }
            }

            _coro = Main.Instance.Kernel.Factory.Coroutine(SendUpdate);
            _coro.Name = $"Update {name}";
            Main.Instance.Kernel.Root.Add(_coro);

            Remote = remote;
            NetworkId = _nextNetworkId++;
            FullName = Utility.GetFullName(this);
            remote.Continue($"AddRemote({FullName}, {NetworkId})");
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

        private void OnDestroy()
        {
            Remote?.Continue($"DestroyObject({NetworkId})");
            _coro?.Complete();
        }
    }
}

