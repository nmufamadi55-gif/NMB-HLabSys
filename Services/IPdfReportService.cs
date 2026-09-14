using System;
using System.Collections.Generic;
using NMB_HLabSys.Models.ViewModels;

namespace NMB_HLabSys.Services
{
    public interface IPdfReportService
    {
        /// <summary>
        /// Generates a PDF byte array for management reports showing total tests performed grouped by category within a date range.
        /// </summary>
        byte[] GenerateCategorySummaryPdf(List<CategoryReportData> reportData, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Generates a PDF byte array for doctor reports showing all test requests submitted by the doctor.
        /// </summary>
        byte[] GenerateDoctorReportPdf(DoctorReportData data);
    }
}