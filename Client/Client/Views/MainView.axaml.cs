using Avalonia.Controls;
using Avalonia.Input;
using Client.ViewModels;
namespace Client.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();

    }

    public async void MessageEnterPressed(object? source, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (e.Key == Key.Enter)
            {
                var viewModel = DataContext as MainViewModel;
                if (viewModel != null)
                {
                    await viewModel.OnEnterKeyPressed();
                }
            }

        }
    }

}
