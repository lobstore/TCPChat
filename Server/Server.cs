using Google.Protobuf;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using TCPChat.Messages;
namespace Server
{
    public class Server
    {
        #region SetupVariables
        private TcpListener tcpListener;
        private ConcurrentDictionary<Guid, TcpClient> clients = new();
        private CancellationTokenSource cancellationTokenSource = new();
        private readonly ConcurrentQueue<ServerMessage> messageQueue = new();
        private Timer? timer;
        #endregion 
        public Server(int port)
        {
            tcpListener = new TcpListener(IPAddress.Any, port);
            
        }

        public async Task Start()
        {
            tcpListener.Start();
            Console.WriteLine("Server started. Waiting for connections...");

            timer = new Timer(SendServerTimeToClients, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
            try
            {
                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    TcpClient tcpClient = await tcpListener.AcceptTcpClientAsync();
                    Guid clientId = Guid.NewGuid();
                    clients.TryAdd(clientId, tcpClient);
                    Console.WriteLine($"Client connected: {tcpClient.Client.RemoteEndPoint} with id {clientId}");

                    Thread clientThread = new Thread(() => { HandleClient(clients[clientId], clientId); });
                    clientThread.Start();
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Server error: {ex.Message}");
            }
            finally
            {
                tcpListener.Stop();
            }
        }

        private void HandleClient(TcpClient client, Guid clientID)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                SendClientIdToClient(stream, clientID);
                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    var serverMessage = new ServerMessage();
                    var message = ServerMessage.Parser.ParseDelimitedFrom(stream);
                    serverMessage.ChatMessage = message.ChatMessage;
                    Console.WriteLine($"Received from {serverMessage.ChatMessage.ClientId}: {serverMessage.ChatMessage.Content}");
                    serverMessage.ChatMessage.ClientId = "Server";
                    messageQueue.Enqueue(serverMessage);


                    if (messageQueue.TryDequeue(out var dequeuedMessage))
                    {
                        dequeuedMessage.WriteDelimitedTo(stream);
                    }
                }
            }
            catch (IOException e)
            {
                Console.WriteLine($"{e.Message} Client ID: {clientID}");
            }
            finally
            {
                client.Close();
                clients.TryRemove(clientID, out _);
            }
        }
        private void SendClientIdToClient(NetworkStream stream, Guid clientId)
        {
            try
            {
                var serverMessage = new ServerMessage();
                var clientIdMessage = new ChatMessage
                {
                    ClientId = "Server",
                    Content = $"{clientId}"
                };
                serverMessage.ChatMessage = clientIdMessage;
                serverMessage.WriteDelimitedTo(stream);
            }
            catch (Exception)
            {


            }

        }

        private void SendServerTimeToClients(object? state)
        {
            if (clients.IsEmpty)
            {
                return;
            }
            foreach (var client in clients.Values)
            {
                try
                {
                    var serverMessage = new ServerMessage();
                    NetworkStream stream = client.GetStream();
                    var timeMessage = new ChatMessage
                    {
                        ClientId = "Server",
                        Content = DateTime.Now.ToString("o") 
                    };
                    serverMessage.ChatMessage = timeMessage;
                    serverMessage.WriteDelimitedTo(stream);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending time to client: {ex.Message}");

                }
            }
        }

        public void Stop()
        {
            cancellationTokenSource.Cancel();
            timer?.Dispose();
            var lastMessage = new ErrorMessage
            {
                Error = $"The server is unavailable, the connection is terminated"
            };
            var serverMessage = new ServerMessage();
            serverMessage.ErrorMessage = lastMessage;
            foreach (var client in clients.Values)
            {
                serverMessage.WriteDelimitedTo(client.GetStream());
                client.Close();
            }
            tcpListener.Stop();
        }

    }

}
