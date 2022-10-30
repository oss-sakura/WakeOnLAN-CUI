using WakeOnLAN;

internal class Program
{
    /// <summary>
    /// WakeOnLAN Main
    /// </summary>
    /// <param name="args">parameters</param>
    /// <returns>last exit code</returns>
    static int Main(string[] args)
    {
        if (!(args.Length == 2 || args.Length == 1))
        {
            Usage();
            return -1;
        }

        Task<int>? task = null;

        if (args.Length == 1)
        {
            task = WakeOnLAN(args[0]);
        }
        else
        {
            // args.Length == 2
            if (int.TryParse(args[1], out int portNo))
            {
                task = WakeOnLAN(args[0], portNo);
            }
            else
            {
                Console.Error.WriteLine("Invalid port number.");
            }
        }

        return task == null ? -1 : task.Result;
    }

    /// <summary>
    /// WOL.WakeOnLan method call.
    /// </summary>
    /// <param name="macAddress"></param>
    /// <returns>exit code</returns>
    /// 

    private static async Task<int> WakeOnLAN(string macAddress)
    {
        int result = await WOL.WakeOnLan(macAddress);
        if (result != 0)
        {
            Console.Error.WriteLine(WOL.ExitCodeMessage);
        }
        return WOL.ExitCode;
    }

    private static async Task<int> WakeOnLAN(string macAddress, int portNo)
    {
        WOL.PortNo = portNo;
        int result = await WOL.WakeOnLan(macAddress);
        if (result != 0)
        {
            Console.Error.WriteLine(WOL.ExitCodeMessage);
        }
        return WOL.ExitCode;
    }

    /// <summary>
    /// Output usage.
    /// </summary>
    private static void Usage()
    {
        Console.WriteLine("Usage: {0} mac-address [port]", System.AppDomain.CurrentDomain.FriendlyName);
        Console.WriteLine("\te.g. {0} 12:34:56:AB:CD:EF", System.AppDomain.CurrentDomain.FriendlyName);
        Console.WriteLine("\te.g. {0} 12-34-56-AB-CD-EF", System.AppDomain.CurrentDomain.FriendlyName);
        Console.WriteLine("\te.g. {0} 12:34:56:AB:CD:EF 7", System.AppDomain.CurrentDomain.FriendlyName);
        Console.WriteLine("\n\tdefault port is 9.");
    }
}
