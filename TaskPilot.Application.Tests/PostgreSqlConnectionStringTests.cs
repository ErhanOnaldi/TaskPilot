using Npgsql;
using TaskPilot.Persistence;

namespace TaskPilot.Application.Tests;

public sealed class PostgreSqlConnectionStringTests
{
    [Fact]
    public void Normalize_keeps_npgsql_key_value_connection_strings()
    {
        const string connectionString = "Host=postgres;Port=5432;Database=taskpilot;Username=user;Password=password";

        Assert.Equal(connectionString, PostgreSqlConnectionString.Normalize(connectionString));
    }

    [Fact]
    public void Normalize_converts_render_style_postgresql_uri()
    {
        var result = PostgreSqlConnectionString.Normalize(
            "postgresql://taskpilot:p%40ss%3Aword@internal-db:5433/taskpilot_pro?sslmode=require");
        var parsed = new NpgsqlConnectionStringBuilder(result);

        Assert.Equal("internal-db", parsed.Host);
        Assert.Equal(5433, parsed.Port);
        Assert.Equal("taskpilot_pro", parsed.Database);
        Assert.Equal("taskpilot", parsed.Username);
        Assert.Equal("p@ss:word", parsed.Password);
        Assert.Equal(SslMode.Require, parsed.SslMode);
    }
}
