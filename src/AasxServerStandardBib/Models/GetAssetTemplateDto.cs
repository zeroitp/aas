namespace AasxServerStandardBib.Models;
using System;
using System.Collections.Generic;
using AasxServerDB.Helpers;

public class GetAssetTemplateDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string NormalizeName => Name.NormalizeAHIName();
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
    public int AssetCount { get; set; }
    public IEnumerable<GetAssetAttributeTemplateSimpleDto> Attributes { get; set; } = new List<GetAssetAttributeTemplateSimpleDto>();
    public string LockedByUpn { get; set; }
    public string CreatedBy { get; set; }
}