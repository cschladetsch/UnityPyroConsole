
namespace App
{
    using UnityEngine;
    using Flow;

    public class Main : Singleton<Main>
    {
        public IKernel Kernel;
        public PyroConsole Console;

        private void Start()
        {
            Kernel = Create.Kernel();
            Console = FindObjectOfType<PyroConsole>();
        }

        private void Update()
        {
            Kernel.Step();
        }
    }
}

