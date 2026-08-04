using System;
using System.Collections.Generic;

namespace NMB_HLabSys.Services
{
    public interface IBarcodeService
    {
        string GenerateBarcode(string prefix = "BAR");
        List<string> GenerateBarcodes(int count, string prefix = "BAR");
        bool ValidateBarcode(string barcode);
    }
}