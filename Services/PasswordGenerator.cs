using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NMB_HLabSys.Services
{
    public class PasswordGenerator : IPasswordGenerator
    {
        private const string Uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string Lowercase = "abcdefghijklmnopqrstuvwxyz";
        private const string Numbers = "0123456789";
        private const string Specials = "@$!%*?&";

        private static readonly Random _random = new Random();

        public string GenerateTemporaryPassword()
        {
            // Format: Prefix + 8 random chars + suffix
            // Example: Nmb#A7kL9mP1!
            var prefix = "Nmb#";
            var suffix = "1!";

            var allChars = Uppercase + Lowercase + Numbers + Specials;
            var randomChars = new string(Enumerable.Repeat(allChars, 8)
                .Select(s => s[_random.Next(s.Length)]).ToArray());

            return prefix + randomChars + suffix;
        }

        public bool IsPasswordValid(string password, out List<string> errors)
        {
            errors = new List<string>();

            if (string.IsNullOrWhiteSpace(password))
            {
                errors.Add("Password is required.");
                return false;
            }

            if (password.Length < 8)
                errors.Add("Password must be at least 8 characters long.");

            if (!Regex.IsMatch(password, @"[A-Z]"))
                errors.Add("Password must contain at least one uppercase letter.");

            if (!Regex.IsMatch(password, @"[a-z]"))
                errors.Add("Password must contain at least one lowercase letter.");

            if (!Regex.IsMatch(password, @"[0-9]"))
                errors.Add("Password must contain at least one number.");

            if (!Regex.IsMatch(password, @"[@$!%*?&]"))
                errors.Add("Password must contain at least one special character (@$!%*?&).");

            return errors.Count == 0;
        }
    }
}