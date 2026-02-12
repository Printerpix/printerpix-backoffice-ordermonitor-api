using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderMonitor.Infrastructure.Data.Entities;

/// <summary>
/// Entity mapping for the luk_MajorProductType table.
/// </summary>
[Table("luk_MajorProductType")]
public class MajorProductTypeEntity
{
    [Key]
    [Column("MProductTypeID")]
    public int MProductTypeId { get; set; }

    [Column("MajorProductTypeName")]
    [StringLength(100)]
    public string? MajorProductTypeName { get; set; }
}
