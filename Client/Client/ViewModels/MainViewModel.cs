using Avalonia.Input;
using ReactiveUI;
using System;
using System.Diagnostics;
using System.Reactive;
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


    private void UpdateErrorTextBox(ErrorMessage message)
    {
        TextBox1 += $"{message.Error}\n";
    }
    private void UpdateTextBox(ChatMessage message)
    {
        TextBox3 += $"{message.ClientId}: {message.Content}\n";
    }

    public async void ConnectButtonClicked()
    {
        Debug.WriteLine("ClickedConnectButton");
        await _client.ConnectToServerAsync();
    }

    public async void OnEnterKeyPressed()
    {
        string textToSend = TextBox2;
        TextBox2 = string.Empty;
        await _client.SendMessageAsync(textToSend);
    }

}
