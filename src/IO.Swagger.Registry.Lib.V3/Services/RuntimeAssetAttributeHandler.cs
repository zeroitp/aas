using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using AHI.Infrastructure.SharedKernel.Extension;
using AasxServerStandardBib.Models;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Globalization;
using AasxServerStandardBib.Services;
using AasxServerStandardBib;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis;
using System.Reflection;
using IO.Swagger.Registry.Lib.V3.Interfaces;
using I.Swagger.Registry.Lib.V3.Services;
using AasxServerDB.Helpers;
using AasxServerDB.Dto;

namespace IO.Swagger.Registry.Lib.V3.Services;

public class RuntimeAssetAttributeHandler(
    ILogger<RuntimeAssetAttributeHandler> logger,
    EventPublisher eventPublisher,
    TimeSeriesService timeSeriesService,
    AasApiHelperService aasApiHelper,
    IAasRegistryService aasRegistryService)
{
    public async Task<string> AddAttributeAsync(AssetAttributeCommand attribute, List<AssetAttributeCommand> inputAttributes, bool isTemplate = false)
    {
        var runtimePayload = JObject.FromObject(attribute.Payload).ToObject<AssetAttributeRuntime>();
        var smc = new SubmodelElementCollection()
        {
            DisplayName = [new LangStringNameType("en-US", attribute.Name)],
            IdShort = attribute.Id.ToString(),
            Category = attribute.AttributeType,
            Extensions = [new Extension(name: "DataType", valueType: DataTypeDefXsd.String, value: attribute.DataType)],
            Value = [TimeSeriesHelper.CreateEmptySnapshot(attribute.DataType)]
        };

        if (attribute.TemplateAttributeId != null)
        {
            smc.Extensions.Add(new Extension(name: "TemplateAttributeId", valueType: DataTypeDefXsd.String, value: attribute.TemplateAttributeId.ToString()));
        }

        if (runtimePayload != null && runtimePayload.EnabledExpression.HasValue && runtimePayload.EnabledExpression.Value)
        {
            var (aasset, elements) = await aasRegistryService.GetFullAasByIdAsync(attribute.AssetId);
            var aas = aasRegistryService.ToAssetAdministrationShell(aasset.AssetAdministrationShell);

            inputAttributes = inputAttributes != null ? inputAttributes.ToList() : new List<AssetAttributeCommand>();
            inputAttributes.AddRange(elements.Where(x => !string.IsNullOrEmpty(x.Category)).Select(x => new AssetAttributeCommand
            {
                Id = Guid.Parse(x.IdShort),
                DataType = x.GetDataType()
            }));
            await ValidateRuntimeAttribute(smc, aas, elements, attribute, inputAttributes, runtimePayload);
        }

        var smElementIdShort = await aasRegistryService.SaveSubmodelElement(smc, attribute.AssetId.ToString(), first: false);

        if (!isTemplate)
        {
            await eventPublisher.Publish(AasEvents.RuntimeElementUpdated, new { attribute = smc, aasId = attribute.AssetId });
            await eventPublisher.Publish(AasEvents.AasUpdated, attribute.AssetId);
        }

        return smElementIdShort;
    }

    public async Task AddAttributeAsync(AssetTemplateAttribute attribute, IEnumerable<AssetTemplateAttribute> inputAttributes, CancellationToken cancellationToken)
    {
        var smc = new SubmodelElementCollection()
        {
            DisplayName = [new LangStringNameType("en-US", attribute.Name)],
            IdShort = attribute.Id.ToString(),
            Category = attribute.AttributeType,
            Extensions = [new Extension(name: "DataType", valueType: DataTypeDefXsd.String, value: attribute.DataType)]
        };

        if (attribute.Payload != null && attribute.Payload.EnabledExpression)
        {
            var (aasset, elements) = await aasRegistryService.GetFullAasByIdAsync(attribute.AssetId.Value);
            var aas = aasRegistryService.ToAssetAdministrationShell(aasset.AssetAdministrationShell);
            await ValidateRuntimeAttribute(smc, aas, elements, attribute, inputAttributes, attribute.Payload);
        }

        await aasRegistryService.SaveSubmodelElement(smc, attribute.AssetId.ToString(), first: false);

        await eventPublisher.Publish(AasEvents.SubmodelElementUpdated, new { attribute = smc, aasId = attribute.AssetId });
        await eventPublisher.Publish(AasEvents.AasUpdated, attribute.AssetId);
    }

    private Task ValidateRuntimeAttribute(ISubmodelElementCollection smc, IAssetAdministrationShell? aas, IEnumerable<ISubmodelElement> currentSmeList,
        AssetTemplateAttribute attribute, IEnumerable<AssetTemplateAttribute> inputAttributes, AttributeMapping runtimePayload)
    {
        var runtime = new AssetAttributeRuntime
        {
            EnabledExpression = runtimePayload.EnabledExpression,
            Expression = runtimePayload.Expression,
            TriggerAttributeId = runtimePayload.TriggerAttributeId
        };
        return ValidateRuntimeAttribute(smc, aas, currentSmeList, attribute, inputAttributes, runtime);
    }

    public async Task ValidateRuntimeAttribute(ISubmodelElementCollection smc, IAssetAdministrationShell? aas, IEnumerable<ISubmodelElement> currentSmeList,
        AssetAttributeCommand attribute, IEnumerable<AssetAttributeCommand> inputAttributes, AssetAttributeRuntime runtimePayload)
    {
        //var assetId = attribute.AssetId;
        var targetValidateAttributes = new List<AssetTemplateAttributeValidationRequest>();
        if (inputAttributes != null && inputAttributes.Any())
        {
            targetValidateAttributes.AddRange(inputAttributes.Select(item => new AssetTemplateAttributeValidationRequest()
            {
                Id = item.Id,
                DataType = item.DataType
            }));
        }
        else
        {
            inputAttributes = new List<AssetAttributeCommand>();
        }

        var aliasAndTargetAliasPairs = await GetAliasTargetMappings(aas, currentSmeList);
        var attributes = currentSmeList.Where(x => !string.IsNullOrEmpty(x.Category)).Select(x =>
        {
            var newAttribute = aliasAndTargetAliasPairs.FirstOrDefault(r => r.Reference.IdShort == x.IdShort);
            return newAttribute.Reference != null
                ? new { x.IdShort, Element = newAttribute.TargetElement }
                : new { x.IdShort, Element = x };
        });
        targetValidateAttributes.AddRange(attributes.Select(att => new AssetTemplateAttributeValidationRequest()
        {
            Id = Guid.Parse(att.IdShort),
            DataType = MappingHelper.ToAhiDataType((att.Element as IProperty)?.ValueType ?? DataTypeDefXsd.String)
        }));

        targetValidateAttributes.AddRange(inputAttributes.Select(att => new AssetTemplateAttributeValidationRequest()
        {
            Id = att.Id,
            DataType = att.DataType
        }));

        var request = new AssetTemplateAttributeValidationRequest()
        {
            Id = Guid.Parse(smc.IdShort),
            DataType = attribute.DataType,
            Expression = runtimePayload.Expression,
            Attributes = targetValidateAttributes
        };
        var (validateResult, expression, matchedAttributes) = await ValidateExpression(request);
        if (!validateResult)
        {
            throw new Exception("Invalid expression");
        }

        runtimePayload.ExpressionCompile = expression;
        var triggers = CreateRuntimeTriggers(runtimePayload, matchedAttributes, inputAttributes.Select(x => x.Id).Concat(matchedAttributes).Distinct());

        smc.Extensions.AddRange([
            new Extension(name: "Expression", valueType: DataTypeDefXsd.String, value: runtimePayload.Expression),
            new Extension(name: "ExpressionCompile", valueType: DataTypeDefXsd.String, value: runtimePayload.ExpressionCompile),
            new Extension(name: "EnabledExpression", valueType: DataTypeDefXsd.Boolean, value: runtimePayload.EnabledExpression.ToString().ToLower(CultureInfo.InvariantCulture)),
            new Extension(name: "TriggerAttributeIds", valueType: DataTypeDefXsd.String, value: JsonConvert.SerializeObject(triggers)) // [NOTE] temp
        ]);

        if (runtimePayload.TriggerAttributeId.HasValue)
            smc.Extensions.Add(new Extension(name: "TriggerAttributeId", valueType: DataTypeDefXsd.String, value: runtimePayload.TriggerAttributeId.ToString()));
    }

    private async Task ValidateRuntimeAttribute(ISubmodelElementCollection smc, IAssetAdministrationShell? aas, IEnumerable<ISubmodelElement> currentSmeList,
        AssetTemplateAttribute attribute, IEnumerable<AssetTemplateAttribute> inputAttributes, AssetAttributeRuntime runtimePayload)
    {
        var assetId = attribute.AssetId;
        var targetValidateAttributes = new List<AssetTemplateAttributeValidationRequest>();
        if (inputAttributes.Any())
        {
            targetValidateAttributes.AddRange(inputAttributes.Select(item => new AssetTemplateAttributeValidationRequest()
            {
                Id = item.Id,
                DataType = item.DataType
            }));
        }

        var aliasAndTargetAliasPairs = await GetAliasTargetMappings(aas, currentSmeList);
        var attributes = currentSmeList.Select(x =>
        {
            var newAttribute = aliasAndTargetAliasPairs.FirstOrDefault(r => r.Reference.IdShort == x.IdShort);
            return newAttribute.Reference != null
                ? new { x.IdShort, Element = newAttribute.TargetElement }
                : new { x.IdShort, Element = x };
        });
        targetValidateAttributes.AddRange(attributes.Select(att => new AssetTemplateAttributeValidationRequest()
        {
            Id = Guid.Parse(att.IdShort),
            DataType = MappingHelper.ToAhiDataType((att.Element as IProperty)?.ValueType ?? DataTypeDefXsd.String)
        }));

        targetValidateAttributes.AddRange(inputAttributes.Select(att => new AssetTemplateAttributeValidationRequest()
        {
            Id = att.Id,
            DataType = att.DataType
        }));

        var request = new AssetTemplateAttributeValidationRequest()
        {
            Id = Guid.Parse(smc.IdShort),
            DataType = attribute.DataType,
            Expression = runtimePayload.Expression,
            Attributes = targetValidateAttributes
        };
        var (validateResult, expression, matchedAttributes) = await ValidateExpression(request);
        if (!validateResult)
        {
            throw new Exception("Invalid expression");
        }

        runtimePayload.ExpressionCompile = expression;
        var triggers = CreateRuntimeTriggers(runtimePayload, matchedAttributes, inputAttributes.Select(x => x.Id).Concat(matchedAttributes).Distinct());

        smc.Extensions.AddRange([
            new Extension(name: "Expression", valueType: DataTypeDefXsd.String, value: runtimePayload.Expression),
            new Extension(name: "ExpressionCompile", valueType: DataTypeDefXsd.String, value: runtimePayload.ExpressionCompile),
            new Extension(name: "EnabledExpression", valueType: DataTypeDefXsd.Boolean, value: runtimePayload.EnabledExpression.ToString().ToLower(CultureInfo.InvariantCulture)),
            new Extension(name: "TriggerAttributeIds", valueType: DataTypeDefXsd.String, value: JsonConvert.SerializeObject(triggers)) // [NOTE] temp
        ]);

        if (runtimePayload.TriggerAttributeId.HasValue)
            smc.Extensions.Add(new Extension(name: "TriggerAttributeId", valueType: DataTypeDefXsd.String, value: runtimePayload.TriggerAttributeId.ToString()));
    }

    private async Task<IEnumerable<(IReferenceElement Reference, IAssetAdministrationShell TargetAas, string TargetSmId, ISubmodelElement TargetElement)>> GetAliasTargetMappings(IAssetAdministrationShell? aas, IEnumerable<ISubmodelElement> currentSmeList)
    {
        var validAliasAssetAttributes = currentSmeList.OfType<IReferenceElement>().ToArray();
        var aliasAndTargetAliasPairs = new List<(IReferenceElement Reference, IAssetAdministrationShell TargetAas, string TargetSmId, ISubmodelElement TargetElement)>();
        foreach (var reference in validAliasAssetAttributes)
        {
            var (aliasAas, smId, aliasSme, _) = await aasApiHelper.GetRootAliasSme(reference);
            if (aliasSme == null)
                continue;
            var pair = (reference, aliasAas, smId, aliasSme);
            aliasAndTargetAliasPairs.Add(pair);
        }
        return aliasAndTargetAliasPairs;
    }

    private IEnumerable<Guid> CreateRuntimeTriggers(
        AssetAttributeRuntime runtimePayload,
        IEnumerable<Guid> matchedAttributes,
        IEnumerable<Guid> triggerAttributeIds
    )
    {
        var triggers = new List<Guid>();
        if (runtimePayload.TriggerAttributeId != null)
        {
            var exist = triggerAttributeIds.Contains(runtimePayload.TriggerAttributeId.Value);
            if (!exist)
            {
                throw new Exception("Trigger not found");
            }

            Guid? triggerAttributeId = runtimePayload.TriggerAttributeId.Value;
            if (!matchedAttributes.Contains(triggerAttributeId.Value))
            {
                triggers.Add(triggerAttributeId.Value);
            }
        }

        foreach (var attributeId in matchedAttributes)
        {
            triggers.Add(attributeId);
        }
        return triggers;
    }

    public async Task<(bool, string, HashSet<Guid>)> ValidateExpression(AssetTemplateAttributeValidationRequest request)
    {
        var expressionValidate = request.Expression;

        // *** TODO: NOW VALUE WILL NOT IN VALUE COLUMN ==> now alway true
        if (string.IsNullOrWhiteSpace(expressionValidate))
            return (false, null, null);

        var matchedAttributes = new HashSet<Guid>();
        TryParseIdProperty(expressionValidate, matchedAttributes);
        if (matchedAttributes.Contains(request.Id))
        {
            // cannot self reference
            return (false, null, null);
        }

        //must not include command attribute in expression
        if (request.Attributes.Any(x => matchedAttributes.Contains(x.Id) && x.AttributeType == AttributeTypeConstants.TYPE_COMMAND))
        {
            throw new Exception("Invalid expression");
        }

        if (matchedAttributes.Any(id => !request.Attributes.Select(t => t.Id).Contains(id)))
        {
            throw new Exception("Target attributes not found");
        }

        var dataType = request.DataType;
        var dictionary = new Dictionary<string, object>();
        expressionValidate = BuildExpression(expressionValidate, request, dictionary);

        if (dataType == DataTypeConstants.TYPE_TEXT
            && !request.Attributes.Any(x => expressionValidate.Contains($"request[\"{x.Id}\"]")))
        {
            //if expression contain special character, we need to escape it one more time
            expressionValidate = expressionValidate.ToJson();
        }

        try
        {
            logger.LogTrace(expressionValidate);

            var scriptOptions = ScriptOptions.Default;

            // Add reference to mscorlib
            var mscorlib = typeof(object).GetTypeInfo().Assembly;
            var systemCore = typeof(System.Linq.Enumerable).GetTypeInfo().Assembly;

            var references = new[] { mscorlib, systemCore };
            scriptOptions = scriptOptions.AddReferences(references);
            scriptOptions = scriptOptions.AddImports("System");

            var value = await CSharpScript.EvaluateAsync(expressionValidate, options: scriptOptions, globals: new RuntimeExpressionGlobals { request = dictionary });
            if (!string.IsNullOrWhiteSpace(value.ToString()))
            {
                var result = value.ParseResultWithDataType(dataType);
                return (result, expressionValidate, matchedAttributes);
            }
        }
        catch (Exception exc)
        {
            logger.LogError(exc, exc.Message);
        }
        return (false, null, null);
    }

    private bool TryParseIdProperty(string expressionValidate, HashSet<Guid> matchedAttributes)
    {
        var m = Regex.Match(expressionValidate, RegexConstants.PATTERN_EXPRESSION_KEY, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(10));
        while (m.Success)
        {
            if (!Guid.TryParse(m.Groups[1].Value, out var idProperty))
                return false;
            if (!matchedAttributes.Contains(idProperty))
                matchedAttributes.Add(idProperty);
            m = m.NextMatch();
        }
        return true;
    }

    private string BuildExpression(string expressionValidate, AssetTemplateAttributeValidationRequest request, Dictionary<string, object> dictionary)
    {
        foreach (var element in request.Attributes)
        {
            object value = null;
            switch (element.DataType?.ToLower())
            {
                case DataTypeConstants.TYPE_DOUBLE:
                    expressionValidate = expressionValidate.Replace($"${{{element.Id}}}$", $"Convert.ToDouble(request[\"{element.Id}\"])");
                    value = 1.0;
                    break;
                case DataTypeConstants.TYPE_INTEGER:
                    expressionValidate = expressionValidate.Replace($"${{{element.Id}}}$", $"Convert.ToInt32(request[\"{element.Id}\"])");
                    value = 1;
                    break;
                case DataTypeConstants.TYPE_BOOLEAN:
                    expressionValidate = expressionValidate.Replace($"${{{element.Id}}}$", $"Convert.ToBoolean(request[\"{element.Id}\"])");
                    value = true;
                    break;
                case DataTypeConstants.TYPE_TIMESTAMP:
                    expressionValidate = expressionValidate.Replace($"${{{element.Id}}}$", $"Convert.ToDouble(request[\"{element.Id}\"])");
                    value = (double)1;
                    break;
                case DataTypeConstants.TYPE_DATETIME:
                    expressionValidate = expressionValidate.Replace($"${{{element.Id}}}$", $"Convert.ToDateTime(request[\"{element.Id}\"])");
                    value = new DateTime(1970, 1, 1);
                    break;
                case DataTypeConstants.TYPE_TEXT:
                    expressionValidate = expressionValidate.Replace($"${{{element.Id}}}$", $"request[\"{element.Id}\"].ToString()");
                    value = "default";
                    break;
            }
            dictionary[element.Id.ToString()] = value;
        }
        return expressionValidate;
    }
}