using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Models;

[Index(nameof(Ticker), IsUnique = true)]
public class IndexComponent
{
    [Required]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string? CompanyName { get; set; }

    public IndexNames ListedIndexes { get; set; } = IndexNames.None;

    [MaxLength(100)]
    public string? Sector { get; set; }

    [MaxLength(100)]
    public string? SubSector { get; set; }

    [MaxLength(8), Required]
    public string? Ticker { get; set; }

    [Range(0, 100)]
    public float SnPWeight { get; set; }

    [Range(0, 100)]
    public float NasdaqWeight { get; set; }

    [Range(0, 100)]
    public float DowWeight { get; set; }

    public void CleanUpValues()
    {
        const string Ampersand = @"&";
        Regex whiteChars = new(@"[\r\n\t'/\\]");

        RegexOptions options = RegexOptions.Multiline;
        Regex htmlAmpToReadAmp = new(@"&amp;", options);

        Ticker = whiteChars.Replace(Ticker ?? string.Empty, string.Empty);
        CompanyName = whiteChars.Replace(CompanyName ?? string.Empty, string.Empty);
        CompanyName = htmlAmpToReadAmp.Replace(CompanyName ?? string.Empty, Ampersand).Trim();
        Sector = whiteChars.Replace(Sector ?? string.Empty, string.Empty);
        Sector = htmlAmpToReadAmp.Replace(Sector, Ampersand).Trim(); ;
        SubSector = whiteChars.Replace(SubSector ?? string.Empty, string.Empty);
        SubSector = htmlAmpToReadAmp.Replace(SubSector, Ampersand).Trim();

        Regex regex = new(@"\.", options);
        Ticker = regex.Replace(Ticker ?? string.Empty, @"-");
    }
}

[Flags]
public enum IndexNames
{
    None = 0b_0000_0000,
    SnP = 0b_0000_0001,
    Nasdaq = 0b_0000_0010,
    Dow = 0b_0000_0100
}