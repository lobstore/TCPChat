namespace Server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Server server = new Server(30015);
            AppDomain.CurrentDomain.ProcessExit += (o, e) => server.Stop();

            server.Start();

            Console.WriteLine("Press enter to stop server");

            Console.ReadLine();
            server.Stop();
        }

    }
}
