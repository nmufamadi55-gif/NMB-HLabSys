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
    }
}