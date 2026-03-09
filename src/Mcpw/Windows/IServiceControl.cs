using Mcpw.Types;

namespace Mcpw.Windows;

/// <summary>
/// Abstraction over Windows Service Control Manager.
/// Implementations use System.ServiceProcess.ServiceController.
/// </summary>
public interface IServiceControl
{
    IEnumerable<ServiceInfo> GetServices();
    ServiceInfo GetService(string name);
    void Start(string name);
    void Stop(string name);
    void Restart(string name);
    void SetStartType(string name, string startType);
}
