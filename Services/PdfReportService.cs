using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.EntityFrameworkCore;
using NMB_HLabSys.Data;
using NMB_HLabSys.Models.ViewModels;
using NMB_HLabSys.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NMB_HLabSys.Services
{
    public class PdfReportService : IPdfReportService
    {
        private readonly ApplicationDbContext _context;

        public PdfReportService(ApplicationDbContext context)
        {
            _context = context;
            // Set QuestPDF License to Community (Free for open source / educational projects)
            QuestPDF.Settings.License = LicenseType.Community;
        }

        // ═══════════════════════════════════════════════════════════
        // MANAGER REPORT — Category Summary
        // ═══════════════════════════════════════════════════════════
        public byte[] GenerateCategorySummaryPdf(List<CategoryReportData> reportData, DateTime startDate, DateTime endDate)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    // HEADER
                    page.Header().Element(headerContainer =>
                    {
                        headerContainer.Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("Nelson Mandela Bay Haematology Diagnostic Laboratories").FontSize(16).Bold().FontColor(Colors.Blue.Darken3);
                                col.Item().Text("NMB-HLabSys — Management Summary Report").FontSize(12).SemiBold().FontColor(Colors.Grey.Darken2);
                                col.Item().Text($"Date Range: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}").FontSize(10).Italic();
                            });

                            row.ConstantItem(100).AlignRight().Text($"Generated:\n{DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(8).FontColor(Colors.Grey.Medium);
                        });
                    });

                    // CONTENT
                    page.Content().PaddingVertical(15).Column(col =>
                    {
                        col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                        col.Item().PaddingBottom(10);

                        int grandTotal = 0;

                        if (reportData == null || reportData.Count == 0)
                        {
                            col.Item().PaddingTop(20).Text("No completed tests found within the specified date range.").FontSize(11).Italic();
                        }
                        else
                        {
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(1);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Blue.Darken3).Padding(5).Text("Test Category").Bold().FontColor(Colors.White);
                                    header.Cell().Background(Colors.Blue.Darken3).Padding(5).AlignRight().Text("Total Tests Performed").Bold().FontColor(Colors.White);
                                });

                                foreach (var category in reportData)
                                {
                                    grandTotal += category.TotalTests;

                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(category.CategoryName).Bold();
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(category.TotalTests.ToString());
                                }

                                table.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Grand Total").Bold();
                                table.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text(grandTotal.ToString()).Bold();
                            });
                        }
                    });

                    // FOOTER
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }

        // ═══════════════════════════════════════════════════════════
        // DOCTOR REPORT — All test requests submitted by the doctor
        // ═══════════════════════════════════════════════════════════
        public byte[] GenerateDoctorReportPdf(DoctorReportData data)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    // HEADER
                    page.Header().Element(header =>
                    {
                        header.Column(col =>
                        {
                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("Nelson Mandela Bay Haematology")
                                        .FontSize(16).Bold().FontColor(Colors.Blue.Darken3);
                                    c.Item().Text("Diagnostic Laboratories")
                                        .FontSize(11).SemiBold().FontColor(Colors.Blue.Medium);
                                    c.Item().Text("Doctor Report — Test Requests")
                                        .FontSize(10).FontColor(Colors.Grey.Darken2).Italic();
                                });
                                row.ConstantItem(140).AlignRight().Column(c =>
                                {
                                    c.Item().Text("Generated").FontSize(8).FontColor(Colors.Grey.Medium);
                                    c.Item().Text($"{DateTime.Now:yyyy-MM-dd HH:mm}")
                                        .FontSize(8).FontColor(Colors.Grey.Darken1);

                                    c.Item().PaddingTop(4).Text("Period")
                                        .FontSize(8).FontColor(Colors.Grey.Medium);
                                    c.Item().Text($"{data.StartDate:yyyy-MM-dd} → {data.EndDate:yyyy-MM-dd}")
                                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                                });
                            });
                            col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                        });
                    });

                    // CONTENT
                    page.Content().PaddingVertical(12).Column(col =>
                    {
                        col.Item().Background(Colors.Blue.Lighten5).Padding(8).Column(c =>
                        {
                            c.Item().Text($"Doctor: {data.DoctorName}").FontSize(10).Bold();
                            c.Item().Text($"Email: {data.DoctorEmail}").FontSize(9).FontColor(Colors.Grey.Darken1);
                        });

                        col.Item().PaddingTop(10);

                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Element(StatCard("Total Requests", data.TotalRequests.ToString(), Colors.Blue.Darken2));
                            row.ConstantItem(6);
                            row.RelativeItem().Element(StatCard("Total Tests", data.TotalTests.ToString(), Colors.Blue.Medium));
                            row.ConstantItem(6);
                            row.RelativeItem().Element(StatCard("Abnormal", data.AbnormalTests.ToString(), Colors.Red.Medium));
                            row.ConstantItem(6);
                            row.RelativeItem().Element(StatCard("Pending", data.PendingTests.ToString(), Colors.Orange.Medium));
                        });

                        col.Item().PaddingTop(15);

                        if (data.Requests == null || data.Requests.Count == 0)
                        {
                            col.Item().PaddingTop(20).Text("No test requests found in this date range.")
                                .FontSize(11).Italic().FontColor(Colors.Grey.Medium);
                            return;
                        }

                        foreach (var req in data.Requests)
                        {
                            col.Item().PaddingTop(8).Element(e =>
                            {
                                e.Column(reqCol =>
                                {
                                    reqCol.Item().Background(Colors.Grey.Lighten4).Padding(6).Row(row =>
                                    {
                                        row.RelativeItem().Column(c =>
                                        {
                                            c.Item().Text(req.RequestNumber)
                                                .FontSize(11).Bold().FontColor(Colors.Blue.Darken3);
                                            c.Item().Text($"Patient: {req.PatientName}  |  SA ID: {req.SAIDNumber}")
                                                .FontSize(9).FontColor(Colors.Grey.Darken2);
                                        });
                                        row.ConstantItem(180).AlignRight().Column(c =>
                                        {
                                            c.Item().Text($"Date: {req.RequestDate:yyyy-MM-dd HH:mm}")
                                                .FontSize(9);
                                            c.Item().Text($"Urgency: {req.Urgency.ToUpper()}  |  Status: {req.Status}")
                                                .FontSize(9);
                                        });
                                    });

                                    if (!string.IsNullOrEmpty(req.ClinicalNotes))
                                    {
                                        reqCol.Item().Background(Colors.Yellow.Lighten5).Padding(5).Text(
                                            $"Notes: {req.ClinicalNotes}")
                                            .FontSize(9).Italic();
                                    }

                                    if (req.Tests.Count > 0)
                                    {
                                        reqCol.Item().PaddingTop(4).Table(table =>
                                        {
                                            table.ColumnsDefinition(cols =>
                                            {
                                                cols.RelativeColumn(3);
                                                cols.RelativeColumn(2);
                                                cols.RelativeColumn(2);
                                                cols.RelativeColumn(2);
                                                cols.RelativeColumn(2);
                                            });

                                            table.Header(h =>
                                            {
                                                h.Cell().Background(Colors.Blue.Darken3).Padding(4)
                                                    .Text("Test").Bold().FontColor(Colors.White).FontSize(9);
                                                h.Cell().Background(Colors.Blue.Darken3).Padding(4)
                                                    .Text("Category").Bold().FontColor(Colors.White).FontSize(9);
                                                h.Cell().Background(Colors.Blue.Darken3).Padding(4)
                                                    .Text("Status").Bold().FontColor(Colors.White).FontSize(9);
                                                h.Cell().Background(Colors.Blue.Darken3).Padding(4)
                                                    .Text("Result").Bold().FontColor(Colors.White).FontSize(9);
                                                h.Cell().Background(Colors.Blue.Darken3).Padding(4)
                                                    .Text("Normal Range").Bold().FontColor(Colors.White).FontSize(9);
                                            });

                                            foreach (var t in req.Tests)
                                            {
                                                var bg = t.IsAbnormal ? Colors.Red.Lighten5 : Colors.White;
                                                var resultColor = t.IsAbnormal ? Colors.Red.Darken2 : Colors.Grey.Darken3;

                                                table.Cell().Background(bg).Padding(4).Text(t.TestName).FontSize(9);
                                                table.Cell().Background(bg).Padding(4).Text(t.Category).FontSize(9);
                                                table.Cell().Background(bg).Padding(4).Text(t.Status).FontSize(9);
                                                table.Cell().Background(bg).Padding(4).Text(t.ResultValue ?? "Pending")
                                                    .FontSize(9).FontColor(resultColor).Bold();
                                                table.Cell().Background(bg).Padding(4).Text(t.NormalRange ?? "N/A").FontSize(9);
                                            }
                                        });
                                    }
                                    else
                                    {
                                        reqCol.Item().PaddingTop(4).Text("No tests on this request.")
                                            .FontSize(9).Italic().FontColor(Colors.Grey.Medium);
                                    }

                                    reqCol.Item().PaddingTop(10);
                                });
                            });
                        }
                    });

                    // FOOTER
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ").FontSize(8).FontColor(Colors.Grey.Medium);
                        x.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                        x.Span(" of ").FontSize(8).FontColor(Colors.Grey.Medium);
                        x.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                });
            });

            return document.GeneratePdf();
        }

        // ═══════════════════════════════════════════════════════════
        // PATIENT REPORT — Test Results (grouped by category)
        // ═══════════════════════════════════════════════════════════
        public byte[] GeneratePatientTestResultsPdf(Patient patient, DateTime fromDate, DateTime toDate)
        {
            var results = _context.TestResults
                .Include(tr => tr.RequestTestType)
                    .ThenInclude(rtt => rtt.TestType)
                        .ThenInclude(tt => tt.Category)
                .Include(tr => tr.RequestTestType)
                    .ThenInclude(rtt => rtt.TestRequest)
                .Where(tr => tr.RequestTestType.TestRequest.PatientID == patient.PatientID
                          && tr.ResultDate >= fromDate
                          && tr.ResultDate <= toDate)
                .OrderBy(tr => tr.RequestTestType.TestType.Category.CategoryName)
                .ThenBy(tr => tr.RequestTestType.TestType.TestName)
                .ToList();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    // HEADER
                    page.Header().Element(header =>
                    {
                        header.Column(col =>
                        {
                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("Nelson Mandela Bay Haematology")
                                        .FontSize(16).Bold().FontColor(Colors.Blue.Darken3);
                                    c.Item().Text("Diagnostic Laboratories")
                                        .FontSize(11).SemiBold().FontColor(Colors.Blue.Medium);
                                    c.Item().Text("Patient Test Results Report")
                                        .FontSize(10).FontColor(Colors.Grey.Darken2).Italic();
                                });
                                row.ConstantItem(140).AlignRight().Column(c =>
                                {
                                    c.Item().Text("Generated").FontSize(8).FontColor(Colors.Grey.Medium);
                                    c.Item().Text($"{DateTime.Now:yyyy-MM-dd HH:mm}")
                                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                                    c.Item().PaddingTop(4).Text("Report Period")
                                        .FontSize(8).FontColor(Colors.Grey.Medium);
                                    c.Item().Text($"{fromDate:yyyy-MM-dd} → {toDate:yyyy-MM-dd}")
                                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                                });
                            });
                            col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                        });
                    });

                    // CONTENT
                    page.Content().PaddingVertical(12).Column(col =>
                    {
                        // Patient info box
                        col.Item().Background(Colors.Blue.Lighten5).Padding(8).Column(c =>
                        {
                            c.Item().Text($"Patient: {patient.FirstName} {patient.LastName}").FontSize(11).Bold();
                            c.Item().Text($"SA ID: {patient.SAIDNumber}  |  DOB: {patient.DateOfBirth:dd MMM yyyy}").FontSize(9);
                            c.Item().Text($"Email: {patient.Email}  |  Cell: {patient.Cellphone}").FontSize(9);
                        });

                        col.Item().PaddingTop(12);

                        if (results.Count == 0)
                        {
                            col.Item().PaddingTop(20).Text("No test results available for this patient in the selected date range.")
                                .FontSize(11).Italic().FontColor(Colors.Grey.Medium);
                            return;
                        }

                        // Group by category
                        var groupedByCategory = results
                            .GroupBy(r => r.RequestTestType.TestType.Category?.CategoryName ?? "Uncategorized")
                            .OrderBy(g => g.Key)
                            .ToList();

                        foreach (var category in groupedByCategory)
                        {
                            col.Item().PaddingTop(8).Text(category.Key)
                                .FontSize(12).Bold().FontColor(Colors.Blue.Darken3);

                            col.Item().PaddingTop(4).Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.RelativeColumn(3);
                                    cols.RelativeColumn(4);
                                    cols.RelativeColumn(3);
                                    cols.RelativeColumn(3);
                                    cols.RelativeColumn(2);
                                });

                                table.Header(h =>
                                {
                                    h.Cell().Background(Colors.Blue.Darken3).Padding(4).Text("Date").Bold().FontColor(Colors.White).FontSize(9);
                                    h.Cell().Background(Colors.Blue.Darken3).Padding(4).Text("Test Name").Bold().FontColor(Colors.White).FontSize(9);
                                    h.Cell().Background(Colors.Blue.Darken3).Padding(4).Text("Result").Bold().FontColor(Colors.White).FontSize(9);
                                    h.Cell().Background(Colors.Blue.Darken3).Padding(4).Text("Normal Range").Bold().FontColor(Colors.White).FontSize(9);
                                    h.Cell().Background(Colors.Blue.Darken3).Padding(4).Text("Status").Bold().FontColor(Colors.White).FontSize(9);
                                });

                                foreach (var r in category)
                                {
                                    var bg = r.IsAbnormal ? Colors.Red.Lighten5 : Colors.White;
                                    var statusColor = r.IsAbnormal ? Colors.Red.Darken2 : Colors.Green.Darken2;
                                    var range = (r.RequestTestType.TestType.NormalRangeMin.HasValue && r.RequestTestType.TestType.NormalRangeMax.HasValue)
                                        ? $"{r.RequestTestType.TestType.NormalRangeMin} - {r.RequestTestType.TestType.NormalRangeMax}"
                                        : "N/A";

                                    table.Cell().Background(bg).Padding(4).Text(r.ResultDate.ToString("dd MMM yyyy")).FontSize(8);
                                    table.Cell().Background(bg).Padding(4).Text(r.RequestTestType.TestType.TestName).FontSize(8);
                                    table.Cell().Background(bg).Padding(4).Text(r.ResultValue ?? "-").FontSize(8).Bold();
                                    table.Cell().Background(bg).Padding(4).Text(range).FontSize(8);
                                    table.Cell().Background(bg).Padding(4).Text(r.IsAbnormal ? "Abnormal" : "Normal")
                                        .FontSize(8).FontColor(statusColor).Bold();
                                }
                            });
                        }

                        // Footnote
                        col.Item().PaddingTop(15).Text("This is a computer-generated report. For clinical interpretation, please consult your doctor.")
                            .FontSize(8).Italic().FontColor(Colors.Grey.Medium);
                    });

                    // FOOTER
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ").FontSize(8).FontColor(Colors.Grey.Medium);
                        x.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                        x.Span(" of ").FontSize(8).FontColor(Colors.Grey.Medium);
                        x.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                });
            });

            return document.GeneratePdf();
        }

        // ═══════════════════════════════════════════════════════════
        // PATIENT REPORT — Medical History
        // ═══════════════════════════════════════════════════════════
        public byte[] GeneratePatientMedicalHistoryPdf(Patient patient)
        {
            var conditions = _context.PatientConditions
                .Include(pc => pc.Condition).ThenInclude(c => c.Category)
                .Where(pc => pc.PatientID == patient.PatientID && pc.IsActive)
                .ToList();

            var allergies = _context.PatientAllergies
                .Include(pa => pa.Allergy).ThenInclude(a => a.Category)
                .Where(pa => pa.PatientID == patient.PatientID)
                .ToList();

            var medications = _context.PatientMedications
                .Include(pm => pm.Medication).ThenInclude(m => m.Category)
                .Where(pm => pm.PatientID == patient.PatientID)
                .ToList();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    // HEADER
                    page.Header().Element(header =>
                    {
                        header.Column(col =>
                        {
                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("Nelson Mandela Bay Haematology")
                                        .FontSize(16).Bold().FontColor(Colors.Blue.Darken3);
                                    c.Item().Text("Diagnostic Laboratories")
                                        .FontSize(11).SemiBold().FontColor(Colors.Blue.Medium);
                                    c.Item().Text("Patient Medical History Report")
                                        .FontSize(10).FontColor(Colors.Grey.Darken2).Italic();
                                });
                                row.ConstantItem(140).AlignRight().Column(c =>
                                {
                                    c.Item().Text("Generated").FontSize(8).FontColor(Colors.Grey.Medium);
                                    c.Item().Text($"{DateTime.Now:yyyy-MM-dd HH:mm}")
                                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                                });
                            });
                            col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                        });
                    });

                    // CONTENT
                    page.Content().PaddingVertical(12).Column(col =>
                    {
                        col.Item().Background(Colors.Blue.Lighten5).Padding(8).Column(c =>
                        {
                            c.Item().Text($"Patient: {patient.FirstName} {patient.LastName}").FontSize(11).Bold();
                            c.Item().Text($"SA ID: {patient.SAIDNumber}  |  DOB: {patient.DateOfBirth:dd MMM yyyy}").FontSize(9);
                            c.Item().Text($"Email: {patient.Email}  |  Cell: {patient.Cellphone}").FontSize(9);
                        });

                        col.Item().PaddingTop(15);

                        // Conditions
                        col.Item().Text("Medical Conditions").FontSize(12).Bold().FontColor(Colors.Blue.Darken3);
                        col.Item().PaddingTop(4);
                        if (conditions.Count > 0)
                        {
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.RelativeColumn(4);
                                    cols.RelativeColumn(3);
                                    cols.RelativeColumn(3);
                                });
                                table.Header(h =>
                                {
                                    h.Cell().Background(Colors.Blue.Darken3).Padding(4).Text("Condition").Bold().FontColor(Colors.White).FontSize(9);
                                    h.Cell().Background(Colors.Blue.Darken3).Padding(4).Text("Category").Bold().FontColor(Colors.White).FontSize(9);
                                    h.Cell().Background(Colors.Blue.Darken3).Padding(4).Text("Diagnosed").Bold().FontColor(Colors.White).FontSize(9);
                                });
                                foreach (var c in conditions)
                                {
                                    table.Cell().Padding(4).Text(c.Condition.ConditionName).FontSize(9);
                                    table.Cell().Padding(4).Text(c.Condition.Category?.CategoryName ?? "-").FontSize(9);
                                    table.Cell().Padding(4).Text(c.DiagnosedDate?.ToString("dd MMM yyyy") ?? "-").FontSize(9);
                                }
                            });
                        }
                        else
                        {
                            col.Item().Text("None recorded.").FontSize(9).Italic().FontColor(Colors.Grey.Medium);
                        }

                        col.Item().PaddingTop(15);

                        // Allergies
                        col.Item().Text("Allergies").FontSize(12).Bold().FontColor(Colors.Blue.Darken3);
                        col.Item().PaddingTop(4);
                        if (allergies.Count > 0)
                        {
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.RelativeColumn(4);
                                    cols.RelativeColumn(3);
                                    cols.RelativeColumn(3);
                                });
                                table.Header(h =>
                                {
                                    h.Cell().Background(Colors.Blue.Darken3).Padding(4).Text("Allergy").Bold().FontColor(Colors.White).FontSize(9);
                                    h.Cell().Background(Colors.Blue.Darken3).Padding(4).Text("Category").Bold().FontColor(Colors.White).FontSize(9);
                                    h.Cell().Background(Colors.Blue.Darken3).Padding(4).Text("Severity").Bold().FontColor(Colors.White).FontSize(9);
                                });
                                foreach (var a in allergies)
                                {
                                    table.Cell().Padding(4).Text(a.Allergy.AllergyName).FontSize(9);
                                    table.Cell().Padding(4).Text(a.Allergy.Category?.CategoryName ?? "-").FontSize(9);
                                    table.Cell().Padding(4).Text(a.Severity ?? "-").FontSize(9);
                                }
                            });
                        }
                        else
                        {
                            col.Item().Text("None recorded.").FontSize(9).Italic().FontColor(Colors.Grey.Medium);
                        }

                        col.Item().PaddingTop(15);

                        // Medications
                        col.Item().Text("Current Medications").FontSize(12).Bold().FontColor(Colors.Blue.Darken3);
                        col.Item().PaddingTop(4);
                        if (medications.Count > 0)
                        {
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.RelativeColumn(4);
                                    cols.RelativeColumn(3);
                                    cols.RelativeColumn(2);
                                    cols.RelativeColumn(3);
                                });
                                table.Header(h =>
                                {
                                    h.Cell().Background(Colors.Blue.Darken3).Padding(4).Text("Medication").Bold().FontColor(Colors.White).FontSize(9);
                                    h.Cell().Background(Colors.Blue.Darken3).Padding(4).Text("Category").Bold().FontColor(Colors.White).FontSize(9);
                                    h.Cell().Background(Colors.Blue.Darken3).Padding(4).Text("Dosage").Bold().FontColor(Colors.White).FontSize(9);
                                    h.Cell().Background(Colors.Blue.Darken3).Padding(4).Text("Frequency").Bold().FontColor(Colors.White).FontSize(9);
                                });
                                foreach (var m in medications)
                                {
                                    table.Cell().Padding(4).Text(m.Medication.MedicationName).FontSize(9);
                                    table.Cell().Padding(4).Text(m.Medication.Category?.CategoryName ?? "-").FontSize(9);
                                    table.Cell().Padding(4).Text(m.Dosage ?? "-").FontSize(9);
                                    table.Cell().Padding(4).Text(m.Frequency ?? "-").FontSize(9);
                                }
                            });
                        }
                        else
                        {
                            col.Item().Text("None recorded.").FontSize(9).Italic().FontColor(Colors.Grey.Medium);
                        }

                        col.Item().PaddingTop(15).Text("This is a computer-generated report.")
                            .FontSize(8).Italic().FontColor(Colors.Grey.Medium);
                    });

                    // FOOTER
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ").FontSize(8).FontColor(Colors.Grey.Medium);
                        x.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                        x.Span(" of ").FontSize(8).FontColor(Colors.Grey.Medium);
                        x.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                });
            });

            return document.GeneratePdf();
        }

        // ─── Helper: small stat card used in the Doctor report header ───
        private static Action<QuestPDF.Infrastructure.IContainer> StatCard(
            string label, string value, string colorHex)
        {
            return container =>
            {
                container
                    .Border(1).BorderColor(Colors.Grey.Lighten2)
                    .Padding(6)
                    .Column(c =>
                    {
                        c.Item().Text(value).FontSize(16).Bold().FontColor(colorHex);
                        c.Item().Text(label).FontSize(8).FontColor(Colors.Grey.Darken1);
                    });
            };
        }
    }
}