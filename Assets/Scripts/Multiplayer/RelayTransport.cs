using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VelocityRivals
{
    /// <summary>Bounded WebSocket text transport. Background tasks never access Unity objects.</summary>
    public sealed class RelayTransport : IDisposable
    {
        readonly ClientWebSocket socket = new ClientWebSocket();
        readonly CancellationTokenSource cancel = new CancellationTokenSource();
        readonly ConcurrentQueue<string> incoming = new ConcurrentQueue<string>();
        readonly ConcurrentQueue<byte[]> outgoing = new ConcurrentQueue<byte[]>();
        readonly SemaphoreSlim available = new SemaphoreSlim(0);
        readonly Task lifetime;
        volatile string error;
        int disposed;
        public string Error => error;
        public Task Completion => lifetime;
        public static Uri ValidateEndpoint(string endpoint)
        {
            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || !string.IsNullOrEmpty(uri.UserInfo) ||
                (uri.Scheme != "wss" && !(uri.Scheme == "ws" && uri.IsLoopback)))
                throw new ArgumentException("Enter a wss:// relay address. Local tests may use ws://127.0.0.1.");
            return uri;
        }
        public RelayTransport(string endpoint)
        {
            Uri uri = ValidateEndpoint(endpoint);
            socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(15);
            lifetime = Task.Run(() => Run(uri));
        }
        public void Send(string json)
        {
            if (Volatile.Read(ref disposed) != 0) throw new ObjectDisposedException(nameof(RelayTransport));
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            if (bytes.Length == 0 || bytes.Length > LanFrameBuffer.MaximumPayload || outgoing.Count >= 128)
                throw new InvalidOperationException("Relay send queue or packet limit exceeded.");
            outgoing.Enqueue(bytes); available.Release();
        }
        public bool TryReceive(out string json) => incoming.TryDequeue(out json);
        private async Task Run(Uri uri)
        {
            try
            {
                using (var connectTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancel.Token))
                {
                    connectTimeout.CancelAfter(TimeSpan.FromSeconds(60));
                    await socket.ConnectAsync(uri, connectTimeout.Token).ConfigureAwait(false);
                }
                Task reader = Read(); Task writer = Write();
                await Task.WhenAny(reader, writer).ConfigureAwait(false);
                cancel.Cancel(); socket.Abort();
                await Task.WhenAll(reader, writer).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Volatile.Read(ref disposed) == 0) error = ex is OperationCanceledException ? "Relay connection timed out or closed." : ex.Message;
            }
            finally { socket.Dispose(); }
        }
        private async Task Read()
        {
            var buffer = new byte[4096];
            while (!cancel.IsCancellationRequested)
            {
                using (var packet = new MemoryStream())
                {
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancel.Token).ConfigureAwait(false);
                        if (result.MessageType != WebSocketMessageType.Text) throw new IOException("Relay connection closed or sent a non-text packet.");
                        if (packet.Length + result.Count > LanFrameBuffer.MaximumPayload) throw new IOException("Relay packet exceeds size limit.");
                        packet.Write(buffer, 0, result.Count);
                    } while (!result.EndOfMessage);
                    if (incoming.Count >= 128) throw new IOException("Relay receive queue limit exceeded.");
                    incoming.Enqueue(new UTF8Encoding(false, true).GetString(packet.ToArray()));
                }
            }
        }
        private async Task Write()
        {
            while (!cancel.IsCancellationRequested)
            {
                await available.WaitAsync(cancel.Token).ConfigureAwait(false);
                if (outgoing.TryDequeue(out var payload))
                    await socket.SendAsync(new ArraySegment<byte>(payload), WebSocketMessageType.Text, true, cancel.Token).ConfigureAwait(false);
            }
        }
        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0) return;
            cancel.Cancel();
            try { socket.Abort(); } catch (ObjectDisposedException) { }
            // Wait asynchronously before disposing primitives used by the worker tasks.
            _ = lifetime.ContinueWith(_ => { cancel.Dispose(); available.Dispose(); }, TaskScheduler.Default);
        }
    }
}
