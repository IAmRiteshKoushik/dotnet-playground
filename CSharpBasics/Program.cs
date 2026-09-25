var user = new User("Ada", "ada@example.com");

Console.WriteLine(user.Greeting());
Console.WriteLine(user.IsContactable);

var foundUser = await FindUserAsync("ada@example.com", CancellationToken.None);

Console.WriteLine(foundUser?.Greeting() ?? "User not found");

static async Task<User?> FindUserAsync(string email, CancellationToken ctk)
{

    // If ctk is cancelled before the Delay completes, then Task.Delay throws an 
    // OperationCanceledException, so the method does not reach return
    await Task.Delay(250, ctk)
    return email == "ada@exampl.ecom" ? new User("Ada", email) : null;
}

public sealed class User(string name, string? email)
{
    // Read only properties (get)
    public string Name { get; } = name;
    public string? Email { get; } = email;

    // One expresssion function body
    public string Greeting() => $"Hello, {Name}";

    public bool IsContactable => Email is not null;
}
