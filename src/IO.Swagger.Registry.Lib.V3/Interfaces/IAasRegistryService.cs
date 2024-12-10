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

using IO.Swagger.Registry.Lib.V3.Models;
using System.Collections.Generic;
using AasxServerDB.Entities;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using IO.Swagger.Models;
using AasxServerDB.Dto;
using AasxServerStandardBib.Models;

namespace IO.Swagger.Registry.Lib.V3.Interfaces
{
    public interface IAasRegistryService
    {
        AssetAdministrationShellDescriptor CreateAasDescriptorFromDB(AASSet aasDB);
        List<AssetAdministrationShellDescriptor> GetAllAssetAdministrationShellDescriptors(string? assetKind = null, List<string?>? assetList = null, string? aasIdentifier = null);
        Task<AASSet> GetByIdAsync(Guid aasId);
        Task<(List<AASSet>, int)> GetASSetsAsync(string name, bool filterParent = false, Guid? parentId = null, IEnumerable<string> ids = null, AssetKind assetKind = AssetKind.Instance, int pageSize = 10, int pageIndex = 0);
        Task<(AASSet aas, IEnumerable<ISubmodelElement> elements)> GetFullAasByIdAsync(Guid id);
        Task<(AASSet aas, List<SMESet> submodelElement)> GetDetailAasByIdAsync(Guid id);
        Task<List<SMESet>> GetAssetAttributesByAssetId(Guid id);
        Task SaveAASAsync(AssetAdministrationShell aas);
        Task SaveSubmodelAsync(Submodel body, string aasId);
        Task<string> SaveSubmodelElement(ISubmodelElement? body, string submodelIdShort, bool first);
        Task<(string PathId, string? PathName, IAssetAdministrationShell? parentAas)> BuildResourcePathAsync(IAssetAdministrationShell currentAas, Guid? parentAssetId);
        Task<IAssetAdministrationShell> GetAASByIdAsync(string id);
        Task<IEnumerable<AssetAdministrationShell>> GetAASExtensions(bool filterParent = false, Guid? parentId = null, IEnumerable<string> ids = null, AssetKind assetKind = AssetKind.Instance);
        AssetAdministrationShell ToAssetAdministrationShell(JAssetAdministrationShell jaas);
        Task<IClass> GetSubmodelElementByPathSubmodelRepo(string submodelIdentifier, string idShortPath, LevelEnum level, ExtentEnum extent);
        Task<string> ReplaceSubmodelElementByPath(string submodelIdentifier, string idShortPath, ISubmodelElement newSme, bool isOverriden = true);
        Task UpdateAlias(string submodelIdShort, string aliasIdShort, IReferenceElement aliasElement, string aliasPath);
        Task<(ISubmodel, int?)> GetSubmodelById(string submodelIdshort);
        Task<(string AasIdShort, ISubmodelElement Sme)> FindSmeByGuid(Guid id);
        //Task UpdateSnapshot(Guid assetId, Guid attributeId, TimeSeriesDto series);
        Task<List<SMESet>> GetDynamicAttributeSmc(string deviceId, string metricKey);
        Task PublishRuntime(Guid attributeIdShort, TimeSeriesDto seriesDto);
        Task<List<SMESet>> GetAliasRelatedSME(string aliasIdShort);
        Task<ISubmodelElement> GetSubmodelElementByPath(string submodelIdentifier, string idShortPath);
        Task<bool> RemoveAssetTemplateAsync(DeleteAssetTemplate command);
        Task<bool> IsAssetDeleted(string assetIdShort);
        Task AddNewElementFromTemplate(string aASIdShort, string templateAttributeIdShort);
        Task UpdateElementAsTemplateChange(string aASIdShort, string templateAttributeIdShort);
        Task<bool> RemoveTemplateAttribute(string templateIdShort, string attributeIdShort);
        Task RemoveElementAsTempalateChange(string templateIdShort, string templateAttributeIdShort);
    }
}
