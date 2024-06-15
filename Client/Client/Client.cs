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
namespace Client
{
    internal class Client
    {
        private TcpClient? tcpServerConnection;
        private NetworkStream? stream;
        private string? clientId;
        public event Action<Message>? MessageReceived;
        public event Action<string>? ErrorMessageRised;
        
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
                    await ReceiveClientIdAsync(tcpServerConnection);
                    await Task.Run(() => ReceiveMessagesAsync());
                }
                catch (Exception e)
                {
                    ErrorMessageRised?.Invoke(e.Message);
                }

            }
        }

        public async Task SendMessageAsync(string? textToSend)
        {
            if (stream == null || clientId == null)
            {
                ErrorMessageRised?.Invoke($"Client is not connected\n");
                return;
            }

            if (!string.IsNullOrEmpty(textToSend))
            {

                var message = new Message
                {
                    ClientId = clientId.ToString(),
                    Content = textToSend
                };

                await Task.Run(() => message.WriteDelimitedTo(stream));
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

                    var response = await Task.Run(() => Message.Parser.ParseDelimitedFrom(stream));

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        MessageReceived?.Invoke(response);
                    });

                }
            }
            catch (IOException e)
            {
                ErrorMessageRised?.Invoke($"{e.Message}");
            }
        }
        private async Task ReceiveClientIdAsync(TcpClient tcpServerConnection)
        {
            try
            {
                var clientIdMessage = await Task.Run(() => Message.Parser.ParseDelimitedFrom(stream));
                Console.WriteLine(clientIdMessage.Content);

                clientId = clientIdMessage.ClientId;
                MessageReceived?.Invoke(clientIdMessage);
            }
            catch (Exception e)
            {
                ErrorMessageRised?.Invoke($"{e.Message}\n");
            }
        }

    }


}
