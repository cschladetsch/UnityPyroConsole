
namespace App
{
    using UnityEngine;
    using Flow;
    using Pyro.Network;

    public class Main : Singleton<Main>
    {
        public bool IsServer = true;
        public IKernel Kernel;
        public PyroConsole Console;

        public IPeer Peer => Console.Peer;

        private void Start()
        {
            DontDestroyOnLoad(gameObject);

            Kernel = Flow.Create.Kernel();
            Console = FindObjectOfType<PyroConsole>();
        }

        private void Update()
        {
            Kernel.Step();
        }
    }
}

