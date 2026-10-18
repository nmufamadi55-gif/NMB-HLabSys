// Services/PasswordHasher.cs
using System.Security.Cryptography;
using System.Text;

namespace NMB_HLabSys.Services
{
    public interface IPasswordHasher
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string hash);
        bool NeedsRehash(string hash);
    }

    public class PasswordHasher : IPasswordHasher
    {
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 10000;

        // PBKDF2 hash = 48 bytes → 64 base64 chars
        private const int Pbkdf2HashLength = 64;

        // Legacy SHA256 hash = 32 bytes → 44 base64 chars
        private const int LegacyHashLength = 44;

        public string HashPassword(string password)
        {
            using var algorithm = new Rfc2898DeriveBytes(
                password,
                SaltSize,
                Iterations,
                HashAlgorithmName.SHA256);

            var salt = algorithm.Salt;
            var key = algorithm.GetBytes(KeySize);

            var hashBytes = new byte[SaltSize + KeySize];
            Array.Copy(salt, 0, hashBytes, 0, SaltSize);
            Array.Copy(key, 0, hashBytes, SaltSize, KeySize);

            return Convert.ToBase64String(hashBytes);
        }

        public bool VerifyPassword(string password, string hash)
        {
            if (string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(password))
                return false;

            return hash.Length switch
            {
                Pbkdf2HashLength => VerifyPbkdf2(password, hash),
                LegacyHashLength => VerifyLegacySha256(password, hash),
                _ => false
            };
        }

        public bool NeedsRehash(string hash)
        {
            if (string.IsNullOrEmpty(hash)) return false;
            return hash.Length != Pbkdf2HashLength;
        }

        private bool VerifyPbkdf2(string password, string hash)
        {
            try
            {
                var hashBytes = Convert.FromBase64String(hash);
                if (hashBytes.Length < SaltSize + KeySize) return false;

                var salt = new byte[SaltSize];
                Array.Copy(hashBytes, 0, salt, 0, SaltSize);

                using var algorithm = new Rfc2898DeriveBytes(
                    password, salt, Iterations, HashAlgorithmName.SHA256);

                var key = algorithm.GetBytes(KeySize);

                for (int i = 0; i < KeySize; i++)
                {
                    if (hashBytes[i + SaltSize] != key[i])
                        return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool VerifyLegacySha256(string password, string hash)
        {
            try
            {
                using var sha = SHA256.Create();
                var computed = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
                var computedBase64 = Convert.ToBase64String(computed);

                return CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(computedBase64),
                    Encoding.UTF8.GetBytes(hash));
            }
            catch
            {
                return false;
            }
        }
    }
}