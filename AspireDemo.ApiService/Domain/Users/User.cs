namespace AspireDemo.ApiService.Domain.Users;

public sealed class User : AggregateRoot
{
    public string Name { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public bool Enabled { get; private set; }

    public User() { }

    private User(string name, string email)
    {
        ApplyChange(new UserCreated(Guid.NewGuid(), name, email));
    }

    public static Result<User> Create(string name, string email)
    {
        if (string.IsNullOrWhiteSpace(name)) return new Error(ErrorType.ValidationProblem, "User name cannot be empty", new Dictionary<string, string[]>{{nameof(name), ["Name cannot be empty"]}});
        if (string.IsNullOrWhiteSpace(email)) return new Error(ErrorType.ValidationProblem, "User email cannot be empty", new Dictionary<string, string[]>{{nameof(email), ["Email cannot be empty"]}});

        return new User(name, email);
    }

    public Result Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName)) return new Error(ErrorType.ValidationProblem, "User name cannot be empty", new Dictionary<string, string[]>{{nameof(newName), ["Name cannot be empty"]}});
        if (newName == Name) return Result.Ok();

        ApplyChange(new UserRenamed(Id, newName));
        return Result.Ok();
    }

    public Result Disable()
    {
        if (!Enabled) return Result.Ok();

        ApplyChange(new UserDisabled(Id));
        return Result.Ok();
    }
    

    private void Apply(UserCreated @event) => ((Id, Name, Email), Enabled) = (@event, true);

    private void Apply(UserRenamed @event) => Name = @event.Name;

    private void Apply(UserDisabled _) => Enabled = false;

    protected override void Apply(Event @event) => Apply(@event as dynamic);
}
