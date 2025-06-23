using System.Linq;
using System.Net.NetworkInformation;
using System.Web;

public static class DeviceInfoHelper
{
    public static string GetIPAddress()
    {
        return HttpContext.Current?.Request?.UserHostAddress ?? "Unknown";
    }

    public static string GetMacAddress()
    {
        try
        {
            return NetworkInterface
                .GetAllNetworkInterfaces()
                .Where(nic => nic.OperationalStatus == OperationalStatus.Up &&
                              nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Select(nic => nic.GetPhysicalAddress().ToString())
                .FirstOrDefault() ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }
}