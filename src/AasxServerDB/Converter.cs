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

using System;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using AasCore.Aas3_0;
using AasxServerDB.Entities;
using AasxServerDB.Repositories;
using AdminShellNS;
using AHI.Infrastructure.Repository.Generic;
using Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nodes = System.Text.Json.Nodes;

namespace AasxServerDB
{
    public static class Converter
    {
        public static AdminShellPackageEnv? GetPackageEnv(string path, AASSet? aasDB, IAASUnitOfWork unitOfWork) 
        {
            if (path.IsNullOrEmpty() || aasDB == null)
                return null;

            AssetAdministrationShell aas = new AssetAdministrationShell(
                id: aasDB.Identifier,
                idShort: aasDB.IdShort,
                assetInformation: new AssetInformation(AssetKind.Type, aasDB.GlobalAssetId),
                submodels: new List<AasCore.Aas3_0.IReference>());
            aas.TimeStampCreate = aasDB.TimeStampCreate;
            aas.TimeStamp = aasDB.TimeStamp;
            aas.TimeStampTree = aasDB.TimeStampTree;

            AdminShellPackageEnv? aasEnv = new AdminShellPackageEnv();
            aasEnv.SetFilename(path);
            aasEnv.AasEnv.AssetAdministrationShells?.Add(aas);

            var submodelDBList = unitOfWork.SMSets.AsFetchable()
                .OrderBy(sm => sm.Id)
                .Where(sm => sm.AASId == aasDB.Id)
                .ToList();
            foreach (var sm in submodelDBList.Select(submodelDB => Converter.GetSubmodel(smDB: submodelDB, unitOfWork: unitOfWork)))
            {
                aas.Submodels?.Add(sm.GetReference());
                aasEnv.AasEnv.Submodels?.Add(sm);
            }

            return aasEnv;
        }

        public static Submodel? GetSubmodel(IAASUnitOfWork unitOfWork, SMSet? smDB = null, string smIdentifier = "")
        {
            if (!smIdentifier.IsNullOrEmpty())
            {
                var smList = unitOfWork.SMSets.AsFetchable().Where(sm => sm.Identifier == smIdentifier).ToList();
                if (smList.Count == 0)
                    return null;
                smDB = smList.First();
            }

            if (smDB == null)
                return null;

            var SMEList = unitOfWork.SMESets.AsFetchable()
                .OrderBy(sme => sme.Id)
                .Where(sme => sme.SMId == smDB.Id)
                .ToList();

            var submodel = new Submodel(smDB.Identifier);
            submodel.IdShort = smDB.IdShort;
            submodel.SemanticId = new Reference(AasCore.Aas3_0.ReferenceTypes.ExternalReference,
                new List<IKey>() { new Key(KeyTypes.GlobalReference, smDB.SemanticId) });
            submodel.SubmodelElements = new List<ISubmodelElement>();

            LoadSME(submodel, null, null, SMEList);

            submodel.TimeStampCreate = smDB.TimeStampCreate;
            submodel.TimeStamp = smDB.TimeStamp;
            submodel.TimeStampTree = smDB.TimeStampTree;
            submodel.SetAllParents();

            if (smDB.Submodel.Extensions != null)
            {
                string extensionJson = System.Text.Json.JsonSerializer.Serialize(smDB.Submodel.Extensions);
                submodel.Extensions = new List<IExtension>(JsonConvert.DeserializeObject<List<Extension>>(extensionJson));
            }

            return submodel;
        }

        private static void LoadSME(Submodel submodel, ISubmodelElement? sme, SMESet? smeSet, List<SMESet> SMEList)
        {
            var smeSets = SMEList.Where(s => s.ParentSMEId == (smeSet != null ? smeSet.Id : null)).OrderBy(s => s.IdShort).ToList();

            foreach (var smel in smeSets)
            {
                // prefix of operation
                var split = !smel.SMEType.IsNullOrEmpty() ? smel.SMEType.Split(VisitorAASX.OPERATION_SPLIT) : [ string.Empty ];
                var oprPrefix = split.Length == 2 ? split[ 0 ] : string.Empty;
                smel.SMEType  = split.Length == 2 ? split[ 1 ] : split[ 0 ];

                // create SME from database
                var nextSME = CreateSME(smel);

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
                            (sme as AnnotatedRelationshipElement).Annotations.Add((IDataElement) nextSME);
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
                        LoadSME(submodel, nextSME, smel, SMEList);
                        break;
                }
            }
        }

        public static ISubmodelElement? CreateSME(SMESet smeSet)
        {
            //var sw = new Stopwatch();
            try
            {
                //sw.Restart();
                ISubmodelElement? sme = null;
                var value = GetValue(smeSet);
                var oValue = GetOValue(smeSet);
                //sw.Stop();
                //Console.WriteLine($"== CreateSME GetValue {sw.Elapsed.TotalMilliseconds}");

                //sw.Restart();
                switch (smeSet.SMEType)
                {
                    case "Rel":
                        sme = new RelationshipElement(
                            first: oValue.ContainsKey("First") ? CreateReferenceFromObject(oValue["First"]) : new Reference(ReferenceTypes.ExternalReference, new List<IKey>()),
                            second: oValue.ContainsKey("Second") ? CreateReferenceFromObject(oValue["Second"]) : new Reference(ReferenceTypes.ExternalReference, new List<IKey>())
                        );
                        break;
                    case "RelA":
                        sme = new AnnotatedRelationshipElement(
                            first: oValue.ContainsKey("First") ? CreateReferenceFromObject(oValue["First"]) : new Reference(ReferenceTypes.ExternalReference, new List<IKey>()),
                            second: oValue.ContainsKey("Second") ? CreateReferenceFromObject(oValue["Second"]) : new Reference(ReferenceTypes.ExternalReference, new List<IKey>()),
                            annotations: new List<IDataElement>()
                        );
                        break;
                    case "Prop":
                        sme = new Property(
                            valueType: Jsonization.Deserialize.DataTypeDefXsdFrom(value.First()[1]),
                            value: value.First()[0],
                            valueId: oValue.ContainsKey("ValueId") ? CreateReferenceFromObject(oValue["ValueId"]) : null
                        );
                        break;
                    case "MLP":
                        sme = new MultiLanguageProperty(
                            value: value.ConvertAll<ILangStringTextType>(val => new LangStringTextType(val[1], val[0])),
                            valueId: oValue.ContainsKey("ValueId") ? CreateReferenceFromObject(oValue["ValueId"]) : null
                        );
                        break;
                    case "Range":
                        var findMin = value.Find(val => val[1].Equals("Min"));
                        var findMax = value.Find(val => val[1].Equals("Max"));
                        sme = new AasCore.Aas3_0.Range(
                            valueType: Jsonization.Deserialize.DataTypeDefXsdFrom(oValue["ValueType"]),
                            min: findMin != null ? findMin[0] : string.Empty,
                            max: findMax != null ? findMax[0] : string.Empty
                            );
                        break;
                    case "Blob":
                        sme = new Blob(
                            value: Encoding.ASCII.GetBytes(value.First()[0]),
                            contentType: value.First()[1]
                            );
                        break;
                    case "File":
                        sme = new AasCore.Aas3_0.File(
                            value: value.First()[0],
                            contentType: value.First()[1]
                            );
                        break;
                    case "Ref":
                        sme = new ReferenceElement(
                            value: oValue.ContainsKey("Value") ? CreateReferenceFromObject(oValue["Value"]) : null
                        );
                        break;
                    case "Cap":
                        sme = new Capability();
                        break;
                    case "SML":
                        sme = new SubmodelElementList(
                            orderRelevant: oValue.ContainsKey("OrderRelevant") ? (bool?)oValue["OrderRelevant"] : true,
                            semanticIdListElement: oValue.ContainsKey("SemanticIdListElement") ? CreateReferenceFromObject(oValue["SemanticIdListElement"]) : null,
                            typeValueListElement: Jsonization.Deserialize.AasSubmodelElementsFrom(oValue["TypeValueListElement"]),
                            valueTypeListElement: oValue.ContainsKey("ValueTypeListElement") ? Jsonization.Deserialize.DataTypeDefXsdFrom(oValue["ValueTypeListElement"]) : null,
                            value: new List<ISubmodelElement>()
                        );
                        break;
                    case "SMC":
                        sme = new SubmodelElementCollection(
                            value: new List<ISubmodelElement>()
                        );
                        break;
                    case "Ent":
                        var spec = new List<ISpecificAssetId>();
                        if (oValue.ContainsKey("SpecificAssetIds"))
                            foreach (var item in (Nodes.JsonArray)oValue["SpecificAssetIds"])
                                spec.Add(Jsonization.Deserialize.SpecificAssetIdFrom(item));
                        sme = new Entity(
                            statements: new List<ISubmodelElement>(),
                            entityType: Jsonization.Deserialize.EntityTypeFrom(value.First()[1]),
                            globalAssetId: value.First()[0],
                            specificAssetIds: oValue.ContainsKey("SpecificAssetIds") ? spec : null
                        );
                        break;
                    case "Evt":
                        sme = new BasicEventElement(
                            observed: oValue.ContainsKey("Observed") ? CreateReferenceFromObject(oValue["Observed"]) : new Reference(ReferenceTypes.ExternalReference, new List<IKey>()),
                            direction: Jsonization.Deserialize.DirectionFrom(oValue["Direction"]),
                            state: Jsonization.Deserialize.StateOfEventFrom(oValue["State"]),
                            messageTopic: oValue.ContainsKey("MessageTopic") ? oValue["MessageTopic"].ToString() : null,
                            messageBroker: oValue.ContainsKey("MessageBroker") ? CreateReferenceFromObject(oValue["MessageBroker"]) : null,
                            lastUpdate: oValue.ContainsKey("LastUpdate") ? oValue["LastUpdate"].ToString() : null,
                            minInterval: oValue.ContainsKey("MinInterval") ? oValue["MinInterval"].ToString() : null,
                            maxInterval: oValue.ContainsKey("MaxInterval") ? oValue["MaxInterval"].ToString() : null
                        );
                        break;
                    case "Opr":
                        sme = new Operation(
                            inputVariables: new List<IOperationVariable>(),
                            outputVariables: new List<IOperationVariable>(),
                            inoutputVariables: new List<IOperationVariable>()
                        );
                        break;
                }
                //sw.Stop();
                //Console.WriteLine($"== CreateSME Switch case {sw.Elapsed.TotalMilliseconds}");

                if (sme == null)
                    return null;

                //sw.Restart();
                using (JsonDocument doc = JsonDocument.Parse(smeSet.RawJson))
                {
                    JsonElement root = doc.RootElement;
                    var extensions = root.GetProperty("Extensions").ToString();
                    var displayNames = root.GetProperty("DisplayName").ToString();

                    if (!string.IsNullOrEmpty(extensions))
                    {
                        var listExtensions = JsonConvert.DeserializeObject<List<Extension>>(extensions);
                        sme.Extensions = new List<IExtension>(listExtensions);
                    }

                    if (!string.IsNullOrEmpty(displayNames))
                    {
                        var listLangs = JsonConvert.DeserializeObject<List<LangStringNameType>>(displayNames);
                        sme.DisplayName = new List<ILangStringNameType>(listLangs);
                    }
                }

                //sw.Stop();
                //Console.WriteLine($"== CreateSME JsonDocument {sw.Elapsed.TotalMilliseconds}");

                //sw.Restart();
                sme.IdShort = smeSet.IdShort;
                sme.TimeStamp = smeSet.TimeStamp;
                sme.TimeStampCreate = smeSet.TimeStampCreate;
                sme.TimeStampTree = smeSet.TimeStampTree;
                sme.Category = smeSet.Category;

                if (!smeSet.SemanticId.IsNullOrEmpty())
                    sme.SemanticId = new Reference(ReferenceTypes.ExternalReference,
                        new List<IKey>() { new Key(KeyTypes.GlobalReference, smeSet.SemanticId) });
                //sw.Stop();
                //Console.WriteLine($"== CreateSME The rest {sw.Elapsed.TotalMilliseconds}");

                return sme;
            }
            catch (Exception ex)
            {

                throw;
            }
            
        }

        public static Dictionary<string, Nodes.JsonNode> GetOValue(SMESet sMESet)
        {
            if (sMESet.OValueSets.Any())
            {
                var dic = sMESet.OValueSets.ToList()
                .ToDictionary(valueDB => valueDB.Attribute, valueDB => valueDB.Value);
                if (dic != null)
                    return dic;
            }

            return new Dictionary<string, Nodes.JsonNode>();
        }

        public static List<string[]> GetValue(SMESet sMESet)
        {
            if (sMESet.TValue == null)
                return [[string.Empty, string.Empty]];

            var list = new List<string[]>();
            switch (sMESet.TValue)
            {
                case "S":
                    list = sMESet.SValueSets.ToList()
                        .ConvertAll<string[]>(valueDB => [valueDB.Value ?? string.Empty, valueDB.Annotation ?? string.Empty]);
                    break;
                case "I":
                    list = sMESet.IValueSets.ToList()
                        .ConvertAll<string[]>(valueDB => [valueDB.Value == null ? string.Empty : valueDB.Value.ToString(), valueDB.Annotation ?? string.Empty]);
                    break;
                case "D":
                    list = sMESet.DValueSets.ToList()
                        .ConvertAll<string[]>(valueDB => [valueDB.Value == null ? string.Empty : valueDB.Value.ToString(), valueDB.Annotation ?? string.Empty]);
                    break;
            }

            if (list.Count > 0 || (!sMESet.SMEType.IsNullOrEmpty() && sMESet.SMEType.Equals("MLP")))
                return list;

            return [[string.Empty, string.Empty]];
        }

        private static Reference CreateReferenceFromObject(Nodes.JsonNode obj)
        {
            var result = Jsonization.Deserialize.ReferenceFrom(obj);
            if (result != null)
                return result;
            else
                return new Reference(ReferenceTypes.ExternalReference, new List<IKey>());
        }

        public static string GetAASXPath(string aasId = "", string submodelId = "")
        {
            //using var db = unitOfWork;
            //int? aasxId = null;
            //if (!submodelId.IsNullOrEmpty())
            //{
            //    var submodelDBList = db.SMSets.Where(s => s.Identifier == submodelId);
            //    if (submodelDBList.Count() > 0)
            //        aasxId = submodelDBList.First().AASXId;
            //}

            //if (!aasId.IsNullOrEmpty())
            //{
            //    var aasDBList = db.AASSets.Where(a => a.Identifier == aasId);
            //    if (aasDBList.Any())
            //        aasxId = aasDBList.First().AASXId;
            //}

            //if (aasxId == null)
            //    return string.Empty;

            //var aasxDBList = db.AASXSets.Where(a => a.Id == aasxId);
            //if (!aasxDBList.Any())
            //    return string.Empty;

            //var aasxDB = aasxDBList.First();
            //return aasxDB.AASX;

            return string.Empty;
        }
    }
}