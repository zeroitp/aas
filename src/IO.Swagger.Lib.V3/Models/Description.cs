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

/*
 * DotAAS Part 2 | HTTP/REST | Repository Service Specification
 *
 * The entire Repository Service Specification as part of Details of the Asset Administration Shell Part 2
 *
 * OpenAPI spec version: V3.0
 *
 * Generated by: https://github.com/swagger-api/swagger-codegen.git
 */

using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace IO.Swagger.Models;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// The Description object enables servers to present their capabilities to the clients, in particular which profiles they implement.
/// At least one defined profile is required. Additional, proprietary attributes might be included. Nevertheless, the server must not expect that a regular
/// client understands them.
/// </summary>
[DataContract]
public partial class Description : IEquatable<Description>
{
    /// <summary>
    /// Gets or Sets Profiles
    /// </summary>

    [DataMember(Name = "profiles")]
    public List<ServerDescriptionProfiles>? Profiles { get; set; }

    /// <summary>
    /// Returns the string presentation of the object
    /// </summary>
    /// <returns>String presentation of the object</returns>
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append("class Description {\n");
        sb.Append("  Profiles: ").Append(Profiles).Append('\n');
        sb.Append("}\n");
        return sb.ToString();
    }

    /// <summary>
    /// Returns the JSON string presentation of the object
    /// </summary>
    /// <returns>JSON string presentation of the object</returns>
    public string ToJson() => JsonSerializer.Serialize(this, Options);

    private static readonly JsonSerializerOptions Options = new()
                                                            {
                                                                WriteIndented          = true,
                                                                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                                                                Converters             = {new JsonStringEnumConverter()}
                                                            };

    /// <summary>
    /// Returns true if objects are equal
    /// </summary>
    /// <param name="obj">Object to be compared</param>
    /// <returns>Boolean</returns>
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((Description)obj);
    }

    /// <summary>
    /// Returns true if Description instances are equal
    /// </summary>
    /// <param name="other">Instance of Description to be compared</param>
    /// <returns>Boolean</returns>
    public bool Equals(Description? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return
            other.Profiles != null && (
                                          Profiles == other.Profiles ||
                                          (Profiles != null &&
                                           Profiles.SequenceEqual(other.Profiles))
                                      );
    }

    /// <summary>
    /// Gets the hash code
    /// </summary>
    /// <returns>Hash code</returns>
    public override int GetHashCode()
    {
        unchecked // Overflow is fine, just wrap
        {
            var hashCode = 41;
            // Suitable nullity checks etc, of course :)
            if (Profiles != null)
            {
                hashCode = (hashCode * 59) + Profiles.GetHashCode();
            }

            return hashCode;
        }
    }

    #region Operators

#pragma warning disable 1591

    public static bool operator ==(Description left, Description right) => Equals(left, right);

    public static bool operator !=(Description left, Description right) => !Equals(left, right);

#pragma warning restore 1591

    #endregion Operators
}