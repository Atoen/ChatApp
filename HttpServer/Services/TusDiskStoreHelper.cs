namespace HttpServer.Services;

public class TusDiskStoreHelper
{
    public string? Path { get; }

    public TusDiskStoreHelper(IConfiguration configuration)
    {
        Path = configuration["Tus:Address"];
    }
}