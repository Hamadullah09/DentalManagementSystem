namespace DentalSurgery.Domain.Common;

/// <summary>Postal address, stored as an owned type on its parent.</summary>
public class Address
{
    public string? Line1 { get; set; }
    public string? Line2 { get; set; }
    public string? City { get; set; }
    public string? County { get; set; }
    public string? PostCode { get; set; }
    public string? Country { get; set; } = "United Kingdom";

    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(Line1) &&
        string.IsNullOrWhiteSpace(City) &&
        string.IsNullOrWhiteSpace(PostCode);

    public string ToSingleLine()
    {
        var parts = new[] { Line1, Line2, City, County, PostCode, Country }
            .Where(p => !string.IsNullOrWhiteSpace(p));
        return string.Join(", ", parts);
    }

    public IReadOnlyList<string> ToLines() =>
        new[] { Line1, Line2, City, County, PostCode, Country }
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p!.Trim())
            .ToList();

    public override string ToString() => ToSingleLine();
}

/// <summary>Name parts shared by patients, staff and contacts.</summary>
public class PersonName
{
    public string? Title { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string? PreferredName { get; set; }
    public string? Suffix { get; set; }

    public string Full => string.Join(" ",
        new[] { Title, FirstName, MiddleName, LastName, Suffix }
            .Where(p => !string.IsNullOrWhiteSpace(p)));

    public string Display => string.IsNullOrWhiteSpace(PreferredName)
        ? $"{FirstName} {LastName}".Trim()
        : $"{PreferredName} {LastName}".Trim();

    public string Listing => $"{LastName}, {FirstName}".Trim(' ', ',');

    public string Initials =>
        string.Concat(new[] { FirstName, LastName }
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => char.ToUpperInvariant(p![0])));

    public override string ToString() => Display;
}

/// <summary>Telephone and electronic contact details.</summary>
public class ContactDetails
{
    public string? MobilePhone { get; set; }
    public string? HomePhone { get; set; }
    public string? WorkPhone { get; set; }
    public string? Email { get; set; }

    /// <summary>Never null, so the owned block always materialises. "Any" when unspecified.</summary>
    public string PreferredContactMethod { get; set; } = "Any";

    public string? BestPhone => MobilePhone ?? HomePhone ?? WorkPhone;
}
