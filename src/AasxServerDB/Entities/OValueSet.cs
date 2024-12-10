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
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Nodes;
using AHI.Infrastructure.Repository.Model.Generic;

namespace AasxServerDB.Entities
{
    public class OValueSet
    {
        public string Attribute { get; set; }
        public JsonNode Value   { get; set; }
    }
}
