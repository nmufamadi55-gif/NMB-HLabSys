using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NMB_HLabSys.Services
{
    public class BarcodeService : IBarcodeService
    {
        private static readonly Random _random = new Random();
        private const string BarcodeChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        public string GenerateBarcode(string prefix = "BAR")
        {
            var datePart = DateTime.Now.ToString("yyyyMMdd");
            var randomPart = GenerateRandomString(6);
            return $"{prefix}-{datePart}-{randomPart}";
        }

        public List<string> GenerateBarcodes(int count, string prefix = "BAR")
        {
            var barcodes = new List<string>();
            var usedCodes = new HashSet<string>();

            for (int i = 0; i < count; i++)
            {
                string barcode;
                int attempts = 0;
                do
                {
                    barcode = GenerateBarcode(prefix);
                    attempts++;
                } while (usedCodes.Contains(barcode) && attempts < 50);

                usedCodes.Add(barcode);
                barcodes.Add(barcode);
            }

            return barcodes;
        }

        public bool ValidateBarcode(string barcode)
        {
            if (string.IsNullOrEmpty(barcode)) return false;

            var parts = barcode.Split('-');
            if (parts.Length != 3) return false;
            if (parts[0] != "BAR") return false;

            if (!DateTime.TryParseExact(parts[1], "yyyyMMdd", null,
                System.Globalization.DateTimeStyles.None, out _)) return false;

            if (parts[2].Length != 6) return false;
            return parts[2].All(c => char.IsLetterOrDigit(c));
        }

        private string GenerateRandomString(int length)
        {
            var result = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                result.Append(BarcodeChars[_random.Next(BarcodeChars.Length)]);
            }
            return result.ToString();
        }
    }
}