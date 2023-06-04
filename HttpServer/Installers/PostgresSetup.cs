using Npgsql;

namespace HttpServer.Installers;

public static class PostgresSetup
{
    public static string CreatePostgresConnectionString(this WebApplicationBuilder builder)
    {
        var connectionBuilder = new NpgsqlConnectionStringBuilder
        {
            Host = builder.Configuration["Postgres:Host"],
            Port = int.Parse(builder.Configuration["Postgres:Port"]!),
            Username = builder.Configuration["Postgres:Username"],
            Password = builder.Configuration["Postgres:Password"],
            Database = builder.Configuration["Postgres:Database"],
        };

        return connectionBuilder.ConnectionString;
    }
}