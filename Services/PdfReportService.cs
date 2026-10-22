using System;
using System.Collections.Generic;
using System.Linq;
using NMB_HLabSys.Models;
using NMB_HLabSys.Models.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NMB_HLabSys.Services
{
    public class PdfReportService : IPdfReportService
    {
        // ─── Brand colours ───
        private const string Navy = "#020D2F";
        private const string Blue = "#0841C9";
        private const string Grey = "#5A6E7A";
        private const string LightBg = "#F0F4F8";
        private const string Border = "#CBD5E1";

        public PdfReportService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        // ═══════════════════════════════════════════════════════════
        // 1. DOCTOR REPORT
        // ═══════════════════════════════════════════════════════════
        public byte[] GenerateDoctorReportPdf(DoctorReportData data)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial").FontColor(Navy));

                    page.Header().Element(ComposeHeader(
                        title: "Test Request Report",
                        subtitle: $"Doctor: {data.DoctorName}",
                        startDate: data.StartDate,
                        endDate: data.EndDate));

                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        col.Item().Element(ComposeSummaryBox(data));
                        col.Item().PaddingTop(20);

                        if (data.Requests == null || data.Requests.Count == 0)
                        {
                            col.Item()
                               .Border(1).BorderColor(Border)
                               .Padding(20)
                               .AlignCenter()
                               .Text("No records found for the selected date range.")
                               .FontSize(11).Italic().FontColor(Grey);
                            return;
                        }

                        var sortedRequests = data.Requests
                            .OrderByDescending(r => r.RequestDate)
                            .ToList();

                        col.Item().Text("1. Test Request Details")
                           .FontSize(13).Bold().FontColor(Navy);
                        col.Item().PaddingBottom(6)
                           .Text($"Total requests: {sortedRequests.Count}")
                           .FontSize(9).Italic().FontColor(Grey);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(70);
                                cols.ConstantColumn(60);
                                cols.RelativeColumn(2);
                                cols.ConstantColumn(60);
                                cols.ConstantColumn(70);
                                cols.RelativeColumn(3);
                            });

                            table.Header(h =>
                            {
                                h.Cell().Element(HeaderCell).Text("Request #").Bold();
                                h.Cell().Element(HeaderCell).Text("Date").Bold();
                                h.Cell().Element(HeaderCell).Text("Patient").Bold();
                                h.Cell().Element(HeaderCell).Text("Urgency").Bold();
                                h.Cell().Element(HeaderCell).Text("Status").Bold();
                                h.Cell().Element(HeaderCell).Text("Tests").Bold();
                            });

                            bool alt = false;
                            foreach (var req in sortedRequests)
                            {
                                var bg = alt ? LightBg : "#FFFFFF";
                                alt = !alt;

                                table.Cell().Element(c => BodyCell(c, bg)).Text(req.RequestNumber).FontSize(9);
                                table.Cell().Element(c => BodyCell(c, bg)).Text(req.RequestDate.ToString("yyyy-MM-dd")).FontSize(9);
                                table.Cell().Element(c => BodyCell(c, bg)).Text(req.PatientName).FontSize(9);
                                table.Cell().Element(c => BodyCell(c, bg)).Text(req.Urgency.ToUpper()).FontSize(9);
                                table.Cell().Element(c => BodyCell(c, bg)).Text(req.Status).FontSize(9);
                                table.Cell().Element(c => BodyCell(c, bg))
                                     .Text(string.Join(", ", req.Tests.Select(t => t.TestName))).FontSize(9);
                            }
                        });
                    });

                    page.Footer().Element(ComposeFooter());
                });
            }).GeneratePdf();
        }

        // ═══════════════════════════════════════════════════════════
        // 2. CATEGORY SUMMARY REPORT
        // ═══════════════════════════════════════════════════════════
        public byte[] GenerateCategorySummaryPdf(
            List<CategoryReportData> reportData,
            DateTime startDate,
            DateTime endDate)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial").FontColor(Navy));

                    page.Header().Element(ComposeHeader(
                        title: "Test Category Summary Report",
                        subtitle: "Tests performed by category",
                        startDate: startDate,
                        endDate: endDate));

                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        if (reportData == null || reportData.Count == 0)
                        {
                            col.Item()
                               .Border(1).BorderColor(Border)
                               .Padding(20)
                               .AlignCenter()
                               .Text("No records found for the selected date range.")
                               .FontSize(11).Italic().FontColor(Grey);
                            return;
                        }

                        int grandTotal = reportData.Sum(r => r.TotalTests);
                        col.Item().Text("1. Summary").FontSize(13).Bold().FontColor(Navy);
                        col.Item().PaddingBottom(6)
                           .Text($"Total categories: {reportData.Count}")
                           .FontSize(9).Italic().FontColor(Grey);
                        col.Item().PaddingBottom(10)
                           .Text($"Grand total of tests performed: {grandTotal}")
                           .FontSize(10).Bold();

                        col.Item().Text("2. Tests by Category").FontSize(13).Bold().FontColor(Navy);
                        col.Item().PaddingBottom(6).Text(" ").FontSize(1);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(4);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(1);
                            });

                            table.Header(h =>
                            {
                                h.Cell().Element(HeaderCell).Text("Test Category").Bold();
                                h.Cell().Element(HeaderCell).AlignRight().Text("Total Tests").Bold();
                                h.Cell().Element(HeaderCell).AlignRight().Text("% of Total").Bold();
                            });

                            bool alt = false;
                            foreach (var cat in reportData.OrderByDescending(c => c.TotalTests))
                            {
                                var bg = alt ? LightBg : "#FFFFFF"; alt = !alt;
                                var pct = grandTotal > 0 ? (cat.TotalTests * 100.0 / grandTotal) : 0;

                                table.Cell().Element(c => BodyCell(c, bg)).Text(cat.CategoryName);
                                table.Cell().Element(c => BodyCell(c, bg)).AlignRight().Text(cat.TotalTests.ToString("N0"));
                                table.Cell().Element(c => BodyCell(c, bg)).AlignRight().Text(pct.ToString("F1") + "%");
                            }

                            table.Cell().Element(TotalCell).Text("Grand Total").Bold();
                            table.Cell().Element(TotalCell).AlignRight().Text(grandTotal.ToString("N0")).Bold();
                            table.Cell().Element(TotalCell).AlignRight().Text("100.0%").Bold();
                        });
                    });

                    page.Footer().Element(ComposeFooter());
                });
            }).GeneratePdf();
        }

        // ═══════════════════════════════════════════════════════════
        // 3. PATIENT TEST RESULTS REPORT   ← NEW
        // ═══════════════════════════════════════════════════════════
        public byte[] GeneratePatientTestResultsPdf(Patient patient, DateTime fromDate, DateTime toDate)
        {
            if (patient == null) throw new ArgumentNullException(nameof(patient));

            if (fromDate > toDate) (fromDate, toDate) = (toDate, fromDate);

            var allResults = (patient.TestRequests ?? Enumerable.Empty<TestRequest>())
                .Where(r => r.RequestDate >= fromDate && r.RequestDate <= toDate.AddDays(1))
                .Where(r => r.Status == "Released" || r.Status == "Appointment Requested")
                .SelectMany(r => (r.RequestTestTypes ?? Enumerable.Empty<RequestTestType>())
                    .SelectMany(rt => (rt.TestResults ?? Enumerable.Empty<TestResult>())
                        .Select(t => new
                        {
                            r.RequestNumber,
                            r.RequestDate,
                            r.Status,
                            TestName = rt.TestType?.TestName ?? "Unknown",
                            Category = rt.TestType?.Category?.CategoryName ?? "Uncategorized",
                            Unit = rt.TestType?.UnitOfMeasurement ?? "",
                            RangeMin = rt.TestType?.NormalRangeMin,
                            RangeMax = rt.TestType?.NormalRangeMax,
                            t.ResultValue,
                            t.IsAbnormal,
                            t.ResultDate,
                            t.Notes
                        })))
                .ToList();

            var groupedResults = allResults
                .GroupBy(x => x.Category)
                .OrderBy(g => g.Key)
                .ToList();

            var fullName = $"{patient.FirstName} {patient.LastName}".Trim();
            var totalResults = allResults.Count;
            var abnormalCount = allResults.Count(x => x.IsAbnormal);
            var requestCount = allResults.Select(x => x.RequestNumber).Distinct().Count();

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial").FontColor(Navy));

                    page.Header().Element(ComposeHeader(
                        title: "My Test Results",
                        subtitle: $"Patient: {fullName}  •  SA ID: {patient.SAIDNumber}",
                        startDate: fromDate,
                        endDate: toDate));

                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        col.Item()
                           .Border(1).BorderColor(Border)
                           .Background(LightBg)
                           .Padding(12)
                           .Column(c =>
                           {
                               c.Item().Text("Summary").FontSize(12).Bold().FontColor(Navy);
                               c.Item().PaddingTop(6).Row(row =>
                               {
                                   SummaryStat(row, "Results", totalResults.ToString());
                                   SummaryStat(row, "Abnormal", abnormalCount.ToString());
                                   SummaryStat(row, "Requests", requestCount.ToString());
                                   SummaryStat(row, "Categories", groupedResults.Count.ToString());
                               });
                           });

                        col.Item().PaddingTop(20);

                        if (totalResults == 0)
                        {
                            col.Item()
                               .Border(1).BorderColor(Border)
                               .Padding(20)
                               .AlignCenter()
                               .Text("No results found for the selected date range.")
                               .FontSize(11).Italic().FontColor(Grey);
                            return;
                        }

                        col.Item().Text("Test Results by Category")
                           .FontSize(13).Bold().FontColor(Navy);
                        col.Item().PaddingBottom(10).Text(" ").FontSize(1);

                        int categoryIndex = 1;
                        foreach (var category in groupedResults)
                        {
                            var categoryResults = category
                                .OrderByDescending(x => x.RequestDate)
                                .ThenBy(x => x.TestName)
                                .ToList();

                            var categoryAbnormal = categoryResults.Count(x => x.IsAbnormal);

                            col.Item().PaddingTop(categoryIndex == 1 ? 0 : 15);

                            col.Item()
                               .Background(Navy)
                               .Padding(10)
                               .Row(row =>
                               {
                                   row.RelativeItem().Column(c =>
                                   {
                                       c.Item().Text($"{categoryIndex}. {category.Key}")
                                          .FontSize(12).Bold().FontColor(Colors.White);
                                       c.Item().Text($"{categoryResults.Count} result{(categoryResults.Count == 1 ? "" : "s")} {(categoryAbnormal > 0 ? $"• {categoryAbnormal} abnormal" : "")}")
                                          .FontSize(9).FontColor(Colors.White).Italic();
                                   });
                               });

                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.ConstantColumn(55);
                                    cols.ConstantColumn(55);
                                    cols.RelativeColumn(3);
                                    cols.RelativeColumn(2);
                                    cols.RelativeColumn(2);
                                    cols.ConstantColumn(50);
                                });

                                table.Header(h =>
                                {
                                    h.Cell().Element(HeaderCell).Text("Request").Bold();
                                    h.Cell().Element(HeaderCell).Text("Date").Bold();
                                    h.Cell().Element(HeaderCell).Text("Test").Bold();
                                    h.Cell().Element(HeaderCell).Text("Result").Bold();
                                    h.Cell().Element(HeaderCell).Text("Normal Range").Bold();
                                    h.Cell().Element(HeaderCell).Text("Status").Bold();
                                });

                                bool alt = false;
                                foreach (var r in categoryResults)
                                {
                                    var bg = r.IsAbnormal
                                        ? "#FEF2F2"
                                        : (alt ? LightBg : "#FFFFFF");
                                    alt = !alt;

                                    table.Cell().Element(c => BodyCell(c, bg)).Text(r.RequestNumber ?? "").FontSize(8);
                                    table.Cell().Element(c => BodyCell(c, bg)).Text(r.ResultDate.ToString("yyyy-MM-dd")).FontSize(8);
                                    table.Cell().Element(c => BodyCell(c, bg)).Text(r.TestName).FontSize(8);
                                    table.Cell().Element(c => BodyCell(c, bg))
                                         .Text($"{r.ResultValue} {r.Unit}".Trim())
                                         .FontSize(8)
                                         .FontColor(r.IsAbnormal ? "#DC2626" : Navy);
                                    table.Cell().Element(c => BodyCell(c, bg))
                                         .Text(r.RangeMin.HasValue && r.RangeMax.HasValue
                                             ? $"{r.RangeMin} - {r.RangeMax}"
                                             : "—").FontSize(8);
                                    table.Cell().Element(c => BodyCell(c, bg))
                                         .Text(r.IsAbnormal ? "Abnormal" : "Normal")
                                         .FontSize(8)
                                         .FontColor(r.IsAbnormal ? "#DC2626" : Navy);
                                }
                            });

                            categoryIndex++;
                        }

                        col.Item().PaddingTop(20)
                           .BorderTop(1).BorderColor(Border)
                           .PaddingTop(10)
                           .Text("Note: This report is for information only and does not replace medical advice. Please consult your doctor to discuss these results.")
                           .FontSize(8).Italic().FontColor(Grey);
                    });

                    page.Footer().Element(ComposeFooter());
                });
            }).GeneratePdf();
        }

        // ═══════════════════════════════════════════════════════════
        // 4. PATIENT MEDICAL HISTORY REPORT   ← NEW
        // ═══════════════════════════════════════════════════════════
        public byte[] GeneratePatientMedicalHistoryPdf(Patient patient)
        {
            if (patient == null) throw new ArgumentNullException(nameof(patient));

            var fullName = $"{patient.FirstName} {patient.LastName}".Trim();
            var generated = DateTime.Now;

            var conditions = (patient.PatientConditions ?? new List<PatientCondition>())
                .Where(c => c.IsActive)
                .Select(c => new
                {
                    c.Condition?.ConditionName,
                    Category = c.Condition?.Category?.CategoryName,
                    c.DiagnosedDate,
                    c.Notes
                }).ToList();

            var allergies = (patient.PatientAllergies ?? new List<PatientAllergy>())
                .Select(a => new
                {
                    a.Allergy?.AllergyName,
                    Category = a.Allergy?.Category?.CategoryName,
                    a.Severity,
                    a.Notes
                }).ToList();

            var medications = (patient.PatientMedications ?? new List<PatientMedication>())
                .Select(m => new
                {
                    m.Medication?.MedicationName,
                    Category = m.Medication?.Category?.CategoryName,
                    m.Dosage,
                    m.Frequency,
                    m.StartDate,
                    m.EndDate,
                    m.IsCurrent,
                    m.Notes
                }).ToList();

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial").FontColor(Navy));

                    page.Header().Element(ComposeHeader(
                        title: "Medical History",
                        subtitle: $"Patient: {fullName}  •  SA ID: {patient.SAIDNumber}",
                        startDate: patient.DateOfBirth,
                        endDate: generated));

                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        // Demographics
                        col.Item().Text("1. Patient Details").FontSize(13).Bold().FontColor(Navy);
                        col.Item().PaddingTop(4).Border(1).BorderColor(Border).Padding(10).Column(c =>
                        {
                            c.Item().Text($"Name:          {fullName}");
                            c.Item().Text($"SA ID:         {patient.SAIDNumber}");
                            c.Item().Text($"Date of Birth: {patient.DateOfBirth:yyyy-MM-dd}");
                            c.Item().Text($"Cellphone:     {patient.Cellphone}");
                            c.Item().Text($"Email:         {patient.Email}");
                            c.Item().Text($"Address:       {patient.HomeAddress ?? "—"}");
                        });

                        // Conditions
                        col.Item().PaddingTop(18).Text("2. Medical Conditions").FontSize(13).Bold().FontColor(Navy);
                        if (conditions.Count == 0)
                        {
                            col.Item().PaddingTop(4).Text("No conditions recorded.").Italic().FontColor(Grey);
                        }
                        else
                        {
                            col.Item().PaddingTop(4).Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(3);
                                    c.RelativeColumn(2);
                                    c.RelativeColumn(2);
                                    c.RelativeColumn(3);
                                });
                                t.Header(h =>
                                {
                                    h.Cell().Element(HeaderCell).Text("Condition").Bold();
                                    h.Cell().Element(HeaderCell).Text("Category").Bold();
                                    h.Cell().Element(HeaderCell).Text("Diagnosed").Bold();
                                    h.Cell().Element(HeaderCell).Text("Notes").Bold();
                                });
                                bool alt = false;
                                foreach (var x in conditions)
                                {
                                    var bg = alt ? LightBg : "#FFFFFF"; alt = !alt;
                                    t.Cell().Element(c => BodyCell(c, bg)).Text(x.ConditionName ?? "Unknown");
                                    t.Cell().Element(c => BodyCell(c, bg)).Text(x.Category ?? "—");
                                    t.Cell().Element(c => BodyCell(c, bg)).Text(x.DiagnosedDate?.ToString("yyyy-MM-dd") ?? "—");
                                    t.Cell().Element(c => BodyCell(c, bg)).Text(x.Notes ?? "");
                                }
                            });
                        }

                        // Allergies
                        col.Item().PaddingTop(18).Text("3. Allergies").FontSize(13).Bold().FontColor(Navy);
                        if (allergies.Count == 0)
                        {
                            col.Item().PaddingTop(4).Text("No allergies recorded.").Italic().FontColor(Grey);
                        }
                        else
                        {
                            col.Item().PaddingTop(4).Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(3);
                                    c.RelativeColumn(2);
                                    c.RelativeColumn(2);
                                    c.RelativeColumn(3);
                                });
                                t.Header(h =>
                                {
                                    h.Cell().Element(HeaderCell).Text("Allergy").Bold();
                                    h.Cell().Element(HeaderCell).Text("Category").Bold();
                                    h.Cell().Element(HeaderCell).Text("Severity").Bold();
                                    h.Cell().Element(HeaderCell).Text("Notes").Bold();
                                });
                                bool alt = false;
                                foreach (var x in allergies)
                                {
                                    var bg = alt ? LightBg : "#FFFFFF"; alt = !alt;
                                    t.Cell().Element(c => BodyCell(c, bg)).Text(x.AllergyName ?? "Unknown");
                                    t.Cell().Element(c => BodyCell(c, bg)).Text(x.Category ?? "—");
                                    t.Cell().Element(c => BodyCell(c, bg)).Text(x.Severity ?? "—");
                                    t.Cell().Element(c => BodyCell(c, bg)).Text(x.Notes ?? "");
                                }
                            });
                        }

                        // Medications
                        col.Item().PaddingTop(18).Text("4. Medications").FontSize(13).Bold().FontColor(Navy);
                        if (medications.Count == 0)
                        {
                            col.Item().PaddingTop(4).Text("No medications recorded.").Italic().FontColor(Grey);
                        }
                        else
                        {
                            col.Item().PaddingTop(4).Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(3);
                                    c.RelativeColumn(2);
                                    c.RelativeColumn(2);
                                    c.RelativeColumn(2);
                                    c.RelativeColumn(2);
                                    c.RelativeColumn(1);
                                });
                                t.Header(h =>
                                {
                                    h.Cell().Element(HeaderCell).Text("Medication").Bold();
                                    h.Cell().Element(HeaderCell).Text("Category").Bold();
                                    h.Cell().Element(HeaderCell).Text("Dosage").Bold();
                                    h.Cell().Element(HeaderCell).Text("Frequency").Bold();
                                    h.Cell().Element(HeaderCell).Text("Period").Bold();
                                    h.Cell().Element(HeaderCell).Text("Current").Bold();
                                });
                                bool alt = false;
                                foreach (var x in medications)
                                {
                                    var bg = alt ? LightBg : "#FFFFFF"; alt = !alt;
                                    t.Cell().Element(c => BodyCell(c, bg)).Text(x.MedicationName ?? "Unknown");
                                    t.Cell().Element(c => BodyCell(c, bg)).Text(x.Category ?? "—");
                                    t.Cell().Element(c => BodyCell(c, bg)).Text(x.Dosage ?? "—");
                                    t.Cell().Element(c => BodyCell(c, bg)).Text(x.Frequency ?? "—");
                                    t.Cell().Element(c => BodyCell(c, bg))
                                        .Text($"{x.StartDate:yyyy-MM-dd} → {(x.EndDate.HasValue ? x.EndDate.Value.ToString("yyyy-MM-dd") : "present")}");
                                    t.Cell().Element(c => BodyCell(c, bg)).Text(x.IsCurrent ? "Yes" : "No");
                                }
                            });
                        }

                        col.Item().PaddingTop(16)
                           .Text("This is a personal health summary and does not replace professional medical advice.")
                           .FontSize(8).Italic().FontColor(Grey);
                    });

                    page.Footer().Element(ComposeFooter());
                });
            }).GeneratePdf();
        }

        // ═══════════════════════════════════════════════════════════
        // 5. STOCK REPORT
        // ═══════════════════════════════════════════════════════════
        public byte[] GenerateStockReportPdf(List<ConsumableViewModel> stockData, DateTime startDate, DateTime endDate)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial").FontColor(Navy));

                    page.Header().Element(ComposeHeader(
                        title: "Stock Inventory Report",
                        subtitle: "Current stock levels and valuation",
                        startDate: startDate,
                        endDate: endDate));

                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        if (stockData == null || stockData.Count == 0)
                        {
                            col.Item().Border(1).BorderColor(Border).Padding(20).AlignCenter().Text("No stock records found.").FontSize(11).Italic().FontColor(Grey);
                            return;
                        }

                        int totalItems = stockData.Count;
                        decimal totalValue = stockData.Sum(s => (s.QuantityOnHand * (s.UnitPrice ?? 0)));
                        int criticalCount = stockData.Count(s => s.StockStatus == "Critical" || s.StockStatus == "Low");

                        col.Item().Text("1. Summary").FontSize(13).Bold().FontColor(Navy);
                        col.Item().PaddingBottom(10).Row(row =>
                        {
                            SummaryStat(row, "Total Items", totalItems.ToString());
                            SummaryStat(row, "Total Value", "R " + totalValue.ToString("N2"));
                            SummaryStat(row, "Low/Critical Items", criticalCount.ToString());
                        });

                        col.Item().PaddingTop(10).Text("2. Stock Details").FontSize(13).Bold().FontColor(Navy);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(3); // Name
                                cols.RelativeColumn(3); // Supplier
                                cols.RelativeColumn(1); // QOH
                                cols.RelativeColumn(1); // Reorder
                                cols.RelativeColumn(1); // Price
                                cols.RelativeColumn(1); // Total Value
                                cols.RelativeColumn(1); // Status
                            });

                            table.Header(h =>
                            {
                                h.Cell().Element(HeaderCell).Text("Consumable").Bold();
                                h.Cell().Element(HeaderCell).Text("Supplier").Bold();
                                h.Cell().Element(HeaderCell).AlignRight().Text("On Hand").Bold();
                                h.Cell().Element(HeaderCell).AlignRight().Text("Reorder").Bold();
                                h.Cell().Element(HeaderCell).AlignRight().Text("Unit Price").Bold();
                                h.Cell().Element(HeaderCell).AlignRight().Text("Total Value").Bold();
                                h.Cell().Element(HeaderCell).Text("Status").Bold();
                            });

                            bool alt = false;
                            foreach (var item in stockData.OrderBy(s => s.ConsumableName))
                            {
                                var bg = alt ? LightBg : "#FFFFFF"; alt = !alt;
                                var lineTotal = item.QuantityOnHand * (item.UnitPrice ?? 0);

                                table.Cell().Element(c => BodyCell(c, bg)).Text(item.ConsumableName).FontSize(9);
                                table.Cell().Element(c => BodyCell(c, bg)).Text(item.SupplierName).FontSize(9);
                                table.Cell().Element(c => BodyCell(c, bg)).AlignRight().Text(item.QuantityOnHand.ToString("N0")).FontSize(9);
                                table.Cell().Element(c => BodyCell(c, bg)).AlignRight().Text(item.ReorderLevel.ToString("N0")).FontSize(9);
                                table.Cell().Element(c => BodyCell(c, bg)).AlignRight().Text(item.UnitPrice?.ToString("N2") ?? "-").FontSize(9);
                                table.Cell().Element(c => BodyCell(c, bg)).AlignRight().Text(lineTotal.ToString("N2")).FontSize(9);
                                table.Cell().Element(c => BodyCell(c, bg)).Text(item.StockStatus).FontSize(9).Bold().FontColor(item.StockStatus == "Critical" ? Colors.Red.Medium : item.StockStatus == "Low" ? Colors.Orange.Medium : Colors.Green.Medium);
                            }
                        });
                    });
                    page.Footer().Element(ComposeFooter());
                });
            }).GeneratePdf();
        }

        // ═══════════════════════════════════════════════════════════
        // 6. STAFF PERFORMANCE REPORT (TECHNICIAN)
        // ═══════════════════════════════════════════════════════════
        public byte[] GenerateStaffPerformancePdf(List<StaffPerformanceData> staffData, DateTime startDate, DateTime endDate)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial").FontColor(Navy));

                    page.Header().Element(ComposeHeader(
                        title: "Staff Performance Report",
                        subtitle: "Tests completed by technicians, grouped by category",
                        startDate: startDate,
                        endDate: endDate));

                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        if (staffData == null || staffData.Count == 0)
                        {
                            col.Item().Border(1).BorderColor(Border).Padding(20).AlignCenter().Text("No completed tests found for this date range.").FontSize(11).Italic().FontColor(Grey);
                            return;
                        }

                        int grandTotal = staffData.Sum(s => s.TotalTests);
                        col.Item().Text("1. Summary").FontSize(13).Bold().FontColor(Navy);
                        col.Item().PaddingBottom(10).Text($"Grand total of tests completed: {grandTotal}").FontSize(10).Bold();

                        col.Item().Text("2. Breakdown by Technician and Category").FontSize(13).Bold().FontColor(Navy);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(3);
                                cols.RelativeColumn(3);
                                cols.RelativeColumn(1);
                            });

                            table.Header(h =>
                            {
                                h.Cell().Element(HeaderCell).Text("Technician").Bold();
                                h.Cell().Element(HeaderCell).Text("Test Category").Bold();
                                h.Cell().Element(HeaderCell).AlignRight().Text("Tests Completed").Bold();
                            });

                            bool alt = false;
                            foreach (var item in staffData.OrderBy(s => s.TechnicianName).ThenBy(s => s.CategoryName))
                            {
                                var bg = alt ? LightBg : "#FFFFFF"; alt = !alt;

                                table.Cell().Element(c => BodyCell(c, bg)).Text(item.TechnicianName).FontSize(9);
                                table.Cell().Element(c => BodyCell(c, bg)).Text(item.CategoryName).FontSize(9);
                                table.Cell().Element(c => BodyCell(c, bg)).AlignRight().Text(item.TotalTests.ToString("N0")).FontSize(9);
                            }

                            table.Cell().Element(TotalCell).Text("Grand Total").Bold();
                            table.Cell().Element(TotalCell).Text("").Bold();
                            table.Cell().Element(TotalCell).AlignRight().Text(grandTotal.ToString("N0")).Bold();
                        });
                    });
                    page.Footer().Element(ComposeFooter());
                });
            }).GeneratePdf();
        }

        // ═══════════════════════════════════════════════════════════
        // 7. PURCHASE ORDERS REPORT
        // ═══════════════════════════════════════════════════════════
        public byte[] GenerateOrdersReportPdf(List<OrderListViewModel> ordersData, DateTime startDate, DateTime endDate)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial").FontColor(Navy));

                    page.Header().Element(ComposeHeader(
                        title: "Purchase Orders Report",
                        subtitle: "Summary of orders placed in the selected date range",
                        startDate: startDate,
                        endDate: endDate));

                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        if (ordersData == null || ordersData.Count == 0)
                        {
                            col.Item().Border(1).BorderColor(Border).Padding(20).AlignCenter().Text("No purchase orders found for this date range.").FontSize(11).Italic().FontColor(Grey);
                            return;
                        }

                        int totalOrders = ordersData.Count;
                        decimal totalSpend = ordersData.Sum(o => o.TotalAmount ?? 0);

                        col.Item().Text("1. Summary").FontSize(13).Bold().FontColor(Navy);
                        col.Item().PaddingBottom(10).Row(row =>
                        {
                            SummaryStat(row, "Total Orders", totalOrders.ToString());
                            SummaryStat(row, "Total Spend", "R " + totalSpend.ToString("N2"));
                        });

                        col.Item().Text("2. Order Details").FontSize(13).Bold().FontColor(Navy);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(2); // Order #
                                cols.RelativeColumn(3); // Supplier
                                cols.RelativeColumn(2); // Date
                                cols.RelativeColumn(2); // Status
                                cols.RelativeColumn(2); // Amount
                            });

                            table.Header(h =>
                            {
                                h.Cell().Element(HeaderCell).Text("Order #").Bold();
                                h.Cell().Element(HeaderCell).Text("Supplier").Bold();
                                h.Cell().Element(HeaderCell).Text("Date").Bold();
                                h.Cell().Element(HeaderCell).Text("Status").Bold();
                                h.Cell().Element(HeaderCell).AlignRight().Text("Total Amount").Bold();
                            });

                            bool alt = false;
                            foreach (var item in ordersData.OrderByDescending(o => o.OrderDate))
                            {
                                var bg = alt ? LightBg : "#FFFFFF"; alt = !alt;

                                table.Cell().Element(c => BodyCell(c, bg)).Text(item.OrderNumber).FontSize(9);
                                table.Cell().Element(c => BodyCell(c, bg)).Text(item.SupplierName).FontSize(9);
                                table.Cell().Element(c => BodyCell(c, bg)).Text(item.OrderDate.ToString("yyyy-MM-dd")).FontSize(9);
                                table.Cell().Element(c => BodyCell(c, bg)).Text(item.Status).FontSize(9);
                                table.Cell().Element(c => BodyCell(c, bg)).AlignRight().Text((item.TotalAmount ?? 0).ToString("N2")).FontSize(9);
                            }

                            table.Cell().Element(TotalCell).Text("Grand Total").Bold();
                            table.Cell().Element(TotalCell).Text("").Bold();
                            table.Cell().Element(TotalCell).Text("").Bold();
                            table.Cell().Element(TotalCell).Text("").Bold();
                            table.Cell().Element(TotalCell).AlignRight().Text(totalSpend.ToString("N2")).Bold();
                        });
                    });
                    page.Footer().Element(ComposeFooter());
                });
            }).GeneratePdf();
        }
        // ═══════════════════════════════════════════════════════════
        // REUSABLE COMPONENTS
        // ═══════════════════════════════════════════════════════════

        private static Action<IContainer> ComposeHeader(
            string title, string subtitle, DateTime startDate, DateTime endDate)
        {
            return container =>
            {
                container.Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Nelson Mandela Bay Haematology")
                                   .FontSize(14).Bold().FontColor(Navy);
                            c.Item().Text("Diagnostic Laboratories")
                                   .FontSize(10).FontColor(Blue);
                        });
                        row.ConstantItem(180).AlignRight().Column(c =>
                        {
                            c.Item().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}")
                                   .FontSize(8).FontColor(Grey);
                            c.Item().Text($"Report period: {startDate:yyyy-MM-dd} — {endDate:yyyy-MM-dd}")
                                   .FontSize(8).FontColor(Grey);
                        });
                    });

                    col.Item().PaddingTop(6);
                    col.Item().LineHorizontal(1.5f).LineColor(Navy);
                    col.Item().PaddingTop(10);

                    col.Item().Text(title).FontSize(18).Bold().FontColor(Navy);
                    if (!string.IsNullOrWhiteSpace(subtitle))
                        col.Item().Text(subtitle).FontSize(10).Italic().FontColor(Grey);

                    col.Item().PaddingBottom(6);
                });
            };
        }

        private static Action<IContainer> ComposeFooter()
        {
            return container =>
            {
                container.Column(col =>
                {
                    col.Item().LineHorizontal(0.5f).LineColor(Border);
                    col.Item().PaddingTop(4).Row(row =>
                    {
                        row.RelativeItem()
                           .Text("NMB-HLabSys — Laboratory Test Request Management System")
                           .FontSize(8).FontColor(Grey);

                        row.ConstantItem(120).AlignRight().Text(text =>
                        {
                            text.Span("Page ").FontSize(8).FontColor(Grey);
                            text.CurrentPageNumber().FontSize(8).Bold().FontColor(Navy);
                            text.Span(" of ").FontSize(8).FontColor(Grey);
                            text.TotalPages().FontSize(8).Bold().FontColor(Navy);
                        });
                    });
                });
            };
        }

        private static Action<IContainer> ComposeSummaryBox(DoctorReportData data)
        {
            return container =>
            {
                container
                    .Border(1).BorderColor(Border)
                    .Background(LightBg)
                    .Padding(12)
                    .Column(col =>
                    {
                        col.Item().Text("Summary").FontSize(12).Bold().FontColor(Navy);
                        col.Item().PaddingTop(6).Row(row =>
                        {
                            SummaryStat(row, "Total Requests", data.TotalRequests.ToString());
                            SummaryStat(row, "Total Tests", data.TotalTests.ToString());
                            SummaryStat(row, "Abnormal Tests", data.AbnormalTests.ToString());
                            SummaryStat(row, "Pending Tests", data.PendingTests.ToString());
                        });
                    });
            };
        }

        private static void SummaryStat(RowDescriptor row, string label, string value)
        {
            row.RelativeItem().Column(c =>
            {
                c.Item().Text(value).FontSize(16).Bold().FontColor(Blue);
                c.Item().Text(label).FontSize(8).FontColor(Grey);
            });
        }

        private static IContainer HeaderCell(IContainer container)
        {
            return container
                .Background(Navy)
                .Padding(5)
                .DefaultTextStyle(x => x.FontColor(Colors.White).FontSize(9));
        }

        private static IContainer BodyCell(IContainer container, string bg)
        {
            return container
                .Background(bg)
                .BorderBottom(0.5f).BorderColor(Border)
                .Padding(5);
        }

        private static IContainer TotalCell(IContainer container)
        {
            return container
                .Background("#E2E8F0")
                .BorderTop(1).BorderColor(Navy)
                .Padding(5);
        }
    }
}