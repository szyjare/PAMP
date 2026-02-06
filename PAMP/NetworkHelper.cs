using System.Diagnostics;
using System.Text.RegularExpressions;

namespace PAMP
{
    public static class NetworkHelper
    {
        public static int GetPortByPid(int pid)
        {
            try
            {
                using (Process p = new Process())
                {
                    p.StartInfo = new ProcessStartInfo
                    {
                        FileName = "netstat",
                        Arguments = "-ano",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    p.Start();
                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();

                    // Parsuje wynik
                    string[] lines = output.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                    foreach (string line in lines)
                    {
                        // Szuka linii, która kończy się PID
                        // Format: TCP  0.0.0.0:80  0.0.0.0:0  LISTENING  1234
                        if (!line.Trim().EndsWith(pid.ToString())) continue;

                        if (!line.Contains("LISTENING")) continue;
                        var parts = Regex.Split(line.Trim(), @"\s+");

                        if (parts.Length >= 2)
                        {
                            string localAddress = parts[1];
                            int lastColonIndex = localAddress.LastIndexOf(':');

                            if (lastColonIndex > 0)
                            {
                                string portStr = localAddress.Substring(lastColonIndex + 1);
                                if (int.TryParse(portStr, out int port))
                                {
                                    return port;
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            return 0;
        }
    }
}