using System;

namespace WpfClient.Models;

public class User : IEquatable<User>
{
    public required string Username { get; init; }
    public required Guid Id { get; init; }

    public bool Equals(User? other)
    {
        return Username == other?.Username;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((User) obj);
    }

    public override int GetHashCode()
    {
        return Username.GetHashCode();
    }

    public static User System { get; } = new() {Username = "System", Id = Guid.Empty};

    public static bool operator == (User? first, User? second)
    {
        if (first is null || second is null) return false;

        return first.Username == second.Username;
    }

    public static bool operator !=(User? first, User? second)
    {
        return !(first == second);
    }
}