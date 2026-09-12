using System;
using System.Collections.Generic;
using System.IO;
using NMB_HLabSys.Models.ViewModels;
using NMB_HLabSys.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NMB_HLabSys.Services
{
    public class PdfReportService : IPdfReportService
    {
        public PdfReportService()
        {
            // Set QuestPDF License to Community (Free for open source / educational projects)
            QuestPDF.Settings.License = LicenseType.Community;
        }

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
                            // Table Header
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3); // Category Name
                                    columns.RelativeColumn(1); // Total Tests
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

                                // Grand Total Row
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

                    // ─── HEADER ───
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

                    // ─── CONTENT ───
                    page.Content().PaddingVertical(12).Column(col =>
                    {
                        // Doctor info
                        col.Item().Background(Colors.Blue.Lighten5).Padding(8).Column(c =>
                        {
                            c.Item().Text($"Doctor: {data.DoctorName}").FontSize(10).Bold();
                            c.Item().Text($"Email: {data.DoctorEmail}").FontSize(9).FontColor(Colors.Grey.Darken1);
                        });

                        col.Item().PaddingTop(10);

                        // Summary stats
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

                        // Requests
                        foreach (var req in data.Requests)
                        {
                            col.Item().PaddingTop(8).Element(e =>
                            {
                                e.Column(reqCol =>
                                {
                                    // Request header
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

                                    // Clinical notes
                                    if (!string.IsNullOrEmpty(req.ClinicalNotes))
                                    {
                                        reqCol.Item().Background(Colors.Yellow.Lighten5).Padding(5).Text(
                                            $"Notes: {req.ClinicalNotes}")
                                            .FontSize(9).Italic();
                                    }

                                    // Tests table
                                    if (req.Tests.Count > 0)
                                    {
                                        reqCol.Item().PaddingTop(4).Table(table =>
                                        {
                                            table.ColumnsDefinition(cols =>
                                            {
                                                cols.RelativeColumn(3);   // Test
                                                cols.RelativeColumn(2);   // Category
                                                cols.RelativeColumn(2);   // Status
                                                cols.RelativeColumn(2);   // Result
                                                cols.RelativeColumn(2);   // Normal Range
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

                    // ─── FOOTER ───
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