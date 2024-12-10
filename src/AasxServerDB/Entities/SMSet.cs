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
using AasCore.Aas3_0;
using AHI.Infrastructure.Repository.Model.Generic;

namespace AasxServerDB.Entities
{
    public class SMSet : BaseEntity, IEntity<int>
    {
        public int Id { get; set; }

        [ForeignKey("AASSet")]
        public int? AASId { get; set; }
        public virtual AASSet? AASSet { get; set; }

        public string? SemanticId { get; set; }
        public string? Identifier { get; set; }
        public string? IdShort { get; set; }

        public DateTime TimeStampCreate { get; set; }
        public DateTime TimeStamp { get; set; }
        public DateTime TimeStampTree { get; set; }

        public virtual ICollection<SMESet> SMESets { get; } = new List<SMESet>();

        [Column(TypeName = "jsonb")]
        public JSubmodel? Submodel { get; set; } = new JSubmodel();
        public SMSet? Parent { get; set; }
    }

    public class JSubmodel
    {
        public List<JExtension>? Extensions { get; set; }
        public string? Category { get; set; }
        public string? IdShort { get; set; }

        /// <summary>
        /// Display name. Can be provided in several languages.
        /// </summary>
        public List<JAbstractLangString>? DisplayName { get; set; }
        public List<JAbstractLangString>? Description { get; set; }
        //public JAdministrativeInformation? Administration { get; set; }
        public string? Id { get; set; }
        public ModellingKind? Kind { get; set; }
        public JReference? SemanticId { get; set; }
        public List<JReference>? SupplementalSemanticIds { get; set; }
        public List<JQualifier>? Qualifiers { get; set; }
        public List<JEmbeddedDataSpecification>? EmbeddedDataSpecifications { get; set; }
        //public string SubmodelElements { get; set; }

        #region Parent

        public string? Parent { get; set; }

        #endregion

        #region TimeStamp

        public DateTime TimeStampCreate { get; set; }
        public DateTime TimeStamp { get; set; }
        public DateTime TimeStampTree { get; set; }

        #endregion

        public string? Name { get; set; }
    }

    public class JAbstractLangString
    {
        public string? Language { get; set; }
        public string? Text { get; set; }
    }

    public class JEmbeddedDataSpecification
    {
        public JReference? DataSpecification { get; set; }
        public string? DataSpecificationContent { get; set; }
    }

    public class JQualifier
    {
        public QualifierKind? Kind { get; set; }
        public string? Type { get; set; }
        public DataTypeDefXsd ValueType { get; set; }
        public string? Value { get; set; }
        public JReference? ValueId { get; set; }

        public QualifierKind KindOrDefault()
        {
            return Kind ?? QualifierKind.ConceptQualifier;
        }
        public JReference? SemanticId { get; set; }
        public List<JReference>? SupplementalSemanticIds { get; set; }
    }
}