/********************************************************************************
* Copyright (c) {2019 - 2024} Contributors to the Eclipse Foundation
*
* See the NOTICE file(s) distributed with this work for additional
* information regarding copyright ownership.
*
* This program and the accompanying materials are made available under the
* terms of the Apache License Version 2.0 which is available at
* https://www.apache.org/licenses/LICENSE-2.0
*
* SPDX-License-Identifier: Apache-2.0
********************************************************************************/

namespace IO.Swagger.Registry.Lib.V3.Services
{
    using AasxServerStandardBib.Logging;
    using IO.Swagger.Registry.Lib.V3.Interfaces;
    using IO.Swagger.Registry.Lib.V3.Models;
    using IO.Swagger.Registry.Lib.V3.Serializers;
    using Microsoft.IdentityModel.Tokens;
    using System.Collections.Generic;
    using System;
    using System.Linq;
    using System.Text.Json.Nodes;
    using AasxServerDB;
    using AasxServerDB.Entities;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using IO.Swagger.Models;
    using AasxServerStandardBib.Interfaces;
    using AasxServerStandardBib.Exceptions;
    using Extensions;
    using AasSecurity.Exceptions;
    using IO.Swagger.Lib.V3.Interfaces;
    using System.Text;
    using Nodes = System.Text.Json.Nodes;
    using AasxServerStandardBib.Models;
    using AasxServerStandardBib.Services;
    using AasxServerStandardBib;
    using Newtonsoft.Json;
    using AHI.Infrastructure.Exception;
    using IO.Swagger.Lib.V3.Services;
    using AHI.Infrastructure.SharedKernel.Model;
    using AasxServerDB.Repositories;
    using AasxServerDB.Dto;
    using I.Swagger.Registry.Lib.V3.Services;
    using AasxServerDB.Helpers;
    using System.Data;

    public class AasRegistryService : IAasRegistryService
    {
        private readonly IAppLogger<AasRegistryService> _logger;
        private readonly IRegistryInitializerService _registryInitializerService;
        private readonly IAASUnitOfWork _unitOfWork;
        private readonly IMetamodelVerificationService _verificationService;
        private readonly IIdShortPathParserService _pathParserService;
        private readonly ILevelExtentModifierService _levelExtentModifierService;
        private readonly TimeSeriesService _timeSeriesService;
        private readonly EventPublisher _eventPublisher;
        private readonly NotificationService _notificationService;

        private const string SML_IdShortPath_Regex = @"\[(?<numbers>[\d]+)\]";
        private string _oprPrefix = string.Empty;

        public AasRegistryService(IAppLogger<AasRegistryService> logger, IRegistryInitializerService registryInitializerService,
            IAASUnitOfWork unitOfWork, IMetamodelVerificationService verificationService, IIdShortPathParserService pathParserService,
            ILevelExtentModifierService levelExtentModifierService, TimeSeriesService timeSeriesService, EventPublisher eventPublisher,
            NotificationService notificationService)
        {
            _logger = logger;
            _registryInitializerService = registryInitializerService;
            _unitOfWork = unitOfWork;
            _verificationService = verificationService;
            _pathParserService = pathParserService;
            _levelExtentModifierService = levelExtentModifierService;
            _timeSeriesService = timeSeriesService;
            _eventPublisher = eventPublisher;
            _notificationService = notificationService;
        }

        public AssetAdministrationShellDescriptor CreateAasDescriptorFromDB(AASSet aasDB)
        {
            return GlobalCreateAasDescriptorFromDB(aasDB);
        }

        public AssetAdministrationShellDescriptor GlobalCreateAasDescriptorFromDB(AASSet aasDB)
        {
            AssetAdministrationShellDescriptor ad = new AssetAdministrationShellDescriptor();
            //string asset = aas.assetRef?[0].Value;
            var globalAssetId = aasDB.GlobalAssetId;

            // ad.Administration.Version = aas.administration.version;
            // ad.Administration.Revision = aas.administration.revision;
            ad.IdShort = aasDB.IdShort;
            ad.Id = aasDB.Identifier ?? string.Empty;
            var e = new Models.Endpoint();
            e.ProtocolInformation = new ProtocolInformation();
            e.ProtocolInformation.Href =
                AasxServer.Program.externalRepository + "/shells/" +
                Base64UrlEncoder.Encode(ad.Id);
            // _logger.LogDebug("AAS " + ad.IdShort + " " + e.ProtocolInformation.Href);
            Console.WriteLine("AAS " + ad.IdShort + " " + e.ProtocolInformation.Href);
            e.Interface = "AAS-1.0";
            ad.Endpoints = new List<Models.Endpoint>
                {
                    e
                };
            ad.GlobalAssetId = globalAssetId;
            //
            ad.SpecificAssetIds = new List<SpecificAssetId>();
            var specificAssetId = new SpecificAssetId("AssetKind", aasDB.AssetKind, externalSubjectId: new Reference(ReferenceTypes.ExternalReference, new List<IKey>() { new Key(KeyTypes.GlobalReference, "assetKind") }));
            ad.SpecificAssetIds.Add(specificAssetId);

            // Submodels
            var submodelDBList = _unitOfWork.SMSets.AsFetchable().Where(s => s.AASId == aasDB.Id);
            if (submodelDBList.Any())
            {
                ad.SubmodelDescriptors = new List<SubmodelDescriptor>();
                foreach (var submodelDB in submodelDBList)
                {
                    SubmodelDescriptor sd = new SubmodelDescriptor();
                    sd.IdShort = submodelDB.IdShort;
                    sd.Id = submodelDB.Identifier ?? string.Empty;
                    var esm = new Models.Endpoint();
                    esm.ProtocolInformation = new ProtocolInformation();
                    esm.ProtocolInformation.Href =
                        AasxServer.Program.externalRepository + "/shells/" +
                        Base64UrlEncoder.Encode(ad.Id) + "/submodels/" +
                        Base64UrlEncoder.Encode(sd.Id);
                    esm.Interface = "SUBMODEL-1.0";
                    sd.Endpoints = new List<Models.Endpoint>
                        {
                            esm
                        };
                    sd.SemanticId = new Reference(ReferenceTypes.ExternalReference, new List<IKey>() { new Key(KeyTypes.GlobalReference, submodelDB.SemanticId) });
                    ad.SubmodelDescriptors.Add(sd);
                }
            }

            return ad;
        }

        //getFromAasRegistry from old implementation
        public List<AssetAdministrationShellDescriptor> GetAllAssetAdministrationShellDescriptors(string? assetKind = null, List<string?>? assetList = null, string? aasIdentifier = null)
        {
            List<AssetAdministrationShellDescriptor> result = new List<AssetAdministrationShellDescriptor>();

            if (aasIdentifier != null && assetList != null)
                return result;

            // Check for stored combined basyx list for descriptors by getRegistry()
            var aasDescriptors = _registryInitializerService.GetAasDescriptorsForSubmodelView();

            if (aasDescriptors != null && aasDescriptors.Count != 0)
            {
                foreach (var ad in aasDescriptors)
                {
                    var found = aasIdentifier == null && assetList.IsNullOrEmpty();
                    if (aasIdentifier != null)
                    {
                        if (aasIdentifier.Equals(ad.Id))
                        {
                            found = true;
                        }
                    }
                    if (found)
                        result.Add(ad);
                }

                return result;
            }

            var aasRegistry = _registryInitializerService.GetAasRegistry();
            if (aasRegistry != null)
            {
                AssetAdministrationShellDescriptor ad = null;
                foreach (var sme in aasRegistry.SubmodelElements)
                {
                    if (sme is SubmodelElementCollection smc)
                    {
                        string aasID = "";
                        string assetID = "";
                        string descriptorJSON = "";
                        foreach (var sme2 in smc.Value)
                        {
                            if (sme2 is Property p)
                            {
                                switch (p.IdShort)
                                {
                                    case "aasID":
                                        aasID = p.Value;
                                        break;
                                    case "assetID":
                                        assetID = p.Value;
                                        break;
                                    case "descriptorJSON":
                                        descriptorJSON = p.Value;
                                        break;
                                }

                            }
                        }
                        bool found = false;
                        if (aasIdentifier == null && assetList.IsNullOrEmpty())
                            found = true;
                        if (aasIdentifier != null)
                        {
                            if (aasID != "" && descriptorJSON != "")
                            {
                                if (aasIdentifier.Equals(aasID))
                                {
                                    found = true;
                                }
                            }
                        }
                        if (!assetList.IsNullOrEmpty())
                        {
                            if (assetID != "" && descriptorJSON != "")
                            {
                                if (assetList.Contains(assetID))
                                {
                                    found = true;
                                }
                            }
                        }
                        if (found)
                        {
                            //ad = JsonConvert.DeserializeObject<AssetAdministrationShellDescriptor>(descriptorJSON);
                            if (!string.IsNullOrEmpty(descriptorJSON))
                            {
                                JsonNode node = System.Text.Json.JsonSerializer.Deserialize<JsonNode>(descriptorJSON);
                                ad = DescriptorDeserializer.AssetAdministrationShellDescriptorFrom(node);
                            }
                            else
                            {
                                ad = null;
                            }
                            result.Add(ad);
                        }
                    }
                }
            }
            return result;
        }

        public Task<AASSet?> GetByIdAsync(Guid aasId)
        {
            return _unitOfWork.AASSets.AsQueryable().Where(x => x.IdShort == aasId.ToString()).FirstOrDefaultAsync();
        }
        public async Task<(List<AASSet>, int)> GetASSetsAsync(string name, bool filterParent = false, Guid? parentId = null, IEnumerable<string> ids = null, AssetKind assetKind = AssetKind.Instance, int pageSize = 10, int pageIndex = 0)
        {
            var query = _unitOfWork.AASSets.AsFetchable()
                .Where(x => (string.IsNullOrEmpty(name) || x.Name.Contains(name) || (string.IsNullOrEmpty(x.Name) && x.IdShort.Contains(name))));

            if (filterParent)
            {
                if (parentId != null)
                {
                    query = query.Where(x => x.Parent.IdShort == parentId.ToString());
                }
                else
                {
                    query = query.Where(x => x.Parent == null);
                }
            }

            if (ids != null && ids.Any())
            {
                query = query.Where(x => ids.Contains(x.IdShort));
            }

            query = query.Where(x => x.AssetKind.ToLower() == assetKind.ToString().ToLower());

            var count = await query.CountAsync();
            var aasSets = await query.Skip(pageSize * pageIndex).Take(pageSize).ToListAsync();
            return (aasSets, count);
        }

        public async Task<(AASSet aas, IEnumerable<ISubmodelElement> elements)> GetFullAasByIdAsync(Guid aasIdShort)
        {
            var aas = await _unitOfWork.AASSets.AsFetchable().Where(x => x.IdShort == aasIdShort.ToString()).FirstOrDefaultAsync();

            var smes = _unitOfWork.SMESets.AsFetchable().Where(x => x.AASIdShort == aasIdShort.ToString())
                .Select(x => Converter.CreateSME(x)).ToList();
            return (aas, smes);
        }

        public async Task<(AASSet aas, List<SMESet> submodelElement)> GetDetailAasByIdAsync(Guid id)
        {
            var aas = await _unitOfWork.AASSets.AsFetchable().Where(x => x.IdShort == id.ToString()).FirstOrDefaultAsync();
            if (aas != null)
            {
                var sMESets = await _unitOfWork.SMESets.AsFetchable()
                    .Where(x => x.AASIdShort == id.ToString() && !string.IsNullOrEmpty(x.Category))
                    .ToListAsync();

                return (aas, sMESets);
            }

            return (new AASSet(), new List<SMESet>());
        }

        public async Task<List<SMESet>> GetAssetAttributesByAssetId(Guid id)
        {
            return await _unitOfWork.SMESets.AsFetchable().Where(x => x.SMSet.AASSet.IdShort == id.ToString() && !string.IsNullOrEmpty(x.Category))
                .AsNoTracking().ToListAsync();
        }

        private void LoadSME(Submodel submodel, ISubmodelElement? sme, SMESet? smeSet, List<SMESet> SMEList)
        {
            //var sw = new Stopwatch();
            var smeSets = SMEList.Where(s => s.ParentSMEId == (smeSet != null ? smeSet.Id : null)).OrderBy(s => s.IdShort).ToList();

            foreach (var smel in smeSets)
            {
                //sw.Restart();
                // prefix of operation
                var split = !smel.SMEType.IsNullOrEmpty() ? smel.SMEType.Split(VisitorAASX.OPERATION_SPLIT) : [string.Empty];
                var oprPrefix = split.Length == 2 ? split[0] : string.Empty;
                smel.SMEType = split.Length == 2 ? split[1] : split[0];

                // create SME from database
                var nextSME = Converter.CreateSME(smel);

                // add sme to sm or sme 
                if (sme == null)
                {
                    submodel.Add(nextSME);
                }
                else
                {
                    switch (smeSet.SMEType)
                    {
                        case "RelA":
                            (sme as AnnotatedRelationshipElement).Annotations.Add((IDataElement)nextSME);
                            break;
                        case "SML":
                            (sme as SubmodelElementList).Value.Add(nextSME);
                            break;
                        case "SMC":
                            (sme as SubmodelElementCollection).Value.Add(nextSME);
                            break;
                        case "Ent":
                            (sme as Entity).Statements.Add(nextSME);
                            break;
                        case "Opr":
                            if (oprPrefix.Equals(VisitorAASX.OPERATION_INPUT))
                                (sme as Operation).InputVariables.Add(new OperationVariable(nextSME));
                            else if (oprPrefix.Equals(VisitorAASX.OPERATION_OUTPUT))
                                (sme as Operation).OutputVariables.Add(new OperationVariable(nextSME));
                            else if (oprPrefix.Equals(VisitorAASX.OPERATION_INOUTPUT))
                                (sme as Operation).InoutputVariables.Add(new OperationVariable(nextSME));
                            break;
                    }
                }

                // recursiv, call for child sme's
                switch (smel.SMEType)
                {
                    case "RelA":
                    case "SML":
                    case "SMC":
                    case "Ent":
                    case "Opr":
                        Console.WriteLine("------------De quy");
                        LoadSME(submodel, nextSME, smel, SMEList);
                        break;
                }
                //sw.Stop();
                //Console.WriteLine($"->>>>>>>>>> LoadSME {smel.SMEType} {smel.Name} take {sw.Elapsed.TotalMilliseconds}");
            }
        }

        public async Task SaveAASAsync(AssetAdministrationShell aas)
        {
            if (aas == null)
            {
                throw new Exception($"Could not proceed, as {nameof(aas)} is null.");
            }

            //Verify the body first
            //_verificationService.VerifyRequestBody(aas);

            var (found, _) = await IsAssetAdministrationShellPresent(aas.Id);
            if (found)
            {
                _logger.LogDebug($"Cannot create requested AAS !!");
                throw new DuplicateException($"AssetAdministrationShell with id {aas.Id} already exists.");
            }

            await CreateAssetAdministrationShellAsync(aas);
        }

        public async Task<(bool, int?)> IsAssetAdministrationShellPresent(string aasIdShort)
        {
            var id = await _unitOfWork.AASSets.AsFetchable().Where(x => x.IdShort == aasIdShort).Select(x => x.Id).FirstOrDefaultAsync();

            return (id > 0, id);
        }

        public async Task<IAssetAdministrationShell> CreateAssetAdministrationShellAsync(IAssetAdministrationShell aas)
        {
            try
            {
                var timeStamp = DateTime.UtcNow;
                aas.TimeStampCreate = timeStamp;
                aas.SetTimeStamp(timeStamp);

                var jsonAAS = System.Text.Json.JsonSerializer.Serialize(aas);

                var newAAS = new AASSet
                {
                    Identifier = aas.IdShort,
                    IdShort = aas.IdShort,
                    Name = aas.DisplayName?[0]?.Text,
                    AssetAdministrationShell = System.Text.Json.JsonSerializer.Deserialize<JAssetAdministrationShell>(jsonAAS),
                    AssetKind = aas.AssetInformation.AssetKind.ToString()
                };

                string parentId = aas.Extensions?.Where(x => x.Name == "ParentAssetId" && x.Value != null).Select(x => x.Value).FirstOrDefault();

                if (!string.IsNullOrEmpty(parentId))
                {
                    newAAS.Parent = await _unitOfWork.AASSets.AsQueryable().Where(x => x.IdShort == parentId).FirstOrDefaultAsync();
                }

                var resourcePath = aas.Extensions != null ? aas.Extensions.Where(x => x.Name == "ResourcePath").Select(x => x.Value).FirstOrDefault() : string.Empty;
                newAAS.ResourcePath = resourcePath;
                newAAS.TemplateId = newAAS.AssetAdministrationShell?.Administration?.TemplateId;

                await _unitOfWork.AASSets.AddAsync(newAAS);

                await _unitOfWork.CommitAsync();

                return aas;
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        public async Task SaveSubmodelAsync(Submodel body, string aasIdShort)
        {
            if (body == null)
            {
                throw new NotAllowed($"Cannot proceed as {nameof(body)} is null");
            }

            if (aasIdShort == null)
            {
                throw new NotAllowed($"Cannot proceed as {nameof(aasIdShort)} is null");
            }

            await CreateSubmodel(body, aasIdShort);
        }

        public async Task<ISubmodel> CreateSubmodel(ISubmodel newSubmodel, string aasIdShort)
        {
            //Verify the body first
            _verificationService.VerifyRequestBody(newSubmodel);
            var (found, _) = await IsSubmodelPresent(newSubmodel.Id);

            if (found)
            {
                _logger.LogDebug($"Cannot create requested Submodel !!");
                throw new DuplicateException($"Submodel with id {newSubmodel.Id} already exists.");
            }

            //Check if corresponding AAS exist. If yes, then add to the same environment
            if (!string.IsNullOrEmpty(aasIdShort))
            {
                var (aasFound, aasDBId) = await IsAssetAdministrationShellPresent(aasIdShort);
                if (aasFound)
                {
                    newSubmodel.SetAllParents(DateTime.UtcNow);
                    var timeStamp = DateTime.UtcNow;
                    newSubmodel.TimeStampCreate = timeStamp;
                    newSubmodel.SetTimeStamp(timeStamp);

                    var json = System.Text.Json.JsonSerializer.Serialize(newSubmodel);

                    try
                    {
                        await _unitOfWork.SMSets.AddAsync(new SMSet
                        {
                            Identifier = newSubmodel.IdShort,
                            IdShort = newSubmodel.IdShort,
                            AASId = aasDBId,
                            Submodel = System.Text.Json.JsonSerializer.Deserialize<JSubmodel>(json)
                        });

                        await _unitOfWork.CommitAsync();
                    }
                    catch (Exception ex)
                    {

                        throw;
                    }

                    return newSubmodel;
                }
            }

            throw new Exception("No package available in the server.");
        }

        public async Task<(bool, int?)> IsSubmodelPresent(string aasIdentifier)
        {
            var dbId = await _unitOfWork.SMSets.AsFetchable().Where(x => x.IdShort == aasIdentifier).Select(x => x.Id).FirstOrDefaultAsync();
            return (dbId > 0, dbId);
        }

        public async Task<string> SaveSubmodelElement(ISubmodelElement? body, string submodelIdShort, bool first)
        {
            if (body == null)
            {
                throw new NotAllowed($"Cannot proceed as {nameof(body)} is null");
            }
            //_logger.LogInformation($"Received request to create a new submodel element in the submodel with id {submodelIdShort}");
            if (submodelIdShort == null)
            {
                throw new NotAllowed($"Cannot proceed as {nameof(submodelIdShort)} is null");
            }

            var smElement = await CreateSubmodelElement(submodelIdShort, body, first);
            return smElement.IdShort;
        }

        public async Task<ISubmodelElement> CreateSubmodelElement(string submodelIdshort, ISubmodelElement newSubmodelElement, bool first)
        {
            //Create new SME
            var (submodel, smDbId) = await GetSubmodelById(submodelIdshort);

            var timeStamp = DateTime.UtcNow;
            newSubmodelElement = await SetAllParentsAndTimestamps(newSubmodelElement, submodel, timeStamp, timeStamp, smDbId.Value, smIdShort: submodel.IdShort, aasIdShort: submodel.IdShort);

            return newSubmodelElement;
        }

        private async Task<ISubmodelElement> SetAllParentsAndTimestamps(ISubmodelElement? submodelElement, IReferable? parent, DateTime timeStamp,
            DateTime timeStampCreate, int smDBId, int parentSMEId = 0, string smIdShort = "", string aasIdShort = "")
        {
            submodelElement.Parent = parent;
            submodelElement.TimeStamp = timeStamp;
            submodelElement.TimeStampTree = timeStamp;
            submodelElement.TimeStampCreate = timeStampCreate;

            submodelElement.SetTimeStamp(timeStamp);

            var json = JsonConvert.SerializeObject(submodelElement, new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });

            int dbSMEId;

            var element = CollectSMEData(submodelElement);
            element.RawJson = json;
            element.SMId = smDBId;
            element.Name = submodelElement.DisplayName[0].Text;
            element.Category = submodelElement.Category;
            element.ParentSMEId = parentSMEId > 0 ? parentSMEId : null;

            element.MetaData = await BuildSMEMetaData(element, submodelElement);

            element.SMIdShort = smIdShort;
            element.AASIdShort = aasIdShort;

            if (submodelElement.Extensions != null)
            {
                element.TemplateId = submodelElement.Extensions.Where(x => x.Name == "TemplateAttributeId").Select(x => x.Value).FirstOrDefault();
            }

            await _unitOfWork.SMESets.AddAsync(element);

            dbSMEId = element.Id;

            foreach (var childElement in submodelElement.EnumerateChildren())
            {
                await SetAllParentsAndTimestamps(childElement, submodelElement, timeStamp, timeStampCreate, smDBId, dbSMEId, smIdShort, aasIdShort);
            }

            await _unitOfWork.CommitAsync();

            return submodelElement;
        }

        private async Task<string> BuildSMEMetaData(SMESet element, ISubmodelElement submodelElement)
        {
            var jsonMetaData = "{}";
            var tempalateAttributeId = GetExtensionValue(submodelElement.Extensions, "TemplateAttributeId");
            switch (element.Category)
            {
                case AttributeTypeConstants.TYPE_DYNAMIC:
                case AttributeTypeConstants.TYPE_COMMAND:
                    var dynamicMetaData = new DynamicMetaData
                    {
                        DeviceId = GetExtensionValue(submodelElement.Extensions, "DeviceId"),
                        MetricKey = GetExtensionValue(submodelElement.Extensions, "MetricKey"),
                        DataType = GetExtensionValue(submodelElement.Extensions, "DataType")
                    };
                    jsonMetaData = JsonConvert.SerializeObject(dynamicMetaData);
                    break;
                case AttributeTypeConstants.TYPE_ALIAS:
                    var aliasMetaData = new AliasMetaData
                    {
                        AliasPath = GetExtensionValue(submodelElement.Extensions, "AliasPath")
                    };

                    if (submodelElement is IReferenceElement alias)
                    {
                        IAssetAdministrationShell aliasAas = null;
                        var aasId = alias?.Value?.Keys[0]?.Value;

                        var smIdRaw = alias?.Value?.Keys[1]?.Value;
                        ISubmodelElement aliasSme = alias;

                        if (!string.IsNullOrEmpty(smIdRaw))
                        {
                            var smeIdPath = alias.Value.Keys[2].Value;
                            aliasSme = await GetSubmodelElementByPathSubmodelRepo(smIdRaw, smeIdPath, LevelEnum.Deep, ExtentEnum.WithoutBlobValue) as ISubmodelElement;
                            aliasMetaData.RefAasId = aasId;
                            aliasMetaData.RefAttributeId = aliasSme.IdShort;
                        }
                    }

                    jsonMetaData = JsonConvert.SerializeObject(aliasMetaData);
                    break;
                case AttributeTypeConstants.TYPE_RUNTIME:
                    var runtimeMetaData = new RuntimeMetaData
                    {
                        DataType = GetExtensionValue(submodelElement.Extensions, "DataType"),
                        Expression = GetExtensionValue(submodelElement.Extensions, "Expression"),
                        ExpressionCompile = GetExtensionValue(submodelElement.Extensions, "ExpressionCompile"),
                        TriggerAttributeId = GetExtensionValue(submodelElement.Extensions, "TriggerAttributeId")
                    };

                    var enanbleExpression = GetExtensionValue(submodelElement.Extensions, "EnabledExpression");
                    var triggerAttributeIds = GetExtensionValue(submodelElement.Extensions, "TriggerAttributeIds");
                    runtimeMetaData.TriggerAttributeIds = JsonConvert.DeserializeObject<IEnumerable<string>>(triggerAttributeIds)?.ToArray();

                    if (bool.TryParse(enanbleExpression, out var value))
                    {
                        runtimeMetaData.EnabledExpression = value;
                    }

                    jsonMetaData = JsonConvert.SerializeObject(runtimeMetaData);
                    break;
                default:
                    break;
            }

            return jsonMetaData;
        }

        private string GetExtensionValue(List<IExtension> extensions, string name)
        {
            return extensions != null
            ? extensions.Where(x => x.Name == name).Select(x => x.Value).FirstOrDefault()
            : string.Empty;
        }

        private SMESet CollectSMEData(ISubmodelElement sme)
        {
            var semanticId = sme.SemanticId.GetAsIdentifier() ?? string.Empty;

            var currentDataTime = DateTime.UtcNow;
            var timeStamp = (sme.TimeStamp == default) ? currentDataTime : sme.TimeStamp;
            var timeStampCreate = (sme.TimeStampCreate == default) ? currentDataTime : sme.TimeStampCreate;
            var timeStampTree = (sme.TimeStampTree == default) ? currentDataTime : sme.TimeStampTree;

            var smeDB = new SMESet
            {
                //ParentSME = _parSME,
                SMEType = ShortSMEType(sme),
                TValue = string.Empty,
                SemanticId = semanticId,
                IdShort = sme.IdShort,
                TimeStamp = timeStamp,
                TimeStampCreate = timeStampCreate,
                TimeStampTree = timeStampTree
            };
            SetValues(sme, smeDB);

            return smeDB;
        }

        private void SetValues(ISubmodelElement sme, SMESet smeDB)
        {
            var lstOValue = smeDB.OValueSets ?? new List<OValueSet>();
            var lstSValue = smeDB.SValueSets ?? new List<SValueSet>();
            var lstIValue = smeDB.IValueSets ?? new List<IValueSet>();
            var lstDValue = smeDB.DValueSets ?? new List<DValueSet>();

            if (sme is RelationshipElement rel)
            {
                lstOValue.Add(new OValueSet { Attribute = "First", Value = Jsonization.Serialize.ToJsonObject(rel.First) });
                lstOValue.Add(new OValueSet { Attribute = "Second", Value = Jsonization.Serialize.ToJsonObject(rel.Second) });
            }
            else if (sme is AnnotatedRelationshipElement relA)
            {
                lstOValue.Add(new OValueSet { Attribute = "First", Value = Jsonization.Serialize.ToJsonObject(relA.First) });
                lstOValue.Add(new OValueSet { Attribute = "Second", Value = Jsonization.Serialize.ToJsonObject(relA.Second) });
            }
            else if (sme is Property prop)
            {
                if (prop.ValueId != null)
                    lstOValue.Add(new OValueSet { Attribute = "ValueId", Value = Jsonization.Serialize.ToJsonObject(prop.ValueId) });

                GetValueAndDataType(prop.Value ?? string.Empty, prop.ValueType, out var tValue, out var sValue, out var iValue, out var dValue);
                if (!tValue.IsNullOrEmpty())
                    smeDB.TValue = tValue;
                else
                    smeDB.TValue = "S";

                if (smeDB.TValue.Equals("S"))
                    lstSValue.Add(new SValueSet { Value = sValue, Annotation = Jsonization.Serialize.DataTypeDefXsdToJsonValue(prop.ValueType).ToString() });
                else if (smeDB.TValue.Equals("I"))
                    lstIValue.Add(new IValueSet { Value = iValue, Annotation = Jsonization.Serialize.DataTypeDefXsdToJsonValue(prop.ValueType).ToString() });
                else if (smeDB.TValue.Equals("D"))
                    lstDValue.Add(new DValueSet { Value = dValue, Annotation = Jsonization.Serialize.DataTypeDefXsdToJsonValue(prop.ValueType).ToString() });
            }
            else if (sme is MultiLanguageProperty mlp)
            {
                if (mlp.ValueId != null)
                    lstOValue.Add(new OValueSet { Attribute = "ValueId", Value = Jsonization.Serialize.ToJsonObject(mlp.ValueId) });

                if (mlp.Value == null || mlp.Value.Count == 0)
                    return;

                smeDB.TValue = "S";
                foreach (var sValueMLP in mlp.Value)
                    if (!sValueMLP.Text.IsNullOrEmpty())
                        lstSValue.Add(new SValueSet() { Annotation = sValueMLP.Language, Value = sValueMLP.Text });
            }
            else if (sme is AasCore.Aas3_0.Range range)
            {
                lstOValue.Add(new OValueSet { Attribute = "ValueType", Value = Jsonization.Serialize.DataTypeDefXsdToJsonValue(range.ValueType) });

                if (range.Min.IsNullOrEmpty() && range.Max.IsNullOrEmpty())
                    return;

                var hasValueMin = GetValueAndDataType(range.Min ?? string.Empty, range.ValueType, out var tableDataTypeMin, out var sValueMin, out var iValueMin, out var dValueMin);
                var hasValueMax = GetValueAndDataType(range.Max ?? string.Empty, range.ValueType, out var tableDataTypeMax, out var sValueMax, out var iValueMax, out var dValueMax);

                // determine which data types apply
                var tableDataType = "S";
                if (!hasValueMin && !hasValueMax) // no value is given
                    return;
                else if (hasValueMin && !hasValueMax) // only min is given
                {
                    tableDataType = tableDataTypeMin;
                }
                else if (!hasValueMin && hasValueMax) // only max is given
                {
                    tableDataType = tableDataTypeMax;
                }
                else if (hasValueMin && hasValueMax) // both values are given
                {
                    if (tableDataTypeMin == tableDataTypeMax) // dataType did not change
                    {
                        tableDataType = tableDataTypeMin;
                    }
                    else if (!tableDataTypeMin.Equals("S") && !tableDataTypeMax.Equals("S")) // both a number
                    {
                        tableDataType = "D";
                        if (tableDataTypeMin.Equals("I"))
                            dValueMin = Convert.ToDouble(iValueMin);
                        else if (tableDataTypeMax.Equals("I"))
                            dValueMax = Convert.ToDouble(iValueMax);
                    }
                    else // default: save in string
                    {
                        tableDataType = "S";
                        if (!tableDataTypeMin.Equals("S"))
                            sValueMin = tableDataTypeMin.Equals("I") ? iValueMin.ToString() : dValueMin.ToString();
                        if (!tableDataTypeMax.Equals("S"))
                            sValueMax = tableDataTypeMax.Equals("I") ? iValueMax.ToString() : dValueMax.ToString();
                    }
                }

                smeDB.TValue = tableDataType.ToString();
                if (tableDataType.Equals("S"))
                {
                    if (hasValueMin)
                        lstSValue.Add(new SValueSet { Value = sValueMin, Annotation = "Min" });
                    if (hasValueMax)
                        lstSValue.Add(new SValueSet { Value = sValueMax, Annotation = "Max" });
                }
                else if (tableDataType.Equals("I"))
                {
                    if (hasValueMin)
                        lstIValue.Add(new IValueSet { Value = iValueMin, Annotation = "Min" });
                    if (hasValueMax)
                        lstIValue.Add(new IValueSet { Value = iValueMax, Annotation = "Max" });
                }
                else if (tableDataType.Equals("D"))
                {
                    if (hasValueMin)
                        lstDValue.Add(new DValueSet { Value = dValueMin, Annotation = "Min" });
                    if (hasValueMax)
                        lstDValue.Add(new DValueSet { Value = dValueMax, Annotation = "Max" });
                }
            }
            else if (sme is Blob blob)
            {
                if (blob.Value.IsNullOrEmpty() && blob.ContentType.IsNullOrEmpty())
                    return;

                smeDB.TValue = "S";
                lstSValue.Add(new SValueSet { Value = blob.Value != null ? Encoding.ASCII.GetString(blob.Value) : string.Empty, Annotation = blob.ContentType });
            }
            else if (sme is AasCore.Aas3_0.File file)
            {
                if (file.Value.IsNullOrEmpty() && file.ContentType.IsNullOrEmpty())
                    return;

                smeDB.TValue = "S";
                lstSValue.Add(new SValueSet { Value = file.Value, Annotation = file.ContentType });
            }
            else if (sme is ReferenceElement refEle)
            {
                if (refEle.Value != null)
                    lstOValue.Add(new OValueSet { Attribute = "Value", Value = Jsonization.Serialize.ToJsonObject(refEle.Value) });
            }
            else if (sme is SubmodelElementList sml)
            {
                if (sml.OrderRelevant != null)
                    lstOValue.Add(new OValueSet { Attribute = "OrderRelevant", Value = sml.OrderRelevant });

                if (sml.SemanticIdListElement != null)
                    lstOValue.Add(new OValueSet { Attribute = "SemanticIdListElement", Value = Jsonization.Serialize.ToJsonObject(sml.SemanticIdListElement) });

                lstOValue.Add(new OValueSet { Attribute = "TypeValueListElement", Value = Jsonization.Serialize.AasSubmodelElementsToJsonValue(sml.TypeValueListElement) });

                if (sml.ValueTypeListElement != null)
                    lstOValue.Add(new OValueSet { Attribute = "ValueTypeListElement", Value = Jsonization.Serialize.DataTypeDefXsdToJsonValue((DataTypeDefXsd)sml.ValueTypeListElement) });
            }
            else if (sme is Entity ent)
            {
                smeDB.TValue = "S";
                lstSValue.Add(new SValueSet { Value = ent.GlobalAssetId, Annotation = Jsonization.Serialize.EntityTypeToJsonValue(ent.EntityType).ToString() });

                if (ent.SpecificAssetIds != null)
                {
                    var jsonArray = new Nodes.JsonArray();
                    foreach (var item in ent.SpecificAssetIds)
                        jsonArray.Add(Jsonization.Serialize.ToJsonObject(item));
                    lstOValue.Add(new OValueSet { Attribute = "SpecificAssetIds", Value = jsonArray });
                }
            }
            else if (sme is BasicEventElement evt)
            {
                lstOValue.Add(new OValueSet { Attribute = "Observed", Value = Jsonization.Serialize.ToJsonObject(evt.Observed) });
                lstOValue.Add(new OValueSet { Attribute = "Direction", Value = Jsonization.Serialize.DirectionToJsonValue(evt.Direction) });
                lstOValue.Add(new OValueSet { Attribute = "State", Value = Jsonization.Serialize.StateOfEventToJsonValue(evt.State) });

                if (evt.MessageTopic != null)
                    lstOValue.Add(new OValueSet { Attribute = "MessageTopic", Value = evt.MessageTopic });

                if (evt.MessageBroker != null)
                    lstOValue.Add(new OValueSet { Attribute = "MessageBroker", Value = Jsonization.Serialize.ToJsonObject(evt.MessageBroker) });

                if (evt.LastUpdate != null)
                    lstOValue.Add(new OValueSet { Attribute = "LastUpdate", Value = evt.LastUpdate });

                if (evt.MinInterval != null)
                    lstOValue.Add(new OValueSet { Attribute = "MinInterval", Value = evt.MinInterval });

                if (evt.MaxInterval != null)
                    lstOValue.Add(new OValueSet { Attribute = "MaxInterval", Value = evt.MaxInterval });
            }

            //var serilizeSettings = new JsonSerializerSettings()
            //{
            //    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            //};

            smeDB.OValue = System.Text.Json.JsonSerializer.Serialize(lstOValue);
            smeDB.SValue = System.Text.Json.JsonSerializer.Serialize(lstSValue);
            smeDB.IValue = System.Text.Json.JsonSerializer.Serialize(lstIValue);
            smeDB.DValue = System.Text.Json.JsonSerializer.Serialize(lstDValue);
        }

        public static Dictionary<DataTypeDefXsd, string> DataTypeToTable = new Dictionary<DataTypeDefXsd, string>() {
            { DataTypeDefXsd.AnyUri, "S" },
            { DataTypeDefXsd.Base64Binary, "S" },
            { DataTypeDefXsd.Boolean, "S" },
            { DataTypeDefXsd.Byte, "I" },
            { DataTypeDefXsd.Date, "S" },
            { DataTypeDefXsd.DateTime, "S" },
            { DataTypeDefXsd.Decimal, "S" },
            { DataTypeDefXsd.Double, "D" },
            { DataTypeDefXsd.Duration, "S" },
            { DataTypeDefXsd.Float, "D" },
            { DataTypeDefXsd.GDay, "S" },
            { DataTypeDefXsd.GMonth, "S" },
            { DataTypeDefXsd.GMonthDay, "S" },
            { DataTypeDefXsd.GYear, "S" },
            { DataTypeDefXsd.GYearMonth, "S" },
            { DataTypeDefXsd.HexBinary, "S" },
            { DataTypeDefXsd.Int, "I" },
            { DataTypeDefXsd.Integer, "I" },
            { DataTypeDefXsd.Long, "I" },
            { DataTypeDefXsd.NegativeInteger, "I" },
            { DataTypeDefXsd.NonNegativeInteger, "I" },
            { DataTypeDefXsd.NonPositiveInteger, "I" },
            { DataTypeDefXsd.PositiveInteger, "I" },
            { DataTypeDefXsd.Short, "I" },
            { DataTypeDefXsd.String, "S" },
            { DataTypeDefXsd.Time, "S" },
            { DataTypeDefXsd.UnsignedByte, "I" },
            { DataTypeDefXsd.UnsignedInt, "I" },
            { DataTypeDefXsd.UnsignedLong, "I" },
            { DataTypeDefXsd.UnsignedShort, "I" }
        };

        private static bool GetValueAndDataType(string value, DataTypeDefXsd dataType, out string tableDataType, out string sValue, out long iValue, out double dValue)
        {
            tableDataType = DataTypeToTable[dataType];
            sValue = string.Empty;
            iValue = 0;
            dValue = 0;

            if (value.IsNullOrEmpty())
                return false;

            // correct table type
            switch (tableDataType)
            {
                case "S":
                    sValue = value;
                    return true;
                case "I":
                    if (Int64.TryParse(value, out iValue))
                        return true;
                    break;
                case "D":
                    if (Double.TryParse(value, out dValue))
                        return true;
                    break;
            }

            // incorrect table type
            if (Int64.TryParse(value, out iValue))
            {
                tableDataType = "I";
                return true;
            }

            if (Double.TryParse(value, out dValue))
            {
                tableDataType = "D";
                return true;
            }

            sValue = value;
            tableDataType = "S";
            return true;
        }

        private string ShortSMEType(ISubmodelElement sme)
        {
            return _oprPrefix + sme switch
            {
                RelationshipElement => "Rel",
                AnnotatedRelationshipElement => "RelA",
                Property => "Prop",
                MultiLanguageProperty => "MLP",
                AasCore.Aas3_0.Range => "Range",
                Blob => "Blob",
                AasCore.Aas3_0.File => "File",
                ReferenceElement => "Ref",
                Capability => "Cap",
                SubmodelElementList => "SML",
                SubmodelElementCollection => "SMC",
                Entity => "Ent",
                BasicEventElement => "Evt",
                Operation => "Opr",
                _ => string.Empty
            };
        }

        private async Task<(bool, ISubmodelElement)> IsSubmodelElementPresent(string submodelIdentifier, string idShortPath)
        {
            var (submodel, _) = await GetSubmodelById(submodelIdentifier);

            if (submodel != null)
            {
                ISubmodelElement output;
                output = GetSubmodelElementByPath(submodel, idShortPath, out IReferable parent);

                if (output != null)
                {
                    //_logger.LogInformation($"Found SubmodelElement at {idShortPath} in submodel with Id {submodelIdentifier}");
                    return (true, output);
                }
            }
            return (false, null);
        }

        public async Task<ISubmodelElement> GetSubmodelElementByPath(string submodelIdentifier, string idShortPath)
        {
            var (found, output) = await IsSubmodelElementPresent(submodelIdentifier, idShortPath);
            if (found)
            {
                return output;
            }
            else
            {
                throw new NotFoundException($"Submodel Element at {idShortPath} not found in the submodel with id {submodelIdentifier}");
            }
        }

        private ISubmodelElement GetSubmodelElementByPath(IReferable parent, string idShortPath, out IReferable outParent)
        {
            ISubmodelElement output = null;
            outParent = parent;

            var idShorts = _pathParserService.ParseIdShortPath(idShortPath);

            if (idShorts.Count == 1)
            {
                return parent.FindSubmodelElementByIdShort((string)idShorts[0]);
            }

            foreach (var idShortObject in idShorts)
            {
                if (output != null)
                {
                    outParent = output;
                }

                if (idShortObject is string idShortStr)
                {
                    output = outParent.FindSubmodelElementByIdShort(idShortStr);
                    if (output == null)
                    {
                        return null;
                    }
                }
                else if (idShortObject is int idShortInt)
                {
                    if (outParent is ISubmodelElementList smeList)
                    {
                        try
                        {
                            output = smeList.Value?[idShortInt];
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            throw new InvalidIdShortPathException(smeList.IdShort + "[" + idShortInt + "]");
                        }

                        if (output == null)
                        {
                            return null;
                        }
                    }
                    else
                    {
                        throw new InvalidIdShortPathException(idShortPath);
                    }
                }
                else
                {
                    throw new Exception($"IdShort of {idShortObject.GetType} not supported.");
                }
            }

            return output;
        }

        public async Task<(ISubmodel, int?)> GetSubmodelById(string submodelIdshort)
        {
            var (found, dbId) = await IsSubmodelPresent(submodelIdshort);
            if (found)
            {
                //_logger.LogDebug($"Found the submodel with Id {submodelIdshort}");

                return (Converter.GetSubmodel(smIdentifier: submodelIdshort, unitOfWork: _unitOfWork), dbId);
            }
            else
            {
                throw new NotFoundException($"Submodel with id {submodelIdshort} NOT found.");
            }
        }

        public async Task<(string PathId, string? PathName, IAssetAdministrationShell? parentAas)> BuildResourcePathAsync(IAssetAdministrationShell currentAas, Guid? parentAssetId)
        {
            var currentName = currentAas.DisplayName?.FirstOrDefault()?.Text;
            if (parentAssetId.HasValue)
            {
                var lsAASs = await GetAASExtensions(ids: new List<string> { parentAssetId.ToString() });
                if (lsAASs != null && lsAASs.Any())
                {
                    var parentAas = lsAASs.First();
                    var parentPathId = parentAas.Extensions.FirstOrDefault(e => e.Name == "ResourcePath").Value;
                    var parentPathName = parentAas.Extensions.FirstOrDefault(e => e.Name == "ResourcePathName").Value;
                    return ($"{parentPathId}/children/{currentAas.Id}", $"{parentPathName}/{currentName}", parentAas);
                }
            }
            return ($"objects/{currentAas.Id}", currentName, null);
        }

        public async Task<IAssetAdministrationShell> GetAASByIdAsync(string id)
        {
            var dbaas = await _unitOfWork.AASSets.AsFetchable().Where(x => x.IdShort == id).FirstOrDefaultAsync();
            return ToAssetAdministrationShell(dbaas.AssetAdministrationShell);
        }

        public async Task<IEnumerable<AssetAdministrationShell>> GetAASExtensions(bool filterParent = false, Guid? parentId = null, IEnumerable<string> ids = null, AssetKind assetKind = AssetKind.Instance)
        {
            var (aasList, _) = await GetASSetsAsync("", filterParent, parentId, ids, assetKind);

            return aasList.Select(x => ToAssetAdministrationShell(x.AssetAdministrationShell));
        }

        public AssetAdministrationShell ToAssetAdministrationShell(JAssetAdministrationShell jaas)
        {
            var extensions =  System.Text.Json.JsonSerializer.Deserialize<List<Extension>>(System.Text.Json.JsonSerializer.Serialize(jaas.Extensions));
            var displaynames = System.Text.Json.JsonSerializer.Deserialize<List<LangStringNameType>>(System.Text.Json.JsonSerializer.Serialize(jaas.DisplayName));
            var descriptions = System.Text.Json.JsonSerializer.Deserialize<List<LangStringTextType>>(System.Text.Json.JsonSerializer.Serialize(jaas.Description));

            var lsExtensions = new List<IExtension>(extensions != null ? extensions : new List<Extension>());
            var lsDisplayName = new List<ILangStringNameType>(displaynames != null ? displaynames : new List<LangStringNameType>());
            var lisDesc = new List<ILangStringTextType>(descriptions != null ? descriptions : new List<LangStringTextType>());

            var assetInfor = new AssetInformation(jaas.AssetInformation.AssetKind, assetType: jaas?.AssetInformation?.AssetType, globalAssetId: jaas?.AssetInformation?.GlobalAssetId);
            var admin = new AdministrativeInformation(version: jaas.Administration.Version, revision: jaas.Administration.Revision, templateId: jaas.Administration.TemplateId);

            return new AssetAdministrationShell(id: jaas.Id, assetInformation: assetInfor, idShort: jaas.IdShort, extensions: lsExtensions, displayName: lsDisplayName, category: jaas.Category, description: lisDesc, administration: admin);
        }

        public async Task<IClass> GetSubmodelElementByPathSubmodelRepo(string submodelIdentifier, string idShortPath, LevelEnum level, ExtentEnum extent)
        {
            //_logger.LogInformation($"Received request to get the submodel element at {idShortPath} from the submodel with id {submodelIdentifier}");

            var submodelElement = await GetSubmodelElementByPath(submodelIdentifier, idShortPath);

            var output = _levelExtentModifierService.ApplyLevelExtent(submodelElement, level, extent);
            return output;
        }

        public async Task<string> ReplaceSubmodelElementByPath(string submodelIdentifier, string idShortPath, ISubmodelElement newSme, bool isOverriden = true)
        {
            return await UpdateSubmodelElement(submodelIdentifier, idShortPath, newSme, isOverriden: isOverriden);
        }

        public async Task<(string AasIdShort, ISubmodelElement Sme)> FindSmeByGuid(Guid id)
        {
            var dbSME = await _unitOfWork.SMESets.AsFetchable().Where(x => x.IdShort == id.ToString()).FirstOrDefaultAsync();

            if (dbSME != null)
            {
                var sme = Converter.CreateSME(dbSME);

                return (dbSME.AASIdShort, sme);
            }

            return (string.Empty, null);
        }

        //public async Task UpdateSnapshot(Guid assetId, Guid attributeId, TimeSeriesDto series)
        //{
        //    await _timeSeriesService.AddSnapshot(assetId, attributeId, TimeSeriesHelper.TimestampToDatetime(series.ts), series.v.ToString());
        //}

        public async Task<List<SMESet>> GetDynamicAttributeSmc(string deviceId, string metricKey)
        {
            return await _unitOfWork.SMESets.AsFetchable().Where(x => x.Category == AttributeTypeConstants.TYPE_DYNAMIC
                                                && x.SMSet.AASSet.AssetAdministrationShell.AssetInformation.AssetKind == AssetKind.Instance
                                                && IsContain(x.MetaData, deviceId, metricKey))
                    .Include(x => x.SMSet).ThenInclude(x => x.AASSet).ToListAsync();
        }

        private static bool IsContain(string metaData, string deviceId, string metricKey)
        {
            var dynamicMeta = JsonConvert.DeserializeObject<DynamicMetaData>(metaData);
            return dynamicMeta.DeviceId == deviceId && dynamicMeta.MetricKey == metricKey;
        }

        public async Task PublishRuntime(Guid attributeIdShort, TimeSeriesDto seriesDto)
        {
            var attribute = await _unitOfWork.SMESets.AsQueryable().Include(x => x.SMSet).ThenInclude(x => x.AASSet).Where(x => x.IdShort == attributeIdShort.ToString()).FirstOrDefaultAsync();
            if (attribute != null && attribute.SMSet != null && attribute.SMSet.AASSet != null && !string.IsNullOrEmpty(attribute.SMSet.AASSet.IdShort))
            {
                await _timeSeriesService.AddRuntimeSeries(Guid.Parse(attribute.SMSet.AASSet.IdShort), attributeIdShort, seriesDto);

                var smc = System.Text.Json.JsonSerializer.Deserialize<SubmodelElementCollection>(attribute.RawJson);

                //var encodedSmId = ConvertHelper.ToBase64(sm.Id);
                //_ = smRepoController.PutSubmodelElementByPathSubmodelRepo(sme, encodedSmId, sme.IdShort, level: LevelEnum.Deep);
                await _eventPublisher.Publish(AasEvents.SubmodelElementUpdated, new { attribute = smc });
                await _eventPublisher.Publish(AasEvents.AasUpdated, attribute.SMSet.AASSet.IdShort);
            }
        }

        public async Task<List<SMESet>> GetAliasRelatedSME(string aliasIdShort)
        {
            return await _unitOfWork.SMESets.AsQueryable().Include(x => x.SMSet).Where(x => x.Category == AttributeTypeConstants.TYPE_ALIAS && IsAliasPathContainsId(x.MetaData, aliasIdShort)).ToListAsync();
        }

        private static bool IsAliasPathContainsId(string aliasMetaData, string idshort)
        {
            var aliasMeta = JsonConvert.DeserializeObject<AliasMetaData>(aliasMetaData);
            return aliasMeta != null && aliasMeta.AliasPath.Contains(idshort);
        }

        public async Task UpdateAlias(string submodelIdShort, string aliasIdShort, IReferenceElement aliasElement, string aliasPath)
        {
            await UpdateSubmodelElement(submodelIdShort, aliasIdShort, aliasElement, aliasPath);
        }

        private async Task<string> UpdateSubmodelElement(string submodelIdentifier, string idShortPath, ISubmodelElement newSme, string aliasPath = "", bool isOverriden = true)
        {
            _verificationService.VerifyRequestBody(newSme);

            try
            {
                var sMSet = await _unitOfWork.SMSets.AsQueryable().Include(x => x.SMESets).Where(x => x.IdShort == submodelIdentifier).FirstOrDefaultAsync();

                var existingSme = sMSet.SMESets.Where(x => x.IdShort == idShortPath && !x.IsDeleted).FirstOrDefault();

                var timeStamp = DateTime.UtcNow;
                //newSme.SetAllParentsAndTimestamps(smeParent, timeStamp, timeStamp);
                newSme.SetTimeStamp(timeStamp);

                await _unitOfWork.SMESets.RemoveAsync(existingSme.Id);

                if (!string.IsNullOrEmpty(existingSme.TemplateId))
                {
                    newSme.Extensions.Add(new Extension(name: "TemplateAttributeId", valueType: DataTypeDefXsd.String, value: existingSme.TemplateId));
                }

                var newSMESet = CollectSMEData(newSme);

                newSMESet.RawJson = JsonConvert.SerializeObject(newSme);
                newSMESet.SMId = sMSet.Id;
                newSMESet.Name = newSme.DisplayName[0].Text;
                newSMESet.Category = newSme.Category;

                newSMESet.MetaData = await BuildSMEMetaData(newSMESet, newSme);
                newSMESet.SMIdShort = sMSet.IdShort;
                newSMESet.AASIdShort = sMSet.IdShort;
                newSMESet.TemplateId = existingSme.TemplateId;

                if (!string.IsNullOrEmpty(existingSme.TemplateId))
                {
                    newSMESet.IsOverridden = isOverriden;
                }

                await _unitOfWork.SMESets.AddAsync(newSMESet);

                await _unitOfWork.CommitAsync();

                return newSMESet.IdShort;
            }
            catch (Exception ex)
            {

            }

            return string.Empty;
        }

        public async Task<bool> RemoveAssetTemplateAsync(DeleteAssetTemplate command)
        {
            if (!command.Ids.Any())
                return true;
            var deleteTemplateNames = new List<string>();
            var assetIds = new List<int>();
            var entityChangedNotification = new EntityChangedNotificationMessage();

            try
            {
                var templateIds = command.Ids.Select(x => x.ToString());

                var relatedAssets = await _unitOfWork.AASSets.AsFetchable().Where(x => templateIds.Contains(x.TemplateId)).ToListAsync();

                //await _unitOfWork.BeginTransactionAsync();
                //await ValidateIfEntitiesLockedAsync(command.Ids, cancellationToken);
                foreach (var assetTemplateId in command.Ids)
                {
                    var assetUsingTemplates = relatedAssets.Where(x => x.TemplateId == assetTemplateId.ToString());

                    if (!assetUsingTemplates.Any())
                        continue;

                    foreach (var asset in assetUsingTemplates)
                    {
                        assetIds.Add(asset.Id);
                        await ProcessAddAttributeStandaloneAsync(asset); //Is it neccessary?
                    }
                }

                //remove tags
                //await _unitOfWork.EntityTags.RemoveByEntityIdsAsync(EntityTypeConstants.ASSET_TEMPLATE, command.Ids.ToList());

                var (_, deletedNames) = await ProcessRemoveAsync(command, entityChangedNotification);
                deleteTemplateNames.AddRange(deletedNames);
            }
            catch (System.Exception ex)
            {
                //await _unitOfWork.RollbackAsync();
                //await _auditLogService.SendLogAsync(ActivityEntityAction.ASSET_TEMPLATE, ActionType.Delete, ex, command.Ids, deleteTemplateNames, payload: command);
                throw;
            }

            foreach (var notificationMessage in entityChangedNotification.Items)
            {
                await _notificationService.NotifyAssetChanged(notificationMessage.Id);
            }

            return true;
        }

        public async Task<bool> IsAssetDeleted(string assetIdShort)
        {
            if (!string.IsNullOrEmpty(assetIdShort))
            {
                return await _unitOfWork.AASSets.AsFetchable().Where(x => x.IdShort == assetIdShort).Select(x => x.IsDeleted).FirstOrDefaultAsync();
            }

            return false;
        }

        private async Task ProcessAddAttributeStandaloneAsync(AASSet asset)
        {
            //var assetDto = GetAssetDto.Create(asset);
            //var attributes = asset.Attributes.Select(attr => JObject.FromObject(attr).ToObject<Asset.Command.AssetAttribute>());
            //foreach (var attrTemplate in asset.AssetTemplate.Attributes)
            //{
            //    var mapping = new AttributeStandaloneMapping();
            //    switch (attrTemplate.AttributeType)
            //    {
            //        case AttributeTypeConstants.TYPE_STATIC:
            //        {
            //            var attrMapping = asset.AssetAttributeStaticMappings.First(x => x.AssetAttributeTemplateId == attrTemplate.Id);
            //            mapping = new AttributeStandaloneMapping(attrMapping.Id, attrMapping.AssetId, attrMapping.AssetAttributeTemplateId);
            //            break;
            //        }
            //        case AttributeTypeConstants.TYPE_DYNAMIC:
            //        {
            //            var attrMapping = asset.AssetAttributeDynamicMappings.First(x => x.AssetAttributeTemplateId == attrTemplate.Id);
            //            mapping = new AttributeStandaloneMapping(attrMapping.Id, attrMapping.AssetId, attrMapping.AssetAttributeTemplateId);
            //            break;
            //        }
            //        case AttributeTypeConstants.TYPE_RUNTIME:
            //        {
            //            var attrMapping = asset.AssetAttributeRuntimeMappings.First(x => x.AssetAttributeTemplateId == attrTemplate.Id);
            //            mapping = new AttributeStandaloneMapping(attrMapping.Id, attrMapping.AssetId, attrMapping.AssetAttributeTemplateId);
            //            break;
            //        }
            //        case AttributeTypeConstants.TYPE_INTEGRATION:
            //        {
            //            var attrMapping = asset.AssetAttributeIntegrationMappings.First(x => x.AssetAttributeTemplateId == attrTemplate.Id);
            //            mapping = new AttributeStandaloneMapping(attrMapping.Id, attrMapping.AssetId, attrMapping.AssetAttributeTemplateId);
            //            break;
            //        }
            //        case AttributeTypeConstants.TYPE_COMMAND:
            //        {
            //            var attrMapping = asset.AssetAttributeCommandMappings.First(x => x.AssetAttributeTemplateId == attrTemplate.Id);
            //            mapping = new AttributeStandaloneMapping(attrMapping.Id, attrMapping.AssetId, attrMapping.AssetAttributeTemplateId);
            //            break;
            //        }
            //        case AttributeTypeConstants.TYPE_ALIAS:
            //        {
            //            var attrMapping = asset.AssetAttributeAliasMappings.First(x => x.AssetAttributeTemplateId == attrTemplate.Id);
            //            mapping = new AttributeStandaloneMapping(attrMapping.Id, attrMapping.AssetId, attrMapping.AssetAttributeTemplateId);
            //            break;
            //        }
            //    }

            //    var newAttribute = new Asset.Command.AssetAttribute
            //    {
            //        Id = mapping.Id,
            //        AssetId = mapping.AssetId,
            //        Name = attrTemplate.Name,
            //        Value = attrTemplate.Value,
            //        AttributeType = attrTemplate.AttributeType,
            //        DataType = attrTemplate.DataType,
            //        UomId = attrTemplate.UomId,
            //        DecimalPlace = attrTemplate.DecimalPlace,
            //        ThousandSeparator = attrTemplate.ThousandSeparator,
            //        Payload = assetDto.Attributes.First(x => x.Id == mapping.Id).Payload,
            //        IsStandalone = true
            //    };

            //    try
            //    {
            //        await _assetAttributeHandler.AddAsync(newAttribute, attributes, CancellationToken.None, ignoreValidation: true);
            //    }
            //    catch (System.Exception ex)
            //    {
            //        _logger.LogError($"Add asset standalone failed - payload {newAttribute.ToJson()}");
            //        _logger.LogError(ex, ex.Message);
            //        throw ex;
            //    }
            //}
        }

        private async Task<(BaseResponse, IEnumerable<string>)> ProcessRemoveAsync(DeleteAssetTemplate command, EntityChangedNotificationMessage changedNotification)
        {
            if (command.Ids == null || !command.Ids.Any())
                return (BaseResponse.Success, Enumerable.Empty<string>());

            var assetTemplateIds = command.Ids.Distinct().Select(x => x.ToString()).ToArray();

            var deleteTemplateNames = new List<string>();

            //list assetTemplate
            var deletedAssets = await _unitOfWork.AASSets.AsFetchable().Where(x => assetTemplateIds.Contains(x.IdShort)).ToListAsync();
            deleteTemplateNames = deletedAssets.Select(x => x.Name).ToList();
            if (assetTemplateIds.Count() > deletedAssets.Count())
            {
                throw new EntityNotFoundException(detailCode: ExceptionErrorCode.DetailCode.ERROR_ENTITY_NOT_FOUND_SOME_ITEMS_DELETED);
            }

            foreach (var assetTemplateId in command.Ids)
            {
                var deleteAsset = deletedAssets.FirstOrDefault(x => x.IdShort == assetTemplateId.ToString());
                if (deleteAsset is null)
                    continue;

                changedNotification.AddItem(Models.EntityType.Asset, Guid.Parse(deleteAsset.IdShort), deleteAsset.Name, EntityChangedAction.Delete, "");//TODO _userContext.Upn
                _unitOfWork.AASSets.RemoveAsync(deleteAsset.Id);
            }

            return (BaseResponse.Success, deleteTemplateNames);
        }

        public async Task AddNewElementFromTemplate(string templateIdShort, string templateAttributeIdShort)
        {
            var attribute = await _unitOfWork.SMESets.AsFetchable().Where(x => x.IdShort == templateAttributeIdShort && x.AASIdShort == templateIdShort).AsNoTracking().FirstOrDefaultAsync();
            var relatedInstaces = await _unitOfWork.AASSets.AsFetchable().Where(x => !x.IsDeleted && x.TemplateId == templateIdShort).AsNoTracking().ToListAsync();
            var attributeType = attribute.Category;
            ISubmodelElement sme = Converter.CreateSME(attribute);

            foreach (var instance in relatedInstaces)
            {
                var templateAttributeExtension = new Extension(name: "TemplateAttributeId", valueType: DataTypeDefXsd.String, value: attribute.IdShort);

                switch (attributeType)
                {
                    case AttributeTypeConstants.TYPE_STATIC:
                    {
                        var pStatic = sme as IProperty;
                        var dataType = MappingHelper.ToAhiDataType(pStatic.ValueType);
                        var property = new Property(
                            valueType: pStatic.ValueType)
                        {
                            DisplayName = [new LangStringNameType("en-US", attribute.Name)],
                            IdShort = Guid.NewGuid().ToString(),
                            Value = pStatic.Value.ParseValueWithDataType(dataType, pStatic.Value, isRawData: false).ToString(),
                            Category = attributeType,
                            Extensions = [templateAttributeExtension]
                        };
                        //var encodedSmId = ConvertHelper.ToBase64(aasId.ToString());
                        //smRepoController.PostSubmodelElementSubmodelRepo(property, encodedSmId, first: false);
                        await SaveSubmodelElement(property, instance.IdShort, first: false);
                        break;
                    }
                    case AttributeTypeConstants.TYPE_ALIAS:
                    {
                        var reference = new ReferenceElement()
                        {
                            Category = attributeType,
                            DisplayName = [new LangStringNameType("en-US", attribute.Name)],
                            IdShort = Guid.NewGuid().ToString(),
                            Extensions = [templateAttributeExtension]
                        };

                        await SaveSubmodelElement(reference, instance.IdShort, first: false);
                        break;
                    }
                    case AttributeTypeConstants.TYPE_DYNAMIC:
                    {
                        var deviceIdProp = new Extension(name: "DeviceId", valueType: DataTypeDefXsd.String, value: string.Empty);
                        var metricKey = new Extension(name: "MetricKey", valueType: DataTypeDefXsd.String, value: string.Empty);
                        var dataType = new Extension(name: "DataType", valueType: DataTypeDefXsd.String, value: string.Empty);

                        //var snapShot = TimeSeriesHelper.CreateEmptySnapshot(templateAttr.DataType);
                        var smc = new SubmodelElementCollection()
                        {
                            DisplayName = [new LangStringNameType("en-US", attribute.Name)],
                            IdShort = Guid.NewGuid().ToString(),
                            Category = attributeType,
                            Extensions = [templateAttributeExtension, deviceIdProp, metricKey, dataType]
                        };

                        //var encodedSmId = ConvertHelper.ToBase64(templateIdShort);
                        //smRepoController.PostSubmodelElementSubmodelRepo(smc, encodedSmId, first: false);
                        await SaveSubmodelElement(smc, instance.IdShort, first: false);
                        break;
                    }
                    case AttributeTypeConstants.TYPE_COMMAND:
                    {
                        var deviceIdProp = new Extension(name: "DeviceId", valueType: DataTypeDefXsd.String, value: string.Empty);
                        var metricKey = new Extension(name: "MetricKey", valueType: DataTypeDefXsd.String, value: string.Empty);
                        var dataType = new Extension(name: "DataType", valueType: DataTypeDefXsd.String, value: string.Empty);

                        //var snapShot = TimeSeriesHelper.CreateEmptySnapshot(DataTypeConstants.TYPE_INTEGER);
                        var smc = new SubmodelElementCollection()
                        {
                            DisplayName = [new LangStringNameType("en-US", attribute.Name)],
                            IdShort = Guid.NewGuid().ToString(),
                            Category = attributeType,
                            Extensions = [templateAttributeExtension, deviceIdProp, metricKey, dataType]
                        };

                        await SaveSubmodelElement(smc, instance.IdShort, first: false);
                        break;
                    }
                    case AttributeTypeConstants.TYPE_RUNTIME:
                    {
                        var smcCommand = sme as ISubmodelElementCollection;
                        var dataType = smcCommand.GetDataType();

                        var smc = smcCommand as SubmodelElementCollection;

                        smc.Extensions.Add(new Extension(name: "TemplateAttributeId", valueType: DataTypeDefXsd.String, value: templateAttributeIdShort));
                        smc.IdShort = attribute.IdShort;
                        smc.Value = [TimeSeriesHelper.CreateEmptySnapshot(smcCommand.GetDataType())];

                        var smElementIdShort = await SaveSubmodelElement(smc, instance.IdShort, first: false);

                        break;
                    }
                }
            }
        }

        public async Task UpdateElementAsTemplateChange(string templateIdShort, string templateAttributeIdShort)
        {
            var templateAttribute = await _unitOfWork.SMESets.AsFetchable().Where(x => x.IdShort == templateAttributeIdShort
                && x.AASIdShort == templateIdShort).AsNoTracking().FirstOrDefaultAsync();
            var relatedAttributes = await _unitOfWork.SMESets.AsFetchable().Where(x => !x.IsDeleted && !x.SMSet.AASSet.IsDeleted
                && x.TemplateId == templateAttributeIdShort && x.SMSet.AASSet.TemplateId == templateIdShort)
                .AsNoTracking().ToListAsync();

            var attributeType = templateAttribute.Category;

            if (relatedAttributes != null)
            {
                switch (attributeType)
                {
                    case AttributeTypeConstants.TYPE_STATIC:
                    {
                        foreach (var updateAttribute in relatedAttributes)
                        {
                            var templateSme = await GetSubmodelElementByPathSubmodelRepo(templateAttribute.AASIdShort, templateAttribute.IdShort, LevelEnum.Deep, ExtentEnum.WithoutBlobValue);
                            var templateStatic = templateSme as IProperty;

                            var updateSme = await GetSubmodelElementByPathSubmodelRepo(updateAttribute.AASIdShort, updateAttribute.IdShort, LevelEnum.Deep, ExtentEnum.WithoutBlobValue);
                            var updateStatic = updateSme as IProperty;

                            var dataType = MappingHelper.ToAhiDataType(templateStatic.ValueType);
                            updateStatic.DisplayName = [new LangStringNameType("en-US", templateAttribute.Name)];
                            updateStatic.ValueType = templateStatic.ValueType;

                            await ReplaceSubmodelElementByPath(updateAttribute.AASIdShort, updateAttribute.IdShort, updateStatic, isOverriden: false);

                            await _eventPublisher.Publish(AasEvents.SubmodelElementUpdated, new { attribute = updateStatic, aasId = updateAttribute.AASIdShort });
                        }

                        break;
                    }
                    case AttributeTypeConstants.TYPE_ALIAS:
                    {
                        foreach (var updateAttribute in relatedAttributes)
                        {
                            var smeResult = await GetSubmodelElementByPathSubmodelRepo(updateAttribute.AASIdShort, updateAttribute.IdShort, LevelEnum.Deep, ExtentEnum.WithoutBlobValue);
                            var property = smeResult as ReferenceElement;
                            property.DisplayName = [new LangStringNameType("en-US", templateAttribute.Name)];

                            var attributeIdShort = await UpdateSubmodelElement(updateAttribute.AASIdShort, updateAttribute.IdShort, property, isOverriden: false);
                            await _eventPublisher.Publish(AasEvents.SubmodelElementUpdated, new { attribute = property, aasId = updateAttribute.AASIdShort });
                        }

                        break;
                    }
                    case AttributeTypeConstants.TYPE_DYNAMIC:
                    {
                        foreach (var updateAttribute in relatedAttributes)
                        {
                            var sme = Converter.CreateSME(templateAttribute);
                            var dynamicAttr = sme as ISubmodelElementCollection;

                            //var snapShot = TimeSeriesHelper.CreateEmptySnapshot(attribute.DataType);
                            var smc = new SubmodelElementCollection()
                            {
                                DisplayName = [new LangStringNameType("en-US", templateAttribute.Name)],
                                IdShort = updateAttribute.IdShort,
                                Category = updateAttribute.Category,
                                Extensions = dynamicAttr.Extensions
                            };

                            var attributeIdShort = await ReplaceSubmodelElementByPath(updateAttribute.AASIdShort, updateAttribute.IdShort, smc);
                            await _eventPublisher.Publish(AasEvents.SubmodelElementUpdated, new { attribute = smc, aasId = updateAttribute.AASIdShort });
                        }

                        break;
                    }
                    case AttributeTypeConstants.TYPE_RUNTIME:
                    {
                        foreach (var updateAttribute in relatedAttributes)
                        {
                            var sme = Converter.CreateSME(templateAttribute);
                            var runtimeAttr = sme as ISubmodelElementCollection;

                            var smc = new SubmodelElementCollection()
                            {
                                DisplayName = [new LangStringNameType("en-US", templateAttribute.Name)],
                                IdShort = updateAttribute.IdShort,
                                Category = updateAttribute.Category,
                                Extensions = runtimeAttr.Extensions
                            };

                            var (aasset, elements) = await GetFullAasByIdAsync(Guid.Parse(updateAttribute.AASIdShort));
                            var aas = ToAssetAdministrationShell(aasset.AssetAdministrationShell);

                            var inputAttributes = elements.Where(x => !string.IsNullOrEmpty(x.Category)).Select(x => new AssetAttributeCommand
                            {
                                Id = Guid.Parse(x.IdShort),
                                DataType = x.GetDataType()
                            });

                            var attributeIdShort = await ReplaceSubmodelElementByPath(updateAttribute.AASIdShort, updateAttribute.IdShort, smc);

                            await _eventPublisher.Publish(AasEvents.SubmodelElementUpdated, new { attribute = smc, aasId = updateAttribute.AASIdShort });
                        }

                        break;
                    }
                    case AttributeTypeConstants.TYPE_COMMAND:
                    {
                        foreach (var updateAttribute in relatedAttributes)
                        {
                            var sme = Converter.CreateSME(templateAttribute);
                            var dynamicAttr = sme as ISubmodelElementCollection;

                            //var snapShot = TimeSeriesHelper.CreateEmptySnapshot(attribute.DataType);
                            var smc = new SubmodelElementCollection()
                            {
                                DisplayName = [new LangStringNameType("en-US", templateAttribute.Name)],
                                IdShort = updateAttribute.IdShort,
                                Category = updateAttribute.Category,
                                Extensions = dynamicAttr.Extensions
                            };

                            var attributeIdShort = await ReplaceSubmodelElementByPath(updateAttribute.AASIdShort, updateAttribute.IdShort, smc);
                            await _eventPublisher.Publish(AasEvents.SubmodelElementUpdated, new { attribute = smc, aasId = updateAttribute.AASIdShort });
                        }
                        break;
                    }
                    default:
                        break;
                }
            }
        }

        public async Task<bool> RemoveTemplateAttribute(string templateIdShort, string attributeIdShort)
        {
            var attribute = await _unitOfWork.SMESets.AsQueryable().Where(x => x.IdShort == attributeIdShort && x.AASIdShort == templateIdShort).FirstOrDefaultAsync();
            if (attribute != null)
            {
                await _unitOfWork.SMESets.RemoveAsync(attribute.Id);
                return true;
            }

            return false;
        }

        public async Task RemoveElementAsTempalateChange(string templateIdShort, string templateAttributeIdShort)
        {
            var relatedAttributes = await _unitOfWork.SMESets.AsQueryable().Where(x => !x.IsDeleted && !x.SMSet.AASSet.IsDeleted && x.TemplateId == templateAttributeIdShort && x.SMSet.AASSet.TemplateId == templateIdShort).ToListAsync();

            if (relatedAttributes != null)
            {
                foreach (var attribute in relatedAttributes)
                {
                    await _unitOfWork.SMESets.RemoveAsync(attribute.Id);
                }

                await _unitOfWork.CommitAsync();
            }
        }
    }
}
