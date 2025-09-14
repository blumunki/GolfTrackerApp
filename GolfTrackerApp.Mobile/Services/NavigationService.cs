using Microsoft.AspNetCore.Components;

namespace GolfTrackerApp.Mobile.Services;

public interface INavigationService
{
    event Action<string>? NavigationRequested;
    void NavigateTo(string page);
    int? SelectedPlayerId { get; set; }
}

public class NavigationService : INavigationService
{
    public event Action<string>? NavigationRequested;
    
    public int? SelectedPlayerId { get; set; }
    
    public void NavigateTo(string page)
    {
        NavigationRequested?.Invoke(page);
    }
}