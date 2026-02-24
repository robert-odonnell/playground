using FamilyChat.Maui.Services;

namespace FamilyChat.Maui;

public partial class AppShell : Shell
{
    public bool ShowAdmin { get; }

    public AppShell(AppState appState)
    {
        InitializeComponent();
        ShowAdmin = appState.CurrentUser?.IsAdmin == true;
        BindingContext = this;
        Routing.RegisterRoute("conversation", typeof(Pages.ConversationPage));
    }
}
