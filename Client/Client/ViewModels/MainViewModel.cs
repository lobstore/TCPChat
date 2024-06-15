using Avalonia.Input;
using System.Diagnostics;
using TCPChat.Messages;

namespace Client.ViewModels;

public class MainViewModel : ViewModelBase
{
    readonly Client _client;
    private string textBox1 = string.Empty;
    private string textBox2 = string.Empty;
    private string textBox3 = string.Empty;

    public string TextBox1
    {
        get { return textBox1; }
        set { textBox1 = value; }
    }
    public string TextBox2 { get { return textBox2; } set { textBox2 = value; } }
    public string TextBox3 { get => textBox3; set => textBox3 = value; }
    public MainViewModel()
    {
        _client = new Client();
        _client.MessageReceived += UpdateTextBox;
    }

    private void UpdateTextBox(Message message)
    {
        textBox3 += $"{message.ClientId}: {message.Content}";
    }

    public async void ConnectButtonClicked()
    {
        Debug.WriteLine("ClickedConnectButton");
        await _client.ConnectToServerAsync();
    }

    public async void MessageEnterPressed(object source, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            await _client.SendMessageAsync(TextBox2);

        }
    }

}
