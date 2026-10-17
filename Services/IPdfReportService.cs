using System;
using System.Collections.Generic;
using NMB_HLabSys.Models.ViewModels;
using NMB_HLabSys.Models;

namespace NMB_HLabSys.Services
{
    public interface IPdfReportService
    {
        byte[] GenerateCategorySummaryPdf(List<CategoryReportData> reportData, DateTime startDate, DateTime endDate);
        byte[] GenerateDoctorReportPdf(DoctorReportData data);

        // ✅ NEW: Patient PDF reports
        byte[] GeneratePatientTestResultsPdf(Patient patient, DateTime fromDate, DateTime toDate);
        byte[] GeneratePatientMedicalHistoryPdf(Patient patient);
    }
}