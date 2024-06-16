using Google.Protobuf;
using System;
using System.Diagnostics;
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

        /// <summary>
        /// Create new connection if there is none
        /// Wait for client id and run receiving
        /// </summary>
        /// <param name="ip">127.0.0.1 by default</param>
        /// <param name="port">30015 by default</param>
        /// <returns></returns>
        public async Task ConnectToServerAsync(string ip = "192.168.0.3", int port = 30015)
        {
            Debug.WriteLine("asdaw");
            isRunning = true;
            while (isRunning)
                if (tcpServerConnection == null || !tcpServerConnection.Connected)
                {
                    cancellationTokenSource = new CancellationTokenSource();
                    Debug.WriteLine("aa");
                    tcpServerConnection = new TcpClient();
                    try
                    {
                        await tcpServerConnection.ConnectAsync(ip, port);
                        stream = tcpServerConnection.GetStream();
                        await Task.Run(() => ErrorMessageRised?.Invoke(new ErrorMessage { Error = "Connected..." }));
                        Task checkConnection = CheckConnection();
                        await ReceiveClientIdAsync();
                        await ReceiveMessagesAsync();
                        await checkConnection;
                    }
                    catch (Exception e)
                    {
                        await Task.Run(() => ErrorMessageRised?.Invoke(new ErrorMessage { Error = e.Message }));

                    }
                }
                else { break; }
        }

        public async Task SendMessageAsync(string? textToSend)
        {

            if (stream == null || clientId == null || tcpServerConnection==null || !tcpServerConnection.Connected)
            {
                await Task.Run(() => ErrorMessageRised?.Invoke(new ErrorMessage { Error = $"Client is not connected\n" }));
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
            catch (Exception)
            {

              
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
                    await ProcessIncomeMessage(response);

                }
            }
            catch (IOException e)
            {
                await Task.Run(() => ErrorMessageRised?.Invoke(new ErrorMessage { Error = $"{e.Message}" }));
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
                await ProcessIncomeMessage(clientIdMessage);
            }
            catch (Exception e)
            {
                await Task.Run(() => ErrorMessageRised?.Invoke(new ErrorMessage { Error = $"{e.Message}\n" }));
                cancellationTokenSource.Cancel();
                tcpServerConnection?.Close();
            }
        }

        private async Task ProcessIncomeMessage(ServerMessage message)
        {
            if (message.ChatMessage != null)
            {
                await Task.Run(() => MessageReceived?.Invoke(message.ChatMessage));

            }
            else if (message.ErrorMessage != null)
            {
                await Task.Run(() => ErrorMessageRised?.Invoke(message.ErrorMessage));
            }
        }
    }


}
