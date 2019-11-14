using Flow;
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
        public Action<ELogLevel, string> OnWrite;

        protected override bool Fail(string text)
        {
            OnWrite?.Invoke(ELogLevel.Error, text);
            base.Fail(text);
            Error(text);
            return false;
        }

        protected bool Warn(string text)
        {
            OnWrite?.Invoke(ELogLevel.Warn, text);
            WriteLine($"Warn: {text}");
            return true;
        }

        protected new bool Error(string text)
        {
            OnWrite?.Invoke(ELogLevel.Error, text);
            WriteLine($"Error: {text}");
            base.Error = text;
            return false;
        }

        protected bool WriteLine(ELogLevel log, string text)
        {
            OnWrite?.Invoke(log, text);
            WriteLine($"{log}: {text}");
            var failed = log != ELogLevel.Error;
            return failed ? Fail(text) : true;
        }

        protected bool WriteLine(string text)
        {
            OnWrite?.Invoke(ELogLevel.Info, text);

            var color = Con.ForegroundColor;
            Con.ForegroundColor = ConsoleColor.Cyan;
            Con.WriteLine();
            Con.WriteLine(text);
            Con.ForegroundColor = color;
            return true;
        }
    }
}

