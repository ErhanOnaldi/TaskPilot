using Npgsql;

namespace TaskPilot.Persistence;

public static class PostgreSqlConnectionString
{
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException("PostgreSql connection string is missing.");

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "postgres" && uri.Scheme != "postgresql"))
            return value;

        var userInfo = uri.UserInfo.Split(':', 2);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty
        };

        foreach (var item in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = item.Split('=', 2);
            if (pair.Length != 2 || !pair[0].Equals("sslmode", StringComparison.OrdinalIgnoreCase))
                continue;

            builder.SslMode = Uri.UnescapeDataString(pair[1]).ToLowerInvariant() switch
            {
                "disable" => SslMode.Disable,
                "allow" => SslMode.Allow,
                "prefer" => SslMode.Prefer,
                "require" => SslMode.Require,
                "verify-ca" => SslMode.VerifyCA,
                "verify-full" => SslMode.VerifyFull,
                _ => builder.SslMode
            };
        }

        return builder.ConnectionString;
    }
}
