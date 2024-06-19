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

        private Timer? timer;
        private bool isRunning = false;
        #endregion 
        public Server(int port)
        {
            tcpListener = new TcpListener(IPAddress.Any, port);
        }

        public void Start()
        {
            tcpListener.Start();
            Console.WriteLine("Server started. Waiting for connections...");
            isRunning = true;
            timer = new Timer(SendServerTimeToClients, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
            Thread run = new Thread(() =>
            {
                TcpClient? tcpClient = null;
                while (isRunning)
                {
                    try
                    {
                        tcpClient = tcpListener.AcceptTcpClient();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Server error: {e.Message}");
                        continue;
                    }
                    if (tcpClient != null)
                    {
                        Guid clientId = Guid.NewGuid();
                        clients.TryAdd(clientId, tcpClient);
                        HandleClient(clients[clientId], clientId);


                    }
                }
            });
            run.Start();
        }

        private void HandleClient(TcpClient client, Guid clientId)
        {
            try
            {
                CancellationTokenSource cancellationTokenSource = new();
                ConcurrentQueue<ServerMessage> messageQueue = new();
                NetworkStream stream = client.GetStream();
                SendClientIdToClient(stream, clientId);
                var serverMessage = new ServerMessage();
                Thread readThread = new Thread(() =>
                {
                    ReadMessage(clientId, messageQueue, stream, serverMessage, cancellationTokenSource);
                });

                Thread writeThread = new Thread(() =>
                {
                    WriteMessage(clientId, messageQueue, stream, cancellationTokenSource);
                });
                writeThread.Start();
                readThread.Start();
                Console.WriteLine($"Client connected: {client.Client.RemoteEndPoint} with id {clientId}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e.Message} Client ID: {clientId}");
                client.Close();
                clients.TryRemove(clientId, out _);
            }
        }

        private void WriteMessage(Guid clientId, ConcurrentQueue<ServerMessage> messageQueue,
                                  NetworkStream stream, CancellationTokenSource cancellationTokenSource)
        {
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    while (!messageQueue.IsEmpty)
                    {
                        if (messageQueue.TryDequeue(out var dequeuedMessage))
                        {
                            dequeuedMessage.WriteDelimitedTo(stream);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{e.Message} Client ID: {clientId}");
                    TcpClient? tcpClient = null;
                    clients.TryRemove(clientId, out tcpClient);
                    tcpClient?.Close();
                    cancellationTokenSource.Cancel();
                }
            }
        }

        private void ReadMessage(Guid clientId, ConcurrentQueue<ServerMessage> messageQueue, NetworkStream stream,
                                 ServerMessage serverMessage, CancellationTokenSource cancellationTokenSource)
        {
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                ServerMessage? message = null;
                try
                {
                    message = ServerMessage.Parser.ParseDelimitedFrom(stream);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{e.Message} Client ID: {clientId}");
                    TcpClient? tcpClient = null;
                    clients.TryRemove(clientId, out tcpClient);
                    tcpClient?.Close();
                    cancellationTokenSource.Cancel();
                }
                if (message != null)
                {
                    if (message.ChatMessage.Content == "close")
                    {
                        stream.Close();
                        Console.WriteLine($"Client ID: {clientId} has closed connection");
                        TcpClient? tcpClient = null;
                        clients.TryRemove(clientId, out tcpClient);
                        tcpClient?.Close();
                        cancellationTokenSource.Cancel();
                    }
                    else
                    {

                        serverMessage.ChatMessage = message.ChatMessage;
                        Console.WriteLine($"Received from {serverMessage.ChatMessage.ClientId}: {serverMessage.ChatMessage.Content}");
                        serverMessage.ChatMessage.ClientId = "Server";
                        messageQueue.Enqueue(serverMessage);
                    }
                }
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
            catch (Exception e)
            {
                Console.WriteLine($"{e.Message} Client ID: {clientId}");
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
            isRunning = false;
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
