﻿using Org.BouncyCastle.Crypto.Tls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace SkunkLab.Channels.Tcp
{
    public class TcpClientChannel2 : TcpChannel
    {
        #region ctor
        public TcpClientChannel2(string hostname, int port, int blockSize, int maxBufferSize, CancellationToken token)
        {
            this.hostname = hostname;
            this.port = port;
            this.blockSize = blockSize;
            this.maxBufferSize = maxBufferSize;
            this.token = token;
            this.queue = new Queue<byte[]>();
        }

        public TcpClientChannel2(string hostname, int port, IPEndPoint localEP, int blockSize, int maxBufferSize, CancellationToken token)
            : this(hostname, port, localEP, null, blockSize, maxBufferSize, token)
        {
        }

        public TcpClientChannel2(IPEndPoint remoteEndpoint, int blockSize, int maxBufferSize, CancellationToken token)
        {
            this.remoteEP = remoteEndpoint;
            this.blockSize = blockSize;
            this.maxBufferSize = maxBufferSize;
            this.token = token;
            this.queue = new Queue<byte[]>();
        }

        public TcpClientChannel2(IPEndPoint remoteEndpoint, IPEndPoint localEP, int blockSize, int maxBufferSize, CancellationToken token)
            : this(remoteEndpoint, localEP, null, blockSize, maxBufferSize, token)
        {
        }

        public TcpClientChannel2(IPAddress address, int port, int blockSize, int maxBufferSize, CancellationToken token)
        {
            this.address = address;
            this.port = port;
            this.blockSize = blockSize;
            this.maxBufferSize = maxBufferSize;
            this.token = token;
            this.queue = new Queue<byte[]>();
        }

        public TcpClientChannel2(IPAddress address, int port, IPEndPoint localEP, int blockSize, int maxBufferSize, CancellationToken token)
            : this(address, port, localEP, null, blockSize, maxBufferSize, token)
        {
        }

        public TcpClientChannel2(string hostname, int port, X509Certificate2 certificate, int blockSize, int maxBufferSize, CancellationToken token)
            : this(hostname, port, null, certificate, blockSize, maxBufferSize, token)
        {
        }

        public TcpClientChannel2(string hostname, int port, IPEndPoint localEP, X509Certificate2 certificate, int blockSize, int maxBufferSize, CancellationToken token)
        {
            this.hostname = hostname;
            this.port = port;
            this.localEP = localEP;
            this.certificate = certificate;
            this.blockSize = blockSize;
            this.maxBufferSize = maxBufferSize;
            this.token = token;
            this.token.Register(async () => await CloseAsync());
            Id = "tcp2-" + Guid.NewGuid().ToString();
            Port = port;
            this.queue = new Queue<byte[]>();
        }

        public TcpClientChannel2(IPEndPoint remoteEndpoint, X509Certificate2 certificate, int blockSize, int maxBufferSize, CancellationToken token)
            : this(remoteEndpoint, null, certificate, blockSize, maxBufferSize, token)
        {
        }

        public TcpClientChannel2(IPEndPoint remoteEndpoint, IPEndPoint localEP, X509Certificate2 certificate, int blockSize, int maxBufferSize, CancellationToken token)
        {
            if (remoteEndpoint == null)
            {
                throw new ArgumentNullException("remoteEndpoint");
            }

            remoteEP = remoteEndpoint;
            this.localEP = localEP;
            this.certificate = certificate;
            this.blockSize = blockSize;
            this.maxBufferSize = maxBufferSize;
            this.token = token;
            Id = "tcp2-" + Guid.NewGuid().ToString();
            this.token.Register(async () => await CloseAsync());

            if (certificate != null)
            {
                IPHostEntry ipHostInfo = Dns.GetHostEntry(remoteEndpoint.Address);
                hostname = ipHostInfo.HostName;
            }

            Port = remoteEndpoint.Port;
            this.queue = new Queue<byte[]>();
        }

        public TcpClientChannel2(IPAddress address, int port, X509Certificate2 certificate, int blockSize, int maxBufferSize, CancellationToken token)
            : this(address, port, null, certificate, blockSize, maxBufferSize, token)
        {

        }
        public TcpClientChannel2(IPAddress address, int port, IPEndPoint localEP, X509Certificate2 certificate, int blockSize, int maxBufferSize, CancellationToken token)
        {
            if (address == null)
            {
                throw new ArgumentNullException("address");
            }

            this.address = address;
            this.port = port;
            this.localEP = localEP;
            this.certificate = certificate;
            this.blockSize = blockSize;
            this.maxBufferSize = maxBufferSize;
            this.token = token;
            Id = "tcp2-" + Guid.NewGuid().ToString();
            this.token.Register(async () => await CloseAsync());

            if (certificate != null)
            {
                IPHostEntry ipHostInfo = Dns.GetHostEntry(address);
                hostname = ipHostInfo.HostName;
            }

            Port = port;
            this.queue = new Queue<byte[]>();
        }


        public TcpClientChannel2(IPAddress address, int port, IPEndPoint localEP, string pskIdentity, byte[] psk, int blockSize, int maxBufferSize, CancellationToken token)
        {
            this.address = address;
            this.port = port;
            this.localEP = localEP;
            this.pskIdentity = pskIdentity;
            this.blockSize = blockSize;
            this.maxBufferSize = maxBufferSize;
            this.psk = psk;
            Id = "tcp2-" + Guid.NewGuid().ToString();
            this.token = token;
            this.queue = new Queue<byte[]>();
        }

        public TcpClientChannel2(IPAddress address, int port, string pskIdentity, byte[] psk, int blockSize, int maxBufferSize, CancellationToken token)
            : this(address, port, null, pskIdentity, psk, blockSize, maxBufferSize, token)
        {
        }


        public TcpClientChannel2(string hostname, int port,  string pskIdentity, byte[] psk, int blockSize, int maxBufferSize, CancellationToken token)
            : this(hostname, port, null, pskIdentity, psk, blockSize, maxBufferSize, token)
        {

        }

        public TcpClientChannel2(string hostname, int port, IPEndPoint localEP, string pskIdentity, byte[] psk, int blockSize, int maxBufferSize, CancellationToken token)
        {
            this.hostname = hostname;
            this.port = port;
            this.pskIdentity = pskIdentity;
            this.localEP = localEP;
            this.psk = psk;
            this.blockSize = blockSize;
            this.maxBufferSize = maxBufferSize;
            this.token = token;
            Id = "tcp2-" + Guid.NewGuid().ToString();
            this.queue = new Queue<byte[]>();
        }

        public TcpClientChannel2(IPEndPoint remoteEP, string pskIdentity, byte[] psk, int blockSize, int maxBufferSize, CancellationToken token)
            : this(remoteEP, null, pskIdentity, psk, blockSize, maxBufferSize, token)
        {
           
        }

        public TcpClientChannel2(IPEndPoint remoteEP, IPEndPoint localEP, string pskIdentity, byte[] psk, int blockSize, int maxBufferSize, CancellationToken token)
        {
            this.remoteEP = remoteEP;
            this.localEP = localEP;
            this.pskIdentity = pskIdentity;
            this.psk = psk;
            this.blockSize = blockSize;
            this.maxBufferSize = maxBufferSize;
            this.token = token;
            Id = "tcp2-" + Guid.NewGuid().ToString();
            this.queue = new Queue<byte[]>();
        }


        #endregion

        #region private member variables

        private TlsClientProtocol protocol;
        private string pskIdentity;
        private byte[] psk;
        private IPEndPoint localEP;
        private X509Certificate2 certificate;
        private TcpClient client;
        private int port;
        private IPAddress address;
        private string hostname;
        private IPEndPoint remoteEP;
        private CancellationToken token;
        private Stream stream;
        private SemaphoreSlim readConnection;
        private SemaphoreSlim writeConnection;
        private NetworkStream localStream;
        private bool disposed;
        private ChannelState _state;
        private int blockSize;
        private int maxBufferSize;
        private Queue<byte[]> queue;

        #endregion

        #region events

        public override event EventHandler<ChannelReceivedEventArgs> OnReceive;
        public override event EventHandler<ChannelCloseEventArgs> OnClose;
        public override event EventHandler<ChannelOpenEventArgs> OnOpen;
        public override event EventHandler<ChannelErrorEventArgs> OnError;
        public override event EventHandler<ChannelStateEventArgs> OnStateChange;

        #endregion

        #region Properties

        public override bool IsConnected
        {
            get
            {
                if (disposed || client == null || client.Client == null)
                {
                    return false;
                }
                else
                {
                    return client.Client.Connected;
                }
            }
        }

        public override bool RequireBlocking
        {
            get { return psk != null; }
        }

        public override string TypeId { get { return "TCP2"; } }

        public override int Port { get; internal set; }

        public override string Id { get; internal set; }

        public override bool IsEncrypted { get; internal set; }

        public override bool IsAuthenticated { get; internal set; }

        public override ChannelState State
        {
            get
            {
                return _state;
            }
            internal set
            {
                if (_state != value)
                {
                    OnStateChange?.Invoke(this, new ChannelStateEventArgs(Id, value));
                }

                _state = value;
            }
        }

        #endregion

        #region methods

        public override async Task AddMessageAsync(byte[] message)
        {
            OnReceive?.Invoke(this, new ChannelReceivedEventArgs(Id, message));
            await Task.CompletedTask;
        }
       
        public override async Task OpenAsync()
        {
            State = ChannelState.Connecting;

            if (localEP != null)
            {
                client = new TcpClient(localEP);
            }
            else
            {
                client = new TcpClient();
            }

            client.LingerState = new LingerOption(true, 0);
            client.NoDelay = true;
            client.ExclusiveAddressUse = false;
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            client.Client.UseOnlyOverlappedIO = true;  

            if (remoteEP != null)
            {
                await client.ConnectAsync(remoteEP.Address, remoteEP.Port);
            }
            else if (address != null)
            {
                await client.ConnectAsync(address, port);
            }
            else if (!String.IsNullOrEmpty(hostname))
            {
                await client.ConnectAsync(hostname, port);
            }
            else
            {
                State = ChannelState.Aborted;
                OnError?.Invoke(this, new ChannelErrorEventArgs(Id, new InvalidOperationException("Tcp client connection parameters not sufficient.")));
            }

            readConnection = new SemaphoreSlim(1);
            writeConnection = new SemaphoreSlim(1);

            localStream = client.GetStream();          

            if (psk != null)
            {
                try
                {
                    //protocol = client.ConnectPskTlsClientNonBlocking(pskIdentity, psk);
                    protocol = client.ConnectPskTlsClient(pskIdentity, psk, localStream);
                    stream = protocol.Stream;
                    IsEncrypted = true;
                }
                catch(Exception ex)
                {
                    Trace.TraceError(ex.Message);
                    OnError?.Invoke(this, new ChannelErrorEventArgs(Id, ex));
                }
            }
            else if (certificate != null)
            {

                try
                {
                    stream = new SslStream(localStream, true, new RemoteCertificateValidationCallback(ValidateCertificate));
                    IsEncrypted = true;
                    X509CertificateCollection certificates = new X509CertificateCollection();
                    X509Certificate cert = new X509Certificate(certificate.RawData);
                    certificates.Add(cert);
                    SslStream sslStream = (SslStream)stream;
                    await sslStream.AuthenticateAsClientAsync(hostname, certificates, SslProtocols.Tls12, true);

                    if (!sslStream.IsEncrypted || !sslStream.IsSigned)
                    {
                        stream.Dispose();
                        throw new AuthenticationException("SSL stream is not both encrypted and signed.");
                    }
                }
                catch (Exception ex)
                {
                    protocol = null;

                    if (client != null)
                    {
                        State = ChannelState.ClosedReceived;
                        client.Close();
                        stream.Close();
                        client = null;
                    }
                   
                    State = ChannelState.Aborted;
                    Trace.TraceError(ex.Message);
                    OnError?.Invoke(this, new ChannelErrorEventArgs(Id, ex));
                }
            }
            else
            {
                stream = localStream;
            }

            State = ChannelState.Open;
            OnOpen?.Invoke(this, new ChannelOpenEventArgs(Id, null));
        }

        public async override Task SendAsync(byte[] msg)
        {
            if (msg.Length > maxBufferSize)
            {
                OnError?.Invoke(this, new ChannelErrorEventArgs(Id, new InvalidDataException("Message exceeds maximum buffer size for sendasync-2")));
                return;
            }

            queue.Enqueue(msg);

            while (queue.Count > 0)
            {
                byte[] message = queue.Dequeue();

                try
                {
                    await writeConnection.WaitAsync();

                    int offset = 0;
                    byte[] buffer = null;

                    int segments = message.Length / blockSize;
                    segments = message.Length % blockSize > 0 ? segments + 1 : segments;

                    int index = 0;
                    while (index < segments)
                    {
                        if (index + 1 == segments)
                        {
                            buffer = new byte[message.Length - offset];
                        }
                        else
                        {
                            buffer = new byte[blockSize];
                        }

                        Buffer.BlockCopy(message, offset, buffer, 0, buffer.Length);
                        offset += blockSize;                        

                        if(stream != null && stream.CanWrite)
                        {
                            if (psk != null)
                            {
                                stream.Write(buffer, 0, buffer.Length);
                                stream.Flush();
                            }
                            else
                            {
                                stream.Write(buffer, 0, buffer.Length);
                                stream.Flush();
                            }
                        }            

                        index++;
                    }

                }
                catch (Exception ex)
                {
                    State = ChannelState.Aborted;
                    Trace.TraceError("Channel {0} send error {1}", Id, ex.Message);
                    OnError?.Invoke(this, new ChannelErrorEventArgs(Id, ex));
                }
                finally
                {
                    writeConnection.Release();
                }
            }
        }

        public override async Task ReceiveAsync()
        {
            byte[] buffer = null;
            int offset = 0;
            int bytesRead = 0;
            byte[] msgBuffer = null;
            byte[] tempBuffer = null;

            try
            {

                while (client != null && client.Connected && !token.IsCancellationRequested)
                {
                    await readConnection.WaitAsync();
                    buffer = new byte[blockSize];

                    bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

                    offset += msgBuffer == null ? 0 : msgBuffer.Length;

                    if (offset + bytesRead > this.maxBufferSize)
                    {
                        Trace.TraceError("Message receives by tcp server channel2 {0} exceeds maximum message size.  Will close channel.", Id);
                        OnError?.Invoke(this, new ChannelErrorEventArgs(Id, new InvalidDataException("Message exceeds max buffer size to read.")));
                        return;
                    }

                    if (bytesRead == 0)
                    {
                        if (msgBuffer == null)
                        {
                            await CloseAsync();
                        }
                        else if (msgBuffer != null)
                        {
                            byte[] receiveBuffer = new byte[msgBuffer.Length];
                            Buffer.BlockCopy(msgBuffer, 0, receiveBuffer, 0, msgBuffer.Length);
                            OnReceive?.Invoke(this, new ChannelReceivedEventArgs(Id, receiveBuffer));
                            msgBuffer = null;
                            tempBuffer = null;
                            offset = 0;
                        }
                    }
                    else if (offset == 0)
                    {
                        msgBuffer = new byte[bytesRead];
                        Buffer.BlockCopy(buffer, 0, msgBuffer, 0, bytesRead);
                    }
                    else
                    {
                        tempBuffer = new byte[offset + bytesRead];
                        Buffer.BlockCopy(msgBuffer, 0, tempBuffer, 0, msgBuffer.Length);
                        Buffer.BlockCopy(buffer, 0, tempBuffer, offset, bytesRead);
                        msgBuffer = tempBuffer;
                    }

                    if (!localStream.DataAvailable || protocol != null)
                    {
                        if (msgBuffer != null)
                        {
                            byte[] receiveBuffer = new byte[msgBuffer.Length];
                            Buffer.BlockCopy(msgBuffer, 0, receiveBuffer, 0, msgBuffer.Length);
                            OnReceive?.Invoke(this, new ChannelReceivedEventArgs(Id, receiveBuffer));
                            msgBuffer = null;
                            tempBuffer = null;
                            offset = 0;
                        }
                        else
                        {
                            await CloseAsync();
                        }
                    }



                    readConnection.Release();
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Tcp Server Channel {0} receive error {1}", Id, ex.Message);
                OnError?.Invoke(this, new ChannelErrorEventArgs(Id, ex));
            }
            finally
            {
                readConnection.Release();
                await CloseAsync();
            }

            //byte[] buffer = null;
            //int offset = 0;
            //int bytesRead = 0;
            //byte[] msgBuffer = null;
            //byte[] tempBuffer = null;

            //try
            //{
            //    while (client.Connected && !token.IsCancellationRequested)
            //    {
            //        await readConnection.WaitAsync();
            //        buffer = new byte[blockSize];

            //        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            //        {
            //            offset += msgBuffer == null ? 0 : msgBuffer.Length;

            //            if (offset + bytesRead > this.maxBufferSize)
            //            {
            //                //await Log.LogErrorAsync("Message receives by tcp client channel2 exceeds maximum message size.  Will close channel.");
            //                OnError?.Invoke(this, new ChannelErrorEventArgs(Id, new IndexOutOfRangeException("TCP client channel receive exceed max buffer size for receiveasync-2")));
            //                //readConnection.Release();
            //                return;
            //            }

            //            if (offset == 0)
            //            {
            //                tempBuffer = new byte[bytesRead];
            //                Buffer.BlockCopy(buffer, 0, tempBuffer, offset, bytesRead);
            //            }
            //            else
            //            {
            //                tempBuffer = new byte[msgBuffer.Length + bytesRead];
            //                Buffer.BlockCopy(msgBuffer, 0, tempBuffer, 0, msgBuffer.Length);
            //                Buffer.BlockCopy(buffer, 0, tempBuffer, offset, bytesRead);
            //            }

            //            if (!localStream.DataAvailable)
            //            {
            //                break;
            //            }
            //        }

            //        if (tempBuffer != null && tempBuffer.Length > 0)
            //        {
            //            msgBuffer = tempBuffer;
            //            OnReceive?.Invoke(this, new ChannelReceivedEventArgs(Id, msgBuffer));
            //            offset = 0;
            //            tempBuffer = null;
            //            msgBuffer = null;

            //            if (this.client == null || !this.client.Client.Blocking)
            //            {
            //                break;
            //            }
            //        }
            //    }
            //}
            //catch (Exception ex)
            //{

            //    if (State != ChannelState.ClosedReceived && State != ChannelState.Closed)
            //    {
            //        State = ChannelState.Aborted;
            //        await Log.LogErrorAsync("Channel {0} receive error {1}", Id, ex.Message);
            //        OnError?.Invoke(this, new ChannelErrorEventArgs(Id, ex));
            //    }
            //}
            //finally
            //{
            //    readConnection.Release();
            //}
        }

        //private async Task ReceiveMessageAsync()
        //{
        //    byte[] msgBuffer = null;
           

        //    while(client.Connected && !token.IsCancellationRequested)
        //    {
        //        try
        //        {
        //            await readConnection.WaitAsync();
        //            byte[] buffer = new byte[blockSize];
        //            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
        //            if(bytesRead == 0)
        //            {
        //                if(msgBuffer == null)
        //                {
        //                    continue;
        //                }
        //                else
        //                {
        //                    byte[] receiveBuffer = new byte[msgBuffer.Length];
        //                    Buffer.BlockCopy(msgBuffer, 0, receiveBuffer, 0, msgBuffer.Length);
        //                    OnReceive?.Invoke(this, new ChannelReceivedEventArgs(Id, receiveBuffer));
        //                    msgBuffer = null;
        //                }
        //            }
        //            else
        //            {
        //                if(msgBuffer == null)
        //                {
        //                    msgBuffer = new byte[bytesRead];
        //                    Buffer.BlockCopy(buffer, 0, msgBuffer, 0, bytesRead);
        //                }
        //                else
        //                {
        //                    byte[] tempBuffer = new byte[bytesRead + msgBuffer.Length];
        //                    Buffer.BlockCopy(msgBuffer, 0, tempBuffer, 0, msgBuffer.Length);
        //                    Buffer.BlockCopy(buffer, 0, tempBuffer, msgBuffer.Length, bytesRead);
        //                    msgBuffer = tempBuffer;                            
        //                }
        //            }
        //        }
        //        catch(Exception ex)
        //        {
        //            OnError?.Invoke(this, new ChannelErrorEventArgs(Id, ex));
        //        }
        //        finally
        //        {
        //            readConnection.Release();
        //        }
        //    }
        //}


        //private async Task ReceiveEncryptedPskNonBlockingAsync()
        //{
        //    byte[] buffer;
        //    int bytesRead;
        //    byte[] msgBuffer = null;

        //    while(client.Connected && !token.IsCancellationRequested)
        //    {
        //        try
        //        {
        //            await readConnection.WaitAsync();

        //            bytesRead = protocol.GetAvailableInputBytes();

        //            if(bytesRead == 0)
        //            {
        //                if (msgBuffer == null)
        //                {
        //                    await Task.Delay(10);
        //                    continue;
        //                }
        //                else
        //                {
        //                    byte[] receiveBuffer = new byte[msgBuffer.Length];
        //                    Buffer.BlockCopy(msgBuffer, 0, receiveBuffer, 0, msgBuffer.Length);
        //                    OnReceive?.Invoke(this, new ChannelReceivedEventArgs(Id, receiveBuffer));
        //                    msgBuffer = null;
        //                }
        //            }
        //            else
        //            {
        //                buffer = new byte[bytesRead];
        //                byte[] cipherBuffer = new byte[bytesRead];
        //                protocol.OfferInput(cipherBuffer);
        //                protocol.ReadInput(buffer, 0, bytesRead);

        //                if (msgBuffer == null)
        //                {
        //                    msgBuffer = new byte[bytesRead];
        //                    Buffer.BlockCopy(buffer, 0, msgBuffer, 0, bytesRead);
        //                }
        //                else
        //                {
        //                    byte[] tempBuffer = new byte[msgBuffer.Length + bytesRead];
        //                    Buffer.BlockCopy(msgBuffer, 0, tempBuffer, 0, msgBuffer.Length);
        //                    Buffer.BlockCopy(buffer, 0, tempBuffer, msgBuffer.Length, bytesRead);
        //                    msgBuffer = tempBuffer;
        //                }
        //            }
        //        }
        //        catch(Exception ex)
        //        {
        //            OnError?.Invoke(this, new ChannelErrorEventArgs(Id, ex));
        //        }
        //        finally
        //        {
        //            readConnection.Release();
        //        }
        //    }
        //}


        public override async Task CloseAsync()
        {
            if (State == ChannelState.Closed)
            {
                return;
            }

            State = ChannelState.ClosedReceived;
                       

            if (this.client != null && this.client.Client.Blocking)
            {
                if (protocol != null)
                {
                    protocol.Close();
                }
            }

            protocol = null;

            if (client != null)
            {
                client.Close();
            }

            State = ChannelState.Closed;
            OnClose?.Invoke(this, new ChannelCloseEventArgs(Id));

            await Task.CompletedTask;
        }


        protected void Disposing(bool dispose)
        {
            if (dispose & !disposed)
            {
                if (this.client != null && this.client.Client.Blocking)
                {
                    protocol.Close();
                }

                protocol = null;

                if (client != null)
                {
                    client.Close();
                }

                client = null;
                disposed = true;

                if (readConnection != null)
                {
                    readConnection.Dispose();
                }

                if (writeConnection != null)
                {
                    writeConnection.Dispose();
                }


            }
        }

        public override void Dispose()
        {
            Disposing(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region private methods

        private bool ValidateCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslpolicyerrors)
        {
            if (sslpolicyerrors != SslPolicyErrors.None)
            {
                return false;
            }

            if (certificate == null)
            {
                return false;
            }
            else
            {
                X509Certificate2 cert = new X509Certificate2(certificate);
                return (cert.NotBefore < DateTime.Now && cert.NotAfter > DateTime.Now);
            }
        }

        #endregion

    }
}
