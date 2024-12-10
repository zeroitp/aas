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

using Microsoft.EntityFrameworkCore;
using Extensions;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using AasCore.Aas3_0;
using System.Linq;
using Newtonsoft.Json.Linq;
using Microsoft.IdentityModel.Tokens;
using Nodes = System.Text.Json.Nodes;
using AHI.Infrastructure.Repository.Model.Generic;
using AasxServerDB.Repositories;
using Microsoft.Extensions.DependencyInjection;
using AasxServerDB.Helpers;
using System.Text.Json;

namespace AasxServerDB.Entities
{
    public class SMESet : BaseAttribute, IEntity<int>
    {
        private static JsonSerializerOptions JsonNodeConvertOptions = new JsonSerializerOptions
        {
            Converters = { new JsonNodeConverter() }
        };

        public SMESet() { }

        private readonly IServiceProvider _serviceProvider;
        public SMESet(IServiceProvider serviceProvider) { _serviceProvider = serviceProvider; }

        public int Id { get; set; }

        [ForeignKey("SMSet")]
        public         int    SMId  { get; set; }
        public virtual SMSet? SMSet { get; set; }

        public         int?    ParentSMEId { get; set; }
        public virtual SMESet? ParentSME   { get; set; }

        public string?  SMEType   { get; set; }
        public string? TValue     { get; set; }
        public string? SemanticId { get; set; }
        public string? IdShort    { get; set; }

        public DateTime TimeStampCreate { get; set; }
        public DateTime TimeStamp       { get; set; }
        public DateTime TimeStampTree   { get; set; }

        [NotMapped]
        public List<IValueSet> IValueSets => !string.IsNullOrEmpty(IValue) ? JsonSerializer.Deserialize<List<IValueSet>>(IValue) : new List<IValueSet>();

        [NotMapped]
        public List<DValueSet> DValueSets => !string.IsNullOrEmpty(DValue) ? JsonSerializer.Deserialize<List<DValueSet>>(DValue) : new List<DValueSet>();

        [NotMapped]
        public List<SValueSet> SValueSets => !string.IsNullOrEmpty(SValue) ? JsonSerializer.Deserialize<List<SValueSet>>(SValue) : new List<SValueSet>();

        [NotMapped]
        public List<OValueSet> OValueSets {
            get
            {
                return !string.IsNullOrEmpty(OValue) ? JsonSerializer.Deserialize<List<OValueSet>>(OValue, JsonNodeConvertOptions) : new List<OValueSet>();
            }
        }

        [Column(TypeName = "jsonb")]
        public string? RawJson { get; set; }
        public string? Name { get; set; }
        public string? Category { get; set; }
        [Column(TypeName = "jsonb")]
        public string? MetaData { get; set; }
        public string? SMIdShort { get; set; }
        public string? AASIdShort { get; set; }

        [Column(TypeName = "jsonb")]
        public string? IValue { get; set; }

        [Column(TypeName = "jsonb")]
        public string? DValue { get; set; }

        [Column(TypeName = "jsonb")]
        public string? SValue { get; set; }

        [Column(TypeName = "jsonb")]
        public string? OValue { get; set; }

        public string? DataType { get; set; }
    }
}