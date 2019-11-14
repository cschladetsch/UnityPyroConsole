using System;
using Con = System.Console;

namespace Pyro.Network.Impl
{
    /// <inheritdoc />
    /// <summary>
    /// Base for Peer, Client and Server.
    /// </summary>
    public class NetworkConsoleWriter
        : Process
    {
        protected override bool Fail(string text)
        {
            base.Fail(text);
            Error(text);
            return false;
        }

        protected new bool Error(string text)
        {
            WriteLine($"Error: {text}");
            base.Error = text;
            return false;
        }

        public event Action<string, ConsoleColor> OnWrite;

        protected bool WriteLine(string text)
        {
            OnWrite?.Invoke(text, ConsoleColor.White);
            var color = Con.ForegroundColor;
            Con.ForegroundColor = ConsoleColor.Cyan;
            Con.WriteLine();
            Con.WriteLine(text);
            Con.ForegroundColor = color;
            return true;
        }
    }
}

