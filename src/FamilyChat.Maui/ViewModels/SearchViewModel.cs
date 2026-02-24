using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyChat.ClientSdk.Api;
using FamilyChat.Contracts.Search;

namespace FamilyChat.Maui.ViewModels;

public partial class SearchViewModel(FamilyChatApiClient apiClient) : ObservableObject
{
    [ObservableProperty]
    private string query = string.Empty;

    public ObservableCollection<SearchHitDto> Hits { get; } = [];

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(Query))
        {
            Hits.Clear();
            return;
        }

        var response = await apiClient.SearchAsync(Query, null, CancellationToken.None);

        Hits.Clear();
        foreach (var hit in response.Hits)
        {
            Hits.Add(hit);
        }
    }
}
