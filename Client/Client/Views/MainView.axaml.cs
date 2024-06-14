using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Client.Views;

public partial class MainView : UserControl
{
    private TcpClient? tcpClient;
    private NetworkStream? stream;
    private StreamWriter? streamWriter;
    private StreamReader? streamReader;
    private string clientId = Guid.NewGuid().ToString();

    public MainView()
    {
        InitializeComponent();
    }
    public async void ConnectButtonClicked(object source, RoutedEventArgs arg)
    {

        textBox1.Text += "";
        Debug.WriteLine("ClickedConnectButton");
        await ConnectToServerAsync();
    }

    private async Task ConnectToServerAsync()
    {
        if (tcpClient == null || !tcpClient.Connected)
        {


            tcpClient = new TcpClient();
            try
            {
                await tcpClient.ConnectAsync("127.0.0.1", 30015);
                stream = tcpClient.GetStream();
                streamWriter = new StreamWriter(stream) { AutoFlush = true };
                streamReader = new StreamReader(stream);
                await Dispatcher.UIThread.InvokeAsync(() => textBox1.Text += $"Connected to server: {tcpClient.Client.RemoteEndPoint}\n");

                Task.Run(() => ReceiveMessagesAsync());
            }
            catch (Exception e)
            {
                Console.WriteLine($"Connection error: {e.Message}");
                await Dispatcher.UIThread.InvokeAsync(() => textBox1.Text += $"Connection error: {e.Message}\n");

            }

        }
    }


    public async void MessageEnterPressed(object source, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            await SendMessageAsync();
        }
    }
    private async Task SendMessageAsync()
    {
        if (streamWriter == null)
        {
            await Dispatcher.UIThread.InvokeAsync(() => textBox1.Text += $"Client is not connected\n");
            return;
        }
        string? textToSend = textBox2.Text;
        textBox2.Text = "";

        if (!string.IsNullOrEmpty(textToSend))
        {
            Debug.WriteLine("Message Has Been Pushed");
            await streamWriter.WriteLineAsync(textToSend);

            Console.WriteLine($"Message sent to server: {textToSend}");
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                textBox1.Text += $"Message has been send: {textToSend}\n";
            });
        }
    }
    private async Task ReceiveMessagesAsync()
    {
        if (streamReader == null)
        {
            return;
        }
        try
        {
            while (true)
            {
                string? serverMessage = await streamReader.ReadLineAsync();

                Console.WriteLine($"Message received from server: {serverMessage}");
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    textBox3.Text += $"Server: {serverMessage}\n";
                });
            }
        }
        catch (IOException e)
        {

        }
    }
}
