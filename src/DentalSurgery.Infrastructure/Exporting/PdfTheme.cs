using DentalSurgery.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DentalSurgery.Infrastructure.Exporting;

/// <summary>Shared print styling so every generated document looks like the same practice.</summary>
public static class PdfTheme
{
    public const string Ink = "#1B2430";
    public const string Muted = "#5F6B7A";
    public const string Faint = "#8A95A3";
    public const string Line = "#DDE3EA";
    public const string LineStrong = "#C3CDD8";
    public const string Accent = "#1266D6";
    public const string AccentSoft = "#EEF4FE";
    public const string Success = "#1C7C4A";
    public const string Danger = "#D13438";
    public const string Warning = "#C77700";
    public const string Surface = "#FAFBFC";

    public const float BodySize = 9f;
    public const float SmallSize = 7.5f;

    public static TextStyle Body => TextStyle.Default.FontSize(BodySize).FontColor(Ink).FontFamily(Fonts.Calibri);
    public static TextStyle Small => Body.FontSize(SmallSize).FontColor(Muted);
    public static TextStyle Label => Body.FontSize(SmallSize).FontColor(Faint).Bold();
    public static TextStyle Heading => Body.FontSize(16).Bold();
    public static TextStyle SubHeading => Body.FontSize(11).Bold();
    public static TextStyle TableHeader => Body.FontSize(SmallSize).Bold().FontColor(Muted);

    /// <summary>
    /// The practice logo, or null when there is none to draw.
    /// <para>
    /// Cached after the first read: a batch export renders the same letterhead
    /// on every page of every document, and re-reading the file each time would
    /// put thousands of disk reads behind one download.
    /// </para>
    /// </summary>
    private static byte[]? _logo;
    private static string? _logoPath;

    private static byte[]? LoadLogo(Practice? practice)
    {
        var path = practice?.LogoPath;
        if (string.IsNullOrWhiteSpace(path)) return null;

        if (_logoPath == path) return _logo;

        try
        {
            var resolved = Path.IsPathRooted(path)
                ? path
                : Path.Combine(AppContext.BaseDirectory, "wwwroot", path);

            _logo = File.Exists(resolved) ? File.ReadAllBytes(resolved) : null;
        }
        catch (IOException)
        {
            // An unreadable logo must not stop an invoice being produced.
            _logo = null;
        }

        _logoPath = path;
        return _logo;
    }

    /// <summary>The practice letterhead used at the top of every document.</summary>
    public static void Letterhead(IContainer container, Practice? practice, string documentTitle, string? reference = null)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    // The logo replaces the typeset practice name rather than
                    // sitting above it: the name is inside the artwork, and
                    // printing both reads as a mistake. Where no logo is
                    // configured, or the file has gone missing, the name is
                    // typeset as before - a document must still render.
                    var logo = LoadLogo(practice);

                    if (logo is not null)
                        left.Item().Height(34).AlignLeft().Image(logo).FitHeight();
                    else
                        left.Item().Text(practice?.Name ?? "Dental Surgery").Style(SubHeading).FontColor(Accent);

                    if (practice is not null)
                    {
                        var address = practice.Address.ToSingleLine();
                        if (!string.IsNullOrWhiteSpace(address))
                            left.Item().Text(address).Style(Small);

                        var contactParts = new[]
                        {
                            practice.Contact.HomePhone, practice.Contact.Email, practice.Website
                        }.Where(p => !string.IsNullOrWhiteSpace(p));

                        var contact = string.Join("  ·  ", contactParts);
                        if (!string.IsNullOrWhiteSpace(contact))
                            left.Item().Text(contact).Style(Small);
                    }
                });

                row.ConstantItem(200).AlignRight().Column(right =>
                {
                    right.Item().AlignRight().Text(documentTitle).Style(Heading);
                    if (!string.IsNullOrWhiteSpace(reference))
                        right.Item().AlignRight().Text(reference).Style(Small).FontColor(Muted);
                });
            });

            column.Item().PaddingTop(6).LineHorizontal(1).LineColor(LineStrong);
        });
    }

    public static void Footer(IContainer container, Practice? practice, string? extra = null)
    {
        container.Column(column =>
        {
            column.Item().PaddingBottom(4).LineHorizontal(0.5f).LineColor(Line);

            column.Item().Row(row =>
            {
                row.RelativeItem().Text(text =>
                {
                    text.DefaultTextStyle(Small.FontSize(7));
                    text.Span(extra ?? practice?.LegalName ?? practice?.Name ?? string.Empty);
                });

                row.ConstantItem(140).AlignRight().Text(text =>
                {
                    text.DefaultTextStyle(Small.FontSize(7));
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                });
            });
        });
    }

    /// <summary>A label/value pair, used in the detail blocks of every document.</summary>
    public static void Field(ColumnDescriptor column, string label, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;

        column.Item().PaddingBottom(2).Row(row =>
        {
            row.ConstantItem(92).Text(label).Style(Label);
            row.RelativeItem().Text(value).Style(Body);
        });
    }

    /// <summary>A soft-filled panel used for totals and callouts.</summary>
    public static IContainer Panel(IContainer container, string background = Surface) =>
        container.Background(background).Border(0.5f).BorderColor(Line).Padding(8);

    public static string Money(decimal value, string symbol = "£") =>
        $"{symbol}{value:N2}";

    public static string Humanise(string? pascalCase) =>
        string.IsNullOrEmpty(pascalCase)
            ? string.Empty
            : System.Text.RegularExpressions.Regex.Replace(pascalCase, "(?<!^)([A-Z])", " $1");

    public static string Humanise(Enum? value) => value is null ? "-" : Humanise(value.ToString());
}
