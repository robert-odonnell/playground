using FamilyChat.Maui.ViewModels;

namespace FamilyChat.Maui.Pages;

public partial class SearchPage : ContentPage
{
    private readonly SearchViewModel _viewModel;

    public SearchPage(SearchViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }
}
