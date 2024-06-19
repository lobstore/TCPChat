using Google.Protobuf;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TCPChat.Messages;
namespace Client
{
    internal class Client
    {
        bool isRunning = false;
        private CancellationTokenSource cancellationTokenSource = new();
        private TcpClient? tcpServerConnection;
        private NetworkStream? stream;
        private string? clientId;
        public event Action<ChatMessage>? MessageReceived;
        public event Action<ErrorMessage>? ErrorMessageRised;

        public async Task ConnectToServerAsync(string ip = "127.0.0.1", int port = 30015)
        {
            if (isRunning) return;
            isRunning = true;
            while (isRunning)
                if (tcpServerConnection == null || !tcpServerConnection.Connected)
                {
                    cancellationTokenSource = new CancellationTokenSource();
                    tcpServerConnection = new TcpClient();
                    try
                    {
                        await tcpServerConnection.ConnectAsync(ip, port);
                        stream = tcpServerConnection.GetStream();
                        SetKeepAlive();
                        ErrorMessageRised?.Invoke(new ErrorMessage { Error = "Connected..." });
                        Task checkConnection = CheckConnection();
                        await ReceiveClientIdAsync();
                        await ReceiveMessagesAsync();
                        await checkConnection;
                    }
                    catch (Exception e)
                    {
                        ErrorMessageRised?.Invoke(new ErrorMessage { Error = e.Message });
                    }
                }
                else { break; }
        }

        private void SetKeepAlive()
        {
            if (stream == null) return;
            try
            {
                stream.Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                stream.Socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, 120);
                stream.Socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, 2);
                stream.Socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, 3);
            }
            catch (Exception)
            {
            }
        }

        public async Task SendMessageAsync(string? textToSend)
        {

            if (stream == null || clientId == null || tcpServerConnection == null || !tcpServerConnection.Connected)
            {
                ErrorMessageRised?.Invoke(new ErrorMessage { Error = $"Client is not connected" });
                return;
            }
            try
            {
                if (!string.IsNullOrEmpty(textToSend))
                {

                    var message = new ChatMessage
                    {
                        ClientId = clientId.ToString(),
                        Content = textToSend
                    };
                    var serverMessage = new ServerMessage();
                    serverMessage.ChatMessage = message;
                    await Task.Run(() => serverMessage.WriteDelimitedTo(stream));
                }
            }
            catch (Exception e)
            {
                ErrorMessageRised?.Invoke(new ErrorMessage { Error = $"{e.Message}" });

            }

        }

        private async Task CheckConnection()
        {
            await Task.Run(async () =>
            {
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    if (tcpServerConnection == null || !tcpServerConnection.Connected || stream == null)
                    {
                        ErrorMessageRised?.Invoke(new ErrorMessage { Error = "Disconnected..." });
                        cancellationTokenSource.Cancel();
                        tcpServerConnection?.Close();
                    }
                    await Task.Delay(2000);
                }
            });
        }

        private async Task ReceiveMessagesAsync()
        {

            try
            {
                while (!cancellationTokenSource.IsCancellationRequested)
                {

                    var response = await Task.Run(() => ServerMessage.Parser.ParseDelimitedFrom(stream));
                    ProcessIncomeMessage(response);

                }
            }
            catch (IOException e)
            {
                ErrorMessageRised?.Invoke(new ErrorMessage { Error = $"{e.Message}" });
                cancellationTokenSource.Cancel();
                tcpServerConnection?.Close();
            }

        }
        private async Task ReceiveClientIdAsync()
        {
            try
            {
                var clientIdMessage = await Task.Run(() => ServerMessage.Parser.ParseDelimitedFrom(stream));
                clientId = clientIdMessage.ChatMessage.Content;
                ProcessIncomeMessage(clientIdMessage);
            }
            catch (Exception e)
            {
                ErrorMessageRised?.Invoke(new ErrorMessage { Error = $"{e.Message}\n" });
                cancellationTokenSource.Cancel();
                tcpServerConnection?.Close();
            }
        }

        private void ProcessIncomeMessage(ServerMessage message)
        {
            if (message.ChatMessage != null)
            {
                MessageReceived?.Invoke(message.ChatMessage);

            }
            else if (message.ErrorMessage != null)
            {
                ErrorMessageRised?.Invoke(message.ErrorMessage);
            }
        }
    }


}
