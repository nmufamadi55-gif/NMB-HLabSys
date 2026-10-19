using System.Security.Cryptography;
using System.Text;

namespace NMB_HLabSys.Services
{
    public interface IPasswordHasher
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string hash);

        /// <summary>
        /// True when the stored hash is NOT the current PBKDF2 format
        /// (e.g. legacy SHA256 or malformed) and should be upgraded on next login.
        /// </summary>
        bool NeedsRehash(string hash);
    }

    public class PasswordHasher : IPasswordHasher
    {
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 10000;

        public string HashPassword(string password)
        {
            using var algorithm = new Rfc2898DeriveBytes(
                password, SaltSize, Iterations, HashAlgorithmName.SHA256);

            var salt = algorithm.Salt;
            var key = algorithm.GetBytes(KeySize);

            var hashBytes = new byte[SaltSize + KeySize];
            Array.Copy(salt, 0, hashBytes, 0, SaltSize);
            Array.Copy(key, 0, hashBytes, SaltSize, KeySize);

            return Convert.ToBase64String(hashBytes);
        }

        public bool VerifyPassword(string password, string hash)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
                return false;

            byte[] hashBytes;
            try { hashBytes = Convert.FromBase64String(hash); }
            catch (FormatException) { return false; }

            // Legacy SHA256 (32 bytes)
            if (hashBytes.Length == 32)
            {
                using var sha = SHA256.Create();
                var legacyHash = Convert.ToBase64String(
                    sha.ComputeHash(Encoding.UTF8.GetBytes(password)));

                return CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(legacyHash),
                    Encoding.UTF8.GetBytes(hash));
            }

            if (hashBytes.Length != SaltSize + KeySize)
                return false;

            var salt = new byte[SaltSize];
            Array.Copy(hashBytes, 0, salt, 0, SaltSize);

            using var algorithm = new Rfc2898DeriveBytes(
                password, salt, Iterations, HashAlgorithmName.SHA256);
            var key = algorithm.GetBytes(KeySize);

            for (int i = 0; i < KeySize; i++)
                if (hashBytes[i + SaltSize] != key[i])
                    return false;

            return true;
        }

        // ─── NEW ───
        public bool NeedsRehash(string hash)
        {
            if (string.IsNullOrWhiteSpace(hash)) return true;

            byte[] bytes;
            try { bytes = Convert.FromBase64String(hash); }
            catch (FormatException) { return true; }

            // Current PBKDF2 format is 48 bytes (16 salt + 32 key).
            // Anything else (legacy SHA256 = 32 bytes, garbage, empty) → rehash.
            return bytes.Length != SaltSize + KeySize;
        }
    }
}