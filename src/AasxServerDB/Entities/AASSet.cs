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

using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using AasCore.Aas3_0;
using AHI.Infrastructure.Repository.Model.Generic;

namespace AasxServerDB.Entities
{
    public class AASSet : BaseEntity, IEntity<int>
    {
        public int Id { get; set; }

        public string? Identifier       { get; set; }
        public string? IdShort          { get; set; }
        public string? AssetKind        { get; set; }
        public string? GlobalAssetId    { get; set; }

        public DateTime TimeStampCreate { get; set; }
        public DateTime TimeStamp       { get; set; }
        public DateTime TimeStampTree   { get; set; }

        public virtual ICollection<SMSet> SMSets { get; } = new List<SMSet>();

        [Column(TypeName = "jsonb")]
        public JAssetAdministrationShell? AssetAdministrationShell { get; set; } = new JAssetAdministrationShell();
        public string? Name { get; set; }
        public AASSet? Parent { get; set; }
        public string? ResourcePath { get; set; }
    }

    public class JAssetAdministrationShell
    {
        public List<JExtension>? Extensions { get; set; }
        public string? Category { get; set; }
        public string? IdShort { get; set; }
        public List<JAbstractLangString>? DisplayName { get; set; }
        public List<JAbstractLangString>? Description { get; set; }
        public JAdministrativeInformation? Administration { get; set; }
        public string? Id { get; set; }
        public List<JEmbeddedDataSpecification>? EmbeddedDataSpecifications { get; set; }
        public JReference? DerivedFrom { get; set; }
        public JAssetInformation? AssetInformation { get; set; }
        //public List<JReference>? Submodels { get; set; }

        #region Parent

        public string? ParentId { get; set; }

        #endregion

        #region TimeStamp

        public DateTime TimeStampCreate { get; set; }
        public DateTime TimeStamp       { get; set; }
        public DateTime TimeStampTree   { get; set; }
        #endregion
    }

    public class JAdministrativeInformation
    {
        public string? Version { get; set; }
        public string? Revision { get; set; }
        //public JReference? Creator { get; set; }
        public string? TemplateId { get; set; }
    }

    public class JExtension
    {
        public string? Name { get; set; }
        public DataTypeDefXsd? ValueType { get; set; }
        public string? Value { get; set; }
        public List<JReference>? RefersTo { get; set; }
        public JReference? SemanticId { get; set; }
        public List<JReference>? SupplementalSemanticIds { get; set; }
    }

    public class JReference
    {
        public ReferenceTypes Type { get; set; }
        [NotMapped]
        public JReference? ReferredSemanticId { get; set; }
        public List<JKey>? Keys { get; set; }

        public Key GetAsExactlyOneKey()
        {
            if (Keys == null || Keys.Count != 1)
            {
                return null;
            }

            var key = Keys[0];
            return new Key(key.Type, key.Value);
        }
    }

    public class JKey
    {
        public KeyTypes Type { get; set; }
        public string? Value { get; set; }
    }

    public class JAssetInformation
    {
        public AssetKind AssetKind { get; set; }
        public string? GlobalAssetId { get; set; }
        public List<JSpecificAssetId>? SpecificAssetIds { get; set; }
        public string? AssetType { get; set; }
        public JResource? DefaultThumbnail { get; set; }
        public IEnumerable<JSpecificAssetId> OverSpecificAssetIdsOrEmpty()
        {
            return SpecificAssetIds
                   ?? System.Linq.Enumerable.Empty<JSpecificAssetId>();
        }
    }

    public class JResource
    {
        public string? Path { get; set; }
        public string? ContentType { get; set; }
    }

    public class JSpecificAssetId
    {
        public string? Name { get; set; }
        public string? Value { get; set; }
        public JReference? ExternalSubjectId { get; set; }
        public JReference? SemanticId { get; set; }
        public List<JReference>? SupplementalSemanticIds { get; set; }
    }
}