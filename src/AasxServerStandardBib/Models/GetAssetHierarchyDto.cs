namespace AasxServerStandardBib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using AasxServerDB.Helpers;
using AHI.Infrastructure.SharedKernel.Model;

public class GetAssetHierarchy : BaseCriteria
{
    public string AssetName { get; set; }
}

public class GetAssetHierarchyDto
{
    public string AssetName { get; set; }
    public Guid Id { get; set; }
    public string Name { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime RootAssetCreatedUtc { get; set; }
    public bool HasWarning { get; set; }
    public Guid? AssetTemplateId { get; set; }
    public int RetentionDays { get; set; }
    public string NormalizeName => Name.NormalizeAHIName();
    public IEnumerable<AssetHierarchyDto> Hierarchy { get; set; }

    static Func<AssetHierarchy, GetAssetHierarchyDto> Converter = Projection.Compile();

    private static Expression<Func<AssetHierarchy, GetAssetHierarchyDto>> Projection
    {
        get
        {
            return entity => new GetAssetHierarchyDto
            {
                Id = entity.AssetId,
                Name = entity.AssetName,
                CreatedUtc = entity.AssetCreatedUtc,
                RootAssetCreatedUtc = GetRootAssetCreatedDate(entity),
                HasWarning = entity.AssetHasWarning,
                AssetTemplateId = entity.AssetTemplateId,
                RetentionDays = entity.AssetRetentionDays,
                Hierarchy = entity.Hierarchy.Select(AssetHierarchyDto.Create)
            };
        }
    }

    private static DateTime GetRootAssetCreatedDate(AssetHierarchy entity)
    {
        return entity.Hierarchy.FirstOrDefault()?.CreatedUtc ?? entity.AssetCreatedUtc;
    }

    public static GetAssetHierarchyDto Create(AssetHierarchy entity)
    {
        if (entity == null)
        {
            return null;
        }

        return Converter(entity);
    }
}

public class AssetHierarchyDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public DateTime CreatedUtc { get; set; }
    public Guid? ParentAssetId { get; set; }
    public bool HasWarning { get; set; }
    public Guid? AssetTemplateId { get; set; }
    public int RetentionDays { get; set; }
    public string NormalizeName => Name.NormalizeAHIName();

    static Func<Hierarchy, AssetHierarchyDto> Converter = Projection.Compile();

    private static Expression<Func<Hierarchy, AssetHierarchyDto>> Projection
    {
        get
        {
            return entity => new AssetHierarchyDto
            {
                Id = entity.Id,
                Name = entity.Name,
                HasWarning = entity.HasWarning,
                ParentAssetId = entity.ParentAssetId,
                AssetTemplateId = entity.TemplateId,
                RetentionDays = entity.RetentionDays,
                CreatedUtc = entity.CreatedUtc
            };
        }
    }

    public static AssetHierarchyDto Create(Hierarchy entity)
    {
        if (entity == null)
        {
            return null;
        }

        return Converter(entity);
    }
}

public class Hierarchy
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public bool HasWarning { get; set; }
    public Guid? ParentAssetId { get; set; }
    public Guid? TemplateId { get; set; }
    public DateTime CreatedUtc { get; set; }
    public int RetentionDays { get; set; }

    public static Hierarchy From(AssetHierarchy asset)
    {
        return new Hierarchy
        {
            CreatedUtc = asset.AssetCreatedUtc,
            HasWarning = asset.AssetHasWarning,
            Id = asset.AssetId,
            Name = asset.AssetName,
            ParentAssetId = asset.ParentAssetId,
            RetentionDays = asset.AssetRetentionDays,
            TemplateId = asset.AssetTemplateId
        };
    }
}

public class AssetHierarchy
{
    public string AssetName { get; set; }
    public Guid AssetId { get; set; }
    public DateTime AssetCreatedUtc { get; set; }
    public bool AssetHasWarning { get; set; }
    public Guid? AssetTemplateId { get; set; }
    public int AssetRetentionDays { get; set; }
    public ICollection<Hierarchy> Hierarchy { get; set; }
    public Guid? ParentAssetId { get; set; }
    public bool IsFoundResult { get; set; }
    public AssetHierarchy()
    {
        Hierarchy = new List<Hierarchy>();
    }
}