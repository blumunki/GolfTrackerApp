using System.ComponentModel.DataAnnotations;

namespace GolfTrackerApp.Web.Models;

public class ApplicationSetting
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Key { get; set; } = string.Empty;

    [StringLength(500)]
    public string Value { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Description { get; set; }

    [StringLength(50)]
    public string Category { get; set; } = "General";

    /// <summary>
    /// The data type of the value: "bool", "int", or "string"
    /// </summary>
    [StringLength(20)]
    public string ValueType { get; set; } = "string";

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(450)]
    public string? UpdatedByUserId { get; set; }
}
