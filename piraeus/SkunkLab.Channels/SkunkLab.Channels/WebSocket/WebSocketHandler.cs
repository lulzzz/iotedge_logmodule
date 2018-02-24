﻿

namespace SkunkLab.Channels.WebSocket
{
    
        using System;
        using System.Collections.Generic;
        using System.ComponentModel;
        using System.Diagnostics;
        using System.Net.WebSockets;
        using System.Runtime.CompilerServices;
        using System.Runtime.InteropServices;
        using System.Text;
        using System.Threading;
        using System.Threading.Tasks;
        using System.Web.WebSockets;

    public delegate void WebSocketOpenHandler(object sender, WebSocketOpenEventArgs args);
    public delegate void WebSocketCloseHandler(object sender, WebSocketCloseEventArgs args);
    public delegate void WebSocketErrorHandler(object sender, WebSocketErrorEventArgs args);
    public delegate void WebSocketReceiveHandler(object sender, WebSocketReceiveEventArgs args);

    public class WebSocketHandler
    {
        public WebSocketHandler(WebSocketConfig config, CancellationToken token)
        {
            this.config = config;
            this.token = token;
        }

        public event WebSocketReceiveHandler OnReceive;
        public event WebSocketErrorHandler OnError;
        public event WebSocketOpenHandler OnOpen;
        public event WebSocketCloseHandler OnClose;

        public AspNetWebSocketContext WebSocketContext { get; set; }

        private WebSocketConfig config;
        private CancellationToken token;
        private readonly TaskQueue _sendQueue = new TaskQueue();

        public void Close()
        {
            CloseAsync();
        }

        internal Task CloseAsync() =>
            this._sendQueue.Enqueue(() => WebSocketContext.WebSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", token));

        private static bool IsFatalException(Exception ex)
        {

            COMException exception = ex as COMException;
            if (exception != null)
            {
                switch (((uint)exception.ErrorCode))
                {
                    case 0x80070026:
                    case 0x800703e3:
                    case 0x800704cd:
                        return false;
                }
            }
            return true;
        }

              

        [EditorBrowsable(EditorBrowsableState.Never)]
        public Task ProcessWebSocketRequestAsync(AspNetWebSocketContext webSocketContext)
        {
            if (webSocketContext == null)
            {
                throw new ArgumentNullException("webSocketContext");
            }

            byte[] buffer = new byte[config.ReceiveLoopBufferSize];
            WebSocket webSocket = webSocketContext.WebSocket;
            return ProcessWebSocketRequestAsync(webSocketContext, () => WebSocketMessageReader.ReadMessageAsync(webSocket, buffer, config.MaxIncomingMessageSize, CancellationToken.None));
        }

        internal async Task ProcessWebSocketRequestAsync(AspNetWebSocketContext webSocketContext, Func<Task<WebSocketMessage>> messageRetriever)
        {
            try
            {
                WebSocketContext = webSocketContext;
                OnOpen?.Invoke(this, new WebSocketOpenEventArgs());

                while (!token.IsCancellationRequested && WebSocketContext.WebSocket.State == WebSocketState.Open)
                {
                    WebSocketMessage message = await messageRetriever();
                    if (message.MessageType == WebSocketMessageType.Binary)
                    {
                        OnReceive?.Invoke(this, new WebSocketReceiveEventArgs(message.Data as byte[]));
                    }
                    else if (message.MessageType == WebSocketMessageType.Text)
                    {
                        OnReceive?.Invoke(this, new WebSocketReceiveEventArgs(Encoding.UTF8.GetBytes(message.Data as string)));
                    }
                    else
                    {
                        //close received
                        OnClose?.Invoke(this, new WebSocketCloseEventArgs(WebSocketCloseStatus.NormalClosure));
                        break;
                    }
                }
            }
            catch (Exception exception)
            {
                if (!(WebSocketContext.WebSocket.State == WebSocketState.CloseReceived ||
                    WebSocketContext.WebSocket.State == WebSocketState.CloseSent))
                {
                    if (IsFatalException(exception))
                    {
                        OnError?.Invoke(this, new WebSocketErrorEventArgs(exception));
                    }
                }
            }
            finally
            {
                try
                {
                    await CloseAsync();
                }
                finally
                {
                    IDisposable disposable = this as IDisposable;
                    if (disposable != null)
                    {
                        disposable.Dispose();
                    }
                }
            }
        }

        public void Send(string message)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }
            Task task = SendAsync(message);
            Task.WaitAll(task);
        }

        public void Send(byte[] message)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            Task task = SendAsync(message, WebSocketMessageType.Binary);
            Task.WaitAll(task);
        }

        internal Task SendAsync(string message) =>
            SendAsync(Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text);

        internal Task SendAsync(byte[] message, WebSocketMessageType messageType) =>
            _sendQueue.Enqueue(() => this.WebSocketContext.WebSocket.SendAsync(new ArraySegment<byte>(message), messageType, true, token));

        

        

       

        

    }
        
    

}
