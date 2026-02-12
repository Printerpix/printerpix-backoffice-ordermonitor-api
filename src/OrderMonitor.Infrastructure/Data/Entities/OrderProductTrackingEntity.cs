using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderMonitor.Infrastructure.Data.Entities;

/// <summary>
/// Entity mapping for the OrderProductTracking table.
/// </summary>
[Table("OrderProductTracking")]
public class OrderProductTrackingEntity
{
    [Key]
    [Column("OPT_ID")]
    public long Id { get; set; }

    [Column("CONumber")]
    [StringLength(50)]
    public string CONumber { get; set; } = string.Empty;

    [Column("Status")]
    public int Status { get; set; }

    [Column("lastUpdatedDate")]
    public DateTime? LastUpdatedDate { get; set; }

    [Column("isPrimaryComponent")]
    public bool IsPrimaryComponent { get; set; }

    [Column("TPartnerCode")]
    public int? TPartnerCode { get; set; }

    [Column("OPT_SnSpId")]
    public int? OptSnSpId { get; set; }

    [Column("OrderDate")]
    public DateTime? OrderDate { get; set; }

    // Navigation properties
    [ForeignKey("CONumber")]
    public ConsolidationOrderEntity? ConsolidationOrder { get; set; }

    [ForeignKey("Status")]
    public TrackingStatusEntity? TrackingStatus { get; set; }

    [ForeignKey("OptSnSpId")]
    public SnSpecificationEntity? SnSpecification { get; set; }

    [ForeignKey("TPartnerCode")]
    public PartnerEntity? Partner { get; set; }
}
