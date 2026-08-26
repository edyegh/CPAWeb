using System;
using System.Security.Cryptography;

namespace CPAWeb.Business.Services.Services
{
    // Գաղտնաբառերը պահվում են PBKDF2-HMAC-SHA256-ով, բաց տեքստով ոչ մի տեղ չեն գրվում.
    // Ձևաչափը՝  pbkdf2-sha256$<iterations>$<salt-base64>$<hash-base64>
    public static class PasswordHasher
    {
        private const string Prefix = "pbkdf2-sha256";
        private const char Separator = '$';

        private const int SaltSize = 16;      // բայթ
        private const int KeySize = 32;       // բայթ
        private const int Iterations = 100_000;

        public static string Hash(string password)
        {
            if (password == null)
                throw new ArgumentNullException(nameof(password));

            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);

            return string.Join(Separator,
                Prefix,
                Iterations.ToString(),
                Convert.ToBase64String(salt),
                Convert.ToBase64String(key));
        }

        public static bool Verify(string password, string encodedHash)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(encodedHash))
                return false;

            var parts = encodedHash.Split(Separator);

            if (parts.Length != 4 || parts[0] != Prefix)
                return false;

            if (!int.TryParse(parts[1], out int iterations) || iterations <= 0)
                return false;

            byte[] salt;
            byte[] expectedKey;

            try
            {
                salt = Convert.FromBase64String(parts[2]);
                expectedKey = Convert.FromBase64String(parts[3]);
            }
            catch (FormatException)
            {
                return false;
            }

            byte[] actualKey = Rfc2898DeriveBytes.Pbkdf2(
                password, salt, iterations, HashAlgorithmName.SHA256, expectedKey.Length);

            // Ժամանակից կախված համեմատություն
            return CryptographicOperations.FixedTimeEquals(actualKey, expectedKey);
        }
    }
}
