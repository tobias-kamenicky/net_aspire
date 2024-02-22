using System.Net.Http.Json;

namespace AspireDemo.Core.User;

public class UserClient(HttpClient httpClient)
{
    public async Task<UserDetails?> GetUserDetailsAsync(Guid id) => await httpClient.GetFromJsonAsync<UserDetails>($"/api/users/{id}");
    public async IAsyncEnumerable<UserListItem> ListUsersAsync()
    {
        var users = await httpClient.GetFromJsonAsync<UserListItem[]>("/api/users");
        foreach (var user in users ?? [])
        {
            yield return user;
        }
    }

    public async Task CreateUserAsync(CreateUser command) => await httpClient.PostAsJsonAsync("/api/users", command);
    public async Task RenameUserAsync(RenameUser command) => await httpClient.PutAsJsonAsync("/api/users/rename", command);
    public async Task DisableUserAsync(DisableUser command) => await httpClient.PostAsJsonAsync("/api/users/disable", command);
}
