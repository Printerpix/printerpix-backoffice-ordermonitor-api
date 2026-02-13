using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderMonitor.Infrastructure.Data.Entities;

/// <summary>
/// Entity mapping for the mas_SnSpecification table.
/// </summary>
[Table("mas_SnSpecification")]
public class SnSpecificationEntity
{
    [Key]
    [Column("SnID")]
    public int SnId { get; set; }

    [Column("MasterProductTypeID")]
    public int? MasterProductTypeId { get; set; }

    [ForeignKey("MasterProductTypeId")]
    public MajorProductTypeEntity? MajorProductType { get; set; }
}
