using FamilyChat.Contracts.Conversations;
using FamilyChat.Maui.ViewModels;

namespace FamilyChat.Maui.Pages;

public partial class ConversationsPage : ContentPage
{
    private readonly ConversationsViewModel _viewModel;

    public ConversationsPage(ConversationsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadCommand.ExecuteAsync(null);
    }

    private async void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is ConversationDto conversation)
        {
            await _viewModel.OpenConversationCommand.ExecuteAsync(conversation);
            if (sender is CollectionView collectionView)
            {
                collectionView.SelectedItem = null;
            }
        }
    }
}
