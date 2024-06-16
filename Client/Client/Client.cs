using Avalonia.Controls;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TCPChat.Messages;
using Google.Protobuf;
using System.Diagnostics;
namespace Client
{
    internal class Client
    {
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
        public async Task ConnectToServerAsync(string ip = "127.0.0.1", int port = 30015)
        {
            if (tcpServerConnection == null || !tcpServerConnection.Connected)
            {
                tcpServerConnection = new TcpClient();
                try
                {
                    await tcpServerConnection.ConnectAsync(ip, port);
                    stream = tcpServerConnection.GetStream();
                    await ReceiveClientIdAsync();
                    await Task.Run(() => ReceiveMessagesAsync());
                }
                catch (Exception e)
                {
                    await Task.Run(() => ErrorMessageRised?.Invoke(new ErrorMessage { Error = e.Message }));
                }

            }
        }

        public async Task SendMessageAsync(string? textToSend)
        {
            if (stream == null || clientId == null)
            {
                await Task.Run(() => ErrorMessageRised?.Invoke(new ErrorMessage { Error = $"Client is not connected\n" }));
                return;
            }

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

        private async Task ReceiveMessagesAsync()
        {

            if (stream == null)
            {
                return;
            }
            try
            {
                while (true)
                {

                    var response = await Task.Run(() => ServerMessage.Parser.ParseDelimitedFrom(stream));
                    await ProcessIncomeMessage(response);

                }
            }
            catch (IOException e)
            {
                await Task.Run(() => ErrorMessageRised?.Invoke(new ErrorMessage { Error = $"{e.Message}" }));
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
