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
using vatsys;

namespace vatACARS.Services.Authority
{
    public class VatACARSAuthority
    {
        private readonly Uri _apiUri;
        private ClientWebSocket _clientWebSocket;
        private CancellationTokenSource _cancellationTokenSource;

        private readonly ConcurrentDictionary<string, TaskCompletionSource<ApiResponse>> _pendingResponses;

        public event Action<string> OnUnmatchedMessage;
        public event Action OnConnectionClosed;

        public VatACARSAuthority(string apiEndpoint)
        {
            _apiUri = new Uri(apiEndpoint);
            _clientWebSocket = new ClientWebSocket();
            _pendingResponses = new ConcurrentDictionary<string, TaskCompletionSource<ApiResponse>>();
        }

        public async Task ConnectAsync(string token)
        {
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                // Connect to the vatACARS gateway.
                await _clientWebSocket.ConnectAsync(_apiUri, _cancellationTokenSource.Token);
                _ = ListenForMessagesAsync(_cancellationTokenSource.Token);
                OnUnmatchedMessage += HandleUnmatchedMessage;

                ApiResponse logonResponse = await SendRequestAsync(Gateway.Authentication, GatewayAction.Authenticate, new Dictionary<string, object>{ { "token", token } });
                ApiResponse stationResponse = await SendRequestAsync(Gateway.Identity, GatewayAction.RegisterClient, new Dictionary<string, object> { { "stationCode", "YSSY" } });
                /*ApiResponse messageResponse = await SendRequestAsync(Gateway.CPDLC, GatewayAction.SendCPDLCMessage, new Dictionary<string, object> {
                    { "recipient", "BAW15" },
                    { "responseCode", "N" },
                    { "message", "PDC 280303\nBAW15 B77W YMML 0000\nCLRD TO WSSS VIA\nKEPPA2 DEP RWY 16\nROUTE: KEPPA H164 LEC A461 AS TIMMI CIN\nCLIMB VIA SID TO: A050\nDEP FREQ: ADVISORY 122.8\nSQUAWK 1562\nONLY READBACK SID, SQUAWK CODE,\nAND BAY NO. ON: 120.5\nGDAY" }
                });
                MessageBox.Show(stationResponse.Message);*/
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<ApiResponse> SendRequestAsync(Gateway gateway, GatewayAction action, Dictionary<string, object> data)
        {
            if (_clientWebSocket.State != WebSocketState.Open) throw new InvalidOperationException("Websocket is not connected");

            string requestId = Guid.NewGuid().ToString();
            data["action"] = action;
            data["requestId"] = requestId;

            var payload = new Dictionary<string, object>
            {
                { "event", gateway },
                { "data", data }
            };

            string message = JsonConvert.SerializeObject(payload);
            var tcs = new TaskCompletionSource<ApiResponse> ();
            _pendingResponses[requestId] = tcs;

            await SendMessageAsync(message);
            return await tcs.Task;
        }

        public async Task SendMessageAsync(string message)
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
                while(_clientWebSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    var result = await _clientWebSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        cancellationToken
                    );

                    if(result.MessageType == WebSocketMessageType.Close)
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
            try {
                var response = JsonConvert.DeserializeObject<ApiResponse>(receivedMessage);
                if(response != null && response.RequestId != "") { }
                {
                    string requestId = response.RequestId;
                    if (_pendingResponses.TryRemove(requestId, out var tcs))
                    {
                        if (response.Status != "success")
                        {
                            Errors.Add(new Exception(response.Message), "vatACARS");
                            tcs.SetException(new Exception(response.Message));
                        }
                        tcs.SetResult(response);
                        return;
                    }
                    OnUnmatchedMessage?.Invoke(receivedMessage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to process message: {ex.Message}");
                OnUnmatchedMessage?.Invoke(receivedMessage);
            }
        }

        public async Task DisconnectAsync()
        {
            if(_clientWebSocket.State == WebSocketState.Open)
            {
                _cancellationTokenSource.Cancel();
                await _clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Identity disconnected", CancellationToken.None);
            }
        }

        public async void HandleUnmatchedMessage(string message)
        {
            MessageBox.Show(message);
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
