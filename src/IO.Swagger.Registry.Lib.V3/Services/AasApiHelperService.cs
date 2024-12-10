namespace IO.Swagger.Registry.Lib.V3.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AasxServerStandardBib.Models;
using AasxServerStandardBib.Utils;
using Extensions;
using IO.Swagger.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AHI.Infrastructure.Exception.Helper;
using AasxServerDB.Entities;
using IO.Swagger.Registry.Lib.V3.Interfaces;
using System.Diagnostics;
using AasxServerDB;
using Microsoft.EntityFrameworkCore;
using AasxServerDB.Repositories;
using AasxServerDB.Dto;
using I.Swagger.Registry.Lib.V3.Services;
using AasxServerDB.Helpers;
using IO.Swagger.Registry.Lib.V3.Models;

public class AasApiHelperService(IAasRegistryService aasRegistryService,
    TimeSeriesService timeSeriesService, IAASUnitOfWork unitOfWork)
{
    public async Task<(IAssetAdministrationShell Aas, string SubmodelId, ISubmodelElement Sme)> GetAliasSme(IReferenceElement reference)
    {
        IAssetAdministrationShell aliasAas = null;
        var aasId = reference?.Value?.Keys[0]?.Value;
        if (!string.IsNullOrEmpty(aasId))
        {
            aliasAas = await GetAASById(aasId);
        }

        var smIdRaw = reference?.Value?.Keys[1]?.Value;
        ISubmodelElement aliasSme = reference;

        if (!string.IsNullOrEmpty(smIdRaw))
        {
            var smeIdPath = reference.Value.Keys[2].Value;
            aliasSme = await aasRegistryService.GetSubmodelElementByPathSubmodelRepo(smIdRaw, smeIdPath, LevelEnum.Deep, ExtentEnum.WithoutBlobValue) as ISubmodelElement;
        }

        return (aliasAas, smIdRaw, aliasSme);
    }

    public async Task<(AASSet AasIdShort, SMESet Sme)> GetAlias(IReferenceElement reference)
    {
        var aasId = reference?.Value?.Keys[0]?.Value;
        var smeId = reference?.Value?.Keys[2]?.Value;

        SMESet sMESet = null;

        if (!string.IsNullOrEmpty(aasId) && !string.IsNullOrEmpty(smeId))
        {
            //aASSet = await db.AASSets.Where(x => x.IdShort == aasId).AsNoTracking().FirstOrDefaultAsync();
            sMESet = await unitOfWork.SMESets.AsFetchable().Where(x => x.IdShort == smeId).Include(x => x.SMSet).
                ThenInclude(x => x.AASSet).AsNoTracking().FirstOrDefaultAsync();
        }

        return (sMESet?.SMSet?.AASSet, sMESet);
    }

    public async Task<(IAssetAdministrationShell Aas, string SubmodelId, ISubmodelElement Sme, string AliasPath)> GetRootAliasSme(IReferenceElement reference)
    {
        var pathMemberIds = new List<string>();
        var (aliasAas, smId, aliasSme) = await GetAliasSme(reference);
        if (!string.IsNullOrEmpty(aliasSme.IdShort))
        {
            pathMemberIds.Add(aliasSme.IdShort);
        }

        while (aliasSme is IReferenceElement refElement && refElement.Value != null)
        {
            (aliasAas, smId, aliasSme) = await GetAliasSme(refElement);
            pathMemberIds.Add(aliasSme.IdShort);
        }

        return (aliasAas, smId, aliasSme, string.Join('|', pathMemberIds));
    }

    public async Task<ISubmodelElement> GetRootAliasFromExtension(IReferenceElement reference)
    {
        if (reference.Extensions != null && reference.Extensions.Count > 0)
        {
            //var rootAssId = GetExtensionValue(reference.Extensions, "RootAasIdShort");
            var rootSmId = GetExtensionValue(reference.Extensions, "RootSmId");
            var rootAttributeId = GetExtensionValue(reference.Extensions, "RootSmeIdShort");
            var aliasPath = GetExtensionValue(reference.Extensions, "AliasPath");

            if (!string.IsNullOrEmpty(rootAttributeId))
            {
                return await aasRegistryService.GetSubmodelElementByPathSubmodelRepo(rootSmId, rootAttributeId, LevelEnum.Deep, ExtentEnum.WithoutBlobValue) as ISubmodelElement;
            }
            else if (!string.IsNullOrEmpty(aliasPath))
            {
                var arr = aliasPath.Split('|');
                if (arr.Length > 0)
                {
                    var lastIdShort = arr[arr.Length - 1];
                    if (Guid.TryParse(lastIdShort, out var aliasId))
                    {
                        var (submodel, aliasSme) = await aasRegistryService.FindSmeByGuid(aliasId);
                        return aliasSme;
                    }
                }
            }
        }

        return null;
    }

    public async Task<SMESet> GetSMERootAliasFromExtension(IReferenceElement reference, List<SMESet> listRootSMEs)
    {
        if (reference.Extensions != null && reference.Extensions.Count > 0)
        {
            var rootAssId = GetExtensionValue(reference.Extensions, "RootAasIdShort");
            var smId = GetExtensionValue(reference.Extensions, "RootSmId");
            var rootAttributeId = GetExtensionValue(reference.Extensions, "RootSmeIdShort");
            var aliasPath = GetExtensionValue(reference.Extensions, "AliasPath");

            if (!string.IsNullOrEmpty(rootAttributeId))
            {
                return listRootSMEs
                    .Where(x => x.IdShort == rootAttributeId && x.AASIdShort == rootAssId).FirstOrDefault();
            }
        }

        return null;
    }

    public GetAssetSimpleDto ToGetAssetSimpleDto(IAssetAdministrationShell aas,
        IAssetAdministrationShell? parent = null,
        IEnumerable<AssetAttributeDto>? attributes = null)
    {
        return new GetAssetSimpleDto()
        {
            AssetTemplateId = Guid.TryParse(aas.Administration?.TemplateId, out var templateId) ? templateId : null, // TODO
            AssetTemplateName = null, // TODO
            Name = aas.DisplayName?.FirstOrDefault()?.Text ?? aas.IdShort,
            Attributes = attributes,
            Children = [],
            CreatedBy = aas.Administration?.Creator?.GetAsExactlyOneKey()?.Value, // TODO
            CreatedUtc = aas.TimeStampCreate,
            CurrentTimestamp = DateTime.UtcNow,
            CurrentUserUpn = null,
            HasWarning = false, // TODO
            UpdatedUtc = aas.TimeStamp,
            RetentionDays = -1, // TODO,
            Id = Guid.TryParse(aas.Id, out var id) ? id : default,
            IsDocument = false, // TODO
            ParentAssetId = Guid.TryParse(aas.Extensions?.FirstOrDefault(e => e.Name == "ParentAssetId")?.Value, out var pId) ? pId : null,
            Parent = parent != null ? ToGetAssetSimpleDto(aas: parent, parent: null, attributes: null) : null,
            ResourcePath = aas.Extensions?.FirstOrDefault(e => e.Name == "ResourcePath")?.Value,
            RequestLockTimeout = null, // TODO
            RequestLockTimestamp = null, // TODO
            RequestLockUserUpn = null, // TODO
        };
    }

    public async Task<AssetAttributeDto> ToAssetAttributeDto(ISubmodelElement sme, Guid aasIdShort)
    {
        if (sme == null)
        {
            return null;
        }

        string templateAttributeId = string.Empty;
        if (sme.Extensions != null)
        {
            templateAttributeId = sme.Extensions.Where(x => x.Name == "TemplateAttributeId").FirstOrDefault()?.Value;
        }

        switch (sme.Category)
        {
            case AttributeTypeConstants.TYPE_STATIC:
            {
                var pStatic = sme as IProperty;
                var snapshotId = Guid.Parse(pStatic.IdShort);
                var dataType = MappingHelper.ToAhiDataType(pStatic.ValueType);
                return new AssetAttributeDto
                {
                    AssetId = aasIdShort,
                    AttributeType = pStatic.Category,
                    CreatedUtc = pStatic.TimeStampCreate,
                    DataType = dataType,
                    DecimalPlace = null, // TODO
                    Deleted = false,
                    Id = snapshotId,
                    Name = pStatic.DisplayName.FirstOrDefault()?.Text ?? pStatic.IdShort,
                    SequentialNumber = -1, // TODO
                    Value = pStatic?.Value.ParseValueWithDataType(dataType, pStatic.Value, isRawData: false),
                    Payload = JObject.FromObject(new
                    {
                        templateAttributeId,
                        id = snapshotId,
                        value = pStatic.Value
                    }).ToObject<AttributeMapping>(),
                    ThousandSeparator = null, // TODO
                    Uom = null, // TODO
                    UomId = null, // TODO
                    UpdatedUtc = pStatic.TimeStamp
                };
            }
            case AttributeTypeConstants.TYPE_ALIAS:
            {
                var rAlias = sme as IReferenceElement;
                var refId = Guid.Parse(rAlias.IdShort);
                var rootSme = await GetRootAliasFromExtension(rAlias);

                var (aliasAAS, aliasSme) = await GetAlias(rAlias);

                var aliasDto = new AssetAttributeDto();
                if (rootSme != null)
                {
                    if (rootSme is IReferenceElement refElement)
                    {
                        if (refElement.Value != null)
                        {
                            aliasDto = await ToAssetAttributeDto(rootSme, aasIdShort);
                        }
                    }
                    else
                    {
                        aliasDto = await ToAssetAttributeDto(rootSme, aasIdShort);
                        if (aliasDto is null)
                            return null;
                    }
                }

                var data = new AssetAttributeDto
                {
                    AssetId = aasIdShort,
                    AttributeType = rAlias.Category,
                    CreatedUtc = rAlias.TimeStampCreate,
                    DataType = aliasDto.DataType,
                    DecimalPlace = aliasDto.DecimalPlace,
                    Deleted = false,
                    Id = refId,
                    Name = rAlias.DisplayName.FirstOrDefault()?.Text ?? rAlias.IdShort,
                    SequentialNumber = -1, // TODO
                    Value = aliasDto.Value,
                    ThousandSeparator = aliasDto.ThousandSeparator,
                    Uom = aliasDto.Uom,
                    UomId = aliasDto.UomId,
                    UpdatedUtc = rAlias.TimeStamp
                };

                if (aliasAAS != null && aliasSme != null)
                {
                    data.Payload = JObject.FromObject(new
                    {
                        id = refId,
                        aliasAssetId = aliasAAS?.IdShort,
                        aliasAttributeId = aliasSme.IdShort,
                        aliasAssetName = aliasAAS.Name ?? aliasAAS?.IdShort,
                        aliasAttributeName = aliasSme?.Name ?? aliasSme?.IdShort,
                        templateAttributeId
                    }).ToObject<AttributeMapping>();
                }

                return data;
            }
            case AttributeTypeConstants.TYPE_RUNTIME:
            {
                var smcRuntime = sme as ISubmodelElementCollection;
                var runtimeId = Guid.Parse(smcRuntime.IdShort);
                var triggerAttributeIdStr = smcRuntime.GetExtensionValue("TriggerAttributeId");

                var triggerAttributeIds = smcRuntime.GetExtensionValue("TriggerAttributeIds");
                var enabledExpression = smcRuntime.GetExtensionValue("EnabledExpression");
                var expression = smcRuntime.GetExtensionValue("Expression");
                var expressionCompile = smcRuntime.GetExtensionValue("ExpressionCompile");

                Guid? triggerAttributeId = !string.IsNullOrEmpty(triggerAttributeIdStr) ? Guid.Parse(triggerAttributeIdStr) : null;
                var triggerAssetId = aasIdShort;
                bool? hasTriggerError = null;
                if (triggerAttributeId != null)
                {
                    var triggers = JsonConvert.DeserializeObject<IEnumerable<Guid>>(triggerAttributeIds);
                    var triggerAssetAttributeExists = triggers.Contains(triggerAttributeId.Value);
                    if (!triggerAssetAttributeExists)
                        hasTriggerError = true;
                }
                AttributeMapping payload = JObject.FromObject(new
                {
                    id = runtimeId,
                    enabledExpression = bool.TryParse(enabledExpression, out var enabled) && enabled,
                    expression,
                    expressionCompile,
                    triggerAssetId,
                    triggerAttributeId,
                    hasTriggerError,
                    templateAttributeId,
                }).ToObject<AttributeMapping>();

                //var snapshotSeries = await timeSeriesService.GetSnapshot(triggerAssetId, runtimeId);
                var snapshotSeries = smcRuntime.GetSnapshotTimeSeries();
                var snapshotValue = snapshotSeries?.v;
                var dataType = smcRuntime.GetDataType();
                return new AssetAttributeDto
                {
                    AssetId = aasIdShort,
                    AttributeType = smcRuntime.Category,
                    CreatedUtc = smcRuntime.TimeStampCreate,
                    DataType = dataType,
                    DecimalPlace = null, // TODO
                    Deleted = false,
                    Id = runtimeId,
                    Name = smcRuntime.DisplayName.FirstOrDefault()?.Text ?? smcRuntime.IdShort,
                    SequentialNumber = -1, // TODO
                    Value = snapshotValue?.ParseValueWithDataType(dataType, $"{snapshotValue}", isRawData: false),
                    Payload = payload,
                    ThousandSeparator = null, // TODO
                    Uom = null, // TODO
                    UomId = null, // TODO
                    UpdatedUtc = smcRuntime.TimeStamp
                };
            }
            case AttributeTypeConstants.TYPE_DYNAMIC:
            {
                var smcDynamic = sme as ISubmodelElementCollection;
                var dataType = smcDynamic.GetDataType();
                var dynamicId = Guid.Parse(smcDynamic.IdShort);
                var deviceId = smcDynamic.GetExtensionValue("DeviceId");
                var metricKey = smcDynamic.GetExtensionValue("MetricKey");
                var snapshotSeries = await timeSeriesService.GetDeviceMetricSnapshot(deviceId, metricKey);

                return new AssetAttributeDto
                {
                    AssetId = aasIdShort,
                    AttributeType = smcDynamic.Category,
                    CreatedUtc = smcDynamic.TimeStampCreate,
                    DataType = dataType,
                    DecimalPlace = null, // TODO
                    Deleted = false,
                    Id = dynamicId,
                    Name = smcDynamic.DisplayName.FirstOrDefault()?.Text ?? smcDynamic.IdShort,
                    SequentialNumber = -1, // TODO
                    Value = snapshotSeries?.v,
                    Payload = JObject.FromObject(new
                    {
                        id = dynamicId,
                        deviceId,
                        metricKey,
                        templateAttributeId
                    }).ToObject<AttributeMapping>(),
                    ThousandSeparator = null, // TODO
                    Uom = null, // TODO
                    UomId = null, // TODO
                    UpdatedUtc = smcDynamic.TimeStamp
                };
            }
            case AttributeTypeConstants.TYPE_COMMAND:
            {
                var smcCommand = sme as ISubmodelElementCollection;
                var dataType = smcCommand.GetDataType();
                var dynamicId = Guid.Parse(smcCommand.IdShort);
                var deviceId = smcCommand.GetExtensionValue("DeviceId");
                var metricKey = smcCommand.GetExtensionValue("MetricKey");

                var snapshotSeries = await timeSeriesService.GetDeviceMetricSnapshot(deviceId, metricKey);
                return new AssetAttributeDto
                {
                    AssetId = aasIdShort,
                    AttributeType = smcCommand.Category,
                    CreatedUtc = smcCommand.TimeStampCreate,
                    DataType = dataType,
                    DecimalPlace = null, // TODO
                    Deleted = false,
                    Id = dynamicId,
                    Name = smcCommand.DisplayName.FirstOrDefault()?.Text ?? smcCommand.IdShort,
                    SequentialNumber = -1, // TODO
                    Value = snapshotSeries?.v,
                    Payload = JObject.FromObject(new
                    {
                        id = dynamicId,
                        deviceId,
                        metricKey,
                        templateAttributeId
                    }).ToObject<AttributeMapping>(),
                    ThousandSeparator = null, // TODO
                    Uom = null, // TODO
                    UomId = null, // TODO
                    UpdatedUtc = smcCommand.TimeStamp
                };
            }
            default:
                return null;
        }
    }

    public async Task<AttributeDto> ToAttributeDto(ISubmodelElement sme, string assetId)
    {
        switch (sme.Category)
        {
            case AttributeTypeConstants.TYPE_STATIC:
            {
                var pStatic = sme as IProperty;
                var dataType = MappingHelper.ToAhiDataType(pStatic.ValueType);
                var tsDto = TimeSeriesHelper.BuildSeriesDto(
                    value: pStatic.Value.ParseValueWithDataType(dataType, pStatic.Value, isRawData: false),
                    timestamp: null // [NOTE] TimeStamp is not serialized
                );
                var tsList = new List<TimeSeriesDto>() { tsDto };
                return new AttributeDto
                {
                    GapfillFunction = PostgresFunction.TIME_BUCKET_GAPFILL,
                    Quality = tsDto.q.GetQualityName(),
                    QualityCode = tsDto.q,
                    AttributeId = Guid.Parse(pStatic.IdShort),
                    AttributeName = pStatic.DisplayName.FirstOrDefault()?.Text ?? pStatic.IdShort,
                    AttributeType = pStatic.Category,
                    Uom = null, // TODO
                    DecimalPlace = null, // TODO
                    ThousandSeparator = null, // TODO
                    Series = tsList,
                    DataType = dataType
                };
            }
            case AttributeTypeConstants.TYPE_ALIAS:
            {
                var rAlias = sme as IReferenceElement;

                var aasIdShort = GetExtensionValue(rAlias.Extensions, "RootAasIdShort");
                var aliasSmeIdShort = GetExtensionValue(rAlias.Extensions, "RootSmeIdShort");
                var aliasPath = GetExtensionValue(rAlias.Extensions, "AliasPath");

                var aliasAttrDto = new AttributeDto();

                if (!string.IsNullOrEmpty(aasIdShort) && !string.IsNullOrEmpty(aliasSmeIdShort))
                {
                    var (_, aliasSme) = await aasRegistryService.FindSmeByGuid(Guid.Parse(aliasSmeIdShort));
                    aliasAttrDto = await ToAttributeDto(aliasSme, aasIdShort);
                    if (aliasAttrDto is null)
                        return null;
                }
                else if (!string.IsNullOrEmpty(aliasPath))
                {
                    var arr = aliasPath.Split('|');
                    if (arr.Length > 0)
                    {
                        var lastIdShort = arr[arr.Length - 1];
                        if (Guid.TryParse(lastIdShort, out Guid aliasId))
                        {
                            var (aasId, aliasSme) = await aasRegistryService.FindSmeByGuid(aliasId);
                            aliasAttrDto = await ToAttributeDto(aliasSme, aasId);
                        }
                    }
                }

                return new AttributeDto
                {
                    GapfillFunction = aliasAttrDto.GapfillFunction,
                    Quality = aliasAttrDto.Quality,
                    QualityCode = aliasAttrDto.QualityCode,
                    AttributeId = Guid.Parse(rAlias.IdShort),
                    AttributeType = rAlias.Category,
                    Uom = aliasAttrDto.Uom,
                    DecimalPlace = aliasAttrDto.DecimalPlace,
                    ThousandSeparator = aliasAttrDto.ThousandSeparator,
                    Series = aliasAttrDto.Series,
                    DataType = aliasAttrDto.DataType
                };
            }
            case AttributeTypeConstants.TYPE_RUNTIME:
            {
                var smcRuntime = sme as ISubmodelElementCollection;
                var tsList = new List<TimeSeriesDto>();
                //var snapshotSeries = await timeSeriesService.GetSnapshot(Guid.Parse(assetId), Guid.Parse(smcRuntime.IdShort));
                var snapshotSeries = smcRuntime.GetSnapshotTimeSeries();
                var dataType = smcRuntime.GetDataType();
                if (snapshotSeries is not null && snapshotSeries.v != null)
                    tsList.Add(snapshotSeries);

                return new AttributeDto
                {
                    GapfillFunction = PostgresFunction.TIME_BUCKET_GAPFILL,
                    Quality = snapshotSeries?.q.GetQualityName(),
                    QualityCode = snapshotSeries?.q,
                    AttributeId = Guid.Parse(smcRuntime.IdShort),
                    AttributeName = smcRuntime.DisplayName.FirstOrDefault()?.Text ?? smcRuntime.IdShort,
                    AttributeType = smcRuntime.Category,
                    Uom = null, // TODO
                    DecimalPlace = null, // TODO
                    ThousandSeparator = null, // TODO
                    Series = tsList,
                    DataType = dataType
                };
            }
            case AttributeTypeConstants.TYPE_DYNAMIC:
            {
                var smcDynamic = sme as ISubmodelElementCollection;
                var tsList = new List<TimeSeriesDto>();
                var dataType = smcDynamic.GetDataType();

                var deviceId = smcDynamic.GetExtensionValue("DeviceId");
                var metricKye = smcDynamic.GetExtensionValue("MetricKey");
                var snapshotSeries = await timeSeriesService.GetDeviceMetricSnapshot(deviceId, metricKye);

                if (snapshotSeries is not null && snapshotSeries.v != null)
                    tsList.Add(snapshotSeries);

                return new AttributeDto
                {
                    GapfillFunction = PostgresFunction.TIME_BUCKET_GAPFILL,
                    Quality = snapshotSeries?.q.GetQualityName(),
                    QualityCode = snapshotSeries?.q,
                    AttributeId = Guid.Parse(smcDynamic.IdShort),
                    AttributeName = smcDynamic.DisplayName.FirstOrDefault()?.Text ?? smcDynamic.IdShort,
                    AttributeType = smcDynamic.Category,
                    Uom = null, // TODO
                    DecimalPlace = null, // TODO
                    ThousandSeparator = null, // TODO
                    Series = tsList,
                    DataType = dataType
                };
            }
            case AttributeTypeConstants.TYPE_COMMAND:
            {
                var smcCommand = sme as ISubmodelElementCollection;
                var tsList = new List<TimeSeriesDto>();

                //var snapshotSeries = await timeSeriesService.GetSnapshot(Guid.Parse(assetId), Guid.Parse(smcCommand.IdShort));
                var snapshotSeries = smcCommand.GetSnapshotTimeSeries();
                var dataType = smcCommand.GetDataType();
                if (snapshotSeries is not null && snapshotSeries.v != null)
                    tsList.Add(snapshotSeries);

                return new AttributeDto
                {
                    GapfillFunction = PostgresFunction.TIME_BUCKET_GAPFILL,
                    Quality = snapshotSeries?.q.GetQualityName(),
                    QualityCode = snapshotSeries?.q,
                    AttributeId = Guid.Parse(smcCommand.IdShort),
                    AttributeName = smcCommand.DisplayName.FirstOrDefault()?.Text ?? smcCommand.IdShort,
                    AttributeType = smcCommand.Category,
                    Uom = null, // TODO
                    DecimalPlace = null, // TODO
                    ThousandSeparator = null, // TODO
                    Series = tsList,
                    DataType = dataType,
                    AttributeNameNormalize = string.Empty, //TODO

                };
            }
            default:
                return null;
        }
    }

    public async Task<AttributeDto> ToAttributeDto(SMESet sMESet)
    {
        if (sMESet == null)
        {
            return null;
        }

        ISubmodelElement sme = Converter.CreateSME(sMESet);

        switch (sme.Category)
        {
            case AttributeTypeConstants.TYPE_STATIC:
            {
                var pStatic = sme as IProperty;
                var dataType = MappingHelper.ToAhiDataType(pStatic.ValueType);
                var tsDto = TimeSeriesHelper.BuildSeriesDto(
                    value: pStatic.Value.ParseValueWithDataType(dataType, pStatic.Value, isRawData: false),
                    timestamp: null // [NOTE] TimeStamp is not serialized
                );
                var tsList = new List<TimeSeriesDto>() { tsDto };
                return new AttributeDto
                {
                    GapfillFunction = PostgresFunction.TIME_BUCKET_GAPFILL,
                    Quality = tsDto.q.GetQualityName(),
                    QualityCode = tsDto.q,
                    AttributeId = Guid.Parse(pStatic.IdShort),
                    AttributeName = pStatic.DisplayName.FirstOrDefault()?.Text ?? pStatic.IdShort,
                    AttributeType = pStatic.Category,
                    Uom = null, // TODO
                    DecimalPlace = null, // TODO
                    ThousandSeparator = null, // TODO
                    Series = tsList,
                    DataType = dataType
                };
            }
            case AttributeTypeConstants.TYPE_ALIAS:
            {
                var rAlias = sme as IReferenceElement;

                var aasIdShort = GetExtensionValue(rAlias.Extensions, "RootAasIdShort");
                var aliasSmeIdShort = GetExtensionValue(rAlias.Extensions, "RootSmeIdShort");
                var aliasPath = GetExtensionValue(rAlias.Extensions, "AliasPath");

                var aliasAttrDto = new AttributeDto();

                if (!string.IsNullOrEmpty(aasIdShort) && !string.IsNullOrEmpty(aliasSmeIdShort))
                {
                    var (_, aliasSme) = await aasRegistryService.FindSmeByGuid(Guid.Parse(aliasSmeIdShort));
                    aliasAttrDto = await ToAttributeDto(aliasSme, aasIdShort);
                    if (aliasAttrDto is null)
                        return null;
                }
                else if (!string.IsNullOrEmpty(aliasPath))
                {
                    var arr = aliasPath.Split('|');
                    if (arr.Length > 0)
                    {
                        var lastIdShort = arr[arr.Length - 1];
                        if (Guid.TryParse(lastIdShort, out Guid aliasId))
                        {
                            var (aasId, aliasSme) = await aasRegistryService.FindSmeByGuid(aliasId);
                            aliasAttrDto = await ToAttributeDto(aliasSme, aasId);
                        }
                    }
                }

                return new AttributeDto
                {
                    GapfillFunction = aliasAttrDto.GapfillFunction,
                    Quality = aliasAttrDto.Quality,
                    QualityCode = aliasAttrDto.QualityCode,
                    AttributeId = Guid.Parse(rAlias.IdShort),
                    AttributeType = rAlias.Category,
                    Uom = aliasAttrDto.Uom,
                    DecimalPlace = aliasAttrDto.DecimalPlace,
                    ThousandSeparator = aliasAttrDto.ThousandSeparator,
                    Series = aliasAttrDto.Series,
                    DataType = aliasAttrDto.DataType
                };
            }
            case AttributeTypeConstants.TYPE_RUNTIME:
            {
                var smcRuntime = sme as ISubmodelElementCollection;
                var tsList = new List<TimeSeriesDto>();
                //var snapshotSeries = await timeSeriesService.GetSnapshot(Guid.Parse(assetId), Guid.Parse(smcRuntime.IdShort));
                var snapshotSeries = smcRuntime.GetSnapshotTimeSeries();
                var dataType = smcRuntime.GetDataType();
                if (snapshotSeries is not null && snapshotSeries.v != null)
                    tsList.Add(snapshotSeries);

                return new AttributeDto
                {
                    GapfillFunction = PostgresFunction.TIME_BUCKET_GAPFILL,
                    Quality = snapshotSeries?.q.GetQualityName(),
                    QualityCode = snapshotSeries?.q,
                    AttributeId = Guid.Parse(smcRuntime.IdShort),
                    AttributeName = smcRuntime.DisplayName.FirstOrDefault()?.Text ?? smcRuntime.IdShort,
                    AttributeType = smcRuntime.Category,
                    Uom = null, // TODO
                    DecimalPlace = null, // TODO
                    ThousandSeparator = null, // TODO
                    Series = tsList,
                    DataType = dataType
                };
            }
            case AttributeTypeConstants.TYPE_DYNAMIC:
            {
                var smcDynamic = sme as ISubmodelElementCollection;
                var tsList = new List<TimeSeriesDto>();
                var dataType = smcDynamic.GetDataType();

                var deviceId = smcDynamic.GetExtensionValue("DeviceId");
                var metricKye = smcDynamic.GetExtensionValue("MetricKey");
                var snapshotSeries = await timeSeriesService.GetDeviceMetricSnapshot(deviceId, metricKye);

                if (snapshotSeries is not null && snapshotSeries.v != null)
                    tsList.Add(snapshotSeries);

                return new AttributeDto
                {
                    GapfillFunction = PostgresFunction.TIME_BUCKET_GAPFILL,
                    Quality = snapshotSeries?.q.GetQualityName(),
                    QualityCode = snapshotSeries?.q,
                    AttributeId = Guid.Parse(smcDynamic.IdShort),
                    AttributeName = smcDynamic.DisplayName.FirstOrDefault()?.Text ?? smcDynamic.IdShort,
                    AttributeType = smcDynamic.Category,
                    Uom = null, // TODO
                    DecimalPlace = null, // TODO
                    ThousandSeparator = null, // TODO
                    Series = tsList,
                    DataType = dataType
                };
            }
            case AttributeTypeConstants.TYPE_COMMAND:
            {
                var smcCommand = sme as ISubmodelElementCollection;
                var tsList = new List<TimeSeriesDto>();

                //var snapshotSeries = await timeSeriesService.GetSnapshot(Guid.Parse(assetId), Guid.Parse(smcCommand.IdShort));
                var snapshotSeries = smcCommand.GetSnapshotTimeSeries();
                var dataType = smcCommand.GetDataType();
                if (snapshotSeries is not null && snapshotSeries.v != null)
                    tsList.Add(snapshotSeries);

                return new AttributeDto
                {
                    GapfillFunction = PostgresFunction.TIME_BUCKET_GAPFILL,
                    Quality = snapshotSeries?.q.GetQualityName(),
                    QualityCode = snapshotSeries?.q,
                    AttributeId = Guid.Parse(smcCommand.IdShort),
                    AttributeName = smcCommand.DisplayName.FirstOrDefault()?.Text ?? smcCommand.IdShort,
                    AttributeType = smcCommand.Category,
                    Uom = null, // TODO
                    DecimalPlace = null, // TODO
                    ThousandSeparator = null, // TODO
                    Series = tsList,
                    DataType = dataType,
                    AttributeNameNormalize = string.Empty, //TODO

                };
            }
            default:
                return null;
        }
    }

    public static string GetExtensionValue(List<IExtension> extensions, string name)
    {
        return extensions != null
            ? extensions.Where(x => x.Name == name).Select(x => x.Value).FirstOrDefault()
            : string.Empty;
    }

    public Task<IAssetAdministrationShell> GetAASById(string id)
    {
        return aasRegistryService.GetAASByIdAsync(id);
    }

    public GetAssetDto ToGetAssetDto(IAssetAdministrationShell aas, IAssetAdministrationShell? parent, IEnumerable<AssetAttributeDto>? assetAttributes)
    {
        return ToGetAssetSimpleDto(aas, parent, assetAttributes);
    }

    public async Task<IEnumerable<AssetAttributeDto>> ToAttributes(IEnumerable<SMESet> submodelElements, bool isTemplateDeleted = false)
    {
        //var sw = new Stopwatch();
        var attributes = new List<AssetAttributeDto>();
        var listRootSMEOfAlias = await GetListRootSMEFromAttributes(submodelElements);
        foreach (var sme in submodelElements)
        {
            //sw.Restart();
            var attr = await ToAssetAttributeDto(sme.AASIdShort, sme, listRootSMEOfAlias, isTemplateDeleted);
            if (attr is not null)
                attributes.Add(attr);
            //sw.Stop();
            //Console.WriteLine($"Convert {sme.Category} take {sw.Elapsed.TotalMilliseconds}");
        }
        return attributes;
    }

    public async Task<AssetAttributeDto> ToAssetAttributeDto(string aasIdShort, SMESet sme, List<SMESet> lstRootSMEAlias, bool isTemplateDeleted = false)
    {
        if (sme == null)
        {
            return null;
        }
        //var sw = new Stopwatch();
        //sw.Restart();
        var templateAttributeId = sme.TemplateId;

        var submodelElement = Converter.CreateSME(sme);

        if (submodelElement.Extensions != null && !isTemplateDeleted)
        {
            templateAttributeId = submodelElement.Extensions.Where(x => x.Name == "TemplateAttributeId").FirstOrDefault()?.Value;
        }

        switch (sme.Category)
        {
            case AttributeTypeConstants.TYPE_STATIC:
            {
                var pStatic = submodelElement as IProperty;
                var snapshotId = Guid.Parse(sme.IdShort);
                var dataType = MappingHelper.ToAhiDataType(pStatic.ValueType);

                return new AssetAttributeDto
                {
                    AssetId = Guid.Parse(aasIdShort),
                    AttributeType = sme.Category,
                    CreatedUtc = sme.TimeStampCreate,
                    DataType = dataType,
                    DecimalPlace = null, // TODO
                    Deleted = false,
                    Id = snapshotId,
                    Name = sme.Name,
                    SequentialNumber = -1, // TODO
                    Payload = JObject.FromObject(new
                    {
                        templateAttributeId,
                        id = snapshotId,
                        value = pStatic.Value
                    }).ToObject<AttributeMapping>(),
                    ThousandSeparator = null, // TODO
                    Uom = null, // TODO
                    UomId = null, // TODO
                    UpdatedUtc = sme.TimeStamp,
                    Value = pStatic?.Value.ParseValueWithDataType(dataType, pStatic.Value, isRawData: false)
                };
            }
            case AttributeTypeConstants.TYPE_ALIAS:
            {
                var rAlias = submodelElement as IReferenceElement;
                var refId = Guid.Parse(sme.IdShort);

                var data = new AssetAttributeDto
                {
                    AssetId = Guid.Parse(aasIdShort),
                    AttributeType = sme.Category,
                    CreatedUtc = sme.TimeStampCreate,
                    Deleted = false,
                    Id = refId,
                    Name = sme.Name,
                    UpdatedUtc = sme.TimeStamp,
                    DataType = rAlias.GetDataType()
                };

                //sw.Restart();
                var rootSme = await GetSMERootAliasFromExtension(rAlias, lstRootSMEAlias);
                //sw.Stop();
                //Console.WriteLine($"==>> Alias get root from extensions {sw.Elapsed.TotalMilliseconds}");

                //sw.Restart();
                var (aliasAAS, aliasSme) = await GetAlias(rAlias);
                //sw.Stop();
                //Console.WriteLine($"==>> Alias get next sme from value {sw.Elapsed.TotalMilliseconds}");

                //sw.Restart();
                var aliasDto = new AssetAttributeDto();
                if (rootSme != null)
                {
                    aliasDto = await ToAssetAttributeDto(rootSme.AASIdShort, rootSme, lstRootSMEAlias);
                    if (aliasDto is null)
                        return null;
                }

                //sw.Stop();
                //Console.WriteLine($"==>> Alias get ref attribute {sw.Elapsed.TotalMilliseconds}");

                data.DataType = aliasDto.DataType;
                data.DecimalPlace = aliasDto.DecimalPlace;
                data.Value = aliasDto.Value;
                data.ThousandSeparator = aliasDto.ThousandSeparator;
                data.Uom = aliasDto.Uom;
                data.UomId = aliasDto.UomId;
                data.SequentialNumber = -1; // TODO

                data.Payload = JObject.FromObject(new
                {
                    id = refId,
                    aliasAssetId = aliasAAS?.IdShort,
                    aliasAttributeId = aliasSme?.IdShort,
                    aliasAssetName = aliasAAS?.Name ?? aliasAAS?.IdShort,
                    aliasAttributeName = aliasSme?.Name ?? aliasSme?.IdShort,
                    templateAttributeId
                }).ToObject<AttributeMapping>();

                return data;
            }
            case AttributeTypeConstants.TYPE_RUNTIME:
            {
                var smcRuntime = submodelElement as ISubmodelElementCollection;
                var runtimeId = Guid.Parse(sme.IdShort);
                var dataType = smcRuntime.GetDataType();
                var runtime = new AssetAttributeDto
                {
                    AssetId = Guid.Parse(aasIdShort),
                    AttributeType = sme.Category,
                    CreatedUtc = sme.TimeStampCreate,
                    DataType = dataType,
                    DecimalPlace = null, // TODO
                    Deleted = false,
                    Id = runtimeId,
                    Name = sme.Name,
                    SequentialNumber = -1, // TODO
                    ThousandSeparator = null, // TODO
                    Uom = null, // TODO
                    UomId = null, // TODO
                    UpdatedUtc = sme.TimeStamp
                };

                var triggerAttributeIdStr = smcRuntime.GetExtensionValue("TriggerAttributeId");

                var triggerAttributeIds = smcRuntime.GetExtensionValue("TriggerAttributeIds");
                var enabledExpression = smcRuntime.GetExtensionValue("EnabledExpression");
                var expression = smcRuntime.GetExtensionValue("Expression");
                var expressionCompile = smcRuntime.GetExtensionValue("ExpressionCompile");

                Guid? triggerAttributeId = !string.IsNullOrEmpty(triggerAttributeIdStr) ? Guid.Parse(triggerAttributeIdStr) : null;
                var triggerAssetId = Guid.Parse(aasIdShort);
                bool? hasTriggerError = null;
                if (triggerAttributeId != null)
                {
                    var triggers = JsonConvert.DeserializeObject<IEnumerable<Guid>>(triggerAttributeIds);
                    var triggerAssetAttributeExists = triggers.Contains(triggerAttributeId.Value);
                    if (!triggerAssetAttributeExists)
                        hasTriggerError = true;
                }
                AttributeMapping payload = JObject.FromObject(new
                {
                    id = runtimeId,
                    enabledExpression = bool.TryParse(enabledExpression, out var enabled) && enabled,
                    expression,
                    expressionCompile,
                    triggerAssetId,
                    triggerAttributeId,
                    hasTriggerError,
                    templateAttributeId,
                }).ToObject<AttributeMapping>();

                //var snapshotSeries = await timeSeriesService.GetSnapshot(triggerAssetId, runtimeId);
                //var snapshotSeries = smcRuntime.GetSnapshotTimeSeries();
                //var snapshotValue = snapshotSeries?.v;

                //runtime.Value = snapshotValue?.ParseValueWithDataType(dataType, $"{snapshotValue}", isRawData: false);
                runtime.Payload = payload;

                return runtime;
            }
            case AttributeTypeConstants.TYPE_DYNAMIC:
            {
                var smcDynamic = submodelElement as ISubmodelElementCollection;
                var dataType = smcDynamic.GetDataType();
                var dynamicId = Guid.Parse(sme.IdShort);
                var deviceId = smcDynamic.GetExtensionValue("DeviceId");
                var metricKey = smcDynamic.GetExtensionValue("MetricKey");

                var dynamic = new AssetAttributeDto
                {
                    AssetId = Guid.Parse(aasIdShort),
                    AttributeType = sme.Category,
                    CreatedUtc = sme.TimeStampCreate,
                    DataType = dataType,
                    DecimalPlace = null, // TODO
                    Deleted = false,
                    Id = dynamicId,
                    Name = sme.Name,
                    SequentialNumber = -1, // TODO
                    ThousandSeparator = null, // TODO
                    Uom = null, // TODO
                    UomId = null, // TODO
                    UpdatedUtc = sme.TimeStamp
                };

                //var snapshotSeries = await timeSeriesService.GetDeviceMetricSnapshot(deviceId, metricKey);
                //dynamic.Value = snapshotSeries?.v;
                dynamic.Payload = JObject.FromObject(new
                {
                    id = dynamicId,
                    deviceId,
                    metricKey,
                    templateAttributeId
                }).ToObject<AttributeMapping>();

                return dynamic;
            }
            case AttributeTypeConstants.TYPE_COMMAND:
            {
                var smcCommand = submodelElement as ISubmodelElementCollection;
                var dataType = smcCommand.GetDataType();

                var dynamicId = Guid.Parse(sme.IdShort);
                var deviceId = smcCommand.GetExtensionValue("DeviceId");
                var metricKey = smcCommand.GetExtensionValue("MetricKey");

                var command = new AssetAttributeDto
                {
                    AssetId = Guid.Parse(aasIdShort),
                    AttributeType = sme.Category,
                    CreatedUtc = sme.TimeStampCreate,
                    DataType = dataType,
                    DecimalPlace = null, // TODO
                    Deleted = false,
                    Id = dynamicId,
                    Name = sme.Name,
                    SequentialNumber = -1, // TODO
                    ThousandSeparator = null, // TODO
                    Uom = null, // TODO
                    UomId = null, // TODO
                    UpdatedUtc = sme.TimeStamp
                };

                //var snapshotSeries = await timeSeriesService.GetDeviceMetricSnapshot(deviceId, metricKey);
                //command.Value = snapshotSeries?.v;
                command.Payload = JObject.FromObject(new
                {
                    id = dynamicId,
                    deviceId,
                    metricKey,
                    templateAttributeId
                }).ToObject<AttributeMapping>();

                return command;
            }
            default:
                return null;
        }
    }

    private async Task<List<SMESet>> GetListRootSMEFromAttributes(IEnumerable<SMESet> attributes)
    {
        var alias = attributes.Where(x => x.Category == AttributeTypeConstants.TYPE_ALIAS).ToList();
        var rootMetaDatas = alias.Select(x => JsonConvert.DeserializeObject<AliasMetaData>(x.MetaData)).ToList();

        var aasIds = rootMetaDatas.Select(x => x.RefAasId).ToList();
        var smeIds = rootMetaDatas.Select(x => x.RefAttributeId).ToList();

        var lstSME = await unitOfWork.SMESets.AsFetchable().Where(x => aasIds.Contains(x.AASIdShort))
            .Where(x => smeIds.Contains(x.IdShort)).ToListAsync();

        return lstSME;
    }

    public async Task<(string PathId, string? PathName, IAssetAdministrationShell? parentAas)> BuildResourcePath(IAssetAdministrationShell currentAas, Guid? parentAssetId)
    {
        var currentName = currentAas.DisplayName?.FirstOrDefault()?.Text;
        if (parentAssetId.HasValue)
        {
            var parentAas = await GetAASById(parentAssetId.ToString());
            var parentPathId = parentAas.Extensions.FirstOrDefault(e => e.Name == "ResourcePath").Value;
            var parentPathName = parentAas.Extensions.FirstOrDefault(e => e.Name == "ResourcePathName").Value;
            return ($"{parentPathId}/children/{currentAas.Id}", $"{parentPathName}/{currentName}", parentAas);
        }
        return ($"objects/{currentAas.Id}", currentName, null);
    }

    public int? GetAttributeDecimalPlace(AssetAttributeCommand attribute)
    {
        return attribute.DataType == DataTypeConstants.TYPE_DOUBLE ? attribute.DecimalPlace : null;
    }

    public int? GetAttributeDecimalPlace(AssetTemplateAttribute attribute)
    {
        return attribute.DataType == DataTypeConstants.TYPE_DOUBLE ? attribute.DecimalPlace : null;
    }

    public AssetHierarchy ToAssetHierarchy(IAssetAdministrationShell aas)
    {
        var ah = new AssetHierarchy()
        {
            AssetTemplateId = Guid.TryParse(aas.Administration?.TemplateId, out var templateId) ? templateId : null, // TODO
            AssetName = aas.DisplayName?.FirstOrDefault()?.Text ?? aas.IdShort,
            AssetId = Guid.TryParse(aas.Id, out var id) ? id : default,
            AssetCreatedUtc = aas.TimeStampCreate,
            AssetHasWarning = false, // TODO
            AssetRetentionDays = -1, // TODO
            ParentAssetId = Guid.TryParse(aas.Extensions?.FirstOrDefault(e => e.Name == "ParentAssetId")?.Value, out var pId) ? pId : null
        };

        return ah;
    }

    public async Task<SendConfigurationResultDto> SendConfigurationToDeviceIotAsync(SendConfigurationToDeviceIot request, GetAssetSimpleDto asset,
        IEnumerable<AssetAttributeDto> attributes,  bool rowVersionCheck = true)
    {
        var commandAttribute = asset.Attributes.FirstOrDefault(x => x.Id == request.AttributeId);
        if (commandAttribute == null)
        {
            throw new Exception("Attribute not found");
        }

        // checking the row version.
        // should be matched with latest in database
        if (rowVersionCheck && request.RowVersion != commandAttribute.Payload.RowVersion)
        {
            throw EntityValidationExceptionHelper.GenerateException(nameof(request.RowVersion), MessageDetailConstants.TOO_MANY_REQUEST, detailCode: MessageDetailConstants.TOO_MANY_REQUEST);
        }

        var deviceId = string.IsNullOrEmpty(request.DeviceId) ? commandAttribute.Payload.DeviceId : request.DeviceId;
        var metricKey = string.IsNullOrEmpty(request.MetricKey) ? commandAttribute.Payload.MetricKey : request.MetricKey;

        var metrics = new List<CloudToDeviceMessage> {
                new CloudToDeviceMessage
                {
                    Key = metricKey,//TODO
                    Value = request.Value?.ToString(),
                    DataType = DataTypeConstants.TYPE_INTEGER //TODO
                }
        };

        var output = new SendConfigurationResultDto(true, new Guid());

        var history = new AssetAttributeCommandHistory()
        {
            AssetAttributeId = request.AttributeId,
            DeviceId = deviceId,
            MetricKey = metricKey,
            RowVersion = new Guid(),// TODO
            Value = request.Value?.ToString(),
            Id = new Guid()
        };

        await timeSeriesService.AddCommandHistory(history);
        return output;
    }

    public AddAssetTemplateDto ToAddAssetTemplateDto(IAssetAdministrationShell aas,
        IEnumerable<AssetAttributeDto>? attributes = null)
    {
        return new AddAssetTemplateDto()
        {
            Id = aas.Id
        };
    }

    public GetAssetTemplateDto ToGetAssetTemplateDto(AASSet aASSet, IEnumerable<GetAssetAttributeTemplateSimpleDto>? attributes = null)
    {
        var aas = aasRegistryService.ToAssetAdministrationShell(aASSet.AssetAdministrationShell);
        return new GetAssetTemplateDto()
        {
            Id = Guid.TryParse(aas.Id, out var id) ? id : default,
            Name = aas.DisplayName?.FirstOrDefault()?.Text ?? aas.IdShort,
            CreatedUtc = aas.TimeStampCreate,
            UpdatedUtc = aas.TimeStamp,
            AssetCount = 0, //TODO
            Attributes = attributes,
            CreatedBy = aas.Administration?.Creator?.GetAsExactlyOneKey()?.Value, // TODO
            LockedByUpn = string.Empty //TODO
        };
    }

    public async Task<IEnumerable<GetAssetAttributeTemplateSimpleDto>> ToTemplateAttributes(IEnumerable<ISubmodelElement> submodelElements, Guid aasTemplateId)
    {
        var attributes = new List<GetAssetAttributeTemplateSimpleDto>();
        foreach (var sme in submodelElements)
        {
            var attr = await ToAssetTemplateAttributeDto(sme, aasTemplateId);
            if (attr is not null)
                attributes.Add(attr);
        }
        return attributes;
    }

    public async Task<GetAssetAttributeTemplateSimpleDto> ToAssetTemplateAttributeDto(ISubmodelElement sme, Guid aasTemplateId)
    {
        switch (sme.Category)
        {
            case AttributeTypeConstants.TYPE_STATIC:
            {
                var pStatic = sme as IProperty;
                var snapshotId = Guid.Parse(pStatic.IdShort);
                var dataType = MappingHelper.ToAhiDataType(pStatic.ValueType);
                return new GetAssetAttributeTemplateSimpleDto
                {
                    AssetTemplateId = aasTemplateId,
                    AttributeType = pStatic.Category,
                    CreatedUtc = pStatic.TimeStampCreate,
                    DataType = dataType,
                    DecimalPlace = null, // TODO
                    Id = snapshotId,
                    Name = pStatic.DisplayName.FirstOrDefault()?.Text ?? pStatic.IdShort,
                    SequentialNumber = -1, // TODO
                    Value = pStatic.Value,
                    Payload = JObject.FromObject(new
                    {
                        // templateAttributeId = sm.Administration?.TemplateId, // [NOTE] AAS doens't have
                        id = snapshotId,
                        value = pStatic.Value
                    }).ToObject<AttributeMapping>(),
                    ThousandSeparator = null, // TODO
                    Uom = null, // TODO
                    UomId = null, // TODO
                    UpdatedUtc = pStatic.TimeStamp
                };
            }
            case AttributeTypeConstants.TYPE_ALIAS:
            {
                var rAlias = sme as IReferenceElement;
                var refId = Guid.Parse(rAlias.IdShort);
                var (aliasAas, _, aliasSme, _) = await GetRootAliasSme(rAlias);
                var aliasDto = await ToAssetAttributeDto(aliasSme, aasTemplateId);
                if (aliasDto is null)
                    return null;
                return new GetAssetAttributeTemplateSimpleDto
                {
                    AssetTemplateId = aasTemplateId,
                    AttributeType = rAlias.Category,
                    CreatedUtc = rAlias.TimeStampCreate,
                    DataType = aliasDto.DataType,
                    DecimalPlace = aliasDto.DecimalPlace,
                    Id = refId,
                    Name = rAlias.DisplayName.FirstOrDefault()?.Text ?? rAlias.IdShort,
                    SequentialNumber = -1, // TODO
                    Value = aliasDto?.Value?.ToString(),
                    Payload = JObject.FromObject(new
                    {
                        id = refId,
                        aliasAssetId = aliasDto.AssetId,
                        aliasAttributeId = Guid.Parse(aliasSme.IdShort),
                        aliasAssetName = aliasAas?.DisplayName?.FirstOrDefault()?.Text ?? aliasAas?.IdShort,
                        aliasAttributeName = aliasSme?.DisplayName?.FirstOrDefault()?.Text ?? aliasSme?.IdShort,
                    }).ToObject<AttributeMapping>(),
                    ThousandSeparator = aliasDto.ThousandSeparator,
                    Uom = aliasDto.Uom,
                    UomId = aliasDto.UomId,
                    UpdatedUtc = rAlias.TimeStamp
                };
            }
            case AttributeTypeConstants.TYPE_RUNTIME:
            {
                var smcRuntime = sme as ISubmodelElementCollection;
                var runtimeId = Guid.Parse(smcRuntime.IdShort);
                var triggerAttributeIdStr = smcRuntime.GetExtensionValue("TriggerAttributeId");
                var triggerAttributeIds = smcRuntime.GetExtensionValue("TriggerAttributeIds");
                var enabledExpression = smcRuntime.GetExtensionValue("EnabledExpression");
                var expression = smcRuntime.GetExtensionValue("Expression");
                var expressionCompile = smcRuntime.GetExtensionValue("ExpressionCompile");

                Guid? triggerAttributeId = triggerAttributeIdStr != null ? Guid.Parse(triggerAttributeIdStr) : null;
                var triggerAssetId = aasTemplateId;
                bool? hasTriggerError = null;
                if (triggerAttributeId != null)
                {
                    var triggers = JsonConvert.DeserializeObject<IEnumerable<Guid>>(triggerAttributeIds);
                    var triggerAssetAttributeExists = triggers.Contains(triggerAttributeId.Value);
                    if (!triggerAssetAttributeExists)
                        hasTriggerError = true;
                }
                AttributeMapping payload = JObject.FromObject(new
                {
                    id = runtimeId,
                    enabledExpression = bool.TryParse(enabledExpression, out var enabled) && enabled,
                    expression,
                    expressionCompile,
                    triggerAssetId,
                    triggerAttributeId,
                    hasTriggerError
                }).ToObject<AttributeMapping>();

                //var snapshotSeries = await timeSeriesService.GetSnapshot(triggerAssetId, runtimeId);
                var snapshotSeries = smcRuntime.GetSnapshotTimeSeries();
                var snapshotValue = snapshotSeries?.v;
                var dataType = smcRuntime.GetDataType();
                return new GetAssetAttributeTemplateSimpleDto
                {
                    AssetTemplateId = aasTemplateId,
                    AttributeType = smcRuntime.Category,
                    CreatedUtc = smcRuntime.TimeStampCreate,
                    DataType = dataType,
                    DecimalPlace = null, // TODO
                    Id = runtimeId,
                    Name = smcRuntime.DisplayName.FirstOrDefault()?.Text ?? smcRuntime.IdShort,
                    SequentialNumber = -1, // TODO
                    Value = snapshotValue?.ToString(),
                    Payload = payload,
                    ThousandSeparator = null, // TODO
                    Uom = null, // TODO
                    UomId = null, // TODO
                    UpdatedUtc = smcRuntime.TimeStamp
                };
            }
            case AttributeTypeConstants.TYPE_DYNAMIC:
            {
                var smcDynamic = sme as ISubmodelElementCollection;
                var dataType = smcDynamic.GetDataType();
                var dynamicId = Guid.Parse(smcDynamic.IdShort);
                var deviceTemplateId = smcDynamic.GetExtensionValue("DeviceTemplateId");
                var metricKey = smcDynamic.GetExtensionValue("MetricKey");
                var markupName = smcDynamic.GetExtensionValue("MarkupName");

                var snapshotSeries = await timeSeriesService.GetDeviceMetricSnapshot(deviceTemplateId, metricKey);

                return new GetAssetAttributeTemplateSimpleDto
                {
                    AssetTemplateId = aasTemplateId,
                    AttributeType = smcDynamic.Category,
                    CreatedUtc = smcDynamic.TimeStampCreate,
                    DataType = dataType,
                    DecimalPlace = null, // TODO
                    Id = dynamicId,
                    Name = smcDynamic.DisplayName.FirstOrDefault()?.Text ?? smcDynamic.IdShort,
                    SequentialNumber = -1, // TODO
                    Value = snapshotSeries?.v?.ToString(),
                    Payload = JObject.FromObject(new
                    {
                        id = dynamicId,
                        deviceTemplateId,
                        metricKey,
                        markupName
                    }).ToObject<AttributeMapping>(),
                    ThousandSeparator = null, // TODO
                    Uom = null, // TODO
                    UomId = null, // TODO
                    UpdatedUtc = smcDynamic.TimeStamp
                };
            }
            case AttributeTypeConstants.TYPE_COMMAND:
            {
                var smcCommand = sme as ISubmodelElementCollection;
                var dataType = smcCommand.GetDataType();
                var dynamicId = Guid.Parse(smcCommand.IdShort);
                var deviceTemplateId = smcCommand.GetExtensionValue("DeviceTemplateId");
                var metricKey = smcCommand.GetExtensionValue("MetricKey");
                var markupName = smcCommand.GetExtensionValue("MarkupName");

                var snapshotSeries = await timeSeriesService.GetDeviceMetricSnapshot(deviceTemplateId, metricKey);

                return new GetAssetAttributeTemplateSimpleDto
                {
                    AssetTemplateId = aasTemplateId,
                    AttributeType = smcCommand.Category,
                    CreatedUtc = smcCommand.TimeStampCreate,
                    DataType = dataType,
                    DecimalPlace = null, // TODO
                    Id = dynamicId,
                    Name = smcCommand.DisplayName.FirstOrDefault()?.Text ?? smcCommand.IdShort,
                    SequentialNumber = -1, // TODO
                    Value = snapshotSeries?.v?.ToString(),
                    Payload = JObject.FromObject(new
                    {
                        id = dynamicId,
                        deviceTemplateId,
                        metricKey,
                        markupName
                    }).ToObject<AttributeMapping>(),
                    ThousandSeparator = null, // TODO
                    Uom = null, // TODO
                    UomId = null, // TODO
                    UpdatedUtc = smcCommand.TimeStamp
                };
            }
            default:
                return null;
        }
    }

    public AssetAttributeCommand ToAssetAttributeCommand(GetAssetAttributeTemplateSimpleDto templateAttr, Guid assetId, Guid? templateAttributeId)
    {
        return new AssetAttributeCommand()
        {
            Id = templateAttr.Id,
            AssetId = assetId,
            AttributeType = templateAttr.AttributeType,
            CreatedUtc = DateTime.UtcNow,
            DataType = templateAttr.DataType,
            DecimalPlace = templateAttr.DecimalPlace,
            Expression = templateAttr.Expression,
            Name = templateAttr.Name,
            Payload = templateAttr.Payload,
            ThousandSeparator = templateAttr.ThousandSeparator,
            Value = templateAttr.Value,
            UomId = templateAttr.UomId,
            SequentialNumber= templateAttr.SequentialNumber,
            TemplateAttributeId = templateAttributeId
        };
    }

    public GetAssetSimpleDto ToGetAssetSimpleDtoFromDB(AASSet aas,
        AASSet? parent = null,
        IEnumerable<AssetAttributeDto>? attributes = null)
    {
        return new GetAssetSimpleDto()
        {
            AssetTemplateId = Guid.TryParse(aas?.TemplateId, out var templateId) ? templateId : null, // TODO
            AssetTemplateName = null, // TODO
            Name = !string.IsNullOrEmpty(aas.Name) ? aas.Name : aas.IdShort,
            Attributes = attributes,
            Children = [],
            CreatedBy = string.Empty, //aas.AssetAdministrationShell.Administration?.Creator?.GetAsExactlyOneKey()?.Value, // TODO
            CreatedUtc = aas.TimeStampCreate,
            CurrentTimestamp = DateTime.UtcNow,
            CurrentUserUpn = null,
            HasWarning = false, // TODO
            UpdatedUtc = aas.TimeStamp,
            RetentionDays = -1, // TODO,
            Id = Guid.TryParse(aas.IdShort, out var id) ? id : default,
            IsDocument = false, // TODO
            ParentAssetId = Guid.TryParse(aas.AssetAdministrationShell.Extensions?.FirstOrDefault(e => e.Name == "ParentAssetId")?.Value, out var pId) ? pId : null,
            Parent = parent != null ? ToGetAssetSimpleDtoFromDB(aas: parent, parent: null, attributes: null) : null,
            ResourcePath = aas.ResourcePath,
            RequestLockTimeout = null, // TODO
            RequestLockTimestamp = null, // TODO
            RequestLockUserUpn = null, // TODO
        };
    }

    public AssetHierarchy ToAssetHierarchy(AASSet aas)
    {
        var ah = new AssetHierarchy()
        {
            AssetTemplateId = Guid.TryParse(aas?.TemplateId, out var templateId) ? templateId : null, // TODO
            AssetName = aas.AssetAdministrationShell.DisplayName?.FirstOrDefault()?.Text ?? aas.IdShort,
            AssetId = Guid.TryParse(aas.IdShort, out var id) ? id : default,
            AssetCreatedUtc = aas.TimeStampCreate,
            AssetHasWarning = false, // TODO
            AssetRetentionDays = -1, // TODO
            ParentAssetId = Guid.TryParse(aas.AssetAdministrationShell.Extensions?.FirstOrDefault(e => e.Name == "ParentAssetId")?.Value, out var pId) ? pId : null
        };

        return ah;
    }

    public GetAssetDto ToGetAssetDtoFromDB(AASSet aas, AASSet? parent, IEnumerable<AssetAttributeDto>? attributes)
    {
        //var assetAttributeDtos = assetAttributes.Select(x => ToAssetAttributeDto(x, Guid.Parse(aas.IdShort)));
        return ToGetAssetSimpleDtoFromDB(aas, parent, attributes);
    }

    //public static AssetAttributeDto ToAssetAttributeDto(SMESet sMESet, Guid aasId)
    //{
    //    return new AssetAttributeDto
    //    {
    //        AssetId = aasId,
    //        Name = sMESet.Name,
    //        AttributeType = sMESet.SMEType
    //    };
    //}
}