using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net;
using System.Text.RegularExpressions;

namespace WakeOnLAN
{
    /// <summary>
    /// Wake-on-LAN implementation class. 
    /// WOL class used source code posted at the following URLs:
    /// https://stackoverflow.com/questions/861873/wake-on-lan-using-c-sharp
    /// https://stackoverflow.com/users/41745/sameet
    /// https://stackoverflow.com/users/5741643/poul-bak
    /// Thanks!😃
    /// </summary>
    public static class WOL
    {
        /// <summary>
        /// Exit code for WOL class.
        /// </summary>
        private static int _ExitCode = 0;

        /// <summary>
        /// Wake-on-LAN magic packet port number.
        /// </summary>
        public static int PortNo { get; set; } = 9;

        /// <summary>
        /// Exception error message.
        /// </summary>
        private static string _ExitCodeMessage = "";

        /// <summary>
        /// Exit code get property.
        /// </summary>
        public static int ExitCode
        {
            get { return _ExitCode; }
        }

        /// <summary>
        /// Exit code message get property.
        /// </summary>
        public static string ExitCodeMessage
        {
            get { return _ExitCodeMessage; }
        }

        /// <summary>
        /// Do Wake-on-LAN.
        /// </summary>
        /// <param name="macAddress">MAC address</param>
        /// <returns>result code. 0 is success.</returns>
        public static async Task<int> WakeOnLan(string macAddress)
        {
            // Build WOL magic packet.
            byte[] magicPacket;
            try
            {
                magicPacket = BuildMagicPacket(macAddress);
            }
            catch (Exception ex)
            {
                _ExitCode = 1;
                _ExitCodeMessage = ex.Message;
                return ExitCode;
            }

            // Send magic packets by UDP.
            try
            {
                if (!NetworkInterface.GetAllNetworkInterfaces().Where((n) =>
                    n.NetworkInterfaceType != NetworkInterfaceType.Loopback && n.OperationalStatus == OperationalStatus.Up).Any())
                {
                    _ExitCode = 2;
                    _ExitCodeMessage = "No valid network adapter found.";
                    return ExitCode;
                }

                foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces().Where((n) =>
                    n.NetworkInterfaceType != NetworkInterfaceType.Loopback && n.OperationalStatus == OperationalStatus.Up))
                {
                    IPInterfaceProperties iPInterfaceProperties = networkInterface.GetIPProperties();
                    foreach (MulticastIPAddressInformation multicastIPAddressInformation in iPInterfaceProperties.MulticastAddresses)
                    {
                        IPAddress multicastIpAddress = multicastIPAddressInformation.Address;
                        if (multicastIpAddress.ToString().StartsWith("ff02::1%", StringComparison.OrdinalIgnoreCase)) // Ipv6: All hosts on LAN (with zone index)
                        {
                            UnicastIPAddressInformation? unicastIPAddressInformation = iPInterfaceProperties.UnicastAddresses.Where((u) =>
                                u.Address.AddressFamily == AddressFamily.InterNetworkV6 && !u.Address.IsIPv6LinkLocal).FirstOrDefault();
                            if (unicastIPAddressInformation != null)
                            {
                                await SendWakeOnLan(unicastIPAddressInformation.Address, multicastIpAddress, magicPacket);
                            }
                        }
                        else if (multicastIpAddress.ToString().Equals("224.0.0.1")) // Ipv4: All hosts on LAN
                        {
                            UnicastIPAddressInformation? unicastIPAddressInformation = iPInterfaceProperties.UnicastAddresses.Where((u) =>
                                u.Address.AddressFamily == AddressFamily.InterNetwork && !iPInterfaceProperties.GetIPv4Properties().IsAutomaticPrivateAddressingActive).FirstOrDefault();
                            if (unicastIPAddressInformation != null)
                            {
                                await SendWakeOnLan(unicastIPAddressInformation.Address, multicastIpAddress, magicPacket);
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                _ExitCode = 3;
                _ExitCodeMessage = ex.Message;
                return ExitCode;
            }
            return 0;
        }

        /// <summary>
        /// Building Wake On Lan Magic Packet.
        /// </summary>
        /// <param name="macAddress">MAC address</param>
        /// <returns>Generated Magic Packet</returns>
        static byte[] BuildMagicPacket(string macAddress) // MacAddress in any standard HEX format
        {
            macAddress = Regex.Replace(macAddress, "[: -]", "");
            byte[] macBytes = Convert.FromHexString(macAddress);

            IEnumerable<byte> header = Enumerable.Repeat((byte)0xff, 6); //First 6 times 0xff
            IEnumerable<byte> data = Enumerable.Repeat(macBytes, 16).SelectMany(m => m); // then 16 times MacAddress
            return header.Concat(data).ToArray();
        }

        /// <summary>
        /// Send UDP Wake On Lan Magic Packet.
        /// </summary>
        /// <param name="localIpAddress"></param>
        /// <param name="multicastIpAddress"></param>
        /// <param name="magicPacket"></param>
        /// <returns></returns>
        static async Task SendWakeOnLan(IPAddress localIpAddress, IPAddress multicastIpAddress, byte[] magicPacket)
        {
            using UdpClient client = new(new IPEndPoint(localIpAddress, 0));
            await client.SendAsync(magicPacket, magicPacket.Length, new IPEndPoint(multicastIpAddress, PortNo));
        }
    }
}
