namespace Server
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Server server = new Server(30015);
           await server.StartAsync();
        }

    }
}
