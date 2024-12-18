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

using System.Collections.Generic;
using System.Runtime.Intrinsics.X86;
using AasxServerDB.Entities;
using AasxServerDB.Repositories;
using AasxServerDB.Result;
using AHI.Infrastructure.Repository.Generic;
using Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using TimeStamp;

namespace AasxServerDB
{
    public class Query(IServiceProvider serviceProvider)
    {
        public static string? ExternalBlazor { get; set; }

        // --------------- API ---------------
        public List<SMResult> SearchSMs(string semanticId = "", string identifier = "", string diff = "")
        {
            //var watch = System.Diagnostics.Stopwatch.StartNew();
            //Console.WriteLine();
            //Console.WriteLine("SearchSMs");
            //Console.WriteLine("Total number of SMs " + unitOfWork.SMSets.Count() + " in " + watch.ElapsedMilliseconds + "ms");

            //watch.Restart();
            var smList = GetSMSet(semanticId, identifier, diff);
            //Console.WriteLine("Found " + smList.Count + " SM in " + watch.ElapsedMilliseconds + "ms");

            //watch.Restart();
            var result = GetSMResult(smList);
            //Console.WriteLine("Collected result in " + watch.ElapsedMilliseconds + "ms");

            return result;
        }

        public int CountSMs(string semanticId = "", string identifier = "", string diff = "")
        {
            //var watch = System.Diagnostics.Stopwatch.StartNew();
            //Console.WriteLine();
            //Console.WriteLine("CountSMs");
            //Console.WriteLine("Total number of SMs " + unitOfWork.SMSets.Count() + " in " + watch.ElapsedMilliseconds + "ms");

            //watch.Restart();
            var smList = GetSMSet(semanticId, identifier, diff);
            var count = smList.Count;
            //Console.WriteLine("Found " + count + " SM in " + watch.ElapsedMilliseconds + "ms");

            return count;
        }

        public List<SMEResult> SearchSMEs(
            string smSemanticId = "", string smIdentifier = "", string semanticId = "", string diff = "",
            string contains = "", string equal = "", string lower = "", string upper = "")
        {
            //var watch = System.Diagnostics.Stopwatch.StartNew();
            //Console.WriteLine();
            //Console.WriteLine("SearchSMEs");
            //Console.WriteLine("Total number of SMEs " + unitOfWork.SMESets.Count() + " in " + watch.ElapsedMilliseconds + "ms");

            //watch.Restart();
            var smeWithValue = GetSMEWithValue(smSemanticId, smIdentifier, semanticId, diff, contains, equal, lower, upper);
            //Console.WriteLine("Found " + smeWithValue.Count + " SMEs in " + watch.ElapsedMilliseconds + "ms");

            //watch.Restart();
            var result = GetSMEResult(smeWithValue);
            //Console.WriteLine("Collected result in " + watch.ElapsedMilliseconds + "ms");

            return result;
        }

        public int CountSMEs(
            string smSemanticId = "", string smIdentifier = "", string semanticId = "", string diff = "",
            string contains = "", string equal = "", string lower = "", string upper = "")
        {
            using var scope = serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetService<IAASUnitOfWork>();

            var withSemanticID = !semanticId.IsNullOrEmpty();
            var withDiff = diff != "";
            var diffDT = TimeStamp.TimeStamp.StringToDateTime(diff);

            //var watch = System.Diagnostics.Stopwatch.StartNew();
            //Console.WriteLine();
            //Console.WriteLine("CountSMEs");
            //Console.WriteLine("Total number of SMEs " + unitOfWork.SMESets.AsFetchable().Count() + " in " + watch.ElapsedMilliseconds + "ms");

            //watch.Restart();

            var count = 0;
            if ((withSemanticID || withDiff)
                && contains.IsNullOrEmpty() && equal.IsNullOrEmpty() && lower.IsNullOrEmpty() && upper.IsNullOrEmpty())
            {
                count = unitOfWork.SMESets.AsFetchable()
                    .Where(sme =>
                        (!withSemanticID || (sme.SemanticId != null && sme.SemanticId == semanticId)) &&
                        (!withDiff || (sme.TimeStamp > diffDT))
                    )
                    .Count();
            }
            else
            {
                var smeWithValue = GetSMEWithValue(smSemanticId, smIdentifier, semanticId, diff, contains, equal, lower, upper);
                count = smeWithValue.Count;
            }

            //Console.WriteLine("Found " + count + " SMEs in " + watch.ElapsedMilliseconds + "ms");

            return count;
        }

        public List<SMEResult> SearchSMEsResult(
            string smSemanticId = "",
            string searchSemanticId = "",  string searchIdShort = "",
            string? equal = "", string? contains = "",
            string resultSemanticId = "", string resultIdShort = "")
        {
            //using var scope = serviceProvider.CreateScope();
            //var unitOfWork = scope.ServiceProvider.GetService<IAASUnitOfWork>();

            //List<SMEResult> result = new List<SMEResult>();

            //if (searchSemanticId.IsNullOrEmpty() && searchIdShort.IsNullOrEmpty())
            //    return result;
            //if (equal.IsNullOrEmpty() && contains.IsNullOrEmpty())
            //    return result;
            //if (resultSemanticId.IsNullOrEmpty() && resultIdShort.IsNullOrEmpty())
            //    return result;

            //bool withI = false;
            //long iEqual = 0;
            //bool withF = false;
            //double fEqual = 0;
            //try
            //{
            //    if (!equal.IsNullOrEmpty())
            //    {
            //        iEqual = Convert.ToInt64(equal);
            //        withI = true;
            //        fEqual = Convert.ToDouble(equal);
            //        withF = true;
            //    }
            //}
            //catch { }

            ////var watch = System.Diagnostics.Stopwatch.StartNew();
            ////Console.WriteLine();
            ////Console.WriteLine("SearchSMEs");
            ////Console.WriteLine("Total number of SMEs " + unitOfWork.SMESets.AsFetchable().Count() + " in " + watch.ElapsedMilliseconds + "ms");
            ////watch.Restart();

            //bool withContains = (!contains.IsNullOrEmpty());
            //bool withEqual = !withContains && (!equal.IsNullOrEmpty());

            //var list = unitOfWork.SValueSets.AsFetchable().Where(v =>
            //    (withContains && v.Value.Contains(contains)) ||
            //    (withEqual && v.Value == equal)
            //    )
            //    .Join(unitOfWork.SMESets.AsFetchable(),
            //        v => v.SMEId,
            //        sme => sme.Id,
            //        (v, sme) => new
            //        {
            //            SMId = sme.SMId,
            //            SemanticId = sme.SemanticId,
            //            IdShort = sme.IdShort,
            //            ParentSme = sme.ParentSMEId,
            //            Value = v.Value
            //        }
            //    )
            //    .Where(s =>
            //        (!searchSemanticId.IsNullOrEmpty() && s.SemanticId == searchSemanticId) ||
            //        (!searchIdShort.IsNullOrEmpty() && s.IdShort == searchIdShort)
            //    )
            //    .Join(unitOfWork.SMSets.AsFetchable(),
            //        v => v.SMId,
            //        s => s.Id,
            //        (v, s) => new
            //        {
            //            Id = s.Id,
            //            SemanticId = s.SemanticId,
            //            ParentSme = v.ParentSme,
            //            Value = v.Value
            //        }
            //    )
            //    .Where(s =>
            //        smSemanticId == "" || s.SemanticId == smSemanticId
            //    )
            //    .ToList();

            //list.AddRange(unitOfWork.IValueSets.AsFetchable().Where(v =>
            //    (withEqual && withI && v.Value == iEqual)
            //    )
            //    .Join(unitOfWork.SMESets.AsFetchable(),
            //        v => v.SMEId,
            //        sme => sme.Id,
            //        (v, sme) => new
            //        {
            //            SMId = sme.SMId,
            //            SemanticId = sme.SemanticId,
            //            IdShort = sme.IdShort,
            //            ParentSme = sme.ParentSMEId,
            //            Value = v.Value.ToString()
            //        }
            //    )
            //    .Where(s =>
            //        (!searchSemanticId.IsNullOrEmpty() && s.SemanticId == searchSemanticId) ||
            //        (!searchIdShort.IsNullOrEmpty() && s.IdShort == searchIdShort)
            //    )
            //    .Join(unitOfWork.SMSets.AsFetchable(),
            //        v => v.SMId,
            //        s => s.Id,
            //        (v, s) => new
            //        {
            //            Id = s.Id,
            //            SemanticId = s.SemanticId,
            //            ParentSme = v.ParentSme,
            //            Value = v.Value
            //        }
            //    )
            //    .Where(s =>
            //        smSemanticId == "" || s.SemanticId == smSemanticId
            //    )
            //    .ToList());

            //list.AddRange(unitOfWork.DValueSets.AsFetchable().Where(v =>
            //    (withEqual && withF && v.Value == fEqual)
            //    )
            //    .Join(unitOfWork.SMESets.AsFetchable(),
            //        v => v.SMEId,
            //        sme => sme.Id,
            //        (v, sme) => new
            //        {
            //            SMId = sme.SMId,
            //            SemanticId = sme.SemanticId,
            //            IdShort = sme.IdShort,
            //            ParentSme = sme.ParentSMEId,
            //            Value = v.Value.ToString()
            //        }
            //    )
            //    .Where(s =>
            //        (!searchSemanticId.IsNullOrEmpty() && s.SemanticId == searchSemanticId) ||
            //        (!searchIdShort.IsNullOrEmpty() && s.IdShort == searchIdShort)
            //    )
            //    .Join(unitOfWork.SMSets.AsFetchable(),
            //        v => v.SMId,
            //        s => s.Id,
            //        (v, s) => new
            //        {
            //            Id = s.Id,
            //            SemanticId = s.SemanticId,
            //            ParentSme = v.ParentSme,
            //            Value = v.Value
            //        }
            //    )
            //    .Where(s =>
            //        smSemanticId == "" || s.SemanticId == smSemanticId
            //    )
            //    .ToList());

            ////Console.WriteLine("Found " + list.Count() + " SMEs in " + watch.ElapsedMilliseconds + "ms");

            //var hSubmodel = new HashSet<long>();
            //var lParentParentNum = new List<int?>();
            //foreach (var l in list)
            //{
            //    hSubmodel.Add(l.Id);
            //    var smeDB = unitOfWork.SMESets.AsFetchable().Where(s => s.Id == l.ParentSme).First();
            //    lParentParentNum.Add(smeDB.ParentSMEId);
            //}

            ////Console.WriteLine("Found " + hSubmodel.Count() + " Submodels");

            ////watch.Restart();

            //var SMEResult = unitOfWork.SMESets.AsFetchable().Where(s =>
            //    hSubmodel.Contains(s.SMId) &&
            //    ((!resultSemanticId.IsNullOrEmpty() && s.SemanticId == resultSemanticId) ||
            //    (!resultIdShort.IsNullOrEmpty() && s.IdShort == resultIdShort))
            //    )
            //    .ToList();

            //if (equal.IsNullOrEmpty())
            //    equal = contains;

            //foreach (var l in SMEResult)
            //{
            //    SMEResult r = new SMEResult();
            //    bool found = false;

            //    var submodelDB = unitOfWork.SMSets.AsFetchable().Where(s => s.Id == l.SMId).First();
            //    if (submodelDB != null && (smSemanticId.IsNullOrEmpty() || submodelDB.SemanticId == smSemanticId))
            //    {
            //        r.value = equal;
            //        r.url = string.Empty;
            //        r.smId = submodelDB.Identifier;
            //        string path = l.IdShort;
            //        int? pId = l.ParentSMEId;
            //        while (pId != null)
            //        {
            //            var smeDB = unitOfWork.SMESets.AsFetchable().Where(s => s.Id == pId).First();
            //            path = smeDB.IdShort + "." + path;
            //            pId = smeDB.ParentSMEId;
            //            if (lParentParentNum.Contains(pId))
            //            {
            //                found = true;
            //                if (l.SMEType == "D")
            //                {
            //                    var v = unitOfWork.SValueSets.AsFetchable().Where(v => v.SMEId == l.Id).FirstOrDefault();
            //                    if (v.Value.ToLower().StartsWith("http"))
            //                        r.url = v.Value;
            //                }
            //            }
            //        }
            //        r.idShortPath = path;
            //        string sub64 = Base64UrlEncoder.Encode(r.smId);
            //        if (r.url.IsNullOrEmpty())
            //            r.url = ExternalBlazor + "/submodels/" + sub64 + "/submodel-elements/" + path + "/attachment";
            //        if (found)
            //            result.Add(r);
            //    }
            //}
            ////Console.WriteLine("Collected result in " + watch.ElapsedMilliseconds + "ms");

            //return result;

            return new List<SMEResult>();
        }

        // --------------- SM Methodes ---------------
        private List<SMSet> GetSMSet(string semanticId = "", string identifier = "", string diffString = "")
        {
            using var scope = serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetService<IAASUnitOfWork>();

            var withSemanticId = !semanticId.IsNullOrEmpty();
            var withIdentifier = !identifier.IsNullOrEmpty();
            var diff = TimeStamp.TimeStamp.StringToDateTime(diffString);
            var withDiff = !diff.Equals(DateTime.MinValue);

            if (!withSemanticId && !withIdentifier && !withDiff)
                return new List<SMSet>();

            return unitOfWork.SMSets.AsFetchable()
                .Where(s =>
                    (!withSemanticId || (s.SemanticId != null && s.SemanticId.Equals(semanticId))) &&
                    (!withIdentifier || (s.Identifier != null && s.Identifier.Equals(identifier))) &&
                    (!withDiff || s.TimeStampTree.CompareTo(diff) > 0))
                .ToList();
        }

        private static List<SMResult> GetSMResult(List<SMSet> smList)
        {
            return smList.ConvertAll(
                sm =>
                {
                    string identifier = (sm != null && !sm.Identifier.IsNullOrEmpty()) ? sm.Identifier : string.Empty;
                    return new SMResult()
                    {
                        smId = identifier,
                        url = $"{ExternalBlazor}/submodels/{Base64UrlEncoder.Encode(identifier)}",
                        timeStampTree = TimeStamp.TimeStamp.DateTimeToString(sm.TimeStampTree)
                    };
                }
            );
        }

        // --------------- SME Methodes ---------------
        private class SMEWithValue
        {
            public SMSet? sm;
            public SMESet? sme;
            public string? value;
        }

        private List<SMEWithValue> GetSMEWithValue(string smSemanticId = "", string smIdentifier = "", string semanticId = "", string diff = "", string contains = "", string equal = "", string lower = "", string upper = "")
        {
            var result = new List<SMEWithValue>();

            var dateTime = TimeStamp.TimeStamp.StringToDateTime(diff);
            var withDiff = diff != "";
            var withSemanticID = !semanticId.IsNullOrEmpty();

            var parameter = 0;
            if (!contains.IsNullOrEmpty())
                parameter++;
            if (!equal.IsNullOrEmpty())
                parameter++;
            if (!(lower.IsNullOrEmpty() && upper.IsNullOrEmpty()))
                parameter++;
            if (parameter > 1 || (semanticId.IsNullOrEmpty() && !withDiff && parameter != 1))
                return result;

            if ((withSemanticID || withDiff)
                && contains.IsNullOrEmpty() && equal.IsNullOrEmpty() && lower.IsNullOrEmpty() && upper.IsNullOrEmpty())
            {
                using AasContext db = new();
                result.AddRange(db.SMESets
                    .Where(sme =>
                        (!withSemanticID || (sme.SemanticId != null && sme.SemanticId == semanticId)) &&
                        (!withDiff || (sme.TimeStamp > dateTime))
                    )
                    .Select(sme => new SMEWithValue { sme = sme })
                    .ToList());
            }
            else
            {
                GetXValue(ref result, semanticId, dateTime, contains, equal, lower, upper);
                GetSValue(ref result, semanticId, dateTime, contains, equal);
                GetIValue(ref result, semanticId, dateTime, equal, lower, upper);
                GetDValue(ref result, semanticId, dateTime, equal, lower, upper);
                GetOValue(ref result, semanticId, dateTime, contains, equal);
            }

            SelectSM(ref result, smSemanticId, smIdentifier);
            return result;
        }

        private static void GetXValue(ref List<SMEWithValue> smeValue, string semanticId = "", DateTime diff = new(), string contains = "", string equal = "", string lower = "", string upper = "")
        {
            var withValue = !contains.IsNullOrEmpty() || !equal.IsNullOrEmpty() || !lower.IsNullOrEmpty() || !upper.IsNullOrEmpty();
            var withSemanticID = !semanticId.IsNullOrEmpty();
            var withDiff = !diff.Equals(DateTime.MinValue);
            if ((!withSemanticID && !withDiff) || withValue)
                return;

            using AasContext db = new();
            smeValue.AddRange(db.SMESets
                        .Where(sme =>
                            (sme.TValue == string.Empty || sme.TValue == null) &&
                            (!withSemanticID || (sme.SemanticId != null && sme.SemanticId.Equals(semanticId))) &&
                            (!withDiff || sme.TimeStamp.CompareTo(diff) > 0))
                        .Select(sme => new SMEWithValue { sme = sme })
                .ToList());
        }

        private static void GetSValue(ref List<SMEWithValue> smeValue, string semanticId = "", DateTime diff = new(), string contains = "", string equal = "")
        {
            var withSemanticID = !semanticId.IsNullOrEmpty();
            var withDiff = !diff.Equals(DateTime.MinValue);
            var withContains = !contains.IsNullOrEmpty();
            var withEqual = !equal.IsNullOrEmpty();
            if (!withSemanticID && !withDiff && !withContains && !withEqual)
                return;

            //using AasContext db = new();
            //smeValue.AddRange(db.SValueSets
            //    .Where(v => v.Value != null &&
            //        (!withContains || v.Value.Contains(contains)) &&
            //        (!withEqual || v.Value.Equals(equal)))
            //    .Join(
            //        db.SMESets
            //            .Where(sme => 
            //                (!withSemanticID || (sme.SemanticId != null && sme.SemanticId.Equals(semanticId))) &&
            //                (!withDiff || sme.TimeStamp.CompareTo(diff) > 0)),
            //        v => v.SMEId, sme => sme.Id, (v, sme) => new SMEWithValue { sme = sme, value = v.Value })
            //    .ToList());
        }

        private static void GetIValue(ref List<SMEWithValue> smeValue, string semanticId = "", DateTime diff = new(), string equal = "", string lower = "", string upper = "")
        {
            var withSemanticID = !semanticId.IsNullOrEmpty();
            var withDiff = !diff.Equals(DateTime.MinValue);
            var withEqual = !equal.IsNullOrEmpty();
            var withCompare = !(lower.IsNullOrEmpty() && upper.IsNullOrEmpty());
            if (!withSemanticID && !withDiff && !withEqual && !withCompare)
                return;

            var iEqual = (long) 0;
            var iLower = (long) 0;
            var iUpper = (long) 0;
            try
            {
                if (withEqual)
                    iEqual = Convert.ToInt64(equal);
                else if (withCompare)
                {
                    iLower = Convert.ToInt64(lower);
                    iUpper = Convert.ToInt64(upper);
                }
            }
            catch 
            {
                return;
            }

            //using AasContext db = new();
            //smeValue.AddRange(db.IValueSets
            //    .Where(v => v.Value != null &&
            //        (!withEqual || v.Value == iEqual) &&
            //        (!withCompare || (v.Value >= iLower && v.Value <= iUpper)))
            //    .Join(
            //        (db.SMESets
            //            .Where(sme =>
            //                (!withSemanticID || (sme.SemanticId != null && sme.SemanticId.Equals(semanticId))) &&
            //                (!withDiff || sme.TimeStamp.CompareTo(diff) > 0))),
            //        v => v.SMEId, sme => sme.Id, (v, sme) => new SMEWithValue { sme = sme, value = v.Value.ToString() })
            //    .ToList());
        }

        private static void GetDValue(ref List<SMEWithValue> smeValue, string semanticId = "", DateTime diff = new(), string equal = "", string lower = "", string upper = "")
        {
            var withSemanticID = !semanticId.IsNullOrEmpty();
            var withDiff = !diff.Equals(DateTime.MinValue);
            var withEqual = !equal.IsNullOrEmpty();
            var withCompare = !(lower.IsNullOrEmpty() && upper.IsNullOrEmpty());
            if (!withSemanticID && !withDiff && !withEqual && !withCompare)
                return;

            var dEqual = (long) 0;
            var dLower = (long) 0;
            var dUpper = (long) 0;
            try
            {
                if (withEqual)
                    dEqual = Convert.ToInt64(equal);
                else if (withCompare)
                {
                    dLower = Convert.ToInt64(lower);
                    dUpper = Convert.ToInt64(upper);
                }
            }
            catch 
            {
                return;
            }

            //using AasContext db = new();
            //smeValue.AddRange(db.DValueSets
            //    .Where(v => v.Value != null &&
            //        (!withEqual || v.Value == dEqual) &&
            //        (!withCompare || (v.Value >= dLower && v.Value <= dUpper)))
            //    .Join(
            //        (db.SMESets
            //            .Where(sme =>
            //                (!withSemanticID || (sme.SemanticId != null && sme.SemanticId.Equals(semanticId))) &&
            //                (!withDiff || sme.TimeStamp.CompareTo(diff) > 0))),
            //        v => v.SMEId, sme => sme.Id, (v, sme) => new SMEWithValue { sme = sme, value = v.Value.ToString() })
            //    .ToList());
        }

        private void GetOValue(ref List<SMEWithValue> smeValue, string semanticId = "", DateTime diff = new(), string contains = "", string equal = "")
        {
            //using var scope = serviceProvider.CreateScope();
            //var unitOfWork = scope.ServiceProvider.GetService<IAASUnitOfWork>();

            //var withSemanticID = !semanticId.IsNullOrEmpty();
            //var withDiff = !diff.Equals(DateTime.MinValue);
            //var withContains = !contains.IsNullOrEmpty();
            //var withEqual = !equal.IsNullOrEmpty();
            //if (!withSemanticID && !withDiff && !withContains && !withEqual)
            //    return;

            //smeValue.AddRange(unitOfWork.OValueSets.AsFetchable()
            //    .Where(v => v.Value != null &&
            //        (!withContains || ((string) v.Value).Contains(contains)) &&
            //        (!withEqual || ((string) v.Value).Equals(equal)))
            //    .Join(
            //        unitOfWork.SMESets.AsFetchable()
            //            .Where(sme =>
            //                (!withSemanticID || (sme.SemanticId != null && sme.SemanticId.Equals(semanticId))) &&
            //                (!withDiff || sme.TimeStamp.CompareTo(diff) > 0)),
            //        v => v.SMEId, sme => sme.Id, (v, sme) => new SMEWithValue { sme = sme, value = (string) v.Value })
            //    .ToList());
        }

        private void SelectSM(ref List<SMEWithValue> smeValue, string semanticId = "", string identifier = "")
        {
            using var scope = serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetService<IAASUnitOfWork>();

            var withSemanticId = !semanticId.IsNullOrEmpty();
            var withIdentifier = !identifier.IsNullOrEmpty();

            smeValue = smeValue
                .Join((unitOfWork.SMSets.AsFetchable().Where(sm =>
                    (!withSemanticId || (sm.SemanticId != null && sm.SemanticId.Equals(semanticId))) &&
                    (!withIdentifier || (sm.Identifier != null && sm.Identifier.Equals(identifier))))),
                    sme => sme.sme.SMId, sm => sm.Id, (sme, sm) => new SMEWithValue { sm = sm, sme = sme.sme, value = sme.value })
                .Where(sme => sme.sm != null)
                .ToList();
        }

        private List<SMEResult> GetSMEResult(List<SMEWithValue> smeList)
        {
            using var scope = serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetService<IAASUnitOfWork>();

            return smeList.ConvertAll(
                sme =>
                {
                    var identifier = (sme != null && sme.sm.Identifier != null) ? sme.sm.Identifier : "";
                    var path = sme.sme.IdShort;
                    int? pId = sme.sme.ParentSMEId;
                    while (pId != null)
                    {
                        var smeDB = unitOfWork.SMESets.AsFetchable().Where(s => s.Id == pId).First();
                        path = $"{smeDB.IdShort}.{path}";
                        pId = smeDB.ParentSMEId;
                    }

                    return new SMEResult()
                    {
                        smId = identifier,
                        value = sme.value,
                        idShortPath = path,
                        url = $"{ExternalBlazor}/submodels/{Base64UrlEncoder.Encode(identifier)}/submodel-elements/{path}",
                        timeStamp = TimeStamp.TimeStamp.DateTimeToString(sme.sme.TimeStamp)
                    };
                }
            );
        }
    }
}
