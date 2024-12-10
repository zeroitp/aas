using AasOpcUaServer;
using AasxMqttServer;
using AasxServerDB;
using AasxRestServerLibrary;
using AdminShellNS;
using Extensions;
using Jose;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Server;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.IO;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Xml;
using System.Xml.Serialization;
using AasxServerDB.Context;

/*
Copyright (c) 2019-2020 PHOENIX CONTACT GmbH & Co. KG <opensource@phoenixcontact.com>, author: Andreas Orzelski
Copyright (c) 2018-2020 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>, author: Michael Hoffmeister
*/

namespace AasxServer
{
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using AasxServerDB.Repositories;
    using AasxServerStandardBib.EventHandlers.Abstracts;
    using AasxServerStandardBib.Models.Program;
    using AasxServerStandardBib.Utils;
    using AasxTimeSeries;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;

    public static partial class Program
    {
        public static IConfiguration con { get; set; }

        public static void saveEnvDynamic(int envIndex)
        {
            var pkg = AasxServer.Program.env[envIndex];
            lock (pkg)
            {
                if (pkg.IsOpen)
                    pkg.TemporarilySaveCloseAndReOpenPackage(lambda: null);
                else
                    pkg.SaveAs(fn: pkg.Filename);
            }
        }

        public static void saveEnv(int envIndex)
        {
            Console.WriteLine("SAVE: " + envFileName[envIndex]);
            string requestedFileName = envFileName[envIndex];
            string copyFileName = Path.GetTempFileName().Replace(".tmp", ".aasx");
            System.IO.File.Copy(requestedFileName, copyFileName, true);
            AasxServer.Program.env[envIndex].SaveAs(copyFileName);
            System.IO.File.Copy(copyFileName, requestedFileName, true);
            System.IO.File.Delete(copyFileName);
        }

        static int oldest = 0;

        public static bool isLoadingDB = false;
        static bool isLoaded = false;

        public static void loadAllPackages()
        {
            //if (!withDb || isLoadingDB || isLoaded)
            //    return;

            //Program.isLoadingDB = true;
            //var aasIDDBList = unitOfWork.AASSets.Select(aas => aas.Identifier).ToList();

            //foreach (var aasIDDB in aasIDDBList)
            //    loadPackageForAas(aasIDDB, out _, out _);

            //isLoaded = true;
            //Program.isLoadingDB = false;
            //Program.signalNewData(2);
        }

        public static bool loadPackageForAas(string aasIdentifier, out IAssetAdministrationShell output, out int packageIndex)
        {
            output = null;
            packageIndex = -1;
            if (!withDb || Program.isLoading)
                return false;

            int i = envimin;
            while (i < env.Length)
            {
                if (env[i] == null)
                    break;

                var aas = env[i].AasEnv.AssetAdministrationShells.Where(a => a.Id.Equals(aasIdentifier));
                if (aas.Any())
                {
                    output = aas.First();
                    packageIndex = i;
                    return true;
                }

                i++;
            }

            // not found in memory
            if (i == env.Length)
            {
                i = oldest++;
                if (oldest == env.Length)
                    oldest = envimin;
            }

            lock (Program.changeAasxFile)
            {
                //envFileName[i] = Converter.GetAASXPath(aasId: aasIdentifier);
                if (envFileName[i].Equals(""))
                    return false;

                if (env[i] != null)
                {
                    Console.WriteLine("UNLOAD: " + envFileName[i]);
                    if (env[i].getWrite())
                    {
                        saveEnv(i);
                        env[i].setWrite(false);
                    }

                    env[i].Close();
                }


                if (!withDbFiles)
                {
                    Console.WriteLine("LOAD: " + envFileName[i]);
                    env[i] = new AdminShellPackageEnv(envFileName[i]);

                    DateTime timeStamp = DateTime.Now;
                    var a = env[i].AasEnv.AssetAdministrationShells[0];
                    a.TimeStampCreate = timeStamp;
                    a.SetTimeStamp(timeStamp);
                    foreach (var submodel in env[i].AasEnv.Submodels)
                    {
                        submodel.TimeStampCreate = timeStamp;
                        submodel.SetTimeStamp(timeStamp);
                        submodel.SetAllParents(timeStamp);
                    }

                    output = a;
                }
                else
                {
                    //using (AasContext db = unitOfWork)
                    //{
                    //    Console.WriteLine("LOAD: " + aasIdentifier);
                    //    var aasDBList = db.AASSets.Where(a => a.Identifier == aasIdentifier);
                    //    var aasDB = aasDBList.First();
                    //    env[i] = Converter.GetPackageEnv(envFileName[i], aasDB);
                    //    output = env[i].AasEnv.AssetAdministrationShells[0];
                    //}
                }

                packageIndex = i;
                Program.signalNewData(2);
                return true;
            }
        }

        public static bool loadPackageForSubmodel(string submodelIdentifier, out ISubmodel output, out int packageIndex)
        {
            output = null;
            packageIndex = -1;
            if (!withDb || Program.isLoading)
                return false;

            int i = envimin;
            while (i < env.Length)
            {
                if (env[i] == null)
                    break;

                var submodels = env[i].AasEnv.Submodels.Where(s => s.Id.Equals(submodelIdentifier));
                if (submodels.Any())
                {
                    output = submodels.First();
                    packageIndex = i;
                    return true;
                }

                i++;
            }

            // not found in memory
            if (i == env.Length)
            {
                i = oldest++;
                if (oldest == env.Length)
                    oldest = envimin;
            }

            lock (Program.changeAasxFile)
            {
                envFileName[i] = Converter.GetAASXPath(submodelId: submodelIdentifier);
                if (envFileName[i].Equals(""))
                    return false;

                if (env[i] != null)
                {
                    Console.WriteLine("UNLOAD: " + envFileName[i]);
                    if (env[i].getWrite())
                    {
                        saveEnv(i);
                        env[i].setWrite(false);
                    }

                    env[i].Close();
                }

                if (!withDbFiles)
                {
                    Console.WriteLine("LOAD: " + envFileName[i]);
                    env[i] = new AdminShellPackageEnv(envFileName[i]);

                    DateTime timeStamp = DateTime.Now;
                    var a = env[i].AasEnv.AssetAdministrationShells[0];
                    a.TimeStampCreate = timeStamp;
                    a.SetTimeStamp(timeStamp);
                    foreach (var submodel in env[i].AasEnv.Submodels)
                    {
                        submodel.TimeStampCreate = timeStamp;
                        submodel.SetTimeStamp(timeStamp);
                        submodel.SetAllParents(timeStamp);
                    }

                    var submodels = env[i].AasEnv.Submodels.Where(s => s.Id.Equals(submodelIdentifier));
                    if (submodels.Any())
                    {
                        output = submodels.First();
                    }
                }
                else
                {
                    //using (AasContext db = unitOfWork)
                    //{
                    //    var submodelDBList = db.SMSets.OrderBy(sm => sm.Id).Where(sm => sm.Identifier == submodelIdentifier).ToList();
                    //    var submodelDB = submodelDBList.First();

                    //    Console.WriteLine("LOAD Submodel: " + submodelDB.IdShort);
                    //    var aasDBList = db.AASSets.Where(a => a.Id == submodelDB.AASId);
                    //    var aasDB = aasDBList.First();
                    //    env[i] = Converter.GetPackageEnv(envFileName[i], aasDB);
                    //    output = Converter.GetSubmodel(smDB: submodelDB);
                    //}
                }

                packageIndex = i;
                Program.signalNewData(2);
                return true;
            }
        }

        public static int envimin = 0;
        public static int envimax = 200;
        public static AdminShellPackageEnv[] env = null;
        public static string[] envFileName = null;
        public static string[] envSymbols = null;

        public static string[] envSubjectIssuer = null;


        public static string hostPort = "";
        public static string blazorPort = "";
        public static string blazorHostPort = "";

        public static IEnumerable<IAssetAdministrationShell> AllAas()
        {
            return env[0].AasEnv.AssetAdministrationShells;
        }

        public static IEnumerable<ISubmodel> AllSubmodels()
        {
            return env[0].AasEnv.Submodels;
        }

        static Dictionary<string, SampleClient.UASampleClient> OPCClients = new Dictionary<string, SampleClient.UASampleClient>();
        public static readonly object opcclientAddLock = new object(); // object for lock around connecting to an external opc server

        static MqttServer AASMqttServer = new MqttServer();

        static bool runOPC = false;

        public static string connectServer = "";
        public static string connectNodeName = "";
        static int connectUpdateRate = 1000;
        static Thread connectThread;
        static bool connectLoop = false;

        public static WebProxy proxy = null;
        public static HttpClientHandler clientHandler = null;

        public static bool noSecurity = false;
        public static bool edit = false;
        public static string externalRest = "";
        public static string externalBlazor = "";
        public static string externalRepository = "";
        public static bool readTemp = false;
        public static int saveTemp = 0;
        public static DateTime saveTempDt = new DateTime();
        public static string secretStringAPI = null;

        public static bool htmlId = false;

        // public static string Email = "";
        public static long submodelAPIcount = 0;

        public static HashSet<object> submodelsToPublish = new HashSet<object>();
        public static HashSet<object> submodelsToSubscribe = new HashSet<object>();

        public static Dictionary<object, string> generatedQrCodes = new Dictionary<object, string>();

        public static string redirectServer = "";
        public static string authType = "";
        public static string getUrl = "";
        public static string getSecret = "";

        public static bool isLoading = true;
        public static int count = 0;

        public static bool initializingRegistry = false;

        public static object changeAasxFile = new object();

        public static Dictionary<string, string> envVariables = new Dictionary<string, string>();

        public static bool withDb = false;
        public static bool withDbFiles = false;
        public static int startIndex = 0;

        public static bool withPolicy = false;

        public static bool showWeight = false;

        private static async Task<int> Run(CommandLineArguments a)
        {
            // Wait for Debugger
            if (a.DebugWait)
            {
                Console.WriteLine("Please attach debugger now to {0}!", a.Host);
                while (!System.Diagnostics.Debugger.IsAttached)
                    System.Threading.Thread.Sleep(100);
                Console.WriteLine("Debugger attached");
            }

            // Read environment variables
            string[] evlist = { "PLCNEXTTARGET", "WITHPOLICY", "SHOWWEIGHT", "AASREPOSITORY" };
            foreach (var ev in evlist)
            {
                string v = System.Environment.GetEnvironmentVariable(ev);
                if (v != null)
                {
                    v = v.Replace("\r", "");
                    v = v.Replace("\n", "");
                    Console.WriteLine("Variable: " + ev + " = " + v);
                    envVariables.Add(ev, v);
                }
            }

            string w;
            if (envVariables.TryGetValue("WITHPOLICY", out w))
            {
                if (w.ToLower() == "true" || w.ToLower() == "on")
                {
                    withPolicy = true;
                }

                if (w.ToLower() == "false" || w.ToLower() == "off")
                {
                    withPolicy = false;
                }

                Console.WriteLine("withPolicy: " + withPolicy);
            }

            if (envVariables.TryGetValue("SHOWWEIGHT", out w))
            {
                if (w.ToLower() == "true" || w.ToLower() == "on")
                {
                    showWeight = true;
                }

                if (w.ToLower() == "false" || w.ToLower() == "off")
                {
                    showWeight = false;
                }

                Console.WriteLine("showWeight: " + showWeight);
            }

            envVariables.TryGetValue("AASREPOSITORY", out externalRepository);

            if (a.Connect != null)
            {
                if (a.Connect.Length == 0)
                {
                    Program.connectServer = "http://admin-shell-io.com:52000";
                    Byte[] barray = new byte[10];
                    RandomNumberGenerator rngCsp = RandomNumberGenerator.Create();
                    rngCsp.GetBytes(barray);
                    Program.connectNodeName = "AasxServer_" + Convert.ToBase64String(barray);
                    Program.connectUpdateRate = 2000;
                    if (a.Name != null && a.Name != "")
                        Program.connectNodeName = a.Name;
                }
                else if (a.Connect.Length == 1)
                {
                    bool parsable = true;

                    string[] c = a.Connect[0].Split(',');
                    if (c.Length == 3)
                    {
                        int rate = 0;
                        try
                        {
                            rate = Convert.ToInt32(c[2]);
                        }
                        catch (FormatException)
                        {
                            parsable = false;
                        }

                        if (parsable)
                        {
                            if (c[0].Length == 0 || c[1].Length == 0 || c[2].Length == 0 || rate <= 0)
                            {
                                parsable = false;
                            }
                            else
                            {
                                Program.connectServer = c[0];
                                Program.connectNodeName = c[1];
                                Program.connectUpdateRate = Convert.ToInt32(c[2]);
                            }
                        }
                    }
                    else
                    {
                        parsable = false;
                    }

                    if (!parsable)
                    {
                        Console.Error.WriteLine(
                                                "Invalid --connect. " +
                                                "Expected a comma-separated values (server, node name, period in milliseconds), " +
                                                $"but got: {a.Connect[0]}");
                        return 1;
                    }
                }

                Console.WriteLine(
                                  $"--connect: " +
                                  $"ConnectServer {connectServer}, " +
                                  $"NodeName {connectNodeName}, " +
                                  $"UpdateRate {connectUpdateRate}");
            }

            /*
             * Set the global variables at this point inferred from the command-line arguments
             */

            if (a.DataPath != null)
            {
                Console.WriteLine($"Serving the AASXs from: {a.DataPath}");
                AasxHttpContextHelper.DataPath = a.DataPath;
                //AasContext._dataPath = AasxHttpContextHelper.DataPath;
            }

            Program.runOPC = a.Opc;
            Program.noSecurity = a.NoSecurity;
            Program.edit = a.Edit;
            Program.readTemp = a.ReadTemp;
            // if (a.SaveTemp > 0)
            saveTemp = a.SaveTemp;
            Program.htmlId = a.HtmlId;
            Program.withDb = a.WithDb;
            Program.withDbFiles = a.WithDb;
            if (a.NoDbFiles)
                Program.withDbFiles = false;
            if (a.StartIndex > 0)
                startIndex = a.StartIndex;
            if (a.AasxInMemory > 0)
                envimax = a.AasxInMemory;
            if (a.SecretStringAPI != null && a.SecretStringAPI != "")
            {
                secretStringAPI = a.SecretStringAPI;
                Console.WriteLine("secretStringAPI = " + secretStringAPI);
            }

            if (a.OpcClientRate != null && a.OpcClientRate < 200)
            {
                Console.WriteLine("Recommend an OPC client update rate > 200 ms.");
            }

            // allocate memory
            env = new AdminShellPackageEnv[envimax];
            envFileName = new string[envimax];
            envSymbols = new string[envimax];
            envSubjectIssuer = new string[envimax];

            // Proxy
            string proxyAddress = "";
            string username = "";
            string password = "";

            if (a.ProxyFile != null)
            {
                if (!System.IO.File.Exists(a.ProxyFile))
                {
                    Console.Error.WriteLine($"Proxy file not found: {a.ProxyFile}");
                    return 1;
                }

                try
                {
                    using (StreamReader sr = new StreamReader(a.ProxyFile))
                    {
                        proxyAddress = sr.ReadLine();
                        username = sr.ReadLine();
                        password = sr.ReadLine();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(a.ProxyFile + " not found!");
                }

                if (proxyAddress != "")
                {
                    proxy = new WebProxy();
                    Uri newUri = new Uri(proxyAddress);
                    proxy.Address = newUri;
                    proxy.Credentials = new NetworkCredential(username, password);
                    Console.WriteLine("Using proxy: " + proxyAddress);

                    clientHandler = new HttpClientHandler { Proxy = proxy, UseProxy = true };
                }
            }

            ;

            hostPort = a.Host + ":" + a.Port;
            blazorHostPort = a.Host + ":" + blazorPort;

            if (a.ExternalRest != null)
            {
                externalRest = a.ExternalRest;
            }
            else
            {
                externalRest = "http://" + hostPort;
            }

            if (a.ExternalBlazor != null)
            {
                externalBlazor = a.ExternalBlazor;
            }
            else
            {
                externalBlazor = "http://" + blazorHostPort;
            }

            externalBlazor = externalBlazor.Replace("\r", "");
            externalBlazor = externalBlazor.Replace("\n", "");

            if (string.IsNullOrEmpty(externalRepository))
            {
                externalRepository = externalBlazor;
            }

            Query.ExternalBlazor = externalBlazor;

            // Pass global options to subprojects
            AdminShellNS.AdminShellPackageEnv.setGlobalOptions(withDb, withDbFiles, a.DataPath);

            // Read root cert from root subdirectory
            Console.WriteLine("Security 1 Startup - Server");
            Console.WriteLine("Security 1.1 Load X509 Root Certificates into X509 Store Root");

            try
            {
                X509Store root = new X509Store("Root", StoreLocation.CurrentUser);
                root.Open(OpenFlags.ReadWrite);

                DirectoryInfo ParentDirectory = new DirectoryInfo(".");

                if (Directory.Exists("./root"))
                {
                    foreach (FileInfo f in ParentDirectory.GetFiles("./root/*.cer"))
                    {
                        X509Certificate2 cert = new X509Certificate2("./root/" + f.Name);

                        root.Add(cert);
                        Console.WriteLine("Security 1.1 Add " + f.Name);
                    }
                }
            }
            catch (CryptographicException cryptographicException)
            {
                Console.WriteLine($"Cannot initialise cryptography: {cryptographicException.Message}");
            }

            if (!Directory.Exists("./temp"))
                Directory.CreateDirectory("./temp");

            string fn = null;

            if (a.Opc)
            {
                Boolean is_BaseAddresses = false;
                Boolean is_uaString = false;
                XmlTextReader reader = new XmlTextReader("Opc.Ua.SampleServer.Config.xml");
                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element: // The node is an element.
                            if (reader.Name == "BaseAddresses")
                                is_BaseAddresses = true;
                            if (reader.Name == "ua:String")
                                is_uaString = true;
                            break;
                        case XmlNodeType.Text: //Display the text in each element.
                            if (is_BaseAddresses && is_uaString)
                            {
                                Console.WriteLine("Connect to OPC UA by: {0}", reader.Value);
                                is_BaseAddresses = false;
                                is_uaString = false;
                            }

                            break;
                        case XmlNodeType.EndElement: //Display the end of the element.
                            break;
                    }
                }
            }

            bool createFilesOnly = false;
            if (System.IO.File.Exists(AasxHttpContextHelper.DataPath + "/FILES.ONLY"))
                createFilesOnly = true;

            int envi = 0;

            // Migrate always
            if (withDb)
            {
                //if (AasContext.IsPostgres)
                //{
                //    Console.WriteLine("Use POSTGRES");
                using (PostgreAasContext db = new PostgreAasContext())
                {
                    db.Database.Migrate();
                }
                //}
                //else
                //{
                //    Console.WriteLine("Use SQLITE");
                //    using (SqliteAasContext db = new SqliteAasContext())
                //    {
                //        db.Database.Migrate();
                //    }
                //}
            }

            // Clear DB
            //if (withDb && startIndex == 0 && !createFilesOnly)
            //{
            //    using (AasContext db = unitOfWork)
            //    {
            //        await db.ClearDB();
            //    }
            //}

            string[] fileNames = null;
            if (Directory.Exists(AasxHttpContextHelper.DataPath))
            {
                if (!Directory.Exists(AasxHttpContextHelper.DataPath + "/xml"))
                    Directory.CreateDirectory(AasxHttpContextHelper.DataPath + "/xml");
                if (!Directory.Exists(AasxHttpContextHelper.DataPath + "/files"))
                    Directory.CreateDirectory(AasxHttpContextHelper.DataPath + "/files");

                var watch = System.Diagnostics.Stopwatch.StartNew();
                fileNames = Directory.GetFiles(AasxHttpContextHelper.DataPath, "*.aasx");
                Array.Sort(fileNames);

                var fi = 0;
                while (fi < fileNames.Length)
                {
                    // try
                    {
                        fn = fileNames[fi];
                        if (fn.ToLower(System.Globalization.CultureInfo.CurrentCulture).Contains("globalsecurity", StringComparison.InvariantCulture) ||
                            fn.ToLower(System.Globalization.CultureInfo.CurrentCulture).Contains("registry", StringComparison.InvariantCulture))
                        {
                            envFileName[envi] = fn;
                            env[envi] = new AdminShellPackageEnv(fn, true, false);
                            //TODO:jtikekar
                            //AasxHttpContextHelper.securityInit(); // read users and access rights from AASX Security
                            //AasxHttpContextHelper.serverCertsInit(); // load certificates of auth servers
                            envi++;
                            envimin = envi;
                            oldest = envi;
                            fi++;
                            continue;
                        }

                        if (fi < startIndex)
                        {
                            fi++;
                            continue;
                        }


                        if (fn != "" && envi < envimax)
                        {
                            string name = Path.GetFileName(fn);
                            string tempName = "./temp/" + Path.GetFileName(fn);

                            // Convert to newest version only
                            if (saveTemp == -1)
                            {
                                env[envi] = new AdminShellPackageEnv(fn, true, false);
                                if (env[envi] == null)
                                {
                                    Console.Error.WriteLine($"Cannot open {fn}. Aborting..");
                                    return 1;
                                }

                                Console.WriteLine((fi + 1) + "/" + fileNames.Length + " " + watch.ElapsedMilliseconds / 1000 + "s " + "SAVE TO TEMP: " + fn);
                                Program.env[envi].SaveAs(tempName);
                                fi++;
                                continue;
                            }

                            if (readTemp && System.IO.File.Exists(tempName))
                            {
                                fn = tempName;
                            }

                            Console.WriteLine((fi + 1) + "/" + fileNames.Length + " " + watch.ElapsedMilliseconds / 1000 + "s" + " Loading {0}...", fn);
                            envFileName[envi] = fn;
                            if (!withDb)
                            {
                                env[envi] = new AdminShellPackageEnv(fn, true, false);
                                if (env[envi] == null)
                                {
                                    Console.Error.WriteLine($"Cannot open {fn}. Aborting..");
                                    return 1;
                                }
                            }
                            else
                            {
                                VisitorAASX.LoadAASXInDB(fn, createFilesOnly, withDbFiles);
                                envFileName[envi] = null;
                                env[envi] = null;
                            }

                            // check if signed
                            string fileCert = "./user/" + name + ".cer";
                            if (System.IO.File.Exists(fileCert))
                            {
                                X509Certificate2 x509 = new X509Certificate2(fileCert);
                                envSymbols[envi] = "S";
                                envSubjectIssuer[envi] = x509.Subject;

                                X509Chain chain = new X509Chain();
                                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                                bool isValid = chain.Build(x509);
                                if (isValid)
                                {
                                    envSymbols[envi] += ";V";
                                    envSubjectIssuer[envi] += ";" + x509.Issuer;
                                }
                            }
                        }

                        fi++;
                        if (withDb)
                        {
                            if (fi % 500 == 0) // every 500
                            {
                                /*
                                Console.WriteLine("DB Save Changes");
                                db.SaveChanges();
                                db.ChangeTracker.Clear();
                                System.GC.Collect();
                                */
                            }
                        }
                        else
                        {
                            envi++;
                        }
                    }
                }

                if (saveTemp == -1)
                    return (0);

                if (withDb)
                {
                    /*
                    Console.WriteLine("DB Save Changes");
                    db.SaveChanges();
                    db.ChangeTracker.Clear();
                    System.GC.Collect();
                    */
                }

                watch.Stop();
                Console.WriteLine(fi + " AASX loaded in " + watch.ElapsedMilliseconds / 1000 + "s");

                fileNames = Directory.GetFiles(AasxHttpContextHelper.DataPath, "*.aasx2");
                Array.Sort(fileNames);

                for (int j = 0; j < fileNames.Length; j++)
                {
                    fn = fileNames[j];

                    if (fn != "" && envi < envimax)
                    {
                        envFileName[envi] = fn;
                        envSymbols[envi] = "L"; // Show lock
                    }

                    envi++;
                }
            }

            if (!withDb)
            {
                // AasxHttpContextHelper.securityInit(); // read users and access rights form AASX Security
                // AasxHttpContextHelper.serverCertsInit(); // load certificates of auth servers
            }

            Console.WriteLine();
            Console.WriteLine("Please wait for the servers to start...");

            if (a.Rest)
            {
                Console.WriteLine("--rest argument is not supported anymore, as the old V2 related REST APIs are deprecated. Please find the new REST APIs on the port 5001.");
            }

            // MICHA MICHA
            AasxTimeSeries.TimeSeries.timeSeriesInit();

            AasxTask.taskInit();

            RunScript(true);

            isLoading = false;

            if (a.Mqtt)
            {
                AASMqttServer.MqttSeverStartAsync().Wait();
                Console.WriteLine("MQTT Publisher started.");
            }

            MySampleServer server = null;
            if (a.Opc)
            {
                server = new MySampleServer(_autoAccept: true, _stopTimeout: 0, _aasxEnv: env);
                Console.WriteLine("OPC UA Server started..");
            }

            if (a.OpcClientRate != null) // read data by OPC UA
            {
                // Initial read of OPC values, will quit the program if it returns false
                if (!ReadOPCClient(true))
                {
                    Console.Error.WriteLine("Failed to read from the OPC client.");
                    return 1;
                }

                Console.WriteLine($"OPC client will be updating every: {a.OpcClientRate} milliseconds");
                SetOPCClientTimer((double)a.OpcClientRate); // read again everytime timer expires
            }

            SetScriptTimer(1000); // also updates balzor view

            if (connectServer != "")
            {
                HttpClient httpClient;
                if (clientHandler == null)
                {
                    clientHandler = new HttpClientHandler();
                    clientHandler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
                    httpClient = new HttpClient(clientHandler);
                }

                httpClient = new HttpClient(clientHandler);

                string payload = "{ \"source\" : \"" + connectNodeName + "\" }";

                //
                string content = "";
                try
                {
                    var contentJson = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
                    // httpClient.PostAsync("http://" + connectServer + "/connect", contentJson).Wait();
                    var result = httpClient.PostAsync(connectServer + "/connect", contentJson).Result;
                    content = ContentToString(result.Content);
                }
                catch
                {
                }

                if (content == "OK")
                {
                    connectThread = new Thread(new ThreadStart(connectThreadLoop));
                    // MICHA
                    // connectThread.Start();
                    connectLoop = true;
                }
                else
                {
                    Console.WriteLine("********** Can not connect to: " + connectServer);
                }
            }

            Program.signalNewData(3);

            if (a.Opc && server != null)
            {
                server.Run(); // wait for CTRL-C
            }
            else
            {
                // no OPC UA: wait only for CTRL-C
                Console.WriteLine("Servers successfully started. Press Ctrl-C to exit...");
            }

            if (connectServer != "")
            {
                if (connectLoop)
                {
                    connectLoop = false;
                }
            }

            AasxRestServer.Stop();

            return 0;
        }

        public static void Main(string[] args)
        {
            Console.WriteLine("args:");
            foreach (var a in args)
            {
                Console.WriteLine(a);
            }

            Console.WriteLine();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // AppContext.SetSwitch("System.Net.Http.UseSocketsHttpHandler", false);
            }

            AasContext._con = con;
            //AHIContext._con = con;
            //if (con != null)
            //{
            //    if (con["DatabaseConnection:ConnectionString"] != null)
            //    {
            //        AasContext.IsPostgres = con["DatabaseConnection:ConnectionString"].ToLower().Contains("host");
            //    }
            //}

            string nl = System.Environment.NewLine;

            var rootCommand = new RootCommand("serve AASX packages over different interfaces")
                              {
                                  new Option<string>(
                                                     new[] {"--host"},
                                                     () => "localhost",
                                                     "Host which the server listens on"),
                                  new Option<string>(
                                                     new[] {"--data-path"},
                                                     "Path to where the AASXs reside"),
                                  new Option<bool>(
                                                   new[] {"--opc"},
                                                   "If set, starts the OPC server"),
                                  new Option<bool>(
                                                   new[] {"--mqtt"},
                                                   "If set, starts a MQTT publisher"),
                                  new Option<bool>(
                                                   new[] {"--debug-wait"},
                                                   "If set, waits for Debugger to attach"),
                                  new Option<int>(
                                                  new[] {"--opc-client-rate"},
                                                  "If set, starts an OPC client and refreshes on the given period " +
                                                  "(in milliseconds)"),
                                  new Option<string>(
                                                     new[] {"--proxy-file"},
                                                     "If set, parses the proxy information from the given proxy file"),
                                  new Option<bool>(
                                                   new[] {"--no-security"},
                                                   "If set, no authentication is required"),
                                  new Option<bool>(
                                                   new[] {"--edit"},
                                                   "If set, allows edits in the user interface"),
                                  new Option<string>(
                                                     new[] {"--name"},
                                                     "Name of the server"),
                                  new Option<string>(
                                                     new[] {"--external-blazor"},
                                                     "external name of the server blazor UI"),
                                  new Option<bool>(
                                                   new[] {"--read-temp"},
                                                   "If set, reads existing AASX from temp at startup"),
                                  new Option<int>(
                                                  new[] {"--save-temp"},
                                                  "If set, writes AASX every given seconds"),
                                  new Option<string>(
                                                     new[] {"--secret-string-api"},
                                                     "If set, allows UPDATE access by query parameter s="),
                                  new Option<bool>(
                                                   new[] {"--html-id"},
                                                   "If set, creates id for HTML objects in blazor tree for testing"),
                                  new Option<string>(
                                                     new[] {"--tag"},
                                                     "Only used to differ servers in task list"),
                                  new Option<int>(
                                                  new[] {"--aasx-in-memory"},
                                                  "If set, size of array of AASX files in memory"),
                                  new Option<bool>(
                                                   new[] {"--with-db"},
                                                   "If set, will use DB by Entity Framework"),
                                  new Option<bool>(
                                                   new[] {"--no-db-files"},
                                                   "If set, do not export files from AASX into ZIP"),
                                  new Option<int>(
                                                  new[] {"--start-index"},
                                                  "If set, start index in list of AASX files")
                              };

            if (args.Length == 0)
            {
                new HelpBuilder(new SystemConsole()).Write(rootCommand);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
                    WindowsConsoleWillBeDestroyedAtTheEnd.Check())
                {
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                }

                return;
            }

            rootCommand.Handler = System.CommandLine.Invocation.CommandHandler.Create((CommandLineArguments a) =>
                                                                                      {
                                                                                          var task = Run(a);
                                                                                          task.Wait();
                                                                                          var op = task.Result;
                                                                                          return Task.FromResult(op);
                                                                                      });

            int exitCode = rootCommand.InvokeAsync(args).Result;
            System.Environment.ExitCode = exitCode;
        }

        /*Publishing the AAS Descriptor*/
        public static void publishDescriptorData(string descriptorData)
        {
            HttpClient httpClient;
            if (clientHandler != null)
            {
                httpClient = new HttpClient(clientHandler);
            }
            else
            {
                httpClient = new HttpClient();
            }

            var descriptorJson = new StringContent(descriptorData, System.Text.Encoding.UTF8, "application/json");
            try
            {
                var result = httpClient.PostAsync(connectServer + "/publish", descriptorJson).Result;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        static bool getDirectory = true;
        static string getDirectoryDestination = "";

        static string getaasxFile_destination = "";
        static string getaasxFile_fileName = "";
        static string getaasxFile_fileData = "";
        static string getaasxFile_fileType = "";
        static int getaasxFile_fileLenBase64 = 0;
        static int getaasxFile_fileLenBinary = 0;
        static int getaasxFile_fileTransmitted = 0;
        static int blockSize = 1500000;

        public static List<TransmitData> tdPending = new List<TransmitData> { };

        public static void connectThreadLoop()
        {
            bool newConnectData = false;

            while (connectLoop)
            {
                TransmitFrame tf = new TransmitFrame { source = connectNodeName };
                TransmitData td = null;

                if (getDirectory)
                {
                    Console.WriteLine("if getDirectory");

                    // AAAS Detail part 2 Descriptor
                    TransmitFrame descriptortf = new TransmitFrame { source = connectNodeName };

                    aasDirectoryParameters adp = new aasDirectoryParameters();

                    adp.source = connectNodeName;

                    int aascount = Program.env.Length;

                    for (int j = 0; j < aascount; j++)
                    {
                        aasListParameters alp = new aasListParameters();

                        if (Program.env[j] != null)
                        {
                            alp.index = j;

                            /* Create Detail part 2 Descriptor Start */
                            aasDescriptor aasDsecritpor = Program.creatAASDescriptor(Program.env[j]);
                            TransmitData aasDsecritporTData = new TransmitData { source = connectNodeName };
                            aasDsecritporTData.type = "register";
                            aasDsecritporTData.destination = "VWS_AAS_Registry";
                            var options = new JsonSerializerOptions { WriteIndented = true, IgnoreNullValues = true };

                            var aasDescriptorJsonData = System.Text.Json.JsonSerializer.Serialize(aasDsecritpor, options);

                            aasDsecritporTData.publish.Add(aasDescriptorJsonData);
                            descriptortf.data.Add(aasDsecritporTData);
                            /* Create Detail part 2 Descriptor END */


                            alp.idShort = Program.env[j].AasEnv.AssetAdministrationShells[0].IdShort;
                            alp.identification = Program.env[j].AasEnv.AssetAdministrationShells[0].Id;
                            alp.fileName = Program.envFileName[j];
                            alp.assetId = "";
                            //var asset = Program.env[j].AasEnv.FindAsset(Program.env[j].AasEnv.AssetAdministrationShells[0].assetRef);
                            var asset = Program.env[j].AasEnv.AssetAdministrationShells[0].AssetInformation;
                            if (asset != null)
                                alp.humanEndPoint = blazorHostPort;
                            alp.restEndPoint = hostPort;

                            adp.aasList.Add(alp);
                        }
                    }

                    string decriptorData = System.Text.Json.JsonSerializer.Serialize(descriptortf, new JsonSerializerOptions { WriteIndented = true, });
                    Program.publishDescriptorData(decriptorData);

                    td = new TransmitData { source = connectNodeName };

                    string json = System.Text.Json.JsonSerializer.Serialize(adp, new JsonSerializerOptions { WriteIndented = true, });
                    td.type = "directory";
                    td.destination = getDirectoryDestination;
                    td.publish.Add(json);
                    tf.data.Add(td);
                    Console.WriteLine("Send directory");

                    getDirectory = false;
                    getDirectoryDestination = "";
                }

                if (getaasxFile_destination != "") // block transfer
                {
                    dynamic res = new System.Dynamic.ExpandoObject();

                    td = new TransmitData { source = connectNodeName };

                    int len = 0;
                    if ((getaasxFile_fileLenBase64 - getaasxFile_fileTransmitted) > blockSize)
                    {
                        len = blockSize;
                    }
                    else
                    {
                        len = getaasxFile_fileLenBase64 - getaasxFile_fileTransmitted;
                    }

                    res.fileData = getaasxFile_fileData.Substring(getaasxFile_fileTransmitted, len);
                    res.fileName = getaasxFile_fileName;
                    res.fileLenBase64 = getaasxFile_fileLenBase64;
                    res.fileLenBinary = getaasxFile_fileLenBinary;
                    res.fileType = getaasxFile_fileType;
                    res.fileTransmitted = getaasxFile_fileTransmitted;

                    string responseJson = System.Text.Json.JsonSerializer.Serialize(res, new JsonSerializerOptions { WriteIndented = true, });
                    td.destination = getaasxFile_destination;
                    td.type = "getaasxBlock";
                    td.publish.Add(responseJson);
                    tf.data.Add(td);

                    getaasxFile_fileTransmitted += len;

                    if (getaasxFile_fileTransmitted == getaasxFile_fileLenBase64)
                    {
                        getaasxFile_destination = "";
                        getaasxFile_fileName = "";
                        getaasxFile_fileData = "";
                        getaasxFile_fileType = "";
                        res.fileLenBase64 = 0;
                        res.fileLenBinary = 0;
                        getaasxFile_fileTransmitted = 0;
                    }
                }

                if (tdPending.Count != 0)
                {
                    foreach (TransmitData tdp in tdPending)
                    {
                        tf.data.Add(tdp);
                    }

                    tdPending.Clear();
                }

                int envi = 0;
                while (env[envi] != null)
                {
                    foreach (var sm in env[envi].AasEnv.Submodels)
                    {
                        if (sm != null && sm.IdShort != null)
                        {
                            bool toPublish = Program.submodelsToPublish.Contains(sm);
                            if (!toPublish)
                            {
                                int count = sm.Qualifiers.Count;
                                if (count != 0)
                                {
                                    int j = 0;

                                    while (j < count) // Scan qualifiers
                                    {
                                        var p = sm.Qualifiers[j] as Qualifier;

                                        if (p.Type == "PUBLISH")
                                        {
                                            toPublish = true;
                                        }

                                        j++;
                                    }
                                }
                            }

                            if (toPublish)
                            {
                                td = new TransmitData { source = connectNodeName };

                                var json = System.Text.Json.JsonSerializer.Serialize(sm, new JsonSerializerOptions { WriteIndented = true, });
                                td.type = "submodel";
                                td.publish.Add(json);
                                tf.data.Add(td);
                                Console.WriteLine("Publish Submodel " + sm.IdShort);
                            }
                        }
                    }

                    envi++;
                }

                string publish = System.Text.Json.JsonSerializer.Serialize(tf, new JsonSerializerOptions { WriteIndented = true, });

                HttpClient httpClient;
                if (clientHandler != null)
                {
                    httpClient = new HttpClient(clientHandler);
                }
                else
                {
                    httpClient = new HttpClient();
                }

                var contentJson = new StringContent(publish, System.Text.Encoding.UTF8, "application/json");

                string content = "";
                try
                {
                    var result = httpClient.PostAsync(connectServer + "/publish", contentJson).Result;
                    content = ContentToString(result.Content);
                }
                catch
                {
                }

                if (content != "")
                {
                    newConnectData = false;
                    string node = "";

                    try
                    {
                        TransmitFrame tf2 = new TransmitFrame();
                        tf2 = JsonSerializer.Deserialize<TransmitFrame>(content);

                        node = tf2.source;
                        foreach (TransmitData td2 in tf2.data)
                        {
                            if (td2.type == "getDirectory")
                            {
                                Console.WriteLine("received getDirectory");
                                getDirectory = true;
                                getDirectoryDestination = td2.source;
                            }

                            if (td2.type == "getaasx" && td2.destination == connectNodeName)
                            {
                                int aasIndex = Convert.ToInt32(td2.extensions);

                                dynamic res = new System.Dynamic.ExpandoObject();

                                Byte[] binaryFile = System.IO.File.ReadAllBytes(Program.envFileName[aasIndex]);
                                string binaryBase64 = Convert.ToBase64String(binaryFile);

                                string payload = "{ \"file\" : \" " + binaryBase64 + " \" }";

                                System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
                                string fileToken = Jose.JWT.Encode(payload, enc.GetBytes(AasxRestServerLibrary.AasxHttpContextHelper.secretString), JwsAlgorithm.HS256);

                                if (fileToken.Length <= blockSize)
                                {
                                    res.fileName = Path.GetFileName(Program.envFileName[aasIndex]);
                                    res.fileData = fileToken;

                                    string responseJson = System.Text.Json.JsonSerializer.Serialize(res, new JsonSerializerOptions { WriteIndented = true, });

                                    TransmitData tdp = new TransmitData();

                                    tdp.source = connectNodeName;
                                    tdp.destination = td2.source;
                                    tdp.type = "getaasxFile";
                                    tdp.publish.Add(responseJson);
                                    tdPending.Add(tdp);
                                }
                                else
                                {
                                    getaasxFile_destination = td2.source;
                                    getaasxFile_fileName = Path.GetFileName(Program.envFileName[aasIndex]);
                                    getaasxFile_fileData = fileToken;
                                    getaasxFile_fileType = "getaasxFileStream";
                                    getaasxFile_fileLenBase64 = getaasxFile_fileData.Length;
                                    getaasxFile_fileLenBinary = binaryFile.Length;
                                    getaasxFile_fileTransmitted = 0;
                                }
                            }

                            if (td2.type == "getaasxstream" && td2.destination == connectNodeName)
                            {
                                int aasIndex = Convert.ToInt32(td2.extensions);

                                dynamic res = new System.Dynamic.ExpandoObject();

                                Byte[] binaryFile = System.IO.File.ReadAllBytes(Program.envFileName[aasIndex]);
                                string binaryBase64 = Convert.ToBase64String(binaryFile);

                                if (binaryBase64.Length <= blockSize)
                                {
                                    res.fileName = Path.GetFileName(Program.envFileName[aasIndex]);
                                    res.fileData = binaryBase64;
                                    Byte[] fileBytes = Convert.FromBase64String(binaryBase64);
                                    string responseJson = System.Text.Json.JsonSerializer.Serialize(res, new JsonSerializerOptions { WriteIndented = true, });


                                    TransmitData tdp = new TransmitData();

                                    tdp.source = connectNodeName;
                                    tdp.destination = td2.source;
                                    tdp.type = "getaasxFile";
                                    tdp.publish.Add(responseJson);
                                    tdPending.Add(tdp);
                                }
                                else
                                {
                                    getaasxFile_destination = td2.source;
                                    getaasxFile_fileName = Path.GetFileName(Program.envFileName[aasIndex]);
                                    getaasxFile_fileData = binaryBase64;
                                    getaasxFile_fileType = "getaasxFile";
                                    getaasxFile_fileLenBase64 = getaasxFile_fileData.Length;
                                    getaasxFile_fileLenBinary = binaryFile.Length;
                                    getaasxFile_fileTransmitted = 0;
                                }
                            }

                            if (td2.type.ToLower().Contains("timeseries"))
                            {
                                string[] split = td2.type.Split('.');
                                foreach (var smc in AasxTimeSeries.TimeSeries.timeSeriesSubscribe)
                                {
                                    if (smc.IdShort == split[0])
                                    {
                                        foreach (var tsb in AasxTimeSeries.TimeSeries.timeSeriesBlockList)
                                        {
                                            if (tsb.sampleStatus.Value == "stop")
                                            {
                                                tsb.sampleStatus.Value = "stopped";
                                            }

                                            if (tsb.sampleStatus.Value != "start")
                                                continue;

                                            if (tsb.block == smc)
                                            {
                                                transformTsbBlock(td2, smc, tsb);
                                            }
                                        }
                                    }
                                }
                            }

                            if (td2.type == "submodel")
                            {
                                foreach (string sm in td2.publish)
                                {
                                    Submodel submodel = null;
                                    try
                                    {
                                        using (var reader = new StringReader(sm))
                                        {
                                            var options = new JsonSerializerOptions();
                                            options.Converters.Add(new AdminShellConverters.JsonAasxConverter("modelType", "name"));

                                            submodel = System.Text.Json.JsonSerializer.Deserialize<Submodel>(reader.ReadToEnd(), options);
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        Console.WriteLine("Can not read SubModel!");
                                        return;
                                    }

                                    // need id for idempotent behaviour
                                    if (submodel.Id == null)
                                    {
                                        Console.WriteLine("Identification of SubModel is (null)!");
                                        return;
                                    }

                                    IAssetAdministrationShell aas = null;
                                    envi = 0;
                                    while (env[envi] != null)
                                    {
                                        aas = env[envi].AasEnv.FindAasWithSubmodelId(submodel.Id);
                                        if (aas != null)
                                            break;
                                        envi++;
                                    }


                                    if (aas != null)
                                    {
                                        // datastructure update
                                        if (env == null || env[envi].AasEnv == null /*|| env[envi].AasEnv.Assets == null*/)
                                        {
                                            Console.WriteLine("Error accessing internal data structures.");
                                            return;
                                        }

                                        var existingSm = env[envi].AasEnv.FindSubmodelById(submodel.Id);
                                        if (existingSm != null)
                                        {
                                            bool toSubscribe = Program.submodelsToSubscribe.Contains(existingSm);
                                            if (!toSubscribe)
                                            {
                                                int eqcount = existingSm.Qualifiers.Count;
                                                if (eqcount != 0)
                                                {
                                                    int j = 0;

                                                    while (j < eqcount) // Scan qualifiers
                                                    {
                                                        var p = existingSm.Qualifiers[j] as Qualifier;

                                                        if (p.Type == "SUBSCRIBE")
                                                        {
                                                            toSubscribe = true;
                                                            break;
                                                        }

                                                        j++;
                                                    }
                                                }
                                            }

                                            if (toSubscribe)
                                            {
                                                Console.WriteLine("Subscribe Submodel " + submodel.IdShort);

                                                int c2 = submodel.Qualifiers.Count;
                                                if (c2 != 0)
                                                {
                                                    int k = 0;

                                                    while (k < c2) // Scan qualifiers
                                                    {
                                                        var q = submodel.Qualifiers[k] as Qualifier;

                                                        if (q.Type == "PUBLISH")
                                                        {
                                                            q.Type = "SUBSCRIBE";
                                                        }

                                                        k++;
                                                    }
                                                }

                                                bool overwrite = true;
                                                int escount = existingSm.SubmodelElements.Count;
                                                int count2 = submodel.SubmodelElements.Count;
                                                if (escount == count2)
                                                {
                                                    int smi = 0;
                                                    while (smi < escount)
                                                    {
                                                        var sme1 = submodel.SubmodelElements[smi];
                                                        var sme2 = existingSm.SubmodelElements[smi];

                                                        if (sme1 is Property)
                                                        {
                                                            if (sme2 is Property)
                                                            {
                                                                (sme2 as Property).Value = (sme1 as Property).Value;
                                                            }
                                                            else
                                                            {
                                                                overwrite = false;
                                                                break;
                                                            }
                                                        }

                                                        smi++;
                                                    }
                                                }

                                                if (!overwrite)
                                                {
                                                    env[envi].AasEnv.Submodels.Remove(existingSm);
                                                    env[envi].AasEnv.Submodels.Add(submodel);

                                                    // add SubmodelRef to AAS            
                                                    // access the AAS
                                                    var key = new Key(KeyTypes.Submodel, submodel.Id);
                                                    var keyList = new List<IKey>() { key };
                                                    var newsmr = new Reference(AasCore.Aas3_0.ReferenceTypes.ModelReference, keyList);
                                                    //var newsmr = SubmodelRef.CreateNew("Submodel", submodel.id);
                                                    var existsmr = aas.HasSubmodelReference(newsmr);
                                                    if (!existsmr)
                                                    {
                                                        aas.Submodels.Add(newsmr);
                                                    }
                                                }

                                                newConnectData = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                    }

                    if (newConnectData)
                    {
                        NewDataAvailable?.Invoke(null, EventArgs.Empty);
                    }
                }

                if (getaasxFile_destination != "") // block transfer
                {
                    Thread.Sleep(500);
                }
                else
                    Thread.Sleep(connectUpdateRate);
            }
        }

        static bool timerSet = false;

        private static void SetOPCClientTimer(double value)
        {
            if (timerSet)
            {
                return;
            }

            timerSet = true;

            AasxTimeSeries.TimeSeries.SetOPCClientThread(value);
        }

        public static event EventHandler NewDataAvailable;

        private static void OnOPCClientNextTimedEvent(Object source, ElapsedEventArgs e)
        {
            ReadOPCClient(false);
            // RunScript(false);
            NewDataAvailable?.Invoke(null, EventArgs.Empty);
        }
    }
}