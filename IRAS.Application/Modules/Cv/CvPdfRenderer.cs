// IRAS.Application/Modules/Cv/CvPdfRenderer.cs
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace IRAS.Application.Modules.Cv
{
    public class CvPdfRenderer : ICvPdfRenderer
    {
        // QuestPDF's Community license is free for this use (individual/academic project,
        // not a >$1M-revenue company) but must be accepted explicitly at process level —
        // idempotent, so setting it per-render is harmless if this ever moves to DI startup
        // instead.
        static CvPdfRenderer() => QuestPDF.Settings.License = LicenseType.Community;

        public byte[] Render(string templateName, RenderedCvData data)
        {
            return templateName switch
            {
                "Modern" => RenderModern(data),
                "Compact" => RenderCompact(data),
                _ => RenderClassic(data), // "Classic" and any unrecognized name
            };
        }

        // ---- Classic: single column, formal, ATS-friendly ----
        private static byte[] RenderClassic(RenderedCvData data)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10.5f).FontColor(Colors.Black));

                    page.Header().Column(col =>
                    {
                        col.Item().Text(data.FullName).FontSize(22).Bold();
                        if (!string.IsNullOrWhiteSpace(data.Headline))
                            col.Item().Text(data.Headline).FontSize(12).FontColor(Colors.Grey.Darken2);
                        col.Item().PaddingTop(4).Text(ContactLine(data)).FontSize(9.5f).FontColor(Colors.Grey.Darken1);
                        col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                    });

                    page.Content().PaddingTop(10).Column(col =>
                    {
                        foreach (var section in data.SectionOrder)
                            RenderSection(col, section, data, headerColor: Colors.Black);
                    });
                });
            }).GeneratePdf();
        }

        // ---- Modern: shaded sidebar (contact + skills) + main content column ----
        private static byte[] RenderModern(RenderedCvData data)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0);
                    page.DefaultTextStyle(x => x.FontSize(10.5f));

                    page.Content().Row(row =>
                    {
                        row.ConstantItem(170).Background(Colors.Blue.Darken3).Padding(16).Column(col =>
                        {
                            col.Item().Text(data.FullName).FontSize(17).Bold().FontColor(Colors.White);
                            if (!string.IsNullOrWhiteSpace(data.Headline))
                                col.Item().PaddingTop(2).Text(data.Headline).FontSize(10).FontColor(Colors.Blue.Lighten4);

                            col.Item().PaddingTop(12).Text("Contact").Bold().FontColor(Colors.White);
                            col.Item().PaddingTop(4).Text(ContactBlock(data)).FontSize(9).FontColor(Colors.Blue.Lighten5);

                            if (data.SectionOrder.Contains("Skills") && data.Skills.Count > 0)
                            {
                                col.Item().PaddingTop(14).Text("Skills").Bold().FontColor(Colors.White);
                                foreach (var skill in data.Skills)
                                    col.Item().PaddingTop(2).Text($"• {skill}").FontSize(9).FontColor(Colors.Blue.Lighten5);
                            }
                        });

                        row.RelativeItem().Padding(20).Column(col =>
                        {
                            foreach (var section in data.SectionOrder.Where(s => s != "Skills"))
                                RenderSection(col, section, data, headerColor: Colors.Blue.Darken3);
                        });
                    });
                });
            }).GeneratePdf();
        }

        // ---- Compact: dense, minimal spacing, smaller type ----
        private static byte[] RenderCompact(RenderedCvData data)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.3f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text(data.FullName).FontSize(16).Bold();
                            row.RelativeItem().AlignRight().Text(ContactLine(data)).FontSize(8).FontColor(Colors.Grey.Darken1);
                        });
                        if (!string.IsNullOrWhiteSpace(data.Headline))
                            col.Item().Text(data.Headline).FontSize(10).FontColor(Colors.Grey.Darken2);
                        col.Item().PaddingTop(4).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                    });

                    page.Content().PaddingTop(6).Column(col =>
                    {
                        col.Spacing(6);
                        foreach (var section in data.SectionOrder)
                            RenderSection(col, section, data, headerColor: Colors.Black, compact: true);
                    });
                });
            }).GeneratePdf();
        }

        // ---- shared section rendering ----

        private static void RenderSection(ColumnDescriptor col, string section, RenderedCvData data, string headerColor, bool compact = false)
        {
            var titleSize = compact ? 11f : 13f;
            var spacingBefore = compact ? 4f : 10f;

            switch (section)
            {
                case "Summary" when !string.IsNullOrWhiteSpace(data.Summary):
                    col.Item().PaddingTop(spacingBefore).Text("Summary").FontSize(titleSize).Bold().FontColor(headerColor);
                    col.Item().PaddingTop(2).Text(data.Summary);
                    break;

                case "Skills" when data.Skills.Count > 0:
                    col.Item().PaddingTop(spacingBefore).Text("Skills").FontSize(titleSize).Bold().FontColor(headerColor);
                    col.Item().PaddingTop(2).Text(string.Join("  •  ", data.Skills));
                    break;

                case "Experience" when data.Experience.Count > 0:
                    col.Item().PaddingTop(spacingBefore).Text("Experience").FontSize(titleSize).Bold().FontColor(headerColor);
                    foreach (var e in data.Experience)
                    {
                        col.Item().PaddingTop(4).Row(row =>
                        {
                            row.RelativeItem().Text($"{e.JobTitle} — {e.CompanyName}").Bold();
                            row.ConstantItem(140).AlignRight().Text(DateRange(e.StartDate, e.EndDate, e.IsCurrent))
                                .FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                        });
                        if (!string.IsNullOrWhiteSpace(e.Description))
                            col.Item().PaddingTop(1).Text(e.Description).FontSize(9);
                    }
                    break;

                case "Education" when data.Education.Count > 0:
                    col.Item().PaddingTop(spacingBefore).Text("Education").FontSize(titleSize).Bold().FontColor(headerColor);
                    foreach (var e in data.Education)
                    {
                        var years = e.StartYear.HasValue || e.EndYear.HasValue
                            ? $"{e.StartYear?.ToString() ?? "?"} – {e.EndYear?.ToString() ?? "Present"}" : "";
                        col.Item().PaddingTop(4).Row(row =>
                        {
                            row.RelativeItem().Text($"{e.Degree}{(string.IsNullOrWhiteSpace(e.FieldOfStudy) ? "" : $" in {e.FieldOfStudy}")} — {e.Institution}").Bold();
                            row.ConstantItem(100).AlignRight().Text(years).FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                        });
                        if (!string.IsNullOrWhiteSpace(e.Grade))
                            col.Item().Text($"Grade: {e.Grade}").FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                    }
                    break;

                case "Certifications" when data.Certifications.Count > 0:
                    col.Item().PaddingTop(spacingBefore).Text("Certifications").FontSize(titleSize).Bold().FontColor(headerColor);
                    foreach (var c in data.Certifications)
                    {
                        var issued = c.IssueDate.HasValue ? $" ({c.IssueDate.Value:yyyy})" : "";
                        col.Item().PaddingTop(2).Text($"{c.Name}{(string.IsNullOrWhiteSpace(c.IssuingOrg) ? "" : $" — {c.IssuingOrg}")}{issued}").FontSize(9.5f);
                    }
                    break;
            }
        }

        private static string DateRange(DateTime start, DateTime? end, bool isCurrent) =>
            $"{start:MMM yyyy} – {(isCurrent ? "Present" : end?.ToString("MMM yyyy") ?? "")}";

        private static string ContactLine(RenderedCvData data)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(data.Email)) parts.Add(data.Email);
            if (!string.IsNullOrWhiteSpace(data.Phone)) parts.Add(data.Phone!);
            if (!string.IsNullOrWhiteSpace(data.GithubUrl)) parts.Add(data.GithubUrl!);
            if (!string.IsNullOrWhiteSpace(data.LinkedInUrl)) parts.Add(data.LinkedInUrl!);
            return string.Join("   |   ", parts);
        }

        private static string ContactBlock(RenderedCvData data)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(data.Email)) parts.Add(data.Email!);
            if (!string.IsNullOrWhiteSpace(data.Phone)) parts.Add(data.Phone!);
            if (!string.IsNullOrWhiteSpace(data.GithubUrl)) parts.Add(data.GithubUrl!);
            if (!string.IsNullOrWhiteSpace(data.LinkedInUrl)) parts.Add(data.LinkedInUrl!);
            return string.Join("\n", parts);
        }
    }
}
