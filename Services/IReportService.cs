//using Microsoft.EntityFrameworkCore;
//using NMB_HLabSys.Data;
//using NMB_HLabSys.Models;
//using NMB_HLabSys.Models.ViewModels;
//using System.Text;

//namespace NMB_HLabSys.Services
//{
//    public interface IReportService
//    {
//        Task<TestsPerformedReportViewModel> GenerateTestsPerformedReportAsync(DateTime startDate, DateTime endDate);
//        Task<StockReportViewModel> GenerateStockReportAsync();
//        Task<TechnicianPerformanceDetailsViewModel> GenerateTechnicianPerformanceReportAsync(int technicianId, DateTime startDate, DateTime endDate);
//        Task<OrderDetailViewModel> GenerateOrderReportAsync(int orderId);
//        Task<byte[]> GeneratePdfReportAsync(string reportType, DateTime startDate, DateTime endDate, int? id = null);
//        Task<string> GenerateCsvReportAsync(string reportType, DateTime startDate, DateTime endDate);
//    }

//    public class ReportService : IReportService
//    {
//        private readonly ApplicationDbContext _context;
//        private readonly IStockManagementService _stockService;
//        private readonly IOrderService _orderService;
//        private readonly ILogger<ReportService> _logger;

//        public ReportService(
//            ApplicationDbContext context,
//            IStockManagementService stockService,
//            IOrderService orderService,
//            ILogger<ReportService> logger)
//        {
//            _context = context;
//            _stockService = stockService;
//            _orderService = orderService;
//            _logger = logger;
//        }

//        public async Task<TestsPerformedReportViewModel> GenerateTestsPerformedReportAsync(DateTime startDate, DateTime endDate)
//        {
//            try
//            {
//                // This would come from TestResults table (other subsystem)
//                // For now, return a placeholder with sample data structure
//                var categories = await _context.TestCategories
//                    .Include(c => c.TestTypes)
//                    .Select(c => new CategoryReportViewModel
//                    {
//                        CategoryName = c.CategoryName,
//                        TotalTests = 0, // Would be from TestResults
//                        VerifiedTests = 0,
//                        AbnormalResults = 0,
//                        AverageTurnaroundTime = 0,
//                        PercentageOfTotal = 0,
//                        TestDetails = c.TestTypes.Select(t => new TestReportDetail
//                        {
//                            TestName = t.TestName,
//                            Count = 0,
//                            VerifiedCount = 0,
//                            AbnormalCount = 0,
//                            AverageTurnaroundTime = 0,
//                            PercentageOfCategory = 0
//                        }).ToList()
//                    })
//                    .ToListAsync();

//                var report = new TestsPerformedReportViewModel
//                {
//                    StartDate = startDate,
//                    EndDate = endDate,
//                    GeneratedDate = DateTime.Now,
//                    GeneratedBy = "System",
//                    Summary = new ReportSummaryViewModel
//                    {
//                        TotalTests = 0,
//                        TotalVerified = 0,
//                        TotalPending = 0,
//                        AbnormalResults = 0,
//                        OverallAverageTurnaroundTime = 0,
//                        UniquePatients = 0,
//                        UniqueDoctors = 0
//                    },
//                    Categories = categories,
//                    Technicians = new List<TechnicianReportViewModel>(),
//                    DailyBreakdown = new List<DailyReportViewModel>()
//                };

//                return report;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error generating tests performed report");
//                throw;
//            }
//        }

//        public async Task<StockReportViewModel> GenerateStockReportAsync()
//        {
//            try
//            {
//                var consumables = await _context.Consumables
//                    .Include(c => c.Supplier)
//                    .Select(c => new StockReportItemViewModel
//                    {
//                        ConsumableName = c.ConsumableName,
//                        SupplierName = c.Supplier != null ? c.Supplier.SupplierName : "Unknown",
//                        QuantityOnHand = c.QuantityOnHand,
//                        ReorderLevel = c.ReorderLevel,
//                        StockStatus = GetStockStatus(c.QuantityOnHand, c.ReorderLevel),
//                        UnitPrice = c.UnitPrice,
//                        TotalValue = c.QuantityOnHand * (c.UnitPrice ?? 0),
//                        UnitOfMeasure = c.UnitOfMeasure,
//                        StorageLocation = c.StorageLocation,
//                        LastRestocked = c.CreatedAt
//                    })
//                    .ToListAsync();

//                var supplierSummaries = await _context.Suppliers
//                    .Include(s => s.Consumables)
//                    .Select(s => new SupplierStockSummary
//                    {
//                        SupplierName = s.SupplierName,
//                        ItemsSupplied = s.Consumables.Count,
//                        ItemsBelowReorder = s.Consumables.Count(c => c.QuantityOnHand <= c.ReorderLevel),
//                        TotalValue = s.Consumables.Sum(c => c.QuantityOnHand * (c.UnitPrice ?? 0)),
//                        ContactPerson = s.ContactPerson,
//                        ContactEmail = s.EmailAddress
//                    })
//                    .ToListAsync();

//                var report = new StockReportViewModel
//                {
//                    GeneratedDate = DateTime.Now,
//                    GeneratedBy = "System",
//                    Summary = new StockReportSummary
//                    {
//                        TotalItems = await _context.Consumables.CountAsync(),
//                        ActiveItems = await _context.Consumables.CountAsync(c => c.IsActive),
//                        InactiveItems = await _context.Consumables.CountAsync(c => !c.IsActive),
//                        LowStockItems = await _context.Consumables.CountAsync(c => c.QuantityOnHand <= c.ReorderLevel),
//                        CriticalStockItems = await _context.Consumables.CountAsync(c => c.QuantityOnHand <= 0),
//                        TotalStockValue = await _context.Consumables.SumAsync(c => c.QuantityOnHand * (c.UnitPrice ?? 0)),
//                        TotalSuppliers = await _context.Suppliers.CountAsync(s => s.IsActive)
//                    },
//                    Items = consumables,
//                    SupplierSummaries = supplierSummaries
//                };

//                return report;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error generating stock report");
//                throw;
//            }
//        }

//        public async Task<TechnicianPerformanceDetailsViewModel> GenerateTechnicianPerformanceReportAsync(int technicianId, DateTime startDate, DateTime endDate)
//        {
//            try
//            {
//                var technician = await _context.LabTechnicians
//                    .Include(t => t.User)
//                    .FirstOrDefaultAsync(t => t.TechnicianID == technicianId);

//                if (technician == null)
//                    throw new ArgumentException($"Technician with ID {technicianId} not found");

//                // This would come from TestResults table (other subsystem)
//                // For now, return placeholder data
//                var report = new TechnicianPerformanceDetailsViewModel
//                {
//                    TechnicianID = technicianId,
//                    //TechnicianName = $"{technician.User.Name} {technician.User.Surname}",
//                    StartDate = startDate,
//                    EndDate = endDate,
//                    Summary = new PerformanceSummaryViewModel
//                    {
//                        TotalTests = 0,
//                        VerifiedTests = 0,
//                        PendingTests = 0,
//                        RejectedTests = 0,
//                        OverallAverageTurnaroundTime = 0,
//                        VerificationRate = 0
//                    },
//                    TestDetails = new List<TestPerformanceViewModel>(),
//                    DailyPerformance = new List<DailyPerformanceViewModel>()
//                };

//                return report;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, $"Error generating technician performance report for ID {technicianId}");
//                throw;
//            }
//        }

//        public async Task<OrderDetailViewModel> GenerateOrderReportAsync(int orderId)
//        {
//            try
//            {
//                return await _orderService.GetOrderDetailsAsync(orderId);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, $"Error generating order report for ID {orderId}");
//                throw;
//            }
//        }

//        public async Task<byte[]> GeneratePdfReportAsync(string reportType, DateTime startDate, DateTime endDate, int? id = null)
//        {
//            try
//            {
//                // This would use a PDF generation library like iTextSharp or QuestPDF
//                // For now, return a placeholder byte array
//                var html = await GenerateReportHtmlAsync(reportType, startDate, endDate, id);
//                var pdfBytes = ConvertHtmlToPdf(html); // Placeholder
//                return await Task.FromResult(pdfBytes);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, $"Error generating PDF report for type {reportType}");
//                throw;
//            }
//        }

//        public async Task<string> GenerateCsvReportAsync(string reportType, DateTime startDate, DateTime endDate)
//        {
//            try
//            {
//                var csv = new StringBuilder();

//                // Add headers
//                csv.AppendLine("Report Type,Generated Date,Start Date,End Date");

//                switch (reportType)
//                {
//                    case "TestsPerformed":
//                        var testReport = await GenerateTestsPerformedReportAsync(startDate, endDate);
//                        csv.AppendLine($"TestsPerformed,{testReport.GeneratedDate},{testReport.StartDate},{testReport.EndDate}");
//                        csv.AppendLine();
//                        csv.AppendLine("Category,Total Tests,Verified,Abnormal,Avg Turnaround Time");
//                        foreach (var category in testReport.Categories)
//                        {
//                            csv.AppendLine($"{category.CategoryName},{category.TotalTests},{category.VerifiedTests},{category.AbnormalResults},{category.AverageTurnaroundTime:F2}");
//                        }
//                        break;

//                    case "StockReport":
//                        var stockReport = await GenerateStockReportAsync();
//                        csv.AppendLine($"StockReport,{stockReport.GeneratedDate},,,");
//                        csv.AppendLine();
//                        csv.AppendLine("Item,Supplier,Quantity,Reorder Level,Status,Unit Price,Total Value");
//                        foreach (var item in stockReport.Items)
//                        {
//                            csv.AppendLine($"{item.ConsumableName},{item.SupplierName},{item.QuantityOnHand},{item.ReorderLevel},{item.StockStatus},{item.UnitPrice},{item.TotalValue}");
//                        }
//                        break;

//                    default:
//                        csv.AppendLine($"Unknown report type: {reportType}");
//                        break;
//                }

//                return await Task.FromResult(csv.ToString());
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, $"Error generating CSV report for type {reportType}");
//                throw;
//            }
//        }

//        private async Task<string> GenerateReportHtmlAsync(string reportType, DateTime startDate, DateTime endDate, int? id = null)
//        {
//            var html = new StringBuilder();
//            html.AppendLine("<!DOCTYPE html><html><head><meta charset='UTF-8'><title>Report</title>");
//            html.AppendLine("<style>");
//            html.AppendLine("body { font-family: Arial, sans-serif; margin: 40px; }");
//            html.AppendLine("h1 { color: #1a237e; border-bottom: 2px solid #1a237e; }");
//            html.AppendLine("table { width: 100%; border-collapse: collapse; margin: 20px 0; }");
//            html.AppendLine("th { background: #1a237e; color: white; padding: 10px; text-align: left; }");
//            html.AppendLine("td { padding: 8px; border: 1px solid #ddd; }");
//            html.AppendLine("tr:nth-child(even) { background: #f9f9f9; }");
//            html.AppendLine(".header { text-align: center; margin-bottom: 30px; }");
//            html.AppendLine(".footer { text-align: center; margin-top: 30px; font-size: 12px; color: #666; }");
//            html.AppendLine("</style></head><body>");

//            html.AppendLine("<div class='header'>");
//            html.AppendLine("<h1>Nelson Mandela Bay Haematology Diagnostic Laboratories</h1>");
//            html.AppendLine($"<h2>Report Type: {reportType}</h2>");
//            html.AppendLine($"<p>Date Range: {startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}</p>");
//            html.AppendLine($"<p>Generated: {DateTime.Now:dd/MM/yyyy HH:mm}</p>");
//            html.AppendLine("</div>");

//            switch (reportType)
//            {
//                case "TestsPerformed":
//                    var testReport = await GenerateTestsPerformedReportAsync(startDate, endDate);
//                    html.AppendLine("<h3>Summary</h3>");
//                    html.AppendLine($"<p>Total Tests: {testReport.Summary.TotalTests}</p>");
//                    html.AppendLine($"<p>Verified: {testReport.Summary.TotalVerified}</p>");
//                    html.AppendLine($"<p>Abnormal Results: {testReport.Summary.AbnormalResults}</p>");

//                    html.AppendLine("<h3>Tests by Category</h3>");
//                    html.AppendLine("<table><tr><th>Category</th><th>Total</th><th>Verified</th><th>Abnormal</th><th>Avg Time</th></tr>");
//                    foreach (var category in testReport.Categories)
//                    {
//                        html.AppendLine($"<tr><td>{category.CategoryName}</td><td>{category.TotalTests}</td><td>{category.VerifiedTests}</td><td>{category.AbnormalResults}</td><td>{category.AverageTurnaroundTime:F2} min</td></tr>");
//                    }
//                    html.AppendLine("</table>");
//                    break;

//                case "StockReport":
//                    var stockReport = await GenerateStockReportAsync();
//                    html.AppendLine("<h3>Stock Summary</h3>");
//                    html.AppendLine($"<p>Total Items: {stockReport.Summary.TotalItems}</p>");
//                    html.AppendLine($"<p>Low Stock Items: {stockReport.Summary.LowStockItems}</p>");
//                    html.AppendLine($"<p>Critical Stock Items: {stockReport.Summary.CriticalStockItems}</p>");
//                    html.AppendLine($"<p>Total Stock Value: R{stockReport.Summary.TotalStockValue:N2}</p>");

//                    html.AppendLine("<h3>Inventory Items</h3>");
//                    html.AppendLine("<table><tr><th>Item</th><th>Supplier</th><th>Quantity</th><th>Reorder Level</th><th>Status</th><th>Value</th></tr>");
//                    foreach (var item in stockReport.Items)
//                    {
//                        html.AppendLine($"<tr><td>{item.ConsumableName}</td><td>{item.SupplierName}</td><td>{item.QuantityOnHand}</td><td>{item.ReorderLevel}</td><td>{item.StockStatus}</td><td>R{item.TotalValue:N2}</td></tr>");
//                    }
//                    html.AppendLine("</table>");
//                    break;

//                default:
//                    html.AppendLine("<p>Report type not implemented.</p>");
//                    break;
//            }

//            html.AppendLine("<div class='footer'>");
//            html.AppendLine($"<p>Generated by NMB-HLabSys Report System | {DateTime.Now:dd/MM/yyyy HH:mm}</p>");
//            html.AppendLine("</div></body></html>");

//            return await Task.FromResult(html.ToString());
//        }

//        private byte[] ConvertHtmlToPdf(string html)
//        {
//            // Placeholder - actual implementation would use iTextSharp, QuestPDF, or similar
//            // For now, return empty byte array
//            return Encoding.UTF8.GetBytes(html);
//        }

//        private static string GetStockStatus(int quantity, int reorderLevel)
//        {
//            if (quantity <= 0) return "Critical";
//            if (quantity <= reorderLevel) return "Low";
//            if (quantity <= reorderLevel * 1.5m) return "Warning";
//            return "Normal";
//        }
//    }
//}