namespace Server.Users;

public class User : IEquatable<User>
{
    public string Username { get; }
    public Guid Uid { get; }
    public string UidString { get; }

    public User(string username, Guid uid)
    {
        Username = username;
        Uid = uid;
        UidString = uid.ToString();
    }

    public static bool operator ==(User first, User second)
    {
        return first.Username == second.Username && first.Uid == second.Uid;
    }

    public static bool operator !=(User first, User second) => !(first == second);

    public bool Equals(User? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Username == other.Username && Uid.Equals(other.Uid) && UidString == other.UidString;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((User) obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Username, Uid, UidString);
    }
}