using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class Server
    {
        private TcpListener tcpListener;
        private ConcurrentDictionary<string, TcpClient> clients = new ConcurrentDictionary<string, TcpClient>();
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        public Server(int port)
        {
            tcpListener = new TcpListener(IPAddress.Any, port);
        }

        public async Task StartAsync()
        {
            tcpListener.Start();
            Console.WriteLine("Server started. Waiting for connections...");

            try
            {
                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    TcpClient tcpClient = await tcpListener.AcceptTcpClientAsync();
                    Console.WriteLine($"Client connected: {tcpClient.Client.RemoteEndPoint}");

                    Task.Run(() => HandleClient(tcpClient), cancellationTokenSource.Token);
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

        private async Task HandleClient(TcpClient client)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                StreamReader reader = new StreamReader(stream);
                StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };

                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {

                    string? clientMessage = await reader.ReadLineAsync();

                    Console.WriteLine($"Message received from client: {clientMessage}");


                    await writer.WriteLineAsync($"Server echo: {clientMessage}");
                }
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                client.Close();
            }
        }

        public void Stop()
        {
            cancellationTokenSource.Cancel();
        }
    }

}
