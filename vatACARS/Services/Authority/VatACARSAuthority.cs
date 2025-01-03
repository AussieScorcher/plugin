using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using vatACARS.Models.Enums;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Windows.Forms;

namespace vatACARS.Services.Authority
{
    public class VatACARSAuthority
    {
        private readonly Uri _apiUri;
        private ClientWebSocket _clientWebSocket;
        private CancellationTokenSource _cancellationTokenSource;

        private readonly ConcurrentDictionary<string, TaskCompletionSource<ApiResponse>> _pendingResponses;
        private readonly ConcurrentQueue<QueuedRequest> _requestQueue;
        private readonly SemaphoreSlim _connectDisconnectSemaphore = new SemaphoreSlim(1, 3);
        private int _reconnectAttempts = 0;
        private string token;

        public event Action<string> OnUnmatchedMessage;
        public event Action OnConnectionClosed;

        public VatACARSAuthority(string apiEndpoint)
        {
            _apiUri = new Uri(apiEndpoint);
            _clientWebSocket = new ClientWebSocket();
            _pendingResponses = new ConcurrentDictionary<string, TaskCompletionSource<ApiResponse>>();
            _requestQueue = new ConcurrentQueue<QueuedRequest>();
            _connectDisconnectSemaphore = new SemaphoreSlim(1, 1);
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public async Task<bool> ConnectAsync(string token)
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            this.token = token;

            try
            {
                await _clientWebSocket.ConnectAsync(_apiUri, _cancellationTokenSource.Token);
                _ = HandleWebSocketReconnect();
                _ = ListenForMessagesAsync(_cancellationTokenSource.Token);
                _ = ProcessQueueAsync();

                OnUnmatchedMessage += HandleUnmatchedMessage;

                ApiResponse logonResponse = await EnqueueRequestAsync(Gateway.Authentication, GatewayAction.Authenticate, new Dictionary<string, object> { { "token", this.token } });
                return logonResponse.Status == "success";
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> StationLogon(string stationCode)
        {
            await _connectDisconnectSemaphore.WaitAsync();
            try
            {
                ApiResponse stationResponse = await EnqueueRequestAsync(Gateway.Identity, GatewayAction.RegisterClient, new Dictionary<string, object> { { "stationCode", stationCode } });
                return stationResponse.Status == "success";
            }
            finally
            {
                _connectDisconnectSemaphore.Release();
            }
        }

        public async Task<bool> StationLogoff()
        {
            await _connectDisconnectSemaphore.WaitAsync();
            try
            {
                ApiResponse logoffResponse = await EnqueueRequestAsync(Gateway.Identity, GatewayAction.Logout, new Dictionary<string, object>() { });
                return logoffResponse.Status == "success";
            } finally
            {
                _connectDisconnectSemaphore.Release();
            }
        }

        private async Task<ApiResponse> EnqueueRequestAsync(Gateway gateway, GatewayAction action, Dictionary<string, object> data)
        {
            var tcs = new TaskCompletionSource<ApiResponse>();
            var request = new QueuedRequest
            {
                Gateway = gateway,
                Action = action,
                Data = data,
                CompletionSource = tcs
            };

            _requestQueue.Enqueue(request);
            return await tcs.Task;
        }

        private async Task ProcessQueueAsync()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                if (_requestQueue.TryDequeue(out var queuedRequest)) await ProcessRequestAsync(queuedRequest);
                await Task.Delay(500);
            }
        }

        private async Task ProcessRequestAsync(QueuedRequest request)
        {
            try
            {
                if (_clientWebSocket.State != WebSocketState.Open)
                {
                    throw new InvalidOperationException("WebSocket is not connected");
                }

                string requestId = Guid.NewGuid().ToString();
                request.Data["action"] = request.Action;
                request.Data["requestId"] = requestId;

                var payload = new Dictionary<string, object>
                {
                    { "event", request.Gateway },
                    { "data", request.Data }
                };

                string message = JsonConvert.SerializeObject(payload);
                _pendingResponses[requestId] = request.CompletionSource;

                await SendMessageAsync(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to process request: {ex.Message}");
                request.CompletionSource.TrySetException(ex);
            }
        }

        private async Task SendMessageAsync(string message)
        {
            if (_clientWebSocket.State == WebSocketState.Open)
            {
                byte[] buffer = Encoding.UTF8.GetBytes(message);
                await _clientWebSocket.SendAsync(
                    new ArraySegment<byte>(buffer),
                    WebSocketMessageType.Text,
                    true,
                    _cancellationTokenSource.Token
                );
            }
        }

        private async Task ListenForMessagesAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[4096];

            try
            {
                while (_clientWebSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    var result = await _clientWebSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        cancellationToken
                    );

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        OnConnectionClosed?.Invoke();
                        break;
                    }

                    string receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    ProcessReceivedMessage(receivedMessage);
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Listening task canceled.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while receiving message: {ex.Message}");
                OnConnectionClosed?.Invoke();
            }
        }

        private void ProcessReceivedMessage(string receivedMessage)
        {
            try
            {
                if (_cancellationTokenSource.Token.IsCancellationRequested) return;
                var response = JsonConvert.DeserializeObject<ApiResponse>(receivedMessage);

                if (response != null && !string.IsNullOrEmpty(response.RequestId))
                {
                    if (_pendingResponses.TryRemove(response.RequestId, out var tcs))
                    {
                        if (response.Status != "success")
                        {
                            tcs.SetException(new Exception(response.Message));
                        }
                        else
                        {
                            tcs.SetResult(response);
                        }
                        return;
                    }
                }

                OnUnmatchedMessage?.Invoke(receivedMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to process message: {ex.Message}");
                OnUnmatchedMessage?.Invoke(receivedMessage);
            }
        }

        public async Task DisconnectAsync()
        {
            _cancellationTokenSource.Cancel();
            if (_clientWebSocket.State == WebSocketState.Open)
            {
                _cancellationTokenSource.Cancel();
                await _clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Identity disconnected", CancellationToken.None);
            }
        }

        private async Task HandleWebSocketReconnect()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                if (_clientWebSocket.State != WebSocketState.Open)
                {
                    _reconnectAttempts++;
                    if(_reconnectAttempts > 3)
                    {
                        Console.WriteLine("Failed to reconnect WebSocket after 3 attempts.");
                        break;
                    }
                    Console.WriteLine("Attempting to reconnect WebSocket...");
                    try
                    {
                        _clientWebSocket.Dispose();
                        _clientWebSocket = new ClientWebSocket();
                        await _clientWebSocket.ConnectAsync(_apiUri, _cancellationTokenSource.Token);
                        ApiResponse logonResponse = await EnqueueRequestAsync(Gateway.Authentication, GatewayAction.Authenticate, new Dictionary<string, object> { { "token", this.token } });
                        if (logonResponse.Status != "success") return;
                        _reconnectAttempts = 0;

                        _ = ListenForMessagesAsync(_cancellationTokenSource.Token);
                        Console.WriteLine("WebSocket reconnected successfully.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Reconnect failed: {ex.Message}");
                        await Task.Delay(5000);
                    }
                }
                await Task.Delay(1000);
            }
        }

        public void HandleUnmatchedMessage(string message)
        {
            MessageBox.Show($"Unmatched message received:\n\n{message}");
        }

        private class QueuedRequest
        {
            public Gateway Gateway { get; set; }
            public GatewayAction Action { get; set; }
            public Dictionary<string, object> Data { get; set; }
            public TaskCompletionSource<ApiResponse> CompletionSource { get; set; }
        }
    }

    public class ApiResponse
    {
        public string Status { get; set; }
        public string RequestId { get; set; }
        public Dictionary<string, object> Data { get; set; }
        public string Message { get; set; }

        public ApiResponse()
        {
            Data = new Dictionary<string, object>();
        }
    }
}
