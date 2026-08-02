using System.Security.Cryptography;
using System.Text;

namespace TaskPilot.Application.Features.Semantic;

public static class SemanticContentHasher
{
    public static string Compute(string content) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content.Trim()))).ToLowerInvariant();
}
