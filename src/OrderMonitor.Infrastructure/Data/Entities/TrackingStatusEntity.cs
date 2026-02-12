using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderMonitor.Infrastructure.Data.Entities;

/// <summary>
/// Entity mapping for the luk_Tracking_Status table.
/// </summary>
[Table("luk_Tracking_Status")]
public class TrackingStatusEntity
{
    [Key]
    [Column("Tracking_Status_id")]
    public int TrackingStatusId { get; set; }

    [Column("Tracking_Status_Name")]
    [StringLength(100)]
    public string? TrackingStatusName { get; set; }
}
