using Google.Protobuf;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using TCPChat.Messages;
namespace Server
{
    public class Server
    {
        private TcpListener tcpListener;
        private ConcurrentDictionary<Guid, TcpClient> clients = new();
        private CancellationTokenSource cancellationTokenSource = new();
        private readonly ConcurrentQueue<Message> messageQueue = new();
        private Timer? timer;

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
                    await SendClientIdToClient(tcpClient.GetStream(), clientId);
                    Task.Run(() => HandleClient(tcpClient, clientId), cancellationTokenSource.Token);

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

        private async Task HandleClient(TcpClient client, Guid clientID)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    var message = await Task.Run(() => Message.Parser.ParseDelimitedFrom(stream));
                    Console.WriteLine($"Received from {message.ClientId}: {message.Content}");
                    messageQueue.Enqueue(message);


                    if (messageQueue.TryDequeue(out var dequeuedMessage))
                    {
                        await Task.Run(() => dequeuedMessage.WriteDelimitedTo(stream));
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
        private async Task SendClientIdToClient(NetworkStream stream, Guid clientId)
        {
            try
            {
                var clientIdMessage = new Message
                {
                    ClientId = clientId.ToString(),
                    Content = $"Your client ID is: {clientId}"
                };
                await Task.Run(() => clientIdMessage.WriteDelimitedTo(stream));
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
                    NetworkStream stream = client.GetStream();
                    var timeMessage = new Message
                    {
                        ClientId = "Server",
                        Content = DateTime.Now.ToString("o") // Отправка текущего времени в формате ISO 8601
                    };
                    timeMessage.WriteDelimitedTo(stream);
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
            var lastMessage = new Message
            {
                ClientId = "Server",
                Content = $"The server is unavailable, the connection is terminated"
            };
            foreach (var client in clients.Values)
            {
                lastMessage.WriteDelimitedTo(client.GetStream());
                client.Close();
            }
            tcpListener.Stop();
        }
    }

}
