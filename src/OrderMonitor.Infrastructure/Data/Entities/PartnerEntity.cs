using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderMonitor.Infrastructure.Data.Entities;

/// <summary>
/// Entity mapping for the Partner_Master table.
/// </summary>
[Table("Partner_Master")]
public class PartnerEntity
{
    [Key]
    [Column("PartnerID")]
    public int PartnerId { get; set; }

    [Column("PartnerDisplayName")]
    [StringLength(100)]
    public string? PartnerDisplayName { get; set; }

    [Column("IsActive")]
    public bool IsActive { get; set; }
}
