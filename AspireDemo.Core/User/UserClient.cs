using System.Net.Http.Json;
using System.Runtime.CompilerServices;

namespace AspireDemo.Core.User;

public class UserClient(HttpClient httpClient)
{
    public async Task<UserDetails?> GetUserDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<UserDetails>($"/api/users/{id}", cancellationToken);

    public async IAsyncEnumerable<UserListItem> ListUsersAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var users = await httpClient.GetFromJsonAsync<UserListItem[]>("/api/users", cancellationToken);
        foreach (var user in users ?? [])
        {
            yield return user;
        }
    }

    public async Task CreateUserAsync(CreateUser command, CancellationToken cancellationToken = default)
        => await httpClient.PostAsJsonAsync("/api/users", command, cancellationToken);
    public async Task RenameUserAsync(RenameUser command, CancellationToken cancellationToken = default) 
        => await httpClient.PutAsJsonAsync("/api/users/rename", command, cancellationToken);
    public async Task DisableUserAsync(DisableUser command, CancellationToken cancellationToken = default)
        => await httpClient.PostAsJsonAsync("/api/users/disable", command, cancellationToken);
}
