namespace IO.Swagger.Controllers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AasxServerDB.Entities;
using AasxServerStandardBib;
using AasxServerStandardBib.Models;
using AasxServerStandardBib.Services;
using AasxServerStandardBib.Utils;
using AHI.Infrastructure.Audit.Constant;
using AHI.Infrastructure.SharedKernel.Extension;
using AHI.Infrastructure.SharedKernel.Model;
using Extensions;
using IO.Swagger.Models;
using IO.Swagger.Registry.Lib.V3.Interfaces;
using IO.Swagger.Registry.Lib.V3.Services;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Pipelines.Sockets.Unofficial.Arenas;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using AasxServerDB.Repositories;
using AasxServerDB.Dto;
using I.Swagger.Registry.Lib.V3.Services;
using AasxServerDB.Helpers;

[ApiController]
[Route("dev/assets")]
public partial class AhiAssetsController(RuntimeAssetAttributeHandler _runtimeAssetAttributeHandler,
    EventPublisher _eventPublisher,
    TimeSeriesService _timeSeriesService,
    AasApiHelperService _aasApiHelper,
    IAasRegistryService _aasRegistryService,
    IAASUnitOfWork _unitOfWork) : ControllerBase
{
    //search asset hierarchy
    [HttpPost("search")]
    public async Task<IActionResult> SearchAsync([FromBody] GetAssetByCriteria command)
    {
        var parentFilter = ParentFilterRegex();
        var parentFilterValue = parentFilter.Match(command.Filter).Groups[1].Value;
        Guid? parentId = parentFilterValue == "null" ? null : Guid.Parse(parentFilterValue);
        AASSet? parent = null;

        if (parentId.HasValue)
            parent = await _aasRegistryService.GetByIdAsync(parentId.Value);

        var (assets, totalCount) = await _aasRegistryService.GetASSetsAsync(name: string.Empty, filterParent: true, parentId, pageSize: command.PageSize, pageIndex: command.PageIndex);

        var ahiResp = new BaseSearchResponse<GetAssetSimpleDto>(
            duration: 0,
            totalCount: totalCount,
            pageSize: command.PageSize,
            pageIndex: command.PageIndex,
            data: assets.Select(x => _aasApiHelper.ToGetAssetSimpleDtoFromDB(x, parent)).ToArray()
        );

        return Ok(ahiResp);
    }

    //load child asset
    [HttpGet("{id}/children")]
    public async Task<IActionResult> LoadChildrenAsync(Guid id)
    {
        var (assets, totalCount) = await _aasRegistryService.GetASSetsAsync(name: string.Empty, filterParent: true, id, pageSize: int.MaxValue, pageIndex: 0);

        var ahiResp = new BaseSearchResponse<GetAssetSimpleDto>(
            duration: 0,
            totalCount: totalCount,
            pageSize: int.MaxValue,
            pageIndex: 0,
            data: assets.Select(x => _aasApiHelper.ToGetAssetSimpleDtoFromDB(x)).ToArray()
        );

        return Ok(ahiResp);
    }

    //Edit asset -> add new aas, child
    [HttpPatch("edit")]
    public async Task<IActionResult> UpsertAssetAsync([FromBody] JsonPatchDocument document)
    {
        var operations = document.Operations;
        var result = new UpsertAssetDto();
        var resultModels = new List<BaseJsonPathDocument>();

        foreach (var operation in operations)
        {
            string path;
            var resultModel = new BaseJsonPathDocument
            {
                OP = operation.op,
                Path = operation.path
            };

            switch (operation.op)
            {
                case "add":
                    path = operation.path.Replace("/", "");
                    var addAssetDto = JObject.FromObject(operation.value).ToObject<AddAsset>();
                    if (Guid.TryParse(path, out var parentAssetId))
                    {
                        //if elementID null => add in root
                        addAssetDto.ParentAssetId = parentAssetId;
                    }

                    var creator = new Reference(ReferenceTypes.ExternalReference, keys: [new Key(KeyTypes.Entity, value: Guid.Empty.ToString())]); // [TODO]
                    var aasId = addAssetDto.Id;
                    var aas = new AssetAdministrationShell(
                        id: aasId.ToString(),
                        assetInformation: new AssetInformation(
                            assetKind: AssetKind.Instance,
                            globalAssetId: addAssetDto.Id.ToString(),
                            specificAssetIds: null,
                            assetType: "Instance")
                    )
                    {
                        Administration = new AdministrativeInformation()
                        {
                            TemplateId = addAssetDto.AssetTemplateId?.ToString(),
                            Creator = creator
                        },
                        DisplayName = [new LangStringNameType("en-US", addAssetDto.Name)],
                        IdShort = aasId.ToString(),
                        Submodels = [],
                        Extensions = []
                    };

                    aas.Extensions.Add(new Extension(
                        name: "ParentAssetId",
                        valueType: DataTypeDefXsd.String,
                        value: addAssetDto.ParentAssetId?.ToString()));

                    var (pathId, pathName, parent) = await _aasRegistryService.BuildResourcePathAsync(aas, parentAssetId: addAssetDto.ParentAssetId);
                    aas.Extensions.Add(new Extension(
                        name: "ResourcePath",
                        valueType: DataTypeDefXsd.String,
                        value: pathId));
                    aas.Extensions.Add(new Extension(
                        name: "ResourcePathName",
                        valueType: DataTypeDefXsd.String,
                        value: pathName));

                    await _aasRegistryService.SaveAASAsync(aas);

                    var defaultSm = new Submodel(id: aas.Id)
                    {
                        Administration = new AdministrativeInformation()
                        {
                            TemplateId = addAssetDto.AssetTemplateId?.ToString(),
                            Creator = creator
                        },
                        DisplayName = [new LangStringNameType("en-US", "Properties")],
                        IdShort = aasId.ToString(),
                        Kind = ModellingKind.Instance,
                        SubmodelElements = []
                    };
                    await _aasRegistryService.SaveSubmodelAsync(defaultSm, aas.IdShort);

                    if (addAssetDto.AssetTemplateId.HasValue)
                    {
                        var (templateAAS, templateAttributes) = await _aasRegistryService.GetFullAasByIdAsync(addAssetDto.AssetTemplateId.Value);
                        var templateAttributeDTO = await _aasApiHelper.ToTemplateAttributes(templateAttributes, Guid.Parse(templateAAS.IdShort));

                        if (templateAttributeDTO != null && templateAttributeDTO.Any())
                        {
                            foreach (var templateAttr in templateAttributeDTO)
                            {
                                var attributeId = templateAttr.Payload.Id;
                                var payloadAttibute = addAssetDto.Mappings.Where(x => x.TemplateAttributeId == attributeId).FirstOrDefault();
                                var templateAttributeId = payloadAttibute.TemplateAttributeId;

                                var templateAttributeExtension = new Extension(name: "TemplateAttributeId", valueType: DataTypeDefXsd.String, value: templateAttributeId.ToString());

                                switch (templateAttr.AttributeType)
                                {
                                    case AttributeTypeConstants.TYPE_STATIC:
                                    {
                                        var property = new Property(
                                            valueType: MappingHelper.ToAasDataType(templateAttr.DataType))
                                        {
                                            DisplayName = [new LangStringNameType("en-US", templateAttr.Name)],
                                            IdShort = templateAttr.Id.ToString(),
                                            Value = templateAttr.Value,
                                            Category = templateAttr.AttributeType,
                                            Extensions = [templateAttributeExtension]
                                        };
                                        //var encodedSmId = ConvertHelper.ToBase64(aasId.ToString());
                                        //smRepoController.PostSubmodelElementSubmodelRepo(property, encodedSmId, first: false);
                                        await _aasRegistryService.SaveSubmodelElement(property, aasId.ToString(), first: false);
                                        break;
                                    }
                                    case AttributeTypeConstants.TYPE_ALIAS:
                                    {
                                        var reference = new ReferenceElement()
                                        {
                                            Category = templateAttr.AttributeType,
                                            DisplayName = [new LangStringNameType("en-US", templateAttr.Name)],
                                            IdShort = templateAttr.Id.ToString(),
                                            Extensions = [templateAttributeExtension]
                                        };

                                        await _aasRegistryService.SaveSubmodelElement(reference, aasId.ToString(), first: false);
                                        break;
                                    }
                                    case AttributeTypeConstants.TYPE_DYNAMIC:
                                    {
                                        var dynamicPayload = JObject.FromObject(templateAttr.Payload).ToObject<AssetAttributeDynamic>();

                                        string deviceId = string.Empty;
                                        if (attributeId != null)
                                        {
                                            deviceId = payloadAttibute != null ? payloadAttibute.DeviceId : templateAttr.Payload.DeviceTemplateId.ToString();
                                        }

                                        if (templateAttr.DataType is not DataTypeConstants.TYPE_INTEGER and not DataTypeConstants.TYPE_DOUBLE)
                                        {
                                            templateAttr.DecimalPlace = null;
                                            templateAttr.ThousandSeparator = null;
                                        }

                                        var deviceIdProp = new Extension(name: "DeviceId", valueType: DataTypeDefXsd.String, value: deviceId);
                                        var metricKey = new Extension(name: "MetricKey", valueType: DataTypeDefXsd.String, value: dynamicPayload.MetricKey);
                                        var dataType = new Extension(name: "DataType", valueType: DataTypeDefXsd.String, value: templateAttr.DataType);

                                        //var snapShot = TimeSeriesHelper.CreateEmptySnapshot(templateAttr.DataType);
                                        var smc = new SubmodelElementCollection()
                                        {
                                            DisplayName = [new LangStringNameType("en-US", templateAttr.Name)],
                                            IdShort = templateAttr.Id.ToString(),
                                            Category = templateAttr.AttributeType,
                                            Extensions = [templateAttributeExtension, deviceIdProp, metricKey, dataType]
                                        };

                                        var snapShotSeries = await _timeSeriesService.GetDeviceMetricSnapshot(dynamicPayload.DeviceId, dynamicPayload.MetricKey);
                                        if (snapShotSeries is not null && snapShotSeries.v != null)
                                            //await _aasRegistryService.UpdateSnapshot(aasId, templateAttr.Id, snapShotSeries);
                                            smc.UpdateSnapshot(snapShotSeries);

                                        var encodedSmId = ConvertHelper.ToBase64(aasId.ToString());
                                        //smRepoController.PostSubmodelElementSubmodelRepo(smc, encodedSmId, first: false);
                                        await _aasRegistryService.SaveSubmodelElement(smc, aasId.ToString(), first: false);
                                        break;
                                    }
                                    case AttributeTypeConstants.TYPE_COMMAND:
                                    {
                                        var commandPayload = JObject.FromObject(templateAttr.Payload).ToObject<AssetCommandAttribute>();
                                        string deviceId = string.Empty;
                                        if (attributeId != null)
                                        {
                                            deviceId = payloadAttibute != null ? payloadAttibute.DeviceId : templateAttr.Payload.DeviceTemplateId.ToString();
                                        }

                                        var deviceIdProp = new Extension(name: "DeviceId", valueType: DataTypeDefXsd.String, value: deviceId);
                                        var metricKey = new Extension(name: "MetricKey", valueType: DataTypeDefXsd.String, value: commandPayload.MetricKey);
                                        var dataType = new Extension(name: "DataType", valueType: DataTypeDefXsd.String, value: DataTypeConstants.TYPE_INTEGER);

                                        //var snapShot = TimeSeriesHelper.CreateEmptySnapshot(DataTypeConstants.TYPE_INTEGER);
                                        var smc = new SubmodelElementCollection()
                                        {
                                            DisplayName = [new LangStringNameType("en-US", templateAttr.Name)],
                                            IdShort = templateAttr.Id.ToString(),
                                            Category = templateAttr.AttributeType,
                                            Extensions = [templateAttributeExtension, deviceIdProp, metricKey, dataType]
                                        };
                                        var snapShotSeries = await _timeSeriesService.GetDeviceMetricSnapshot(commandPayload.DeviceId, commandPayload.MetricKey);
                                        if (snapShotSeries is not null && snapShotSeries.v != null)
                                            //await _aasRegistryService.UpdateSnapshot(aasId, templateAttr.Id, snapShotSeries);
                                            smc.UpdateSnapshot(snapShotSeries);

                                        var encodedSmId = ConvertHelper.ToBase64(aasId.ToString());
                                        //smRepoController.PostSubmodelElementSubmodelRepo(smc, encodedSmId, first: false);
                                        await _aasRegistryService.SaveSubmodelElement(smc, aasId.ToString(), first: false);
                                        break;
                                    }
                                }
                            }

                            //put command attribute handler at last as the relationship with the other attributes
                            var commandAttributes = templateAttributeDTO.Where(x => x.AttributeType == AttributeTypeConstants.TYPE_RUNTIME).ToList();
                            if (templateAttributeDTO.Any())
                            {
                                foreach (var comm in commandAttributes)
                                {
                                    var attributeId = comm.Payload.Id;
                                    var payloadAttibute = addAssetDto.Mappings.FirstOrDefault(x => x.TemplateAttributeId == attributeId);
                                    var templateAttributeId = payloadAttibute.TemplateAttributeId;

                                    var assetAttrCommand = _aasApiHelper.ToAssetAttributeCommand(comm, aasId, templateAttributeId != Guid.Empty ? templateAttributeId : null);
                                    await _runtimeAssetAttributeHandler.AddAttributeAsync(assetAttrCommand, inputAttributes: null, isTemplate: false);
                                }
                            }
                        }
                    }

                    resultModel.Values = _aasApiHelper.ToGetAssetSimpleDto(aas);
                    break;

                case "edit":
                    break;

                case "edit_parent":
                    break;

                case "remove":
                    break;
            }

            resultModels.Add(resultModel);
        }
        result.Data = resultModels;

        //Program.saveEnvDynamic(0);
        return Ok(result);
    }

    [HttpPatch("{assetId}/attributes")]
    public async Task<IActionResult> UpsertAttributesAsync(Guid assetId, [FromBody] JsonPatchDocument document)
    {
        // Including the Attribute's name come from Request of Add & Edit action - For Delete, we will only load from DB.
        var addEditAttributes = document.Operations
                                        .Where(x => x.op != PatchActionConstants.REMOVE)
                                        .Select(x => x.value.ToJson().FromJson<AssetAttributeCommand>())
                                        .Select(a => new KeyValuePair<Guid, string>(a.Id, a.Name));
        var deleteAttributes = document.Operations
                                        .Where(x => x.op == PatchActionConstants.REMOVE)
                                        .Select(x => x.value.ToJson().FromJson<DeleteAssetAttribute>())
                                        .SelectMany(a => a.Ids.Select(id => new KeyValuePair<Guid, string>(id, null)));
        var auditAttributes = addEditAttributes.Union(deleteAttributes).ToList();

        var mainAction = document.Operations.All(x => string.Equals(x.op, PatchActionConstants.REMOVE, StringComparison.InvariantCultureIgnoreCase))
                                ? ActionType.Delete
                                : ActionType.Update;

        var resultModels = new List<BaseJsonPathDocument>();
        {
            string path;
            Guid attributeId;
            var operations = document.Operations;
            var index = 0;
            var inputAttributes = operations
                .Where(x => x.op == PatchActionConstants.ADD || x.op == PatchActionConstants.EDIT)
                .Select(x => x.value.ToJson().FromJson<AssetAttributeCommand>());

            foreach (var operation in operations)
            {
                index++;
                var resultModel = new BaseJsonPathDocument
                {
                    OP = operation.op,
                    Path = operation.path
                };
                switch (operation.op)
                {
                    case PatchActionConstants.ADD:
                    {
                        var attribute = operation.value.ToJson().FromJson<AssetAttributeCommand>();
                        attribute.Id = attribute.Id != Guid.Empty ? attribute.Id : Guid.NewGuid();
                        attribute.AssetId = assetId;
                        attribute.SequentialNumber = index;
                        attribute.DecimalPlace = _aasApiHelper.GetAttributeDecimalPlace(attribute);

                        switch (attribute.AttributeType)
                        {
                            case AttributeTypeConstants.TYPE_STATIC:
                            {
                                var property = new Property(
                                    valueType: MappingHelper.ToAasDataType(attribute.DataType))
                                {
                                    DisplayName = [new LangStringNameType("en-US", attribute.Name)],
                                    IdShort = attribute.Id.ToString(),
                                    Value = attribute.Value,
                                    Category = attribute.AttributeType
                                };

                                await _aasRegistryService.SaveSubmodelElement(property, assetId.ToString(), first: false);
                                break;
                            }
                            case AttributeTypeConstants.TYPE_ALIAS:
                            {
                                var aliasPayload = JObject.FromObject(attribute.Payload).ToObject<AssetAttributeAlias>();
                                var (aas, elements) = await _aasRegistryService.GetFullAasByIdAsync(aliasPayload.AliasAssetId.Value);
                                var aliasAas = _aasRegistryService.ToAssetAdministrationShell(aas.AssetAdministrationShell);

                                var aliasSme = elements.First(sme => sme.IdShort == aliasPayload.AliasAttributeId.ToString());

                                var reference = new ReferenceElement()
                                {
                                    Category = attribute.AttributeType,
                                    DisplayName = [new LangStringNameType("en-US", attribute.Name)],
                                    IdShort = attribute.Id.ToString(),
                                    Value = new Reference(ReferenceTypes.ModelReference, [aliasAas.ToKey(), new Key(KeyTypes.Submodel, aliasAas.IdShort), aliasSme.ToKey()])
                                };

                                var (rootAas, smId, rootSme, aliasPath) = await _aasApiHelper.GetRootAliasSme(reference);

                                reference.Extensions = new List<IExtension>
                                {
                                    new Extension(
                                        name: "RootAasIdShort",
                                        valueType: DataTypeDefXsd.String,
                                        value: rootAas.IdShort?.ToString()),
                                    new Extension(
                                        name: "RootSmId",
                                        valueType: DataTypeDefXsd.String,
                                        value: smId),
                                    new Extension(
                                        name: "RootSmeIdShort",
                                        valueType: DataTypeDefXsd.String,
                                        value: rootSme.IdShort?.ToString()),
                                    new Extension(
                                        name: "AliasPath",
                                        valueType: DataTypeDefXsd.String,
                                        value: aliasPath?.ToString())
                                };

                                var dataType = string.Empty;
                                if (rootSme is ISubmodelElementCollection smeCollection)
                                {
                                    dataType = smeCollection.GetDataType();
                                }
                                else if(rootSme is IProperty smeProp)
                                {
                                    dataType = MappingHelper.ToAhiDataType(smeProp.ValueType);
                                }
                                reference.Extensions.Add(new Extension(
                                        name: "DataType",
                                        valueType: DataTypeDefXsd.String,
                                        value: dataType));

                                await _aasRegistryService.SaveSubmodelElement(reference, assetId.ToString(), first: false);
                                break;
                            }
                            case AttributeTypeConstants.TYPE_RUNTIME:
                            {
                                await _runtimeAssetAttributeHandler.AddAttributeAsync(attribute, inputAttributes != null ? inputAttributes.ToList() : null, isTemplate: false);
                                break;
                            }
                            case AttributeTypeConstants.TYPE_DYNAMIC:
                            {
                                var dynamicPayload = JObject.FromObject(attribute.Payload).ToObject<AssetAttributeDynamic>();
                                if (attribute.DataType is not DataTypeConstants.TYPE_INTEGER and not DataTypeConstants.TYPE_DOUBLE)
                                {
                                    attribute.DecimalPlace = null;
                                    attribute.ThousandSeparator = null;
                                }

                                var deviceId = new Extension(name: "DeviceId", valueType: DataTypeDefXsd.String, value: dynamicPayload.DeviceId);
                                var metricKey = new Extension(name: "MetricKey", valueType: DataTypeDefXsd.String, value: dynamicPayload.MetricKey);
                                var dataType = new Extension(name: "DataType", valueType: DataTypeDefXsd.String, value: attribute.DataType);
                                var snapShot = TimeSeriesHelper.CreateEmptySnapshot(attribute.DataType);
                                var smc = new SubmodelElementCollection()
                                {
                                    DisplayName = [new LangStringNameType("en-US", attribute.Name)],
                                    IdShort = attribute.Id.ToString(),
                                    Category = attribute.AttributeType,
                                    Extensions = [deviceId, metricKey, dataType],
                                    Value = [snapShot]
                                };

                                await _aasRegistryService.SaveSubmodelElement(smc, assetId.ToString(), first: false);

                                var snapShotSeries = await _timeSeriesService.GetDeviceMetricSnapshot(dynamicPayload.DeviceId, dynamicPayload.MetricKey);
                                if (snapShotSeries is not null && snapShotSeries.v != null)
                                    //await _aasRegistryService.UpdateSnapshot(assetId, attribute.Id, snapShotSeries);
                                    smc.UpdateSnapshot(snapShotSeries);

                                break;
                            }
                            case AttributeTypeConstants.TYPE_COMMAND:
                            {
                                var commandPayload = JObject.FromObject(attribute.Payload).ToObject<AssetCommandAttribute>();

                                var deviceId = new Extension(name: "DeviceId", valueType: DataTypeDefXsd.String, value: commandPayload.DeviceId);
                                var metricKey = new Extension(name: "MetricKey", valueType: DataTypeDefXsd.String, value: commandPayload.MetricKey);
                                var dataType = new Extension(name: "DataType", valueType: DataTypeDefXsd.String, value: DataTypeConstants.TYPE_INTEGER);

                                var snapShot = TimeSeriesHelper.CreateEmptySnapshot(DataTypeConstants.TYPE_INTEGER);
                                var smc = new SubmodelElementCollection()
                                {
                                    DisplayName = [new LangStringNameType("en-US", attribute.Name)],
                                    IdShort = attribute.Id.ToString(),
                                    Category = attribute.AttributeType,
                                    Extensions = [deviceId, metricKey, dataType],
                                    Value = [snapShot]
                                };

                                await _aasRegistryService.SaveSubmodelElement(smc, assetId.ToString(), first: false);

                                var snapShotSeries = await _timeSeriesService.GetDeviceMetricSnapshot(commandPayload.DeviceId, commandPayload.MetricKey);
                                if (snapShotSeries is not null && snapShotSeries.v != null)
                                {
                                    //await _aasRegistryService.UpdateSnapshot(assetId, attribute.Id, snapShotSeries);
                                    smc.UpdateSnapshot(snapShotSeries);
                                }

                                break;
                            }
                        }
                        break;
                    }
                    case PatchActionConstants.EDIT:
                    {
                        path = operation.path.Replace("/", "");
                        if (Guid.TryParse(path, out attributeId))
                        {
                            var updateAttribute = operation.value.ToJson().FromJson<AssetAttributeCommand>();
                            updateAttribute.AssetId = assetId;
                            updateAttribute.Id = attributeId;
                            updateAttribute.DecimalPlace = _aasApiHelper.GetAttributeDecimalPlace(updateAttribute);

                            switch (updateAttribute.AttributeType)
                            {
                                case AttributeTypeConstants.TYPE_STATIC:
                                {
                                    var smeIdPath = updateAttribute.Id.ToString();
                                    var smeResult = await _aasRegistryService.GetSubmodelElementByPathSubmodelRepo(assetId.ToString(), smeIdPath, LevelEnum.Deep, ExtentEnum.WithoutBlobValue);
                                    var property = smeResult as IProperty;
                                    property.DisplayName = [new LangStringNameType("en-US", updateAttribute.Name)];
                                    property.Value = updateAttribute.Value;
                                    await _timeSeriesService.AddStaticSeries(assetId, attributeId, series: TimeSeriesHelper.BuildSeriesDto(value: updateAttribute.Value));
                                    await _aasRegistryService.ReplaceSubmodelElementByPath(assetId.ToString(), smeIdPath, property);

                                    await _eventPublisher.Publish(AasEvents.SubmodelElementUpdated, new { attribute = property, aasId = assetId });
                                    await _eventPublisher.Publish(AasEvents.AasUpdated, assetId);
                                    break;
                                }
                                case AttributeTypeConstants.TYPE_ALIAS:
                                {
                                    var smeIdPath = updateAttribute.Id.ToString();
                                    var smeResult = await _aasRegistryService.GetSubmodelElementByPathSubmodelRepo(assetId.ToString(), smeIdPath, LevelEnum.Deep, ExtentEnum.WithoutBlobValue);
                                    var property = smeResult as ReferenceElement;
                                    property.DisplayName = [new LangStringNameType("en-US", updateAttribute.Name)];

                                    if (updateAttribute.Payload.AliasAssetId.HasValue && updateAttribute.Payload.AliasAttributeId.HasValue)
                                    {
                                        var (refAASet, refElements) = await _aasRegistryService.GetFullAasByIdAsync(updateAttribute.Payload.AliasAssetId.Value);
                                        var refAas = _aasRegistryService.ToAssetAdministrationShell(refAASet.AssetAdministrationShell);

                                        var aliasSmelement = refElements.First(sme => sme.IdShort == updateAttribute.Payload.AliasAttributeId.ToString());

                                        property.Value = new Reference(ReferenceTypes.ModelReference, [refAas.ToKey(), new Key(KeyTypes.Submodel, refAas.IdShort), aliasSmelement.ToKey()]);
                                    }

                                    await _timeSeriesService.AddStaticSeries(assetId, attributeId, series: TimeSeriesHelper.BuildSeriesDto(value: updateAttribute.Value));

                                    var (rootAas, smId, rootSme, aliasPath) = await _aasApiHelper.GetRootAliasSme(property);

                                    property.Extensions = new List<IExtension>
                                        {
                                            new Extension(
                                                name: "RootAasIdShort",
                                                valueType: DataTypeDefXsd.String,
                                                value: rootAas.IdShort?.ToString()),
                                            new Extension(
                                                name: "RootSmId",
                                                valueType: DataTypeDefXsd.String,
                                                value: smId),
                                            new Extension(
                                                name: "RootSmeIdShort",
                                                valueType: DataTypeDefXsd.String,
                                                value: rootSme.IdShort?.ToString()),
                                            new Extension(
                                                name: "AliasPath",
                                                valueType: DataTypeDefXsd.String,
                                                value: aliasPath?.ToString())
                                        };

                                    var dataType = string.Empty;
                                    if (rootSme is ISubmodelElementCollection smeCollection)
                                    {
                                        dataType = smeCollection.GetDataType();
                                    }
                                    else if (rootSme is IProperty smeProp)
                                    {
                                        dataType = MappingHelper.ToAhiDataType(smeProp.ValueType);
                                    }
                                    property.Extensions.Add(new Extension(
                                            name: "DataType",
                                            valueType: DataTypeDefXsd.String,
                                            value: dataType));

                                    await _aasRegistryService.UpdateAlias(assetId.ToString(), smeIdPath, property, aliasPath);
                                    await _eventPublisher.Publish(AasEvents.AliasElementUpdated, new { attribute = property, aasId = assetId });
                                    await _eventPublisher.Publish(AasEvents.AasUpdated, assetId);
                                    break;
                                }

                                default:
                                    break;
                            }

                        }
                        break;
                    }

                    case PatchActionConstants.EDIT_TEMPLATE:
                        break;

                    case PatchActionConstants.REMOVE:
                        break;
                }

                resultModels.Add(resultModel);
            }
        }

        var result = new UpsertAssetAttributeDto
        {
            Data = resultModels
        };

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAssetByIdAsync([FromRoute] Guid id)
    {
        //var sw = new Stopwatch();
        //sw.Start();
        var (aas, submodelElements) = await _aasRegistryService.GetDetailAasByIdAsync(id);
        //sw.Stop();
        //Console.WriteLine($"*******************Detail get data: {sw.Elapsed.TotalMilliseconds} ms");

        //check template
        var aasTemplateIdShort = aas.TemplateId;
        var isTemplateDeleted = false;
        if (!string.IsNullOrEmpty(aasTemplateIdShort))
        {
            isTemplateDeleted = await _aasRegistryService.IsAssetDeleted(aasTemplateIdShort);
        }

        //sw.Restart();
        var attributes = await _aasApiHelper.ToAttributes(submodelElements, isTemplateDeleted: isTemplateDeleted);
        //sw.Stop();
        //Console.WriteLine($"*******************Detail convert data: {sw.Elapsed.TotalMilliseconds} ms");

        return Ok(_aasApiHelper.ToGetAssetDtoFromDB(aas, aas.Parent, attributes));
    }

    [HttpGet("{id}/fetch")]
    public async Task<IActionResult> FetchAsync(Guid id)
    {
        //var sw = new Stopwatch();
        //sw.Start();
        var (aas, smes) = await _aasRegistryService.GetDetailAasByIdAsync(id);
        //sw.Stop();

        //Console.WriteLine($"Fetch get data: {sw.Elapsed.TotalMilliseconds} ms");

        //sw.Restart();
        var aasTemplateIdShort = aas.TemplateId;
        var isTemplateDeleted = false;
        if (!string.IsNullOrEmpty(aasTemplateIdShort))
        {
            isTemplateDeleted = await _aasRegistryService.IsAssetDeleted(aasTemplateIdShort);
        }
        var attributes = await _aasApiHelper.ToAttributes(smes, isTemplateDeleted);

        //sw.Stop();
        //Console.WriteLine($"Fetch convert data: {sw.Elapsed.TotalMilliseconds} ms");

        return Ok(_aasApiHelper.ToGetAssetSimpleDtoFromDB(aas, attributes: attributes));
    }

    [HttpGet("{id}/snapshot")]
    public async Task<IActionResult> GetAttributeSnapshotAsync(Guid id)
    {
        //var sw = new Stopwatch();
        //sw.Start();
        var (aas, submodelElements) = await _aasRegistryService.GetDetailAasByIdAsync(id);
        //sw.Stop();
        //Console.WriteLine($"snapshot get data: {sw.Elapsed.TotalMilliseconds} ms");

        //sw.Restart();
        var attributes = new List<AttributeDto>();

        foreach (var sme in submodelElements)
        {
            var attr = await _aasApiHelper.ToAttributeDto(sme);
            if (attr is not null)
                attributes.Add(attr);
        }

        //sw.Stop();
        //Console.WriteLine($"snapshot convert data: {sw.Elapsed.TotalMilliseconds} ms");

        return Ok(new HistoricalDataDto
        {
            AssetId = id,
            AssetName = aas.AssetAdministrationShell.DisplayName.FirstOrDefault()?.Text ?? aas.IdShort,
            Attributes = attributes
        });
    }

    [HttpPost("paths")]
    public async Task<IActionResult> GetAssetPathsAsync([FromBody] IEnumerable<Guid> ids)
    {
        var paths = new List<AssetPathDto>();
        var aasList = await _aasRegistryService.GetAASExtensions(ids: ids.Select(x => x.ToString()));

        foreach (var aas in aasList)
        {
            var pathId = aas.Extensions.FirstOrDefault(e => e.Name == "ResourcePath")?.Value
                .Replace("objects/", string.Empty)
                .Replace("children/", string.Empty);
            var pathName = aas.Extensions.FirstOrDefault(e => e.Name == "ResourcePathName")?.Value;
            var assetPathDto = new AssetPathDto(Guid.Parse(aas.Id), pathId, pathName);
            paths.Add(assetPathDto);
        }

        return Ok(paths);
    }

    [HttpPost("attributes/validate")]
    public async Task<IActionResult> ValidateAssetAttributesAsync(ValidateAssetAttributeList command)
    {
        command.ValidationType = ValidationType.Asset;
        // [TODO]
        var response = new ValidateAssetAttributeListResponse()
        {
            Properties = []
        };
        return Ok(response);
    }

    [HttpPost("attributes/validate/multiple")]
    public async Task<IActionResult> ValidateMultipleAssetAttributesAsync(ValidateMultipleAssetAttributeList command)
    {
        command.ValidationType = ValidationType.Asset;
        // [TODO]
        var response = new List<ValidateMultipleAssetAttributeListResponse>();
        return Ok(response);
    }

    [HttpPost("attributes/runtime/publish")]
    public async Task<IActionResult> PublishRuntimeValue([FromQuery] Guid attributeId, [FromBody] TimeSeriesDto seriesDto)
    {
        _aasRegistryService.PublishRuntime(attributeId, seriesDto);
        return Ok(true);
    }

    [HttpPost("hierarchy/search")]
    public async Task<IActionResult> SearchHierarchyAsync([FromBody] GetAssetHierarchy command)
    {
        var start = DateTime.UtcNow;

        var (assets, totalCount) = await _aasRegistryService.GetASSetsAsync(name: command.AssetName, filterParent: false, parentId: null, pageSize: command.PageSize, pageIndex: command.PageIndex);

        var assetHierarchies = assets != null ? assets.Select(_aasApiHelper.ToAssetHierarchy) : new List<AssetHierarchy>();

        AssetHierarchy lastAsset = null;
        var foundAssets = new List<AssetHierarchy>();

        foreach (var asset in assetHierarchies)
        {
            asset.IsFoundResult = asset.AssetName.Contains(command.AssetName);
            if (asset.IsFoundResult)
            {
                lastAsset = asset;
                lastAsset.Hierarchy = new List<Hierarchy>
                        {
                            Hierarchy.From(lastAsset)
                        };
                foundAssets.Add(lastAsset);
                continue;
            }
            else
            {
                lastAsset = null;
            }

            if (lastAsset == null)
                continue;
            var hierarchy = lastAsset.Hierarchy as List<Hierarchy>;
            var parentOfIdx = hierarchy.FindIndex(a => asset.AssetId == a.ParentAssetId);
            hierarchy.Insert(parentOfIdx > -1 ? parentOfIdx : 0, Hierarchy.From(asset));
        }

        var rootAssets = foundAssets.Select(GetAssetHierarchyDto.Create).OrderBy(x => x.RootAssetCreatedUtc).ThenBy(x => x.CreatedUtc);
        var totalMilliseconds = (long)(DateTime.UtcNow - start).TotalMilliseconds;
        var ahiResp = new BaseSearchResponse<GetAssetHierarchyDto>(totalMilliseconds, assets.Count(), command.PageSize, command.PageIndex, rootAssets);

        return Ok(ahiResp);
    }

    [HttpPost("{id}/attributes/{attributeId}/push")]
    public async Task<IActionResult> SendConfigurationToIotDevice([FromRoute] Guid id, [FromRoute] Guid attributeId, [FromBody] SendConfigurationToDeviceIot command)
    {
        var dto = new DateTimeOffset(DateTime.UtcNow);

        var seriesDto = new TimeSeriesDto() { ts = dto.ToUnixTimeSeconds(), v = command.Value, q = 192 };
        await _timeSeriesService.AddRuntimeSeries(id, attributeId, seriesDto);
        //await _aasRegistryService.UpdateSnapshot(id, attributeId, seriesDto);


        command.AssetId = id;
        command.AttributeId = attributeId;

        var (aas, smes) = await _aasRegistryService.GetDetailAasByIdAsync(id);
        var attributes = await _aasApiHelper.ToAttributes(smes);
        var ahiAsset = _aasApiHelper.ToGetAssetSimpleDtoFromDB(aas, parent: null, attributes: attributes);

        var response = await _aasApiHelper.SendConfigurationToDeviceIotAsync(command, ahiAsset, attributes, false);

        return Ok(response);
    }

    [HttpPost("device-metric-series")]
    public async Task<IActionResult> PublishDeviceMetricSeries([FromQuery] string deviceId, [FromQuery] string metricKey, [FromBody] TimeSeriesDto seriesDto)
    {
        await _timeSeriesService.AddDeviceMetricSeries(deviceId, metricKey, seriesDto);

        var attributes = await _aasRegistryService.GetDynamicAttributeSmc(deviceId, metricKey);
        foreach (var sMESet in attributes)
        {
            await _eventPublisher.Publish(AasEvents.SubmodelElementUpdated, new { attribute = sMESet, aasId = sMESet.SMSet.AASSet.IdShort });
            await _eventPublisher.Publish(AasEvents.AasUpdated, sMESet.SMSet.IdShort);
            Thread.Sleep(System.TimeSpan.FromSeconds(2));
        }

        return Ok(true);
    }

    [GeneratedRegex(@"""parentAssetId == (.+?)""")]
    private static partial Regex ParentFilterRegex();


    [HttpPost("feed-data")]
    public async Task<IActionResult> FeedData(string deviceId, string metricKey, string baseAliasAssetId, string baseAliasAttributeId, int parentChildLevel, int numberAsset, int numberAttriberPerAsset)
    {
        Guid parentID = Guid.Empty;
        int currentLevel = 1;
        //10k asset
        for (var i = 0; i < numberAsset; i++)
        {
            var assetJson = new JsonPatchDocument();
            assetJson.Add("/", new
            {
                name = $"asset_{DateTime.UtcNow.Ticks.ToString()}",
                retentionDays = 90,
                parentAssetId = parentID != Guid.Empty ? parentID.ToString() : ""
            });

            var upsert = await UpsertAssetAsync(assetJson);

            var assetResult = upsert as ObjectResult;
            var upsertResult = assetResult.Value as UpsertAssetDto;

            if (upsertResult != null && upsertResult.Data != null && upsertResult.Data.Count > 0 && upsertResult.Data[0].Values != null)
            {
                var asset = upsertResult.Data[0].Values as GetAssetSimpleDto;

                //100 attribute
                var attributeJson = new JsonPatchDocument();

                for (var j = 0; j < numberAttriberPerAsset; j++)
                {
                    var attribute = GetRandomAttributeObject(asset.Id.ToString(), j, deviceId, metricKey, baseAliasAssetId, baseAliasAttributeId);
                    attributeJson.Add("/", attribute);
                }

                await UpsertAttributesAsync(asset.Id, attributeJson);
                parentID = asset.Id;
            }

            currentLevel++;
            if (currentLevel > parentChildLevel)
            {
                parentID = Guid.Empty;
                currentLevel = 1;
            }
        }

        return Ok(true);
    }

    [HttpPost("cleardb")]
    public async Task<IActionResult> ClearDB()
    {
        // Queue up all delete operations asynchronously
        await _unitOfWork.AASSets.ClearDB();
        await _unitOfWork.SMSets.ClearDB();
        await _unitOfWork.SMESets.ClearDB();
        //await _unitOfWork.IValueSets.ClearDB();
        //await _unitOfWork.SValueSets.ClearDB();
        //await _unitOfWork.DValueSets.ClearDB();
        //await _unitOfWork.OValueSets.ClearDB();

        // Save changes to the database
        await _unitOfWork.CommitAsync();
        return Ok();
    }

    [HttpPost("create-test-entity")]
    public async Task<IActionResult> CreateEntity(Guid aasId)
    {
        var staticElement = new Property(valueType: DataTypeDefXsd.String)
        {
            DisplayName = [new LangStringNameType("en-US", "static under entity")],
            IdShort = Guid.NewGuid().ToString(),
            Value = "sameple value",
            Category = AttributeTypeConstants.TYPE_STATIC
        };

        var extension = new Extension(name: "EntityExtension1", valueType: DataTypeDefXsd.String, value: "Extension 1 value");

        var entity = new Entity(EntityType.CoManagedEntity, new List<IExtension> { extension }, category: "entity", idShort: Guid.NewGuid().ToString(), 
            displayName: [new LangStringNameType("en-US", "Entity display name 1")], statements: new List<ISubmodelElement> { staticElement }, globalAssetId: Guid.NewGuid().ToString());

        await _aasRegistryService.SaveSubmodelElement(entity, aasId.ToString(), first: false);

        return Ok();
    }

    private object GetRandomAttributeObject(string assetId, int index, string deviceId, string metricKey,
        string aliasAssetId, string aliasAttributeId)
    {
        Random rnd = new Random();

        var listAttributes = new List<object>()
            {
                new
                    {
                        assetId,
                        attributeType = "static",
                        dataType = "text",
                        id = Guid.NewGuid().ToString(),
                        name = $"static {index}",
                        value = index.ToString()
                    },
                new
                    {
                        assetId,
                        attributeType = "dynamic",
                        dataType = "int",
                        id = Guid.NewGuid().ToString(),
                        name = $"dynamic {index}",
                        thousandSeparator = true,
                        payload = new {
                            deviceId,
                            metricKey
                        }
                    },
                new
                    {
                        assetId,
                        attributeType = "alias",
                        id = Guid.NewGuid().ToString(),
                        name = $"alias {index}",
                        payload = new
                        {
                            aliasAssetId,
                            aliasAttributeId
                        }
                    }
            };

        return listAttributes[rnd.Next(0, listAttributes.Count)];
    }
}