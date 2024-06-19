using ReactiveUI;
using System.Diagnostics;
using System.Threading.Tasks;
using TCPChat.Messages;

namespace Client.ViewModels;

public class MainViewModel : ViewModelBase
{
    public Client _client;
    private string textBox1 = string.Empty;
    private string textBox2 = string.Empty;
    private string textBox3 = string.Empty;

    public string TextBox1
    {
        get { return textBox1; }
        set { this.RaiseAndSetIfChanged(ref textBox1, value); }
    }
    public string TextBox2
    {
        get { return textBox2; }
        set { this.RaiseAndSetIfChanged(ref textBox2, value); }
    }
    public string TextBox3
    {
        get { return textBox3; }
        set { this.RaiseAndSetIfChanged(ref textBox3, value); }
    }
    public MainViewModel()
    {
        _client = new Client();
        _client.MessageReceived += UpdateTextBox;
        _client.ErrorMessageRised += UpdateErrorTextBox;
    }

    private async void UpdateErrorTextBox(ErrorMessage message)
    {
        await Task.Run(() => { TextBox1 += $"{message.Error}\n"; });
    }
    private async void UpdateTextBox(ChatMessage message)
    {
        await Task.Run(() => { TextBox3 += $"{message.ClientId}: {message.Content}\n"; });
    }

    public async void ConnectButtonClicked()
    {
        Debug.WriteLine("ClickedConnectButton");
        await _client.ConnectToServerAsync();
    }

    public async Task OnEnterKeyPressed()
    {
        string textToSend = TextBox2;
        TextBox2 = string.Empty;
        await _client.SendMessageAsync(textToSend);
    }

}
