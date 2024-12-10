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

using System.ComponentModel.DataAnnotations;
using AasCore.Aas3_0;
using AasxServerDB.Entities;
using AasxServerDB.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;

namespace AasxServerDB
{
    public class PageRetriever(IServiceProvider serviceProvider)
    {
        public static List<AASXSet> GetPageAASXData(int size = 1000, string searchLower = "", long aasxid = 0)
        {
            //return unitOfWork.AASXSets
            //    .OrderBy(a => a.Id)
            //    .Where(a => (aasxid == 0 || a.Id == aasxid) &&
            //    (searchLower.IsNullOrEmpty() || a.AASX.ToLower().Contains(searchLower)))
            //    .Take(size)
            //    .ToList();
            return new List<AASXSet>();
        }

        public List<AASSet> GetPageAASData(int size = 1000, DateTime dateTime = new DateTime(), string searchLower = "", long aasxid = 0, long aasid = 0)
        {
            using var scope = serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetService<IAASUnitOfWork>();

            bool withDateTime = !dateTime.Equals(DateTime.MinValue);
            return unitOfWork.AASSets.AsFetchable()
                .OrderBy(a => a.Id)
                .Where(a => (aasid == 0 || a.Id == aasid) &&
                    (searchLower.IsNullOrEmpty() ||
                    (a.IdShort != null && a.IdShort.ToLower().Contains(searchLower)) ||
                    (a.Identifier != null && a.Identifier.ToLower().Contains(searchLower)) ||
                    (a.AssetKind != null && a.AssetKind.ToLower().Contains(searchLower)) ||
                    (a.GlobalAssetId != null && a.GlobalAssetId.ToLower().Contains(searchLower)) ||
                    (withDateTime && a.TimeStampTree.CompareTo(dateTime) > 0)))
                .Take(size)
                .ToList();
        }

        public List<SMSet> GetPageSMData(int size = 1000, DateTime dateTime = new DateTime(), string searchLower = "", long aasxid = 0, long aasid = 0, long smid = 0)
        {
            using var scope = serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetService<IAASUnitOfWork>();

            bool withDateTime = !dateTime.Equals(DateTime.MinValue);
            return unitOfWork.SMSets.AsFetchable()
                .OrderBy(s => s.Id)
                .Where(s => (aasid == 0 || s.AASId == aasid) && (smid == 0 || s.Id == smid) &&
                    (searchLower.IsNullOrEmpty() ||
                    (s.Identifier != null && s.Identifier.ToLower().Contains(searchLower)) ||
                    (s.IdShort != null && s.IdShort.ToLower().Contains(searchLower)) ||
                    (s.SemanticId != null && s.SemanticId.ToLower().Contains(searchLower)) ||
                    (withDateTime && s.TimeStampTree.CompareTo(dateTime) > 0)))
                .Take(size)
                .ToList();
        }

        public List<SMESet> GetPageSMEData(int size = 1000, DateTime dateTime = new DateTime(), string searchLower = "", long smid = 0, long smeid = 0, long parid = 0)
        {
            //using var scope = serviceProvider.CreateScope();
            //var unitOfWork = scope.ServiceProvider.GetService<IAASUnitOfWork>();

            //var withDateTime = !dateTime.Equals(DateTime.MinValue);
            //var data = new List<SMESet>();
            //data = unitOfWork.SMESets.AsFetchable()
            //        .OrderBy(sme => sme.Id)
            //        .Where(sme => (smid == 0 || sme.SMId == smid) && (smeid == 0 || sme.Id == smeid) && (parid == 0 || sme.ParentSMEId == parid) &&
            //            (searchLower.IsNullOrEmpty() ||
            //            (sme.IdShort != null && sme.IdShort.ToLower().Contains(searchLower)) ||
            //            (sme.SemanticId != null && sme.SemanticId.ToLower().Contains(searchLower)) ||
            //            (sme.SMEType != null && sme.SMEType.ToLower().Contains(searchLower)) ||
            //            (sme.TValue != null && sme.TValue.ToLower().Contains(searchLower)) ||
            //            (withDateTime && sme.TimeStamp.CompareTo(dateTime) > 0) ||
            //            unitOfWork.OValueSets.AsFetchable().Any(sv => sv.SMEId == sme.Id && (sv.Attribute.ToLower().Contains(searchLower) || ((string)sv.Value).ToLower().Contains(searchLower))) ||
            //            (sme.TValue != null && (
            //                (sme.TValue.Equals("S") && unitOfWork.SValueSets.AsFetchable().Any(sv => sv.SMEId == sme.Id && ((sv.Annotation != null && sv.Annotation.ToLower().Contains(searchLower)) || (sv.Value != null && sv.Value.ToLower().Contains(searchLower))))) ||
            //                (sme.TValue.Equals("I") && unitOfWork.IValueSets.AsFetchable().Any(sv => sv.SMEId == sme.Id && ((sv.Annotation != null && sv.Annotation.ToLower().Contains(searchLower)) || (sv.Value != null && sv.Value.ToString().ToLower().Contains(searchLower))))) ||
            //                (sme.TValue.Equals("D") && unitOfWork.DValueSets.AsFetchable().Any(sv => sv.SMEId == sme.Id && ((sv.Annotation != null && sv.Annotation.ToLower().Contains(searchLower)) || (sv.Value != null && sv.Value.ToString().ToLower().Contains(searchLower)))))))))
            //        .Take(size)
            //        .ToList();
            //return data;

            return new List<SMESet>();
        }

        public List<SValueSet> GetPageSValueData(int size = 1000, string searchLower = "", long smeid = 0)
        {
            //using var scope = serviceProvider.CreateScope();
            //var unitOfWork = scope.ServiceProvider.GetService<IAASUnitOfWork>();

            //return unitOfWork.SValueSets.AsFetchable()
            //    .OrderBy(v => v.SMEId)
            //    .Where(v => (smeid == 0 || v.SMEId == smeid) &&
            //        (searchLower.IsNullOrEmpty() ||
            //        (v.Value != null && v.Value.ToLower().Contains(searchLower)) ||
            //        (v.Annotation != null && v.Annotation.ToLower().Contains(searchLower))))
            //    .Take(size)
            //    .ToList();

            return new List<SValueSet>();
        }

        public List<IValueSet> GetPageIValueData(int size = 1000, string searchLower = "", long smeid = 0)
        {
            //using var scope = serviceProvider.CreateScope();
            //var unitOfWork = scope.ServiceProvider.GetService<IAASUnitOfWork>();

            //if (!Int64.TryParse(searchLower, out var iEqual))
            //    iEqual = 0;

            //return unitOfWork.IValueSets.AsFetchable()
            //    .OrderBy(v => v.SMEId)
            //    .Where(v => (smeid == 0 || v.SMEId == smeid) &&
            //                (searchLower.IsNullOrEmpty() ||
            //                (v.Annotation != null && v.Annotation.ToLower().Contains(searchLower)) ||
            //                (iEqual == 0 || v.Value == iEqual)))
            //    .Take(size)
            //    .ToList();

            return new List<IValueSet>();
        }

        public List<DValueSet> GetPageDValueData(int size = 1000, string searchLower = "", long smeid = 0)
        {
            //using var scope = serviceProvider.CreateScope();
            //var unitOfWork = scope.ServiceProvider.GetService<IAASUnitOfWork>();

            //if (!double.TryParse(searchLower, out var dEqual))
            //    dEqual = 0;

            //return unitOfWork.DValueSets.AsFetchable()
            //    .OrderBy(v => v.SMEId)
            //    .Where(v => (smeid == 0 || v.SMEId == smeid) &&
            //                (searchLower.IsNullOrEmpty() ||
            //                (v.Annotation != null && v.Annotation.ToLower().Contains(searchLower)) ||
            //                (dEqual == 0 || v.Value == dEqual)))
            //    .Take(size)
            //    .ToList();

            return new List<DValueSet>();
        }

        public List<OValueSet> GetPageOValueData(int size = 1000, string searchLower = "", long smeid = 0)
        {
            //using var scope = serviceProvider.CreateScope();
            //var unitOfWork = scope.ServiceProvider.GetService<IAASUnitOfWork>();

            //return unitOfWork.OValueSets.AsFetchable()
            //    .OrderBy(v => v.SMEId)
            //    .Where(v => (smeid == 0 || v.SMEId == smeid) &&
            //        (searchLower.IsNullOrEmpty() ||
            //        (v.Attribute != null && v.Attribute.ToLower().Contains(searchLower)) ||
            //        (v.Value != null && ((string) v.Value).ToLower().Contains(searchLower))))
            //    .Take(size)
            //    .ToList();

            return new List<OValueSet>();
        }
    }
}