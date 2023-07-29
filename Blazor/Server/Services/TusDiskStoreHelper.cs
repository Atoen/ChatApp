namespace Blazor.Server.Services;

public class TusDiskStoreHelper
{
    public string? Path { get; }

    public TusDiskStoreHelper(IConfiguration configuration)
    {
        Path = configuration["Tus:Address"];
    }
}