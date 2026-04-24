using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PolizasBimbo.Application.Abstractions;

namespace PolizasBimbo.Infrastructure.Security;

public sealed class TokenSignerOptions
{
    public string SigningKey { get; set; } = string.Empty;
}

public sealed class JwtTokenSigner : ITokenSigner
{
    private readonly byte[] _key;

    public JwtTokenSigner(IOptions<TokenSignerOptions> options)
    {
        var raw = options.Value.SigningKey;
        if (string.IsNullOrWhiteSpace(raw) || raw.Length < 32)
            throw new InvalidOperationException("TokenSigner:SigningKey debe tener al menos 32 caracteres.");
        _key = Encoding.UTF8.GetBytes(raw);
    }

    public string Issue(Guid jti, int policyId, DateTime issuedAt, TimeSpan ttl)
    {
        var header = new { alg = "HS256", typ = "JWT" };
        var payload = new
        {
            jti = jti.ToString("D"),
            polId = policyId,
            iat = ToUnix(issuedAt),
            exp = ToUnix(issuedAt.Add(ttl))
        };

        var headerPart = Base64UrlJson(header);
        var payloadPart = Base64UrlJson(payload);
        var signingInput = $"{headerPart}.{payloadPart}";
        var signature = Base64UrlBytes(HmacSha256(signingInput));
        return $"{signingInput}.{signature}";
    }

    public TokenPayload? Validate(string token, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        var parts = token.Split('.');
        if (parts.Length != 3) return null;

        var expectedSig = Base64UrlBytes(HmacSha256($"{parts[0]}.{parts[1]}"));
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(expectedSig),
                Encoding.ASCII.GetBytes(parts[2])))
            return null;

        try
        {
            var json = DecodeBase64UrlToString(parts[1]);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var jti = Guid.Parse(root.GetProperty("jti").GetString()!);
            var polId = root.GetProperty("polId").GetInt32();
            var exp = root.GetProperty("exp").GetInt64();
            var expiresAt = DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime;
            return new TokenPayload(jti, polId, expiresAt);
        }
        catch
        {
            return null;
        }
    }

    private byte[] HmacSha256(string input)
    {
        using var h = new HMACSHA256(_key);
        return h.ComputeHash(Encoding.UTF8.GetBytes(input));
    }

    private static long ToUnix(DateTime utc) => new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc)).ToUnixTimeSeconds();

    private static string Base64UrlJson(object value)
        => Base64UrlBytes(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value)));

    private static string Base64UrlBytes(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string DecodeBase64UrlToString(string input)
    {
        var padded = input.Replace('-', '+').Replace('_', '/');
        padded += (padded.Length % 4) switch { 2 => "==", 3 => "=", _ => "" };
        return Encoding.UTF8.GetString(Convert.FromBase64String(padded));
    }
}
