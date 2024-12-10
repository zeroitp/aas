namespace AasxServerStandardBib.Models.Program;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AasOpcUaServer;
using AdminShellNS;
using Opc.Ua.Configuration;
using Opc.Ua.Server;
using Opc.Ua;

public class MySampleServer
{
    SampleServer server;
    Task status;
    DateTime lastEventTime;
    int serverRunTime = Timeout.Infinite;
    static bool autoAccept = false;
    static ExitCode exitCode;

    static AdminShellPackageEnv[] aasxEnv = null;

    // OZ
    public static ManualResetEvent quitEvent;

    public MySampleServer(bool _autoAccept, int _stopTimeout, AdminShellPackageEnv[] _aasxEnv)
    {
        autoAccept = _autoAccept;
        aasxEnv = _aasxEnv;
        serverRunTime = _stopTimeout == 0 ? Timeout.Infinite : _stopTimeout * 1000;
    }

    public void Run()
    {
        try
        {
            exitCode = ExitCode.ErrorServerNotStarted;
            ConsoleSampleServer().Wait();
            Console.WriteLine("Servers succesfully started. Press Ctrl-C to exit...");
            exitCode = ExitCode.ErrorServerRunning;
        }
        catch (Exception ex)
        {
            Utils.Trace("ServiceResultException:" + ex.Message);
            Console.WriteLine("Exception: {0}", ex.Message);
            exitCode = ExitCode.ErrorServerException;
            return;
        }

        quitEvent = new ManualResetEvent(false);
        try
        {
            Console.CancelKeyPress += (sender, eArgs) =>
            {
                quitEvent.Set();
                eArgs.Cancel = true;
            };
        }
        catch
        {
        }

        // wait for timeout or Ctrl-C
        quitEvent.WaitOne(serverRunTime);

        if (server != null)
        {
            Console.WriteLine("Server stopped. Waiting for exit...");

            using (SampleServer _server = server)
            {
                // Stop status thread
                server = null;
                status.Wait();
                // Stop server and dispose
                _server.Stop();
            }
        }

        exitCode = ExitCode.Ok;
    }

    public static ExitCode ExitCode { get => exitCode; }

    private static void CertificateValidator_CertificateValidation(CertificateValidator validator, CertificateValidationEventArgs e)
    {
        if (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted)
        {
            e.Accept = autoAccept;
            if (autoAccept)
            {
            }
            else
            {
            }
        }
    }

    private async Task ConsoleSampleServer()
    {
        ApplicationInstance.MessageDlg = new ApplicationMessageDlg();
        ApplicationInstance application = new ApplicationInstance();

        application.ApplicationName = "UA Core Sample Server";
        application.ApplicationType = ApplicationType.Server;
        application.ConfigSectionName = Utils.IsRunningOnMono() ? "Opc.Ua.MonoSampleServer" : "Opc.Ua.SampleServer";

        // load the application configuration.
        ApplicationConfiguration config = await application.LoadApplicationConfiguration(true);

        // check the application certificate.
        bool haveAppCertificate = await application.CheckApplicationInstanceCertificate(true, 0);

        if (!haveAppCertificate)
        {
            throw new Exception("Application instance certificate invalid!");
        }

        if (!config.SecurityConfiguration.AutoAcceptUntrustedCertificates)
        {
            config.CertificateValidator.CertificateValidation += new CertificateValidationEventHandler(CertificateValidator_CertificateValidation);
        }

        // start the server.
        server = new SampleServer(aasxEnv);
        await application.Start(server);

        // start the status thread
        status = Task.Run(new Action(StatusThread));

        // print notification on session events
        server.CurrentInstance.SessionManager.SessionActivated += EventStatus;
        server.CurrentInstance.SessionManager.SessionClosing += EventStatus;
        server.CurrentInstance.SessionManager.SessionCreated += EventStatus;
    }

    private void EventStatus(Opc.Ua.Server.Session session, SessionEventReason reason)
    {
        lastEventTime = DateTime.UtcNow;
        PrintSessionStatus(session, reason.ToString());
    }

    void PrintSessionStatus(Opc.Ua.Server.Session session, string reason, bool lastContact = false)
    {
        lock (session.DiagnosticsLock)
        {
            string item = String.Format("{0,9}:{1,20}:", reason, session.SessionDiagnostics.SessionName);
            if (lastContact)
            {
                item += String.Format("Last Event:{0:HH:mm:ss}", session.SessionDiagnostics.ClientLastContactTime.ToLocalTime());
            }
            else
            {
                if (session.Identity != null)
                {
                    item += String.Format(":{0,20}", session.Identity.DisplayName);
                }

                item += String.Format(":{0}", session.Id);
            }
        }
    }

    private async void StatusThread()
    {
        while (server != null)
        {
            if (DateTime.UtcNow - lastEventTime > TimeSpan.FromMilliseconds(6000))
            {
                IList<Opc.Ua.Server.Session> sessions = server.CurrentInstance.SessionManager.GetSessions();
                for (int ii = 0; ii < sessions.Count; ii++)
                {
                    Opc.Ua.Server.Session session = sessions[ii];
                    PrintSessionStatus(session, "-Status-", true);
                }

                lastEventTime = DateTime.UtcNow;
            }

            await Task.Delay(1000);
        }
    }
}