using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Models;

[Index(nameof(Ticker), IsUnique = false)]
public class SelectedTicker
{
    [Required]
    public int Id { get; set; }

    [MaxLength(8), Required]
    public string Ticker { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public double Close { get; set; }
    public double AnnualPercentGain { get; set; }
    public double HalfYearlyPercentGain { get; set; }
    public double QuarterYearlyPercentGain { get; set; }
    public DateTime LastUpdated { get; set; }
    public string? CompanyName { get; set; }
}