using FamilyChat.Maui.ViewModels;

namespace FamilyChat.Maui.Pages;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
