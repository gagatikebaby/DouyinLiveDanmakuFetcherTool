using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DouyinLiveReceiver.Models;

namespace DouyinLiveReceiver.Services
{
    /// <summary>
    /// UDP 消息接收服务，监听指定端口接收直播消息
    /// </summary>
    public class UdpReceiverService : IDisposable
    {
        private readonly int _port;
        private UdpClient _udpClient;
        private CancellationTokenSource _cts;
        private bool _isRunning;
        private readonly object _lock = new object();
        private int _messageCount = 0;

        public event Action<LiveMessage> OnMessageReceived;
        public event Action<string> OnError;
        public event Action OnStarted;
        public event Action OnStopped;

        public bool IsRunning => _isRunning;
        public int ReceivedMessageCount => _messageCount;

        public UdpReceiverService(int port = 9999)
        {
            _port = port;
        }

        public void Start()
        {
            lock (_lock)
            {
                if (_isRunning) return;

                try
                {
                    _cts?.Cancel();
                    _cts?.Dispose();
                    _udpClient?.Close();
                    _udpClient?.Dispose();

                    _udpClient = new UdpClient(_port);
                    _cts = new CancellationTokenSource();
                    _isRunning = true;
                    _messageCount = 0;

                    OnStarted?.Invoke();

                    Task.Run(() => ReceiveLoop(_cts.Token));
                }
                catch (Exception ex)
                {
                    _isRunning = false;
                    _udpClient?.Dispose();
                    _udpClient = null;
                    _cts?.Dispose();
                    _cts = null;
                    OnError?.Invoke($"启动失败: {ex.Message}");
                }
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                if (!_isRunning) return;

                _isRunning = false;
                _cts?.Cancel();
                _udpClient?.Close();

                _cts?.Dispose();
                _cts = null;
                _udpClient?.Dispose();
                _udpClient = null;

                OnStopped?.Invoke();
            }
        }

        private async void ReceiveLoop(CancellationToken token)
        {
            var lastReceivedTime = DateTime.MinValue;
            string lastReceivedMessage = null;

            while (!token.IsCancellationRequested && IsRunning)
            {
                try
                {
                    UdpClient client;
                    lock (_lock)
                    {
                        client = _udpClient;
                    }

                    if (client == null) break;

                    var result = await client.ReceiveAsync();
                    var json = Encoding.UTF8.GetString(result.Buffer);

                    var currentTime = DateTime.Now;
                    if (json == lastReceivedMessage && (currentTime - lastReceivedTime).TotalMilliseconds < 100)
                    {
                        System.Diagnostics.Debug.WriteLine($"[UDP] 跳过重复消息: {json}");
                        continue;
                    }

                    lastReceivedTime = currentTime;
                    lastReceivedMessage = json;

                    System.Diagnostics.Debug.WriteLine($"[UDP] 收到消息 #{++_messageCount}: {json}");

                    var message = MessageParser.Parse(json);

                    if (message != null)
                    {
                        OnMessageReceived?.Invoke(message);
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (SocketException) { break; }
                catch (ObjectDisposedException) { break; }
                catch (Exception ex)
                {
                    OnError?.Invoke($"解析消息失败: {ex.Message}");
                }
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
