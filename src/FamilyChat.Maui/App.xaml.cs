using FamilyChat.Maui.Pages;

namespace FamilyChat.Maui;

public partial class App : Application
{
    public App(LoginPage loginPage)
    {
        InitializeComponent();
        MainPage = new NavigationPage(loginPage);
    }
}
