using System.Security.Cryptography;
using System.Text;

namespace Backend.Service;

public static class TokenHelper
{
    public static string Generate() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}