namespace IO.Swagger.Controllers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AasxServerStandardBib;
using AasxServerStandardBib.Models;
using AasxServerStandardBib.Services;
using AHI.Infrastructure.SharedKernel.Extension;
using AHI.Infrastructure.SharedKernel.Model;
using Extensions;
using IO.Swagger.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using AasCore.Aas3_0;
using IO.Swagger.Registry.Lib.V3.Services;
using IO.Swagger.Registry.Lib.V3.Interfaces;
using IO.Swagger.Registry.Lib.V3.Models;
using I.Swagger.Registry.Lib.V3.Services;
using AasxServerDB.Helpers;
using AasxServerDB.Dto;
using Org.BouncyCastle.Asn1.Cms;

[ApiController]
[Route("dev/assettemplates")]
public partial class AhiAssetTemplatesController(
    RuntimeAssetAttributeHandler runtimeAssetAttributeHandler,
    EventPublisher eventPublisher,
    TimeSeriesService timeSeriesService,
    AasApiHelperService aasApiHelper,
    IAasRegistryService aasRegistryService
) : ControllerBase
{
    //search asset hierarchy
    [HttpPost]
    //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.AssetTemplate.ENTITY_NAME, "dev/assettemplates", Privileges.AssetTemplate.Rights.WRITE_ASSET_TEMPLATE)]
    public async Task<IActionResult> AddAsync([FromBody] AddAssetTemplate addElement)
    {
        var creator = new Reference(ReferenceTypes.ExternalReference, keys: [new Key(KeyTypes.Entity, value: Guid.Empty.ToString())]); // [TODO]
        var assetId = Guid.NewGuid();
        var aas = new AssetAdministrationShell(
            id: assetId.ToString(),
            assetInformation: new AssetInformation(
                assetKind: AssetKind.Type,
                globalAssetId: assetId.ToString(),
                specificAssetIds: null,
                assetType: "Template")
        )
        {
            Administration = new AdministrativeInformation()
            {
                Creator = creator
            },
            DisplayName = [new LangStringNameType("en-US", addElement.Name)],
            IdShort = assetId.ToString(),
            Submodels = [],
            Extensions = []
        };

        await aasRegistryService.SaveAASAsync(aas);

        var defaultSm = new Submodel(id: aas.Id)
        {
            Administration = new AdministrativeInformation()
            {
                Creator = creator
            },
            DisplayName = [new LangStringNameType("en-US", "Properties")],
            IdShort = assetId.ToString(),
            Kind = ModellingKind.Instance,
            SubmodelElements = []
        };
        await aasRegistryService.SaveSubmodelAsync(defaultSm, aas.IdShort);

        //process add attribute: wrap to jsonPath for add in property service
        if (addElement.Attributes != null && addElement.Attributes.Any())
        {
            var index = 0;
            foreach (var attribute in addElement.Attributes)
            {
                index++;
                //attribute.Id = Guid.NewGuid();
                attribute.AssetId = assetId;
                attribute.SequentialNumber = index;
                attribute.DecimalPlace = aasApiHelper.GetAttributeDecimalPlace(attribute);

                switch (attribute.AttributeType)
                {
                    case AttributeTypeConstants.TYPE_STATIC:
                    {
                        try
                        {
                            var property = new Property(
                            valueType: MappingHelper.ToAasDataType(attribute.DataType))
                            {
                                DisplayName = [new LangStringNameType("en-US", attribute.Name)],
                                IdShort = attribute.Id.ToString(),
                                Value = attribute.Value,
                                Category = attribute.AttributeType
                            };

                            await aasRegistryService.SaveSubmodelElement(property, assetId.ToString(), first: false);
                            break;
                        }
                        catch (Exception ex)
                        {

                            throw;
                        }
                        
                    }
                    case AttributeTypeConstants.TYPE_ALIAS:
                    {
                        try
                        {
                            var reference = new ReferenceElement()
                            {
                                Category = attribute.AttributeType,
                                DisplayName = [new LangStringNameType("en-US", attribute.Name)],
                                IdShort = attribute.Id.ToString()
                            };

                            await aasRegistryService.SaveSubmodelElement(reference, assetId.ToString(), first: false);
                            break;
                        }
                        catch (Exception ex)
                        {

                            throw;
                        }
                    }
                    case AttributeTypeConstants.TYPE_RUNTIME:
                    {
                        try
                        {
                            await runtimeAssetAttributeHandler.AddAttributeAsync(attribute, addElement.Attributes, cancellationToken: default);
                            break;
                        }
                        catch (Exception ex)
                        {

                            throw;
                        }
                    }
                    case AttributeTypeConstants.TYPE_DYNAMIC:
                    {
                        try
                        {
                            if (attribute.DataType is not DataTypeConstants.TYPE_INTEGER and not DataTypeConstants.TYPE_DOUBLE)
                            {
                                attribute.DecimalPlace = null;
                                attribute.ThousandSeparator = null;
                            }

                            var deviceId = new Extension(name: "DeviceTemplateId", valueType: DataTypeDefXsd.String, value: attribute.Payload.DeviceTemplateId.ToString());
                            var metricKey = new Extension(name: "MetricKey", valueType: DataTypeDefXsd.String, value: attribute.Payload.MetricKey);
                            var dataType = new Extension(name: "DataType", valueType: DataTypeDefXsd.String, value: attribute.DataType);
                            var markupName = new Extension(name: "MarkupName", valueType: DataTypeDefXsd.String, value: attribute.Payload.MarkupName);

                            //var snapShot = TimeSeriesHelper.CreateEmptySnapshot(attribute.DataType);
                            var smc = new SubmodelElementCollection()
                            {
                                DisplayName = [new LangStringNameType("en-US", attribute.Name)],
                                IdShort = attribute.Id.ToString(),
                                Category = attribute.AttributeType,
                                Extensions = [deviceId, metricKey, markupName, dataType]
                            };


                            await aasRegistryService.SaveSubmodelElement(smc, assetId.ToString(), first: false);
                            break;
                        }
                        catch (Exception ex)
                        {

                            throw;
                        }
                    }
                    case AttributeTypeConstants.TYPE_COMMAND:
                    {
                        try
                        {
                            var deviceId = new Extension(name: "DeviceTemplateId", valueType: DataTypeDefXsd.String, value: attribute.Payload.DeviceTemplateId.ToString());
                            var metricKey = new Extension(name: "MetricKey", valueType: DataTypeDefXsd.String, value: attribute.Payload.MetricKey);
                            var dataType = new Extension(name: "DataType", valueType: DataTypeDefXsd.String, value: DataTypeConstants.TYPE_INTEGER);
                            var markupName = new Extension(name: "MarkupName", valueType: DataTypeDefXsd.String, value: attribute.Payload.MarkupName);

                            //var snapShot = TimeSeriesHelper.CreateEmptySnapshot(DataTypeConstants.TYPE_INTEGER);
                            var smc = new SubmodelElementCollection()
                            {
                                DisplayName = [new LangStringNameType("en-US", attribute.Name)],
                                IdShort = attribute.Id.ToString(),
                                Category = attribute.AttributeType,
                                Extensions = [deviceId, metricKey, markupName, dataType]
                            };

                            await aasRegistryService.SaveSubmodelElement(smc, assetId.ToString(), first: false);
                            break;
                        }
                        catch (Exception ex)
                        {

                            throw;
                        }
                    }
                }
            }
        }

        var response = aasApiHelper.ToGetAssetSimpleDto(aas);

        return Created($"/dev/assettemplates/{response.Id}", response);
    }

    [HttpPut("{id}")]
    //[RightsAuthorizeFilter(Privileges.AssetTemplate.FullRights.WRITE_ASSET_TEMPLATE)]
    //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.AssetTemplate.ENTITY_NAME, "dev/assettemplates/{id}", Privileges.AssetTemplate.Rights.WRITE_ASSET_TEMPLATE)]
    public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] UpdateAssetTemplate command)
    {
        string path;
        int index = 0;
        foreach (var operation in command.Attributes)
        {
            index++;
            switch (operation.op)
            {
                case PatchActionConstants.ADD:
                {
                    var attribute = System.Text.Json.JsonSerializer.Serialize(operation.value).FromJson<AssetAttributeCommand>();
                    attribute.Id = Guid.NewGuid();
                    attribute.AssetId = id;
                    attribute.SequentialNumber = index;
                    attribute.DecimalPlace = aasApiHelper.GetAttributeDecimalPlace(attribute);

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

                            var attributeIdShort = await aasRegistryService.SaveSubmodelElement(property, id.ToString(), first: false);
                            await eventPublisher.Publish(AasEvents.TemplateElementUpdated, new TemplateAttributeUpdatedMessage { AASIdShort = id.ToString(), AttributeIdShort = attributeIdShort, Type = AttributeUpdatedType.Add });
                            break;
                        }
                        case AttributeTypeConstants.TYPE_ALIAS:
                        {
                            var reference = new ReferenceElement()
                            {
                                Category = attribute.AttributeType,
                                DisplayName = [new LangStringNameType("en-US", attribute.Name)],
                                IdShort = attribute.Id.ToString()
                            };

                            var attributeIdShort = await aasRegistryService.SaveSubmodelElement(reference, id.ToString(), first: false);
                            await eventPublisher.Publish(AasEvents.TemplateElementUpdated, new TemplateAttributeUpdatedMessage { AASIdShort = id.ToString(), AttributeIdShort = attributeIdShort, Type = AttributeUpdatedType.Add });
                            break;
                        }
                        case AttributeTypeConstants.TYPE_RUNTIME:
                        {
                            var lstUpdatedAttributes = command.Attributes.Select(x => x.value).ToList();
                            var inputAttributes = System.Text.Json.JsonSerializer.Serialize(lstUpdatedAttributes).FromJson<List<AssetAttributeCommand>>();

                            var (aasset, elements) = await aasRegistryService.GetFullAasByIdAsync(id);

                            inputAttributes.AddRange(elements.Where(x => !string.IsNullOrEmpty(x.Category)).Select(x => new AssetAttributeCommand
                            {
                                Id = Guid.Parse(x.IdShort),
                                DataType = x.GetDataType()
                            }));

                            var attributeIdShort = await runtimeAssetAttributeHandler.AddAttributeAsync(attribute, inputAttributes, isTemplate: true);
                            await eventPublisher.Publish(AasEvents.TemplateElementUpdated, new TemplateAttributeUpdatedMessage { AASIdShort = id.ToString(), AttributeIdShort = attributeIdShort, Type = AttributeUpdatedType.Add });
                            break;
                        }
                        case AttributeTypeConstants.TYPE_DYNAMIC:
                        {
                            if (attribute.DataType is not DataTypeConstants.TYPE_INTEGER and not DataTypeConstants.TYPE_DOUBLE)
                            {
                                attribute.DecimalPlace = null;
                                attribute.ThousandSeparator = null;
                            }

                            var deviceId = new Extension(name: "DeviceTemplateId", valueType: DataTypeDefXsd.String, value: attribute.Payload.DeviceTemplateId.ToString());
                            var metricKey = new Extension(name: "MetricKey", valueType: DataTypeDefXsd.String, value: attribute.Payload.MetricKey);
                            var dataType = new Extension(name: "DataType", valueType: DataTypeDefXsd.String, value: attribute.DataType);
                            var markupName = new Extension(name: "MarkupName", valueType: DataTypeDefXsd.String, value: attribute.Payload.MarkupName);

                            //var snapShot = TimeSeriesHelper.CreateEmptySnapshot(attribute.DataType);
                            var smc = new SubmodelElementCollection()
                            {
                                DisplayName = [new LangStringNameType("en-US", attribute.Name)],
                                IdShort = attribute.Id.ToString(),
                                Category = attribute.AttributeType,
                                Extensions = [deviceId, metricKey, markupName, dataType]
                            };

                            var attributeIdShort = await aasRegistryService.SaveSubmodelElement(smc, id.ToString(), first: false);
                            await eventPublisher.Publish(AasEvents.TemplateElementUpdated, new TemplateAttributeUpdatedMessage { AASIdShort = id.ToString(), AttributeIdShort = attributeIdShort, Type = AttributeUpdatedType.Add });
                            break;
                        }
                        case AttributeTypeConstants.TYPE_COMMAND:
                        {
                            var commandPayload = JObject.FromObject(attribute.Payload).ToObject<AssetCommandAttribute>();

                            var deviceId = new Extension(name: "DeviceTemplateId", valueType: DataTypeDefXsd.String, value: attribute.Payload.DeviceTemplateId.ToString());
                            var metricKey = new Extension(name: "MetricKey", valueType: DataTypeDefXsd.String, value: attribute.Payload.MetricKey);
                            var dataType = new Extension(name: "DataType", valueType: DataTypeDefXsd.String, value: DataTypeConstants.TYPE_INTEGER);
                            var markupName = new Extension(name: "MarkupName", valueType: DataTypeDefXsd.String, value: attribute.Payload.MarkupName);

                            //var snapShot = TimeSeriesHelper.CreateEmptySnapshot(DataTypeConstants.TYPE_INTEGER);
                            var smc = new SubmodelElementCollection()
                            {
                                DisplayName = [new LangStringNameType("en-US", attribute.Name)],
                                IdShort = attribute.Id.ToString(),
                                Category = attribute.AttributeType,
                                Extensions = [deviceId, metricKey, markupName, dataType]
                            };

                            var attributeIdShort = await aasRegistryService.SaveSubmodelElement(smc, id.ToString(), first: false);
                            await eventPublisher.Publish(AasEvents.TemplateElementUpdated, new TemplateAttributeUpdatedMessage { AASIdShort = id.ToString(), AttributeIdShort = attributeIdShort, Type = AttributeUpdatedType.Add });
                            break;
                        }
                    }
                    break;
                }
                case PatchActionConstants.EDIT:
                {
                    path = operation.path.Replace("/", "");
                    if (Guid.TryParse(path, out Guid attributeId))
                    {
                        var updateAttribute = System.Text.Json.JsonSerializer.Serialize(operation.value).FromJson<AssetAttributeCommand>();
                        updateAttribute.AssetId = id;
                        updateAttribute.Id = attributeId;
                        updateAttribute.DecimalPlace = aasApiHelper.GetAttributeDecimalPlace(updateAttribute);

                        switch (updateAttribute.AttributeType)
                        {
                            case AttributeTypeConstants.TYPE_STATIC:
                            {
                                var smeIdPath = updateAttribute.Id.ToString();
                                var smeResult = await aasRegistryService.GetSubmodelElementByPathSubmodelRepo(id.ToString(), smeIdPath, LevelEnum.Deep, ExtentEnum.WithoutBlobValue);
                                var property = smeResult as IProperty;
                                property.DisplayName = [new LangStringNameType("en-US", updateAttribute.Name)];
                                property.Value = updateAttribute.Value;
                                property.ValueType = MappingHelper.ToAasDataType(updateAttribute.DataType);

                                await timeSeriesService.AddStaticSeries(id, attributeId, series: TimeSeriesHelper.BuildSeriesDto(value: updateAttribute.Value));
                                var attributeIdShort = await aasRegistryService.ReplaceSubmodelElementByPath(id.ToString(), smeIdPath, property);

                                await eventPublisher.Publish(AasEvents.TemplateElementUpdated, new TemplateAttributeUpdatedMessage { AASIdShort = id.ToString(), AttributeIdShort = attributeIdShort, Type = AttributeUpdatedType.Edit });
                                break;
                            }
                            case AttributeTypeConstants.TYPE_ALIAS:
                            {
                                var smeIdPath = updateAttribute.Id.ToString();
                                var smeResult = await aasRegistryService.GetSubmodelElementByPathSubmodelRepo(id.ToString(), smeIdPath, LevelEnum.Deep, ExtentEnum.WithoutBlobValue);
                                var property = smeResult as IReferenceElement;
                                property.DisplayName = [new LangStringNameType("en-US", updateAttribute.Name)];

                                await timeSeriesService.AddStaticSeries(id, attributeId, series: TimeSeriesHelper.BuildSeriesDto(value: updateAttribute.Value));
                                var attributeIdShort = await aasRegistryService.ReplaceSubmodelElementByPath(id.ToString(), smeIdPath, property);

                                await eventPublisher.Publish(AasEvents.TemplateElementUpdated, new TemplateAttributeUpdatedMessage { AASIdShort = id.ToString(), AttributeIdShort = attributeIdShort, Type = AttributeUpdatedType.Edit });
                                break;
                            }
                            case AttributeTypeConstants.TYPE_DYNAMIC:
                            {
                                var smeIdPath = updateAttribute.Id.ToString();

                                if (updateAttribute.DataType is not DataTypeConstants.TYPE_INTEGER and not DataTypeConstants.TYPE_DOUBLE)
                                {
                                    updateAttribute.DecimalPlace = null;
                                    updateAttribute.ThousandSeparator = null;
                                }

                                var deviceId = new Extension(name: "DeviceTemplateId", valueType: DataTypeDefXsd.String, value: updateAttribute.Payload.DeviceTemplateId.ToString());
                                var metricKey = new Extension(name: "MetricKey", valueType: DataTypeDefXsd.String, value: updateAttribute.Payload.MetricKey);
                                var dataType = new Extension(name: "DataType", valueType: DataTypeDefXsd.String, value: updateAttribute.DataType);
                                var markupName = new Extension(name: "MarkupName", valueType: DataTypeDefXsd.String, value: updateAttribute.Payload.MarkupName);

                                //var snapShot = TimeSeriesHelper.CreateEmptySnapshot(attribute.DataType);
                                var smc = new SubmodelElementCollection()
                                {
                                    DisplayName = [new LangStringNameType("en-US", updateAttribute.Name)],
                                    IdShort = updateAttribute.Id.ToString(),
                                    Category = updateAttribute.AttributeType,
                                    Extensions = [deviceId, metricKey, markupName, dataType]
                                };

                                var attributeIdShort = await aasRegistryService.ReplaceSubmodelElementByPath(id.ToString(), smeIdPath, smc);
                                await eventPublisher.Publish(AasEvents.TemplateElementUpdated, new TemplateAttributeUpdatedMessage { AASIdShort = id.ToString(), AttributeIdShort = attributeIdShort, Type = AttributeUpdatedType.Edit });
                                break;
                            }
                            case AttributeTypeConstants.TYPE_RUNTIME:
                            {
                                var smeIdPath = updateAttribute.Id.ToString();

                                var lstUpdatedAttributes = command.Attributes.Select(x => x.value).ToList();
                                var inputAttributes = System.Text.Json.JsonSerializer.Serialize(lstUpdatedAttributes).FromJson<List<AssetAttributeCommand>>();

                                var runtimePayload = JObject.FromObject(updateAttribute.Payload).ToObject<AssetAttributeRuntime>();
                                var smc = new SubmodelElementCollection()
                                {
                                    DisplayName = [new LangStringNameType("en-US", updateAttribute.Name)],
                                    IdShort = updateAttribute.Id.ToString(),
                                    Category = updateAttribute.AttributeType,
                                    Extensions = [new Extension(name: "DataType", valueType: DataTypeDefXsd.String, value: updateAttribute.DataType)]
                                };

                                if (runtimePayload != null && runtimePayload.EnabledExpression.HasValue && runtimePayload.EnabledExpression.Value)
                                {
                                    var (aasset, elements) = await aasRegistryService.GetFullAasByIdAsync(updateAttribute.AssetId);
                                    var aas = aasRegistryService.ToAssetAdministrationShell(aasset.AssetAdministrationShell);

                                    inputAttributes.AddRange(elements.Where(x => !string.IsNullOrEmpty(x.Category)).Select(x => new AssetAttributeCommand
                                    {
                                        Id = Guid.Parse(x.IdShort),
                                        DataType = x.GetDataType()
                                    }));
                                    await runtimeAssetAttributeHandler.ValidateRuntimeAttribute(smc, aas, elements, updateAttribute, inputAttributes, runtimePayload);
                                }

                                var attributeIdShort = await aasRegistryService.ReplaceSubmodelElementByPath(id.ToString(), smeIdPath, smc);

                                await eventPublisher.Publish(AasEvents.TemplateElementUpdated, new TemplateAttributeUpdatedMessage { AASIdShort = id.ToString(), AttributeIdShort = attributeIdShort, Type = AttributeUpdatedType.Edit });

                                break;
                            }
                            case AttributeTypeConstants.TYPE_COMMAND:
                            {
                                var smeIdPath = updateAttribute.Id.ToString();
                                var commandPayload = JObject.FromObject(updateAttribute.Payload).ToObject<AssetCommandAttribute>();

                                var deviceId = new Extension(name: "DeviceTemplateId", valueType: DataTypeDefXsd.String, value: updateAttribute.Payload.DeviceTemplateId.ToString());
                                var metricKey = new Extension(name: "MetricKey", valueType: DataTypeDefXsd.String, value: updateAttribute.Payload.MetricKey);
                                var dataType = new Extension(name: "DataType", valueType: DataTypeDefXsd.String, value: DataTypeConstants.TYPE_INTEGER);
                                var markupName = new Extension(name: "MarkupName", valueType: DataTypeDefXsd.String, value: updateAttribute.Payload.MarkupName);

                                //var snapShot = TimeSeriesHelper.CreateEmptySnapshot(DataTypeConstants.TYPE_INTEGER);
                                var smc = new SubmodelElementCollection()
                                {
                                    DisplayName = [new LangStringNameType("en-US", updateAttribute.Name)],
                                    IdShort = updateAttribute.Id.ToString(),
                                    Category = updateAttribute.AttributeType,
                                    Extensions = [deviceId, metricKey, markupName, dataType]
                                };

                                var attributeIdShort = await aasRegistryService.ReplaceSubmodelElementByPath(id.ToString(), smeIdPath, smc);
                                await eventPublisher.Publish(AasEvents.TemplateElementUpdated, new TemplateAttributeUpdatedMessage { AASIdShort = id.ToString(), AttributeIdShort = attributeIdShort, Type = AttributeUpdatedType.Edit });
                                break;
                            }
                        }

                    }
                    break;
                }

                case PatchActionConstants.EDIT_TEMPLATE:
                    break;
                case PatchActionConstants.REMOVE:
                {
                    path = operation.path.Replace("/", "");
                    if (Guid.TryParse(path, out Guid attributeId))
                    {
                        var deleted = await aasRegistryService.RemoveTemplateAttribute(id.ToString(), attributeId.ToString());
                        if (deleted)
                        {
                            await eventPublisher.Publish(AasEvents.TemplateElementUpdated, new TemplateAttributeUpdatedMessage { AASIdShort = id.ToString(), AttributeIdShort = attributeId.ToString(), Type = AttributeUpdatedType.Remove });
                        }
                    }
                    break;
                }
            }
        }

        return Accepted($"/dev/assettemplates/{id}", new UpdateAssetTemplateDto { Id = id });
    }

    [HttpGet("{id}", Name = "GetAssetTemplateById")]
    //[RightsAuthorizeFilter(Privileges.AssetTemplate.FullRights.READ_ASSET_TEMPLATE)]
    //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.AssetTemplate.ENTITY_NAME, "dev/assettemplates/{id}", Privileges.AssetTemplate.Rights.READ_ASSET_TEMPLATE)]
    public async Task<IActionResult> GetEntityByIdAsync([FromRoute] Guid id)
    {
        var (aas, elements) = await aasRegistryService.GetFullAasByIdAsync(id);
        var attributes = await aasApiHelper.ToTemplateAttributes(elements, Guid.Parse(aas.IdShort));
        var asset = aasApiHelper.ToGetAssetTemplateDto(aas, attributes);
        return Ok(asset);
    }

    [HttpPost("search")]
    //[RightsAuthorizeFilter(Privileges.AssetTemplate.FullRights.READ_ASSET_TEMPLATE)]
    //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.AssetTemplate.ENTITY_NAME, "dev/assettemplates/search", Privileges.AssetTemplate.Rights.READ_ASSET_TEMPLATE)]
    public async Task<IActionResult> SearchAsync([FromBody] GetAssetTemplateByCriteria command)
    {
        var (aassets, count) = await aasRegistryService.GetASSetsAsync(name: string.Empty, assetKind: AssetKind.Type, pageSize: int.MaxValue);

        var aass = aassets.Select(x => aasApiHelper.ToGetAssetTemplateDto(x));

        var ahiResp = new BaseSearchResponse<GetAssetTemplateDto>(
            duration: 0,
            totalCount: count,
            pageSize: command.PageSize,
            pageIndex: command.PageIndex,
            data: aass
        );

        return Ok(ahiResp);
    }

    [HttpPost("archive")]
    //[RightsAuthorizeFilter(Privileges.AssetTemplate.FullRights.READ_ASSET_TEMPLATE)]
    public async Task<IActionResult> ArchiveAsync([FromBody] ArchiveAssetTemplate command)
    {
        //var response = await _mediator.Send(command);
        //return Ok(response);
        return Ok();
    }

    [HttpPost("archive/verify")]
    //[RightsAuthorizeFilter(Privileges.AssetTemplate.FullRights.READ_ASSET_TEMPLATE)]
    public async Task<IActionResult> VerifyArchiveAsync([FromBody] VerifyAssetTemplate command)
    {
        //var response = await _mediator.Send(command);
        //return Ok(response);
        return Ok();
    }

    [HttpPost("retrieve")]
    //[RightsAuthorizeFilter(Privileges.AssetTemplate.FullRights.WRITE_ASSET_TEMPLATE)]
    public async Task<IActionResult> RetrieveAsync([FromBody] RetrieveAssetTemplate command)
    {
        //var response = await _mediator.Send(command);
        //return Ok(response);
        return Ok();
    }

    [HttpPost("import")]
    //[RightsAuthorizeFilter(Privileges.AssetTemplate.FullRights.WRITE_ASSET_TEMPLATE)]
    //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.AssetTemplate.ENTITY_NAME, "dev/assettemplates/import", Privileges.AssetTemplate.Rights.WRITE_ASSET_TEMPLATE)]
    public async Task<IActionResult> ImportAsync([FromBody] ImportFile command)
    {
        //command.ObjectType = FileEntityConstants.ASSET_TEMPLATE;
        //var response = await _mediator.Send(command);
        //return Ok(response);
        return Ok();
    }

    [HttpPost("export")]
    //[RightsAuthorizeFilter(Privileges.Configuration.FullRights.SHARE_CONFIGURATION)]
    //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.Configuration.ENTITY_NAME, "dev/assettemplates/export", Privileges.Configuration.Rights.SHARE_CONFIGURATION)]
    public async Task<IActionResult> ExportAsync([FromBody] ExportAssetTemplate command)
    {
        //command.ObjectType = FileEntityConstants.ASSET_TEMPLATE;
        //var response = await _mediator.Send(command);
        //return Ok(response);
        return Ok();
    }

    [HttpDelete]
    //[RightsAuthorizeFilter(Privileges.AssetTemplate.FullRights.DELETE_ASSET_TEMPLATE)]
    //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.AssetTemplate.ENTITY_NAME, "dev/assettemplates", Privileges.AssetTemplate.Rights.DELETE_ASSET_TEMPLATE)]
    public async Task<IActionResult> DeleteListAsync([FromBody] DeleteAssetTemplate command)
    {
        var response = await aasRegistryService.RemoveAssetTemplateAsync(command);
        return Ok(new BaseResponse(response, null));
    }

    [HttpPost("asset/{id}/create")]
    //[RightsAuthorizeFilter(Privileges.AssetTemplate.FullRights.WRITE_ASSET_TEMPLATE)]
    //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.AssetTemplate.ENTITY_NAME, "dev/assettemplates/asset/{id}/create", Privileges.AssetTemplate.Rights.WRITE_ASSET_TEMPLATE)]
    public async Task<IActionResult> CreateFromAssetAsync(Guid id)
    {
        //var command = new CreateAssetTemplateFromAsset(id);
        //var response = await _mediator.Send(command);
        //return Ok(response);
        return Ok();
    }

    [HttpGet("{id}/attributes/generate/assembly")]
    //[AllowAnonymous]
    public async Task<IActionResult> GenerateAssetTemplateAttributeAssemblyAsync(Guid id, [FromQuery] string token, [FromServices] ITokenService tokenService)
    {
        //var tokenValid = await tokenService.CheckTokenAsync(token);
        //if (!tokenValid)
        //{
        //    return NotFound(new { IsSuccess = false, Message = id });
        //}
        //var command = new GenerateAssetTemplateAttributeAssembly(id);
        //var response = await _mediator.Send(command);
        //return File(response.Data, "application/octet-stream", response.Name);
        return File(new byte[0], string.Empty);
    }

    [HttpHead("{id}")]
    //[RightsAuthorizeFilter(Privileges.AssetTemplate.FullRights.READ_ASSET_TEMPLATE, Privileges.Asset.FullRights.WRITE_ASSET)]
    //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, "", "dev/assettemplates/{id}",
    //Privileges.AssetTemplate.FullRights.READ_ASSET_TEMPLATE,
    //     Privileges.Asset.FullRights.WRITE_ASSET)]
    public async Task<IActionResult> CheckExistingAssetTemplateAsync([FromRoute] Guid id)
    {
        //var command = new CheckExistingAssetTemplate(new[] { id });
        //var response = await _mediator.Send(command);
        //return Ok(response);
        return Ok();
    }

    [HttpPost("exist")]
    //[RightsAuthorizeFilter(Privileges.AssetTemplate.FullRights.READ_ASSET_TEMPLATE, Privileges.Asset.FullRights.WRITE_ASSET)]
    //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, "", "dev/assettemplates/exist",
    //Privileges.AssetTemplate.FullRights.READ_ASSET_TEMPLATE,
    //    Privileges.Asset.FullRights.WRITE_ASSET)]
    public async Task<IActionResult> CheckExistingAssetTemplateAsync([FromBody] CheckExistingAssetTemplate command)
    {
        //var response = await _mediator.Send(command);
        //return Ok(response);
        return Ok();
    }

    [HttpGet("{id}/assets")]
    public async Task<IActionResult> GetAssetsByTemplateId(Guid id)
    {
        //var command = new GetAssetsByTemplateId(id);
        //var response = await _mediator.Send(command);
        //return Ok(response);
        return Ok();
    }

    [HttpGet("{id}/fetch")]
    public async Task<IActionResult> FetchAsync(Guid id)
    {
        var (aas, elements) = await aasRegistryService.GetFullAasByIdAsync(id);
        var attributes = await aasApiHelper.ToTemplateAttributes(elements, Guid.Parse(aas.IdShort));
        var asset = aasApiHelper.ToGetAssetTemplateDto(aas, attributes);
        return Ok(asset);
    }

    [HttpPost("attributes/validate")]
    public async Task<IActionResult> ValidateAssetAttributesAsync([FromBody] ValidateAssetAttributeList command)
    {
        command.ValidationType = ValidationType.AssetTemplate;
        // [TODO]
        var response = new ValidateAssetAttributeListResponse()
        {
            Properties = []
        };

        return Ok(response);
    }

    [HttpGet("{id}/validate")]
    public async Task<IActionResult> ValidateAssetTemplateAsync(Guid id)
    {
        //var command = new ValidateAssetTemplate(id);
        //var response = await _mediator.Send(command);
        //return Ok(response);
        return Ok();
    }

    [HttpPost("attributes/parse")]
    //[RightsAuthorizeFilter(Privileges.AssetTemplate.FullRights.WRITE_ASSET_TEMPLATE)]
    public async Task<IActionResult> ParseAsync([FromBody] ParseAttributeTemplate command)
    {
        //command.ObjectType = FileEntityConstants.ASSET_TEMPLATE_ATTRIBUTE;
        //var response = await _mediator.Send(command);
        //return Ok(response);
        return Ok();
    }

    [HttpPost("attributes/export")]
    //[RightsAuthorizeFilter(Privileges.Configuration.FullRights.SHARE_CONFIGURATION)]
    public async Task<IActionResult> ExportAttributeAsync([FromBody] ExportAssetTemplateAttribute command)
    {
        //command.ObjectType = FileEntityConstants.ASSET_TEMPLATE_ATTRIBUTE;
        //var response = await _mediator.Send(command);
        //return Ok(response);
        return Ok();
    }
}