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

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using AasxServerDB;
using AasxServerDB.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

/*
 * https://learn.microsoft.com/en-us/ef/core/get-started/overview/first-app?tabs=netcore-cli
 * 
 * Initial Migration
 * Add-Migration InitialCreate -Context SqliteAasContext -OutputDir Migrations\Sqlite
 * Add-Migration InitialCreate -Context PostgreAasContext -OutputDir Migrations\Postgres
 * 
 * Change database
 * Add-Migration XXX -Context SqliteAasContext
 * Add-Migration XXX -Context PostgreAasContext
 * Update-Database -Context SqliteAasContext
 * Update-Database -Context PostgreAasContext
 */

namespace AasxServerDB
{
    public class AasContext : DbContext
    {
        public static IConfiguration? _con { get; set; }

        public DbSet<AASSet> AASSets { get; set; }
        public DbSet<SMSet> SMSets { get; set; }
        public DbSet<SMESet> SMESets { get; set; }
        //public DbSet<SValueSet> SValueSets { get; set; }
        //public DbSet<IValueSet> IValueSets { get; set; }
        //public DbSet<DValueSet> DValueSets { get; set; }
        //public DbSet<OValueSet> OValueSets { get; set; }

        //protected override void OnConfiguring(DbContextOptionsBuilder options)
        //{
        //    if (_con == null)
        //        throw new Exception("No Configuration!");

        //    var connectionString = _con["DatabaseConnection:ConnectionString"];
        //    if (connectionString.IsNullOrEmpty())
        //        throw new Exception("No connectionString in appsettings");

        //    if (connectionString != null && connectionString.Contains("$DATAPATH"))
        //        connectionString = connectionString.Replace("$DATAPATH", _dataPath);

        //    connectionString = connectionString.Replace("{{projectid}}", ProjectId);

        //    if (connectionString != null && connectionString.ToLower().Contains("host")) // PostgreSQL
        //    {
        //        IsPostgres = true;
        //        options.UseNpgsql(connectionString);
        //    }
        //    else // SQLite
        //    {
        //        IsPostgres = false;
        //        options.UseSqlite(connectionString);
        //    }
        //}

        public AasContext()
        {
        }

        public AasContext(DbContextOptions<AasContext> options) : base(options)
        {
        }

        protected AasContext(DbContextOptions options) : base(options)
        {
        }

        public async Task ClearDB()
        {
            // Queue up all delete operations asynchronously
            await AASSets.ExecuteDeleteAsync();
            await SMSets.ExecuteDeleteAsync();
            await SMESets.ExecuteDeleteAsync();
            //await IValueSets.ExecuteDeleteAsync();
            //await SValueSets.ExecuteDeleteAsync();
            //await DValueSets.ExecuteDeleteAsync();
            //await OValueSets.ExecuteDeleteAsync();

            // Save changes to the database
            SaveChanges();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<OValueSet>()
            //    .Property(e => e.Value)
            //    .HasConversion(
            //        v => v.ToJsonString(null),
            //        v => JsonNode.Parse(v, null, default));

            modelBuilder
                .Entity<AASSet>()
                .OwnsOne(aas => aas.AssetAdministrationShell, builder =>
                {
                    builder.ToJson();
                    builder.OwnsMany(x => x.DisplayName);
                    builder.OwnsMany(x => x.Extensions, sub =>
                    {
                        sub.OwnsMany(x => x.SupplementalSemanticIds, y => { y.OwnsMany(x => x.Keys); });
                        sub.OwnsMany(x => x.RefersTo, y =>
                        {
                            y.OwnsMany(x => x.Keys);
                        });
                        sub.OwnsOne(x => x.SemanticId, y =>
                        {
                            y.OwnsMany(x => x.Keys);
                        });
                    });
                    builder.OwnsMany(x => x.Description);
                    builder.OwnsMany(x => x.EmbeddedDataSpecifications, sub =>
                    {
                        sub.OwnsOne(x => x.DataSpecification, y => { y.OwnsMany(x => x.Keys); });
                    });
                    //builder.OwnsMany(x => x.Submodels, y => { y.OwnsMany(x => x.Keys); });
                    builder.OwnsOne(x => x.Administration, sub =>
                    {
                        //sub.OwnsOne(x => x.Creator, y =>
                        //{
                        //    y.OwnsMany(x => x.Keys);
                        //});
                    });
                    builder.OwnsOne(x => x.DerivedFrom, y => { y.OwnsMany(x => x.Keys); });
                    builder.OwnsOne(x => x.AssetInformation, sub =>
                    {
                        sub.OwnsMany(x => x.SpecificAssetIds, y =>
                        {
                            y.OwnsOne(x => x.ExternalSubjectId, z => { z.OwnsMany(x => x.Keys); });
                            y.OwnsOne(x => x.SemanticId, z => { z.OwnsMany(x => x.Keys); });
                            y.OwnsMany(x => x.SupplementalSemanticIds, z => { z.OwnsMany(x => x.Keys); });
                        });
                        sub.OwnsOne(x => x.DefaultThumbnail);
                    });
                });

            modelBuilder
                .Entity<SMSet>()
                .OwnsOne(sm => sm.Submodel, builder =>
                {
                    builder.ToJson();
                    builder.OwnsMany(x => x.DisplayName);
                    builder.OwnsMany(x => x.Extensions, sub =>
                    {
                        sub.OwnsMany(x => x.SupplementalSemanticIds, y => { y.OwnsMany(x => x.Keys); });
                        sub.OwnsMany(x => x.RefersTo, y =>
                        {
                            y.OwnsMany(x => x.Keys);
                        });
                        sub.OwnsOne(x => x.SemanticId, y =>
                        {
                            y.OwnsMany(x => x.Keys);
                        });
                    });
                    builder.OwnsMany(x => x.Description);
                    builder.OwnsMany(x => x.EmbeddedDataSpecifications, sub =>
                    {
                        sub.OwnsOne(x => x.DataSpecification, y => { y.OwnsMany(x => x.Keys); });
                    });
                    builder.OwnsMany(x => x.SupplementalSemanticIds, y =>
                    {
                        y.OwnsMany(x => x.Keys);
                    });
                    builder.OwnsMany(x => x.Qualifiers, sub =>
                    {
                        sub.OwnsOne(x => x.ValueId, y => { y.OwnsMany(x => x.Keys); });
                        sub.OwnsOne(x => x.SemanticId, y => { y.OwnsMany(x => x.Keys); });
                        sub.OwnsMany(x => x.SupplementalSemanticIds, y => { y.OwnsMany(x => x.Keys); });
                    });
                    //builder.OwnsMany(x => x.SubmodelElements);
                    //builder.OwnsOne(x => x.Administration, sub =>
                    //{
                    //    //sub.OwnsOne(x => x.Creator, y =>
                    //    //{
                    //    //    y.OwnsMany(x => x.Keys);
                    //    //});
                    //});
                    builder.OwnsOne(x => x.SemanticId, sub => { sub.OwnsMany(x => x.Keys); });
                    //builder.OwnsOne(x => x.Parent);
                });

            //modelBuilder
            //    .Entity<SMESet>()
            //    .OwnsOne(sme => sme.SubmodelElement, builder => { builder.ToJson(); });
        }
    }
}