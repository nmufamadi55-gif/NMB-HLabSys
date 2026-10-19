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

            var results = (patient.TestRequests ?? Enumerable.Empty<TestRequest>())
                .Where(r => r.RequestDate >= fromDate && r.RequestDate <= toDate.AddDays(1))
                .SelectMany(r => (r.RequestTestTypes ?? Enumerable.Empty<RequestTestType>())
                    .SelectMany(rt => (rt.TestResults ?? Enumerable.Empty<TestResult>())
                        .Select(t => new
                        {
                            r.RequestNumber,
                            r.RequestDate,
                            r.Status,
                            TestName = rt.TestType?.TestName ?? "Unknown",
                            Category = rt.TestType?.Category?.CategoryName ?? "—",
                            Unit = rt.TestType?.UnitOfMeasurement ?? "",
                            RangeMin = rt.TestType?.NormalRangeMin,
                            RangeMax = rt.TestType?.NormalRangeMax,
                            t.ResultValue,
                            t.IsAbnormal,
                            t.ResultDate,
                            t.Notes
                        })))
                .OrderByDescending(x => x.RequestDate)
                .ThenBy(x => x.TestName)
                .ToList();

            var fullName = $"{patient.FirstName} {patient.LastName}".Trim();

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
                                   SummaryStat(row, "Results", results.Count.ToString());
                                   SummaryStat(row, "Abnormal", results.Count(x => x.IsAbnormal).ToString());
                                   SummaryStat(row, "Requests", results.Select(x => x.RequestNumber).Distinct().Count().ToString());
                               });
                           });

                        col.Item().PaddingTop(20);

                        if (results.Count == 0)
                        {
                            col.Item()
                               .Border(1).BorderColor(Border)
                               .Padding(20)
                               .AlignCenter()
                               .Text("No results found for the selected date range.")
                               .FontSize(11).Italic().FontColor(Grey);
                            return;
                        }

                        col.Item().Text("1. Test Results").FontSize(13).Bold().FontColor(Navy);
                        col.Item().PaddingBottom(6).Text(" ").FontSize(1);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(60);
                                cols.ConstantColumn(55);
                                cols.RelativeColumn(3);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(2);
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
                            foreach (var r in results)
                            {
                                var bg = alt ? LightBg : "#FFFFFF"; alt = !alt;

                                table.Cell().Element(c => BodyCell(c, bg)).Text(r.RequestNumber ?? "").FontSize(9);
                                table.Cell().Element(c => BodyCell(c, bg)).Text(r.RequestDate.ToString("yyyy-MM-dd")).FontSize(9);
                                table.Cell().Element(c => BodyCell(c, bg)).Text(r.TestName).FontSize(9);
                                table.Cell().Element(c => BodyCell(c, bg)).Text($"{r.ResultValue} {r.Unit}".Trim()).FontSize(9);
                                table.Cell().Element(c => BodyCell(c, bg))
                                     .Text(r.RangeMin.HasValue && r.RangeMax.HasValue
                                         ? $"{r.RangeMin} - {r.RangeMax}"
                                         : "—").FontSize(9);
                                table.Cell().Element(c => BodyCell(c, bg))
                                     .Text(r.IsAbnormal ? "Abnormal" : "Normal").FontSize(9);
                            }
                        });

                        col.Item().PaddingTop(10)
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