using System.Management;
using System.ServiceProcess;
using Mcpw.Types;
using SC = System.ServiceProcess.ServiceController;

namespace Mcpw.Windows;

/// <summary>
/// Service Control Manager access via System.ServiceProcess.ServiceController.
/// Start-type changes use WMI because SCM API has no direct setter.
/// </summary>
public sealed class ServiceControl : IServiceControl
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    public IEnumerable<ServiceInfo> GetServices() =>
        SC.GetServices().Select(Map).ToList();

    public ServiceInfo GetService(string name)
    {
        using var sc = new SC(name);
        return Map(sc);
    }

    public void Start(string name)
    {
        using var sc = new SC(name);
        if (sc.Status == ServiceControllerStatus.Running) return;
        sc.Start();
        sc.WaitForStatus(ServiceControllerStatus.Running, DefaultTimeout);
    }

    public void Stop(string name)
    {
        using var sc = new SC(name);
        if (sc.Status == ServiceControllerStatus.Stopped) return;
        sc.Stop();
        sc.WaitForStatus(ServiceControllerStatus.Stopped, DefaultTimeout);
    }

    public void Restart(string name)
    {
        Stop(name);
        Start(name);
    }

    public void SetStartType(string name, string startType)
    {
        // WMI is the only clean way to change startup type without P/Invoke
        var startMode = startType.ToLowerInvariant() switch
        {
            "auto"     => "Automatic",
            "manual"   => "Manual",
            "disabled" => "Disabled",
            _          => throw new ArgumentException($"Unknown start type: '{startType}'"),
        };

        using var searcher = new ManagementObjectSearcher(
            $"SELECT * FROM Win32_Service WHERE Name='{EscapeWql(name)}'");
        foreach (ManagementObject obj in searcher.Get())
            obj.InvokeMethod("ChangeStartMode", new object[] { startMode });
    }

    // ── helpers ───────────────────────────────────────────────────────────

    private static ServiceInfo Map(SC sc)
    {
        sc.Refresh();
        return new ServiceInfo
        {
            Name        = sc.ServiceName,
            DisplayName = sc.DisplayName,
            Status      = sc.Status.ToString(),
            StartType   = sc.StartType.ToString(),
        };
    }

    private static string EscapeWql(string s) => s.Replace("'", "\\'");
}
