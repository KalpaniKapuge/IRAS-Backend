// IRAS.Application/Modules/Cv/CvPdfRenderer.cs
using System.Reflection;
using QuestPDF.Drawing;
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
        // instead. Roboto is registered here too: it's the exact font the web app loads from
        // Google Fonts (see frontend's index.html / tailwind.config.ts) — without registering
        // it, QuestPDF/SkiaSharp silently falls back to a generic system font and the PDF's
        // typography visibly diverges from the live CV preview.
        static CvPdfRenderer()
        {
            QuestPDF.Settings.License = LicenseType.Community;
            RegisterFonts();
        }

        private static void RegisterFonts()
        {
            var assembly = Assembly.GetExecutingAssembly();
            foreach (var name in assembly.GetManifestResourceNames())
            {
                if (!name.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase)) continue;
                using var stream = assembly.GetManifestResourceStream(name);
                if (stream is not null) FontManager.RegisterFont(stream);
            }
        }

        private const string FontFamily = "Roboto";

        // Exact hex values from the web preview's Tailwind classes (teal-900, amber-400,
        // etc.) so the downloaded PDF uses the same colors the candidate saw on screen —
        // not just a similar shade picked independently in QuestPDF's own palette.
        private static class Hex
        {
            public const string Teal900 = "#134e4a";
            public const string Teal950 = "#042f2e";
            public const string Amber400 = "#fbbf24";
            public const string Neutral900 = "#171717";
            public const string Orange500 = "#f97316";
            public const string Orange600 = "#ea580c";
            public const string Slate900 = "#0f172a";
            public const string Slate600 = "#475569";
            public const string Slate500 = "#64748b";
            public const string Slate300 = "#cbd5e1";
            public const string Slate200 = "#e2e8f0";
            public const string White = "#ffffff";
        }

        // Matches each web template's own header treatment for a section title exactly —
        // see classic/modern/compact-template.tsx: Classic uses plain bold text, Modern adds
        // a colored border-bottom, Compact uses a solid colored "badge" pill.
        private enum HeaderStyle { Plain, Underline, Badge }

        // Exact path data from lucide-react (node_modules/lucide-react/dist/esm/icons/*.js) —
        // the same icon set the web templates import (Phone/Mail/Github/Linkedin/ExternalLink)
        // — so the PDF's contact/link rows show real icon glyphs instead of plain text, matching
        // the live preview instead of silently dropping this part of the design.
        private static class IconPath
        {
            public const string Phone = "<path d=\"M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07 19.5 19.5 0 0 1-6-6 19.79 19.79 0 0 1-3.07-8.67A2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72 12.84 12.84 0 0 0 .7 2.81 2 2 0 0 1-.45 2.11L8.09 9.91a16 16 0 0 0 6 6l1.27-1.27a2 2 0 0 1 2.11-.45 12.84 12.84 0 0 0 2.81.7A2 2 0 0 1 22 16.92z\"/>";
            public const string Mail = "<rect width=\"20\" height=\"16\" x=\"2\" y=\"4\" rx=\"2\"/><path d=\"m22 7-8.97 5.7a1.94 1.94 0 0 1-2.06 0L2 7\"/>";
            public const string Github = "<path d=\"M15 22v-4a4.8 4.8 0 0 0-1-3.5c3 0 6-2 6-5.5.08-1.25-.27-2.48-1-3.5.28-1.15.28-2.35 0-3.5 0 0-1 0-3 1.5-2.64-.5-5.36-.5-8 0C6 2 5 2 5 2c-.3 1.15-.3 2.35 0 3.5A5.403 5.403 0 0 0 4 9c0 3.5 3 5.5 6 5.5-.39.49-.68 1.05-.85 1.65-.17.6-.22 1.23-.15 1.85v4\"/><path d=\"M9 18c-4.51 2-5-2-7-2\"/>";
            public const string Linkedin = "<path d=\"M16 8a6 6 0 0 1 6 6v7h-4v-7a2 2 0 0 0-2-2 2 2 0 0 0-2 2v7h-4v-7a6 6 0 0 1 6-6z\"/><rect width=\"4\" height=\"12\" x=\"2\" y=\"9\"/><circle cx=\"4\" cy=\"4\" r=\"2\"/>";
            public const string ExternalLink = "<path d=\"M15 3h6v6\"/><path d=\"M10 14 21 3\"/><path d=\"M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6\"/>";
        }

        private static string Svg(string innerPath, string color) =>
            $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"{color}\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\">{innerPath}</svg>";

        private static void RenderIconLine(ColumnDescriptor target, string iconPath, string iconColor, string text, string textColor, float fontSize = 8.5f, float iconSize = 10)
        {
            target.Item().Row(row =>
            {
                row.ConstantItem(iconSize).Height(iconSize).AlignTop().Svg(Svg(iconPath, iconColor));
                row.ConstantItem(5);
                row.RelativeItem().Text(text).FontSize(fontSize).FontColor(textColor);
            });
        }

        private static readonly HashSet<string> SidebarSections = new() { "Skills", "Languages" };

        // A short CV rendered at the tightest (scale 1) spacing leaves a large blank gap at
        // the bottom of an A4 page — the live web preview never shows this because it isn't
        // page-bound, but a physical A4 page is a fixed size, so "no wasted space" and "true
        // A4 pages" can only both hold if the whitespace *between* sections/entries grows to
        // actually fill the page. Font sizes are never touched (that would visibly diverge
        // from the preview) — only spacing/padding is scaled. This renders repeatedly at
        // increasing scale and keeps the largest one that still fits on a single page; a CV
        // that already needs >1 page at the tightest spacing is left alone rather than made
        // worse.
        public byte[] Render(string templateName, RenderedCvData data)
        {
            byte[] RenderAt(float scale) => templateName switch
            {
                "Modern" => RenderModern(data, scale),
                "Compact" => RenderCompact(data, scale),
                _ => RenderClassic(data, scale), // "Classic" and any unrecognized name
            };

            var best = RenderAt(1f);
            if (CountPages(best) > 1) return best;

            for (var scale = 1.15f; scale <= 2.4f; scale += 0.15f)
            {
                var candidate = RenderAt(scale);
                if (CountPages(candidate) > 1) break;
                best = candidate;
            }

            return best;
        }

        private static int CountPages(byte[] pdfBytes)
        {
            var text = System.Text.Encoding.Latin1.GetString(pdfBytes);
            return System.Text.RegularExpressions.Regex.Matches(text, @"/Type\s*/Page[^s]").Count;
        }

        // All three templates use a two-column Row (sidebar + main content) as the page's
        // top-level content. Verified empirically (throwaway test harness, since removed)
        // that QuestPDF *does* paginate this correctly across multiple pages — the sidebar's
        // background fills every page and the main column's Column content flows onto
        // subsequent pages exactly like a plain single-column layout would. The earlier
        // DocumentLayoutException this project hit was unrelated: it came from a Row of
        // skill chips using AutoItem() (no wrap, exceeds page *width*), not from nesting a
        // Row inside page.Content() for pagination.

        // ---- Classic: white page, thin left sidebar (photo/contact/skills), timeline main column ----
        private static byte[] RenderClassic(RenderedCvData data, float scale)
        {
            var mainSections = data.SectionOrder.Where(s => !SidebarSections.Contains(s)).ToList();

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0);
                    page.DefaultTextStyle(x => x.FontFamily(FontFamily).FontSize(10.5f).FontColor(Hex.Slate900));

                    page.Content().Row(row =>
                    {
                        row.ConstantItem(160).BorderRight(1).BorderColor(Hex.Slate200).Padding(15 * scale).Column(sidebar =>
                        {
                            sidebar.Spacing(12 * scale);

                            RenderCircularPhoto(sidebar.Item(), data, 64, 2, Hex.Slate900, Hex.Slate300, Hex.Slate900);

                            sidebar.Item().Column(inner =>
                            {
                                inner.Item().BorderBottom(1).BorderColor(Hex.Slate300).PaddingBottom(3)
                                    .Text("CONTACT").FontSize(8).Bold().FontColor(Hex.Slate900);
                                inner.Item().PaddingTop(6 * scale).Column(rows =>
                                {
                                    rows.Spacing(5 * scale);
                                    if (!string.IsNullOrWhiteSpace(data.Phone))
                                        RenderIconLine(rows, IconPath.Phone, Hex.Slate900, data.Phone, Hex.Slate600);
                                    if (!string.IsNullOrWhiteSpace(data.Email))
                                        RenderIconLine(rows, IconPath.Mail, Hex.Slate900, data.Email, Hex.Slate600);
                                });
                            });

                            if (data.Skills.Count > 0)
                            {
                                sidebar.Item().Column(inner =>
                                {
                                    inner.Item().BorderBottom(1).BorderColor(Hex.Slate300).PaddingBottom(3)
                                        .Text("SKILLS").FontSize(8).Bold().FontColor(Hex.Slate900);
                                    inner.Item().PaddingTop(6 * scale).Column(items =>
                                    {
                                        items.Spacing(3 * scale);
                                        foreach (var skill in data.Skills)
                                            items.Item().Text(skill).FontSize(8.5f).FontColor(Hex.Slate600);
                                    });
                                });
                            }
                        });

                        row.RelativeItem().Padding(18 * scale).Column(main =>
                        {
                            main.Item().Text(data.FullName).FontSize(22).Bold().FontColor(Hex.Slate900);
                            if (!string.IsNullOrWhiteSpace(data.Headline))
                                main.Item().PaddingTop(2).Text(data.Headline.ToUpperInvariant()).FontSize(10).FontColor(Hex.Slate500);
                            main.Item().PaddingTop(6).Height(2).Width(60).Background(Hex.Slate900);

                            main.Item().PaddingTop(12 * scale).Column(col =>
                            {
                                col.Spacing(8 * scale);
                                foreach (var section in mainSections)
                                    RenderSection(col, section, data, headerColor: Hex.Slate900, accentColor: Hex.Slate500, HeaderStyle.Plain, scale: scale);
                            });
                        });
                    });
                });
            }).GeneratePdf();
        }

        // ---- Modern: teal + gold hero band once at the top, then a persistent teal sidebar + white main column ----
        private static byte[] RenderModern(RenderedCvData data, float scale)
        {
            var sidebarOrder = data.SectionOrder.Where(SidebarSections.Contains).ToList();
            var mainSections = data.SectionOrder.Where(s => !SidebarSections.Contains(s)).ToList();

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0);
                    page.DefaultTextStyle(x => x.FontFamily(FontFamily).FontSize(10.5f));

                    page.Content().Column(outer =>
                    {
                        outer.Item().Column(hero =>
                        {
                            hero.Item().Background(Hex.Teal900).Padding(18 * scale).Row(row =>
                            {
                                RenderCircularPhoto(row.ConstantItem(60), data, 60, 2, Hex.Amber400, Hex.Teal900, Hex.Amber400);
                                row.ConstantItem(16);
                                row.RelativeItem().Column(text =>
                                {
                                    text.Item().Text(data.FullName).FontSize(22).Bold().FontColor(Hex.White);
                                    if (!string.IsNullOrWhiteSpace(data.Headline))
                                        text.Item().PaddingTop(2).Text(data.Headline).FontSize(11).FontColor(Hex.Amber400);
                                });
                            });
                            hero.Item().Height(4).Background(Hex.Amber400);
                        });

                        outer.Item().Row(row =>
                        {
                            row.ConstantItem(190).Background(Hex.Teal950).Padding(15 * scale).Column(sidebar =>
                            {
                                sidebar.Spacing(10 * scale);

                                sidebar.Item().Column(rows =>
                                {
                                    rows.Spacing(5 * scale);
                                    if (!string.IsNullOrWhiteSpace(data.Email))
                                        RenderIconLine(rows, IconPath.Mail, Hex.Amber400, data.Email, Hex.White);
                                    if (!string.IsNullOrWhiteSpace(data.Phone))
                                        RenderIconLine(rows, IconPath.Phone, Hex.Amber400, data.Phone, Hex.White);
                                    if (!string.IsNullOrWhiteSpace(data.GithubUrl))
                                        RenderIconLine(rows, IconPath.Github, Hex.Amber400, data.GithubUrl, Hex.White);
                                    if (!string.IsNullOrWhiteSpace(data.LinkedInUrl))
                                        RenderIconLine(rows, IconPath.Linkedin, Hex.Amber400, data.LinkedInUrl, Hex.White);
                                });

                                foreach (var section in sidebarOrder)
                                {
                                    RenderSidebarList(sidebar, section, data,
                                        headerPill: true, headerColor: Hex.Amber400,
                                        bulletColor: Hex.Amber400, textColor: Hex.White, accentColor: Hex.Amber400, scale: scale);
                                }
                            });

                            row.RelativeItem().Padding(15 * scale).Column(main =>
                            {
                                main.Spacing(8 * scale);
                                foreach (var section in mainSections)
                                    RenderSection(main, section, data, headerColor: Hex.Teal900, accentColor: Hex.Amber400, HeaderStyle.Underline, scale: scale);
                            });
                        });
                    });
                });
            }).GeneratePdf();
        }

        // ---- Compact: dark sidebar (photo/contact/skills/languages) top-to-bottom, dense white main column ----
        private static byte[] RenderCompact(RenderedCvData data, float scale)
        {
            var sidebarOrder = data.SectionOrder.Where(SidebarSections.Contains).ToList();
            var mainSections = data.SectionOrder.Where(s => !SidebarSections.Contains(s)).ToList();

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0);
                    page.DefaultTextStyle(x => x.FontFamily(FontFamily).FontSize(9));

                    page.Content().Row(row =>
                    {
                        row.ConstantItem(190).Background(Hex.Neutral900).Padding(15 * scale).Column(sidebar =>
                        {
                            sidebar.Spacing(10 * scale);

                            RenderCircularPhoto(sidebar.Item(), data, 70, 3, Hex.Orange500, Hex.Neutral900, Hex.Orange500);

                            sidebar.Item().Column(inner =>
                            {
                                inner.Item().BorderBottom(2).BorderColor(Hex.Orange500).PaddingBottom(3)
                                    .Text("CONTACT").FontSize(8).Bold().FontColor(Hex.Orange500);
                                inner.Item().PaddingTop(6 * scale).Column(rows =>
                                {
                                    rows.Spacing(5 * scale);
                                    if (!string.IsNullOrWhiteSpace(data.Phone))
                                        RenderIconLine(rows, IconPath.Phone, Hex.Orange500, data.Phone, Hex.Slate200, fontSize: 8);
                                    if (!string.IsNullOrWhiteSpace(data.Email))
                                        RenderIconLine(rows, IconPath.Mail, Hex.Orange500, data.Email, Hex.Slate200, fontSize: 8);
                                    if (!string.IsNullOrWhiteSpace(data.LinkedInUrl))
                                        RenderIconLine(rows, IconPath.Linkedin, Hex.Orange500, data.LinkedInUrl, Hex.Slate200, fontSize: 8);
                                    if (!string.IsNullOrWhiteSpace(data.GithubUrl))
                                        RenderIconLine(rows, IconPath.Github, Hex.Orange500, data.GithubUrl, Hex.Slate200, fontSize: 8);
                                });
                            });

                            foreach (var section in sidebarOrder)
                            {
                                RenderSidebarList(sidebar, section, data,
                                    headerPill: false, headerColor: Hex.Orange500,
                                    bulletColor: Hex.Orange500, textColor: Hex.Slate200, accentColor: Hex.Orange500,
                                    headerUnderline: true, scale: scale);
                            }
                        });

                        row.RelativeItem().Padding(15 * scale).Column(main =>
                        {
                            main.Item().Text(data.FullName).FontSize(18).Bold().FontColor(Hex.Neutral900);
                            if (!string.IsNullOrWhiteSpace(data.Headline))
                                main.Item().PaddingTop(1).Text(data.Headline).FontSize(9.5f).Bold().FontColor(Hex.Orange600);

                            main.Item().PaddingTop(8 * scale).Column(col =>
                            {
                                col.Spacing(6 * scale);
                                foreach (var section in mainSections)
                                    RenderSection(col, section, data, headerColor: Hex.Orange600, accentColor: Hex.Orange500, HeaderStyle.Badge, compact: true, scale: scale);
                            });
                        });
                    });
                });
            }).GeneratePdf();
        }

        // ---- shared sidebar rendering (Skills / Languages as plain bulleted lists, matching
        // each web template's own <ul>/<li> markup — not boxed chips) ----

        private static void RenderSidebarList(
            ColumnDescriptor sidebar, string section, RenderedCvData data,
            bool headerPill, string headerColor, string bulletColor, string textColor, string accentColor,
            float scale, bool headerUnderline = false)
        {
            if (section == "Skills" && data.Skills.Count > 0)
            {
                sidebar.Item().Column(inner =>
                {
                    RenderSidebarHeader(inner, "SKILLS", headerColor, headerPill, headerUnderline);
                    inner.Item().PaddingTop(6 * scale).Column(items =>
                    {
                        items.Spacing(3 * scale);
                        foreach (var skill in data.Skills)
                        {
                            items.Item().Text(t =>
                            {
                                t.Span("• ").FontColor(bulletColor);
                                t.Span(skill).FontColor(textColor).FontSize(8.5f);
                            });
                        }
                    });
                });
            }
            else if (section == "Languages" && data.Languages.Count > 0)
            {
                sidebar.Item().Column(inner =>
                {
                    RenderSidebarHeader(inner, "LANGUAGES", headerColor, headerPill, headerUnderline);
                    inner.Item().PaddingTop(6 * scale).Column(items =>
                    {
                        items.Spacing(3 * scale);
                        foreach (var l in data.Languages)
                        {
                            items.Item().Text(t =>
                            {
                                t.Span(l.LanguageName).FontColor(textColor).FontSize(8.5f);
                                t.Span($" — {l.Proficiency}").FontColor(accentColor).FontSize(8.5f);
                            });
                        }
                    });
                });
            }
        }

        private static void RenderSidebarHeader(ColumnDescriptor inner, string label, string color, bool pill, bool underline)
        {
            if (pill)
            {
                // Matches Modern's web sidebar header: a full-width rounded pill with a
                // border, centered label text (Tailwind's `rounded-full border ... text-center`).
                inner.Item().Border(1).BorderColor(color).CornerRadius(10)
                    .PaddingVertical(4).AlignCenter().Text(label).FontSize(8).Bold().FontColor(color);
            }
            else if (underline)
            {
                inner.Item().BorderBottom(2).BorderColor(color).PaddingBottom(3)
                    .Text(label).FontSize(8.5f).Bold().FontColor(color);
            }
            else
            {
                inner.Item().Text(label).FontSize(8.5f).Bold().FontColor(color);
            }
        }

        // ---- shared main-content section rendering ----

        private static void RenderSection(ColumnDescriptor col, string section, RenderedCvData data, string headerColor, string accentColor, HeaderStyle headerStyle, float scale, bool compact = false)
        {
            var titleSize = compact ? 11f : 13f;

            void Header(ColumnDescriptor target, string label)
            {
                switch (headerStyle)
                {
                    case HeaderStyle.Underline:
                        // Matches Modern's web section headers: `border-b-2 ... pb-1`.
                        target.Item().BorderBottom(1.5f).BorderColor(accentColor).PaddingBottom(3)
                            .Text(label).FontSize(titleSize).Bold().FontColor(headerColor);
                        break;
                    case HeaderStyle.Badge:
                        // Matches Compact's web section headers: a solid, inline-block colored
                        // badge (`inline-block rounded bg-orange-500 px-2.5 py-1 text-white`).
                        // A single short static label is safe as a Row's AutoItem — unlike the
                        // many-variable-length-items case that caused the earlier chip-overflow
                        // bug, this is one bounded string, so it can never exceed page width.
                        target.Item().Row(row =>
                        {
                            row.AutoItem().Background(accentColor).CornerRadius(3).Padding(4)
                                .Text(label).FontSize(titleSize - 2).Bold().FontColor(Hex.White);
                            row.RelativeItem();
                        });
                        break;
                    default:
                        target.Item().Text(label).FontSize(titleSize).Bold().FontColor(headerColor);
                        break;
                }
            }

            switch (section)
            {
                case "Summary" when !string.IsNullOrWhiteSpace(data.Summary):
                    col.Item().Column(inner =>
                    {
                        Header(inner, "Profile");
                        inner.Item().PaddingTop(2 * scale).Text(data.Summary).Justify();
                    });
                    break;

                case "Experience" when data.Experience.Count > 0:
                    col.Item().Column(inner =>
                    {
                        Header(inner, "Experience");
                        inner.Item().PaddingTop(3 * scale).Column(entries =>
                        {
                            entries.Spacing(5 * scale);
                            foreach (var e in data.Experience)
                            {
                                entries.Item().BorderLeft(2).BorderColor(accentColor).PaddingLeft(8).Column(item =>
                                {
                                    item.Item().Row(row =>
                                    {
                                        row.RelativeItem().Text($"{e.JobTitle} — {e.CompanyName}").Bold();
                                        row.ConstantItem(140).AlignRight().Text(DateRange(e.StartDate, e.EndDate, e.IsCurrent))
                                            .FontSize(8.5f).FontColor(Hex.Slate500);
                                    });
                                    if (!string.IsNullOrWhiteSpace(e.Description))
                                        item.Item().PaddingTop(1).Text(e.Description).FontSize(9).Justify();
                                });
                            }
                        });
                    });
                    break;

                case "Education" when data.Education.Count > 0:
                    col.Item().Column(inner =>
                    {
                        Header(inner, "Education");
                        inner.Item().PaddingTop(3 * scale).Column(entries =>
                        {
                            entries.Spacing(4 * scale);
                            foreach (var e in data.Education)
                            {
                                var years = e.StartYear.HasValue || e.EndYear.HasValue
                                    ? $"{e.StartYear?.ToString() ?? "?"} – {e.EndYear?.ToString() ?? "Present"}" : "";
                                entries.Item().BorderLeft(2).BorderColor(accentColor).PaddingLeft(8).Column(item =>
                                {
                                    item.Item().Row(row =>
                                    {
                                        row.RelativeItem().Text($"{e.Degree}{(string.IsNullOrWhiteSpace(e.FieldOfStudy) ? "" : $" in {e.FieldOfStudy}")} — {e.Institution}").Bold();
                                        row.ConstantItem(100).AlignRight().Text(years).FontSize(8.5f).FontColor(Hex.Slate500);
                                    });
                                    if (!string.IsNullOrWhiteSpace(e.Grade))
                                        item.Item().Text($"Grade: {e.Grade}").FontSize(8.5f).FontColor(Hex.Slate500);
                                });
                            }
                        });
                    });
                    break;

                case "Certifications" when data.Certifications.Count > 0:
                    col.Item().Column(inner =>
                    {
                        Header(inner, "Certifications");
                        inner.Item().PaddingTop(3 * scale).Column(entries =>
                        {
                            entries.Spacing(2 * scale);
                            foreach (var c in data.Certifications)
                            {
                                var issued = c.IssueDate.HasValue ? $" ({c.IssueDate.Value:yyyy})" : "";
                                entries.Item().Text($"{c.Name}{(string.IsNullOrWhiteSpace(c.IssuingOrg) ? "" : $" — {c.IssuingOrg}")}{issued}").FontSize(9.5f);
                            }
                        });
                    });
                    break;

                case "Projects" when data.Projects.Count > 0:
                    col.Item().Column(inner =>
                    {
                        Header(inner, "Projects");
                        inner.Item().PaddingTop(3 * scale).Column(entries =>
                        {
                            entries.Spacing(5 * scale);
                            foreach (var p in data.Projects)
                            {
                                entries.Item().BorderLeft(2).BorderColor(accentColor).PaddingLeft(8).Column(item =>
                                {
                                    item.Item().Text(p.Title).Bold();
                                    if (!string.IsNullOrWhiteSpace(p.Description))
                                        item.Item().PaddingTop(1).Text(p.Description).FontSize(9).Justify();
                                    if (!string.IsNullOrWhiteSpace(p.ProjectUrl))
                                        RenderIconLine(item, IconPath.ExternalLink, accentColor, p.ProjectUrl, accentColor, fontSize: 8.5f, iconSize: 9);
                                });
                            }
                        });
                    });
                    break;
            }
        }

        private static string DateRange(DateTime start, DateTime? end, bool isCurrent) =>
            $"{start:MMM yyyy} – {(isCurrent ? "Present" : end?.ToString("MMM yyyy") ?? "")}";

        // Mirrors initialsFromFullName in the web templates (templates/types.ts) — a
        // candidate without an uploaded photo sees a circular initials avatar there, not a
        // blank gap, so the PDF needs the same fallback.
        private static string Initials(string fullName)
        {
            var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var first = parts.Length > 0 ? parts[0][0].ToString() : "";
            var last = parts.Length > 1 ? parts[^1][0].ToString() : "";
            var result = (first + last).ToUpperInvariant();
            return result.Length == 0 ? "?" : result;
        }

        private static void RenderCircularPhoto(IContainer container, RenderedCvData data, float size, float borderWidth, string borderColor, string bgColor, string textColor)
        {
            var circle = container.Width(size).Height(size).Border(borderWidth).BorderColor(borderColor).CornerRadius(size / 2);
            if (data.PhotoBytes is not null)
                circle.Image(data.PhotoBytes).FitArea();
            else
                circle.Background(bgColor).AlignCenter().AlignMiddle().Text(Initials(data.FullName)).FontSize(size / 3).Bold().FontColor(textColor);
        }
    }
}
