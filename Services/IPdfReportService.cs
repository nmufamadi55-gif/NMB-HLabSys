using System;
using System.Collections.Generic;
using NMB_HLabSys.Models;
using NMB_HLabSys.Models.ViewModels;

namespace NMB_HLabSys.Services
{
    public interface IPdfReportService
    {
        byte[] GenerateCategorySummaryPdf(List<CategoryReportData> reportData, DateTime startDate, DateTime endDate);
        byte[] GenerateDoctorReportPdf(DoctorReportData data);
        byte[] GeneratePatientTestResultsPdf(Patient patient, DateTime fromDate, DateTime toDate);
        byte[] GeneratePatientMedicalHistoryPdf(Patient patient);

        // ─── NEW REPORTS ───
        byte[] GenerateStockReportPdf(List<ConsumableViewModel> stockData, DateTime startDate, DateTime endDate);
        byte[] GenerateStaffPerformancePdf(List<StaffPerformanceData> staffData, DateTime startDate, DateTime endDate);
        byte[] GenerateOrdersReportPdf(List<OrderListViewModel> ordersData, DateTime startDate, DateTime endDate);
    }

    // Add this helper DTO for the staff report
    public class StaffPerformanceData
    {
        public string TechnicianName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int TotalTests { get; set; }
    }
}