namespace Server;

public record Message(string Content, string Sender, DateTime TimeStamp)
{
    public override string ToString() => $"[{Sender}] {Content}";
}