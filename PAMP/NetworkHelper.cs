using System.Diagnostics;
using System.Text.RegularExpressions;

namespace PAMP;

public static class NetworkHelper
{
    public static async Task<int> GetPortByPidAsync(int pid, CancellationToken cancellationToken = default)
    {
        if (pid <= 0) return 0;

        try
        {
            using var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "netstat",
                    Arguments = "-ano",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            p.Start();
            string output = await p.StandardOutput.ReadToEndAsync(cancellationToken);
            await p.WaitForExitAsync(cancellationToken);

            string pidString = pid.ToString();
            string[] lines = output.Split(["\r\n", "\r", "\n"], StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                ReadOnlySpan<char> trimmed = line.AsSpan().Trim();
                if (!trimmed.EndsWith(pidString.AsSpan()) || !line.Contains("LISTENING"))
                    continue;

                string[] parts = Regex.Split(line.Trim(), @"\s+");
                if (parts.Length >= 2)
                {
                    string localAddress = parts[1];
                    int lastColonIndex = localAddress.LastIndexOf(':');
                    if (lastColonIndex > 0 && int.TryParse(localAddress.AsSpan(lastColonIndex + 1), out int port))
                    {
                        return port;
                    }
                }
            }
        }
        catch { }

        return 0;
    }

    public static int GetPortByPid(int pid)
    {
        // Synchroniczny fallback w razie potrzeby
        return GetPortByPidAsync(pid).GetAwaiter().GetResult();
    }
}