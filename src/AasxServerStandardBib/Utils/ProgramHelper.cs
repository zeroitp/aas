using AasxServer;
using AasxServerStandardBib.EventHandlers.Abstracts;
using AasxServerStandardBib.Models.Program;
using AasxTimeSeries;
using AdminShellNS;
using Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Opc.Ua;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace AasxServer;

public static partial class Program
{
    public static ulong dataVersion = 0;

    public static async Task StartEventHandlers(IHost host)
    {
        var eventHandlers = host.Services.GetRequiredService<IEnumerable<IEventHandler>>();

        foreach (var eventHandler in eventHandlers)
            await eventHandler.Start();
    }

    public static string getBetween(AdminShellPackageEnv env, string strStart, string strEnd)
    {
        string strSource = env.getEnvXml();
        if (strSource != null && strSource.Contains(strStart) && strSource.Contains(strEnd))
        {
            int Start, End;
            Start = strSource.IndexOf(strStart, 0) + strStart.Length;
            End = strSource.IndexOf(strEnd, Start);
            return strSource.Substring(Start, End - Start);
        }

        return "";
    }

    public static (ISubmodel Submodel, ISubmodelElement Sme) FindSmeByGuid(Guid id)
    {
        var idStr = id.ToString();
        foreach (var sm in Program.AllSubmodels())
        {
            foreach (var sme in sm.SubmodelElements)
            {
                if (sme.IdShort == idStr)
                    return (sm, sme);
            }
        }
        return default;
    }

    public static string ContentToString(this HttpContent httpContent)
    {
        var readAsStringAsync = httpContent.ReadAsStringAsync();
        return readAsStringAsync.Result;
    }

    public static aasDescriptor creatAASDescriptor(AdminShellPackageEnv adminShell)
    {
        aasDescriptor aasD = new aasDescriptor();
        string endpointAddress = "http://" + hostPort;

        aasD.idShort = adminShell.AasEnv.AssetAdministrationShells[0].IdShort;
        aasD.identification = adminShell.AasEnv.AssetAdministrationShells[0].Id;
        aasD.description = adminShell.AasEnv.AssetAdministrationShells[0].Description;

        AASxEndpoint endp = new AASxEndpoint();
        endp.address = endpointAddress + "/aas/" + adminShell.AasEnv.AssetAdministrationShells[0].IdShort;
        aasD.endpoints.Add(endp);

        int submodelCount = adminShell.AasEnv.Submodels.Count;
        for (int i = 0; i < submodelCount; i++)
        {
            SubmodelDescriptors sdc = new SubmodelDescriptors();

            sdc.administration = adminShell.AasEnv.Submodels[i].Administration as AdministrativeInformation;
            sdc.description = adminShell.AasEnv.Submodels[i].Description;
            sdc.identification = adminShell.AasEnv.Submodels[i].Id;
            sdc.idShort = adminShell.AasEnv.Submodels[i].IdShort;
            sdc.semanticId = adminShell.AasEnv.Submodels[i].SemanticId as Reference;

            AASxEndpoint endpSub = new AASxEndpoint();
            endpSub.address = endpointAddress + "/aas/" + adminShell.AasEnv.AssetAdministrationShells[0].IdShort +
                              "/submodels/" + adminShell.AasEnv.Submodels[i].IdShort;
            endpSub.type = "http";
            sdc.endpoints.Add(endpSub);

            aasD.submodelDescriptors.Add(sdc);
        }

        return aasD;
    }

    public static void connectPublish(string type, string json)
    {
        if (Program.connectServer == "")
            return;

        TransmitData tdp = new TransmitData();

        tdp.source = Program.connectNodeName;
        tdp.type = type;
        tdp.publish.Add(json);
        Program.tdPending.Add(tdp);
    }

    private static void transformTsbBlock(TransmitData td2, SubmodelElementCollection smc, TimeSeries.TimeSeriesBlock tsb)
    {
        foreach (var data in td2.publish)
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new AdminShellConverters.JsonAasxConverter("modelType", "name"));

            SubmodelElementCollection smcData;

            using (TextReader reader = new StringReader(data))
            {
                string jsonString = reader.ReadToEnd();
                smcData = System.Text.Json.JsonSerializer.Deserialize<SubmodelElementCollection>(jsonString, options);

                if (smcData != null && smc.Value.Count < 100)
                {
                    if (tsb.data != null)
                    {
                        int maxCollections = Convert.ToInt32(tsb.maxCollections.Value);
                        int actualCollections = tsb.data.Value.Count;
                        if (actualCollections < maxCollections ||
                            (tsb.sampleMode.Value == "continuous" && actualCollections == maxCollections))
                        {
                            tsb.data.Value.Add(smcData);
                            actualCollections++;
                        }

                        if (actualCollections > maxCollections)
                        {
                            tsb.data.Value.RemoveAt(0);
                            actualCollections--;
                        }

                        tsb.actualCollections.Value = actualCollections.ToString();
                        /*
                                                            tsb.lowDataIndex =
                                                                Convert.ToInt32(tsb.data.Value[0].submodelElement.IdShort.Substring("data".Length));
                                                            tsb.highDataIndex =
                                                                Convert.ToInt32(tsb.data.Value[tsb.data.Value.Count - 1].submodelElement.IdShort.Substring("data".Length));
                                                            */
                        signalNewData(1);
                    }
                }
            }
        }
    }

    // 0 == same tree, only values changed
    // 1 == same tree, structure may change
    // 2 == build new tree, keep open nodes
    // 3 == build new tree, all nodes closed
    // public static int signalNewDataMode = 2;
    public static void signalNewData(int mode)
    {
        // signalNewDataMode = mode;
        // NewDataAvailable?.Invoke(null, EventArgs.Empty);
        NewDataAvailable?.Invoke(null, new NewDataAvailableArgs(mode));
    }

    public static void OnOPCClientNextTimedEvent()
    {
        ReadOPCClient(false);
        // RunScript(false);
        NewDataAvailable?.Invoke(null, EventArgs.Empty);
    }

    static Boolean ReadOPCClient(bool initial)
    /// <summary>
    /// Update AAS property values from external OPC servers.
    /// Only submodels which have the appropriate qualifier are affected.
    /// However, this will attempt to get values for all properties of the submodel.
    /// TODO: Possilby add a qualifier to specifiy which values to get? Or NodeIds per alue?
    /// </summary>
    {
        if (env == null)
            return false;

        lock (Program.changeAasxFile)
        {
            int i = 0;
            while (env[i] != null)
            {
                foreach (var sm in env[i].AasEnv.Submodels)
                {
                    if (sm != null && sm.IdShort != null)
                    {
                        int count = sm.Qualifiers.Count;
                        if (count != 0)
                        {
                            int stopTimeout = Timeout.Infinite;
                            bool autoAccept = true;
                            // Variablen aus AAS Qualifiern
                            string Username = "";
                            string Password = "";
                            string URL = "";
                            int Namespace = 0;
                            string Path = "";

                            int j = 0;

                            while (j < count) // URL, Username, Password, Namespace, Path
                            {
                                var p = sm.Qualifiers[j] as Qualifier;

                                switch (p.Type)
                                {
                                    case "OPCURL": // URL
                                        URL = p.Value;
                                        break;
                                    case "OPCUsername": // Username
                                        Username = p.Value;
                                        break;
                                    case "OPCPassword": // Password
                                        Password = p.Value;
                                        break;
                                    case "OPCNamespace": // Namespace
                                                         // TODO: if not int, currently throws nondescriptive error
                                        if (int.TryParse(p.Value, out int tmpI))
                                            Namespace = tmpI;
                                        break;
                                    case "OPCPath": // Path
                                        Path = p.Value;
                                        break;
                                    case "OPCEnvVar": // Only if enviroment variable ist set
                                                      // VARIABLE=VALUE
                                        string[] split = p.Value.Split('=');
                                        if (split.Length == 2)
                                        {
                                            string value = "";
                                            if (envVariables.TryGetValue(split[0], out value))
                                            {
                                                if (split[1] != value)
                                                    URL = ""; // continue
                                            }
                                        }

                                        break;
                                }

                                j++;
                            }

                            if (URL == "")
                            {
                                continue;
                            }

                            if (URL == "" || Namespace == 0 || Path == "" || (Username == "" && Password != "") || (Username != "" && Password == ""))
                            {
                                Console.WriteLine("Incorrent or missing qualifier. Aborting ...");
                                return false;
                            }

                            if (Username == "" && Password == "")
                            {
                                Console.WriteLine("Using Anonymous to login ...");
                            }

                            // try to get the client from dictionary, else create and add it
                            SampleClient.UASampleClient client;
                            lock (Program.opcclientAddLock)
                            {
                                if (!OPCClients.TryGetValue(URL, out client))
                                {
                                    try
                                    {
                                        // make OPC UA client
                                        client = new SampleClient.UASampleClient(URL, autoAccept, stopTimeout, Username, Password);
                                        Console.WriteLine("Connecting to external OPC UA Server at {0} with {1} ...", URL, sm.IdShort);
                                        client.ConsoleSampleClient().Wait();
                                        // add it to the dictionary under this submodels idShort
                                        OPCClients.Add(URL, client);
                                    }
                                    catch (AggregateException ae)
                                    {
                                        bool cantconnect = false;
                                        ae.Handle((x) =>
                                        {
                                            if (x is ServiceResultException)
                                            {
                                                cantconnect = true;
                                                return true; // this exception handled
                                            }

                                            return false; // others not handled, will cause unhandled exception
                                        }
                                                 );
                                        if (cantconnect)
                                        {
                                            // stop processing OPC read because we couldnt connect
                                            // but return true as this shouldn't stop the main loop
                                            Console.WriteLine(ae.Message);
                                            Console.WriteLine("Could not connect to {0} with {1} ...", URL, sm.IdShort);
                                            return true;
                                        }
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Already connected to OPC UA Server at {0} with {1} ...", URL, sm.IdShort);
                                }
                            }

                            Console.WriteLine("==================================================");
                            Console.WriteLine("Read values for {0} from {1} ...", sm.IdShort, URL);
                            Console.WriteLine("==================================================");

                            // over all SMEs
                            count = sm.SubmodelElements.Count;
                            for (j = 0; j < count; j++)
                            {
                                var sme = sm.SubmodelElements[j];
                                // some preparations for multiple AAS below
                                int serverNamespaceIdx = 3; //could be gotten directly from the nodeMgr in OPCWrite instead, only pass the string part of the Id

                                string AASSubmodel
                                    = env[i].AasEnv.AssetAdministrationShells[0].IdShort + "." +
                                      sm.IdShort; // for multiple AAS, use something like env.AasEnv.AssetAdministrationShells[i].IdShort;
                                string serverNodePrefix = string.Format("ns={0};s=AASROOT.{1}", serverNamespaceIdx, AASSubmodel);
                                string nodePath = Path; // generally starts with Submodel idShort
                                WalkSubmodelElement(sme, nodePath, serverNodePrefix, client, Namespace, NewDataAvailable);
                            }
                        }
                    }
                }

                i++;
            }
        }

        if (!initial)
        {
            changeDataVersion();
        }

        return true;
    }

    public static void changeDataVersion() { dataVersion++; }
    public static ulong getDataVersion() { return (dataVersion); }

    public static void WalkSubmodelElement(ISubmodelElement sme, string nodePath, string serverNodePrefix, SampleClient.UASampleClient client, int clientNamespace, EventHandler NewDataAvailable)
    {
        if (sme is Property)
        {
            var p = sme as Property;
            var clientNodeName = nodePath + p.IdShort;
            var serverNodeId = $"{serverNodePrefix}.{p.IdShort}.Value";
            var clientNode = new NodeId(clientNodeName, (ushort)clientNamespace);
            UpdatePropertyFromOPCClient(p, serverNodeId, client, clientNode, NewDataAvailable);
        }
        else if (sme is SubmodelElementCollection)
        {
            var collection = sme as SubmodelElementCollection;
            foreach (var t in collection.Value)
            {
                var newNodeIdBase = $"{nodePath}.{collection.IdShort}";
                WalkSubmodelElement(t, newNodeIdBase, serverNodePrefix, client, clientNamespace, NewDataAvailable);
            }
        }
    }

    private static void UpdatePropertyFromOPCClient(Property p, string serverNodeId, SampleClient.UASampleClient client, NodeId clientNodeId, EventHandler NewDataAvailable)
    {
        string value = "";

        bool write = (p.FindQualifierOfType("OPCWRITE") != null);
        if (write)
            value = p.Value;

        try
        {
            // ns=#;i=#
            string[] split = (clientNodeId.ToString()).Split('#');
            if (split.Length == 2)
            {
                uint i = Convert.ToUInt16(split[1]);
                split = clientNodeId.ToString().Split('=');
                split = split[1].Split(';');
                ushort ns = Convert.ToUInt16(split[0]);
                clientNodeId = new NodeId(i, ns);
                Console.WriteLine("New node id: ", clientNodeId.ToString());
            }

            Console.WriteLine(string.Format("{0} <= {1}", serverNodeId, value));
            if (write)
            {
                short i = Convert.ToInt16(value);
                client.WriteSubmodelElementValue(clientNodeId, i);
            }
            else
                value = client.ReadSubmodelElementValue(clientNodeId);
        }
        catch (ServiceResultException ex)
        {
            Console.WriteLine(string.Format("OPC ServiceResultException ({0}) trying to read {1}", ex.Message, clientNodeId.ToString()));
            return;
        }

        // update in AAS env
        if (!write)
        {
            p.Value = value;
            //p.Set(p.ValueType, value);
            signalNewData(0);

            // update in OPC
            if (!OPCWrite(serverNodeId, value))
                Console.WriteLine("OPC write not successful.");
        }
    }

    private static Boolean OPCWrite(string nodeId, object value, bool runOPC = false)
    /// <summary>
    /// Writes to (i.e. updates values of) Nodes in the AAS OPC Server
    /// </summary>
    {
        if (!runOPC)
        {
            return true;
        }

        AasOpcUaServer.AasModeManager nodeMgr = AasOpcUaServer.AasEntityBuilder.nodeMgr;

        if (nodeMgr == null)
        {
            // if Server has not started yet, the AasNodeManager is null
            Console.WriteLine("OPC NodeManager not initialized.");
            return false;
        }

        // Find node in Core3OPC Server to update it
        BaseVariableState bvs = nodeMgr.Find(nodeId) as BaseVariableState;

        if (bvs == null)
        {
            Console.WriteLine("node {0} does not exist in server!", nodeId);
            return false;
        }

        var convertedValue = Convert.ChangeType(value, bvs.Value.GetType());
        if (!object.Equals(bvs.Value, convertedValue))
        {
            bvs.Value = convertedValue;
            // TODO: timestamp UtcNow okay or get this internally from the Server?
            bvs.Timestamp = DateTime.UtcNow;
            bvs.ClearChangeMasks(null, false);
        }

        return true;
    }

    public static bool ParseJson(SubmodelElementCollection c, object o, List<string> filter,
                                     Property minDiffAbsolute = null, Property minDiffPercent = null,
                                     AdminShellPackageEnv envaas = null)
    {
        var newMode = 0;
        var timeStamp = DateTime.UtcNow;
        var ok = false;

        var iMinDiffAbsolute = 1;
        var iMinDiffPercent = 0;
        if (minDiffAbsolute != null)
            iMinDiffAbsolute = Convert.ToInt32(minDiffAbsolute.Value);
        if (minDiffPercent != null)
            iMinDiffPercent = Convert.ToInt32(minDiffPercent.Value);

        switch (o)
        {
            case JsonDocument doc:
                ok |= ParseJson(c, doc.RootElement, filter, minDiffAbsolute, minDiffPercent, envaas);
                break;
            case JsonElement el:
                foreach (JsonProperty jp1 in el.EnumerateObject())
                {
                    if (filter != null && filter.Count != 0)
                    {
                        if (!filter.Contains(jp1.Name))
                            continue;
                    }

                    SubmodelElementCollection c2;
                    switch (jp1.Value.ValueKind)
                    {
                        case JsonValueKind.Array:
                            c2 = c.FindFirstIdShortAs<SubmodelElementCollection>(jp1.Name);
                            if (c2 == null)
                            {
                                c2 = new SubmodelElementCollection(idShort: jp1.Name);
                                c.Value.Add(c2);
                                c2.TimeStampCreate = timeStamp;
                                c2.SetTimeStamp(timeStamp);
                                newMode = 1;
                            }

                            var count = 1;
                            foreach (var subEl in jp1.Value.EnumerateArray())
                            {
                                var n = $"{jp1.Name}_array_{count++}";
                                var c3 =
                                    c2.FindFirstIdShortAs<SubmodelElementCollection>(n);
                                if (c3 == null)
                                {
                                    c3 = new SubmodelElementCollection(idShort: n);
                                    c2.Value.Add(c3);
                                    c3.TimeStampCreate = timeStamp;
                                    c3.SetTimeStamp(timeStamp);
                                    newMode = 1;
                                }

                                ok |= ParseJson(c3, subEl, filter, minDiffAbsolute, minDiffPercent, envaas);
                            }

                            break;
                        case JsonValueKind.Object:
                            c2 = c.FindFirstIdShortAs<SubmodelElementCollection>(jp1.Name);
                            if (c2 == null)
                            {
                                c2 = new SubmodelElementCollection(idShort: jp1.Name);
                                c.Value.Add(c2);
                                c2.TimeStampCreate = timeStamp;
                                c2.SetTimeStamp(timeStamp);
                                newMode = 1;
                            }

                            ok |= ParseJson(c2, jp1.Value, filter, minDiffAbsolute, minDiffPercent, envaas);
                            break;
                    }
                }

                break;
            default:
                throw new ArgumentException("Unsupported argument type for JSON parsing.");
        }

        envaas?.setWrite(true);
        Program.signalNewData(newMode);
        return ok;
    }

    private static System.Timers.Timer scriptTimer;

    private static void SetScriptTimer(double value)
    {
        // Create a timer with a two second interval.
        scriptTimer = new System.Timers.Timer(value);
        // Hook up the Elapsed event for the timer. 
        scriptTimer.Elapsed += OnScriptTimedEvent;
        scriptTimer.AutoReset = true;
        scriptTimer.Enabled = true;
    }

    private static void OnScriptTimedEvent(Object source, ElapsedEventArgs e)
    {
        RunScript(false);
        // NewDataAvailable?.Invoke(null, EventArgs.Empty);
    }

    private static System.Timers.Timer restTimer;

    private static void SetRestTimer(double value)
    {
        // Create a timer with a two second interval.
        restTimer = new System.Timers.Timer(value);
        // Hook up the Elapsed event for the timer. 
        restTimer.Elapsed += OnRestTimedEvent;
        restTimer.AutoReset = true;
        restTimer.Enabled = true;
    }

    static bool _resTalreadyRunning = false;
    static long countGetPut = 0;

    private static void OnRestTimedEvent(Object source, ElapsedEventArgs e)
    {
        _resTalreadyRunning = true;

        string GETSUBMODEL = "";
        string GETURL = "";
        string PUTSUBMODEL = "";
        string PUTURL = "";

        // Search for submodel REST and scan qualifiers for GET and PUT commands
        foreach (var sm in env[0].AasEnv.Submodels)
        {
            if (sm != null && sm.IdShort != null && sm.IdShort == "REST")
            {
                int count = sm.Qualifiers.Count;
                if (count != 0)
                {
                    int j = 0;

                    while (j < count) // Scan qualifiers
                    {
                        var p = sm.Qualifiers[j] as Qualifier;

                        if (p.Type == "GETSUBMODEL")
                        {
                            GETSUBMODEL = p.Value;
                        }

                        if (p.Type == "GETURL")
                        {
                            GETURL = p.Value;
                        }

                        if (p.Type == "PUTSUBMODEL")
                        {
                            PUTSUBMODEL = p.Value;
                        }

                        if (p.Type == "PUTURL")
                        {
                            PUTURL = p.Value;
                        }

                        j++;
                    }
                }
            }
        }

        if (GETSUBMODEL != "" && GETURL != "") // GET
        {
            Console.WriteLine("{0} GET Submodel {1} from URL {2}.", countGetPut++, GETSUBMODEL, GETURL);

            var sm = "";
            try
            {
                var client = new AasxRestServerLibrary.AasxRestClient(GETURL);
                sm = client.GetSubmodel(GETSUBMODEL);
            }
            catch (Exception)
            {
                Console.WriteLine("Can not connect to REST server {0}.", GETURL);
            }

            Submodel submodel = null;
            try
            {
                var options = new JsonSerializerOptions();
                options.Converters.Add(new AdminShellConverters.JsonAasxConverter("modelType", "name"));
                using var reader = new StringReader(sm);
                var jsonString = reader.ReadToEnd();
                submodel = System.Text.Json.JsonSerializer.Deserialize<Submodel>(jsonString, options);
            }
            catch (Exception)
            {
                Console.WriteLine("Can not read SubModel {0}.", GETSUBMODEL);
                return;
            }

            // need id for idempotent behaviour
            if (submodel.Id == null)
            {
                Console.WriteLine("Identification of SubModel {0} is (null).", GETSUBMODEL);
                return;
            }

            var aas = env[0].AasEnv.FindAasWithSubmodelId(submodel.Id);

            // datastructure update
            if (env == null || env[0].AasEnv == null /*|| env[0].AasEnv.Assets == null*/)
            {
                Console.WriteLine("Error accessing internal data structures.");
                return;
            }

            // add Submodel
            var existingSm = env[0].AasEnv.FindSubmodelById(submodel.Id);
            if (existingSm != null)
                env[0].AasEnv.Submodels.Remove(existingSm);
            env[0].AasEnv.Submodels.Add(submodel);

            // add SubmodelRef to AAS            
            // access the AAS
            var keyList = new List<IKey>() { new Key(KeyTypes.Submodel, submodel.Id) };
            var newsmr = new Reference(AasCore.Aas3_0.ReferenceTypes.ModelReference, keyList);
            var existsmr = aas.HasSubmodelReference(newsmr);
            if (!existsmr)
            {
                aas.AddSubmodelReference(newsmr);
            }
        }

        if (PUTSUBMODEL != "" && PUTURL != "") // PUT
        {
            Console.WriteLine("{0} PUT Submodel {1} from URL {2}.", countGetPut++, PUTSUBMODEL, PUTURL);

            {
                var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
                foreach (var sm in env[0].AasEnv.Submodels)
                {
                    if (sm != null && sm.IdShort != null && sm.IdShort == PUTSUBMODEL)
                    {
                        var json = System.Text.Json.JsonSerializer.Serialize(sm, jsonOptions);

                        try
                        {
                            var client = new AasxRestServerLibrary.AasxRestClient(PUTURL);
                            string result = client.PutSubmodel(json);
                        }
                        catch
                        {
                            Console.WriteLine("Can not connect to REST server {0}", PUTURL);
                        }
                    }
                }
            }
        }

        _resTalreadyRunning = false;

        // start MQTT Client as a worker (will start in the background)
        var worker = new BackgroundWorker();
        worker.DoWork += async (s1, e1) =>
        {
            try
            {
                await AasxMqttClient.MqttClient.StartAsync(env);
            }
            catch (Exception)
            {
            }
        };
        worker.RunWorkerCompleted += (s1, e1) =>
        {
        };
        worker.RunWorkerAsync();
    }

    static void RunScript(bool init)
    {
        if (env == null)
            return;

        // if (countRunScript++ > 1)
        //    return;

        lock (Program.changeAasxFile)
        {
            int i = 0;
            while (i < env.Length && env[i] != null)
            {
                if (env[i].AasEnv.Submodels != null)
                {
                    foreach (var sm in env[i].AasEnv.Submodels)
                    {
                        if (sm != null && sm.IdShort != null)
                        {
                            int count = sm.Qualifiers != null ? sm.Qualifiers.Count : 0;
                            if (count != 0)
                            {
                                var q = sm.Qualifiers[0] as Qualifier;
                                if (q.Type == "SCRIPT")
                                {
                                    // Triple
                                    // Reference to property with Number
                                    // Reference to submodel with numbers/strings
                                    // Reference to property to store found text
                                    count = sm.SubmodelElements.Count;
                                    int smi = 0;
                                    while (smi < count)
                                    {
                                        var sme1 = sm.SubmodelElements[smi++];
                                        if (sme1.Qualifiers == null || sme1.Qualifiers.Count == 0)
                                        {
                                            continue;
                                        }

                                        var qq = sme1.Qualifiers[0] as Qualifier;

                                        if (qq.Type == "Add")
                                        {
                                            int v = Convert.ToInt32((sme1 as Property).Value);
                                            v += Convert.ToInt32(qq.Value);
                                            (sme1 as Property).Value = v.ToString();
                                            continue;
                                        }

                                        if (qq.Type == "GetValue")
                                        {
                                        }

                                        if (qq.Type == "GetJSON")
                                        {
                                            if (init)
                                                return;

                                            if (Program.isLoading)
                                                return;

                                            if (!(sme1 is ReferenceElement))
                                            {
                                                continue;
                                            }

                                            string url = qq.Value;
                                            string username = "";
                                            string password = "";

                                            if (sme1.Qualifiers.Count == 3)
                                            {
                                                qq = sme1.Qualifiers[1] as Qualifier;
                                                if (qq.Type != "Username")
                                                    continue;
                                                username = qq.Value;
                                                qq = sme1.Qualifiers[2] as Qualifier;
                                                if (qq.Type != "Password")
                                                    continue;
                                                password = qq.Value;
                                            }

                                            var handler = new HttpClientHandler();
                                            handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
                                            var client = new HttpClient(handler);

                                            if (username != "" && password != "")
                                            {
                                                var authToken = System.Text.Encoding.ASCII.GetBytes(username + ":" + password);
                                                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                                                 Convert.ToBase64String(authToken));
                                            }

                                            Console.WriteLine("GetJSON: " + url);
                                            string response = client.GetStringAsync(url).Result;
                                            Console.WriteLine(response);

                                            if (response != "")
                                            {
                                                var r12 = sme1 as ReferenceElement;
                                                var ref12 = env[i].AasEnv.FindReferableByReference(r12.GetModelReference());
                                                if (ref12 is SubmodelElementCollection)
                                                {
                                                    var c1 = ref12 as SubmodelElementCollection;
                                                    var parsed = JsonDocument.Parse(response);
                                                    ParseJson(c1, parsed, null);
                                                }
                                            }

                                            continue;
                                        }

                                        if (qq.Type != "SearchNumber" || smi >= count)
                                        {
                                            continue;
                                        }

                                        var sme2 = sm.SubmodelElements[smi++];
                                        if (sme2.Qualifiers.Count == 0)
                                        {
                                            continue;
                                        }

                                        qq = sme2.Qualifiers[0] as Qualifier;
                                        if (qq.Type != "SearchList" || smi >= count)
                                        {
                                            continue;
                                        }

                                        var sme3 = sm.SubmodelElements[smi++];
                                        if (sme3.Qualifiers.Count == 0)
                                        {
                                            continue;
                                        }

                                        qq = sme3.Qualifiers[0] as Qualifier;
                                        if (qq.Type != "SearchResult")
                                        {
                                            break;
                                        }

                                        if (sme1 is ReferenceElement &&
                                            sme2 is ReferenceElement &&
                                            sme3 is ReferenceElement)
                                        {
                                            var r1 = sme1 as ReferenceElement;
                                            var r2 = sme2 as ReferenceElement;
                                            var r3 = sme3 as ReferenceElement;
                                            var ref1 = env[i].AasEnv.FindReferableByReference(r1.GetModelReference());
                                            var ref2 = env[i].AasEnv.FindReferableByReference(r2.GetModelReference());
                                            var ref3 = env[i].AasEnv.FindReferableByReference(r3.GetModelReference());
                                            if (ref1 is Property && ref2 is Submodel && ref3 is Property)
                                            {
                                                var p1 = ref1 as Property;
                                                // Simulate changes
                                                var sm2 = ref2 as Submodel;
                                                var p3 = ref3 as Property;
                                                int count2 = sm2.SubmodelElements.Count;
                                                for (int j = 0; j < count2; j++)
                                                {
                                                    var sme = sm2.SubmodelElements[j];
                                                    if (sme.IdShort == p1.Value)
                                                    {
                                                        p3.Value = (sme as Property).Value;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                i++;
            }
        }

        return;
    }
}

