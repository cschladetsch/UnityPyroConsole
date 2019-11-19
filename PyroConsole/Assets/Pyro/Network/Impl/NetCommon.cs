using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Flow;
using Pyro.Exec;
using Context = Pyro.ExecutionContext.Context;
using ELanguage = Pyro.Language.ELanguage;

namespace Pyro.Network.Impl
{
    /// <summary>
    /// Functionality common to both Client and Server aspects of a Peer
    /// </summary>
    public abstract class NetCommon
        : NetworkConsoleWriter
        , INetCommon
    {
        // TODO: should a server have a different context for each client?
        public Context Context => _Context;
        public abstract Socket Socket { get; set; }

        protected Peer _Peer;
        protected Context _Context;
        protected Executor _Exec => _Context.Executor;
        protected IRegistry _Registry => _Context.Registry;
        protected bool _Stopping;

        public IFuture<DateTime> Ping()
        {
            throw new NotImplementedException();
        }
        
        protected NetCommon(Peer peer)
        {
            _Peer = peer;
            _Context = new Context {Language = ELanguage.Pi};
            RegisterTypes.Register(_Context.Registry);
        }

        protected Continuation TranslatePi(string pi)
        {
            if (_Context.Translate(pi, out var cont))
                return cont;

            Error(_Context.Error);
            return null;
        }

        protected bool Send(string text)
        {
            return Send(Socket, text);
        }

        protected bool Send(Socket socket, string text)
        {
            if (socket == null)
                return Fail("No socket to send with");

            var byteData = Encoding.ASCII.GetBytes(text + '~');
            socket.BeginSend(byteData, 0, byteData.Length, 0, Sent, socket);
            return true;
        }

        private static void Sent(IAsyncResult ar)
        {
            var socket = ar.AsyncState as Socket;
            int? sent = socket?.EndSend(ar);
        }

        protected IPAddress GetAddress(string hostname)
        {
            // TODO: search for Ip6 address first
            return Dns.GetHostAddresses(hostname).FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
        }

        protected void Receive(Socket socket)
        {
            var state = new StateObject {workSocket = socket};
            socket.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, ReadCallback, state);
        }

        protected virtual bool ProcessReceived(Socket sender, string pi)
        {
            return WriteLine($"Recv: {pi}");
        }

        protected IPEndPoint GetLocalEndPoint(int port)
        {
            var address = GetAddress(Dns.GetHostName());
            if (address != null)
                return new IPEndPoint(address, port);

            Error("Couldn't find suitable localhost address");
            return null;
        }

        protected void ReadCallback(IAsyncResult ar)
        {
            try
            {
                ProcessRead(ar);
            }
            catch (ObjectDisposedException)
            {
                if (!_Stopping)
                    throw;
            }
            catch (Exception e)
            {
                Error($"{e.Message}");
            }
        }

        private void ProcessRead(IAsyncResult ar)
        {
            var state = (StateObject) ar.AsyncState;
            var socket = state.workSocket;

            var bytesRead = socket.EndReceive(ar);
            if (bytesRead <= 0)
                return;

            EndReceive(state, bytesRead, socket);
        }

        private void EndReceive(StateObject state, int bytesRead, Socket socket)
        {
            state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));
            ProcessInput(state, socket);
            socket.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, ReadCallback, state);
        }

        private void ProcessInput(StateObject state, Socket socket)
        {
            var content = state.sb.ToString();
            var end = content.IndexOf('~'); // yes. this means we can't use tilde anywhere in scripts!
            if (end < 0)
                return;

            try
            {
                ProcessReceived(socket, content.Substring(0, end));
            }
            catch (Exception e)
            {
                Error($"ProcessInput Error: {e.Message}");
            }

            ResetState(state, content, end);
        }

        private static void ResetState(StateObject state, string content, int end)
        {
            state.sb.Clear();
            state.sb.Append(content.Substring(end + 1));
        }
    }
}

