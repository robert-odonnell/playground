using FamilyChat.Maui.ViewModels;

namespace FamilyChat.Maui.Pages;

[QueryProperty(nameof(ConversationId), "conversationId")]
public partial class ConversationPage : ContentPage
{
    private readonly ConversationViewModel _viewModel;

    public string ConversationId
    {
        set
        {
            if (Guid.TryParse(value, out var parsed))
            {
                _ = _viewModel.InitializeCommand.ExecuteAsync(parsed);
            }
        }
    }

    public ConversationPage(ConversationViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_viewModel.ConversationId != Guid.Empty)
        {
            await _viewModel.LoadLatestCommand.ExecuteAsync(null);
        }
    }
}
