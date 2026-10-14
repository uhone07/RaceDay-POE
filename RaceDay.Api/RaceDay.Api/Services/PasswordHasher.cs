using System.Security.Cryptography;

namespace RaceDay.Api.Services;

public static class PasswordHasher
{
    private const int SaltSize = 16;   // 128-bit salt
    private const int KeySize = 32;    // 256-bit hash
    private const int Iterations = 100_000;

    // Turns a plain password into "iterations.salt.hash" text we can store
    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(
            password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);

        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
    }

    // Checks a login password against the stored hash
    public static bool Verify(string password, string storedHash)
    {
        var parts = storedHash.Split('.');
        if (parts.Length != 3) return false;

        var iterations = int.Parse(parts[0]);
        var salt = Convert.FromBase64String(parts[1]);
        var expectedKey = Convert.FromBase64String(parts[2]);

        var actualKey = Rfc2898DeriveBytes.Pbkdf2(
            password, salt, iterations, HashAlgorithmName.SHA256, expectedKey.Length);

        return CryptographicOperations.FixedTimeEquals(actualKey, expectedKey);
    }
}