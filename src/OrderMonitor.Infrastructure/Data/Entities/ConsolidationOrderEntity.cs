using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderMonitor.Infrastructure.Data.Entities;

/// <summary>
/// Entity mapping for the ConsolidationOrder table.
/// </summary>
[Table("ConsolidationOrder")]
public class ConsolidationOrderEntity
{
    [Key]
    [Column("CONumber")]
    [StringLength(50)]
    public string CONumber { get; set; } = string.Empty;

    [Column("orderNumber")]
    [StringLength(50)]
    public string? OrderNumber { get; set; }

    [Column("websiteCode")]
    [StringLength(20)]
    public string? WebsiteCode { get; set; }

    public ICollection<OrderProductTrackingEntity> OrderProductTrackings { get; set; } = [];
}
