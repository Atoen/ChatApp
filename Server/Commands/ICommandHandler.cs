using Server.Users;

namespace Server.Commands;

public interface ICommandHandler
{
    char Prefix { get; }

    public bool CaseSensitive { get; set; }

    Task Handle(TcpUser user, string command);
}