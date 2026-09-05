using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    const string DefaultHost = "192.168.88.1";
    const int DefaultPort1 = 54321;
    const int DefaultPort2 = 12345;
    const int DefaultDelaySeconds = 3;
    const int ConnectTimeoutMs = 200;

    class Options
    {
        public string Host { get; set; } = DefaultHost;
        public int Port1 { get; set; } = DefaultPort1;
        public int Port2 { get; set; } = DefaultPort2;
        public int DelaySeconds { get; set; } = DefaultDelaySeconds;
    }

    enum ParseResult
    {
        Success,
        Help,
        Error
    }

    static async Task<int> Main(string[] args)
    {
        var options = new Options();
        var result = ParseArguments(args, options);

        if (result == ParseResult.Help)
        {
            ShowHelp();
            return 0;
        }

        if (result == ParseResult.Error)
        {
            return 1;
        }

        IPAddress? resolvedAddress = await TryResolveHostAsync(options.Host);

        if (resolvedAddress == null)
        {
            return 1;
        }

        Console.WriteLine("MikroTik Access Trigger");
        Console.WriteLine($"Host:    {options.Host}");
        Console.WriteLine($"Address: {resolvedAddress}");
        Console.WriteLine($"Port 1:  {options.Port1}");
        Console.WriteLine($"Port 2:  {options.Port2}");
        Console.WriteLine($"Delay:   {options.DelaySeconds} second(s)");
        Console.WriteLine();

        Console.WriteLine($"Triggering port {options.Port1}...");
        await TriggerPortAsync(resolvedAddress, options.Port1);

        Console.WriteLine($"Waiting {options.DelaySeconds} second(s)...");
        await Task.Delay(options.DelaySeconds * 1000);

        Console.WriteLine($"Triggering port {options.Port2}...");
        await TriggerPortAsync(resolvedAddress, options.Port2);

        Console.WriteLine();
        Console.WriteLine("Done.");

        return 0;
    }

    static async Task TriggerPortAsync(IPAddress address, int port)
    {
        using var socket = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Stream,
            ProtocolType.Tcp);

        using var cts = new CancellationTokenSource(ConnectTimeoutMs);

        try
        {
            // Initiate a TCP connection attempt.
            // The connection is cancelled after a short timeout.
            // The initial SYN is enough to trigger the MikroTik rule.
            await socket.ConnectAsync(
                new IPEndPoint(address, port),
                cts.Token);
        }
        catch
        {
            // The connection is expected to fail.
            // The initial SYN is what triggers the MikroTik rule.
        }
    }

    static async Task<IPAddress?> TryResolveHostAsync(string host)
    {
        if (IPAddress.TryParse(host, out IPAddress? parsedAddress))
        {
            if (parsedAddress.AddressFamily != AddressFamily.InterNetwork)
            {
                Console.WriteLine(
                    "Error: only IPv4 addresses are supported.");

                return null;
            }

            return parsedAddress;
        }

        try
        {
            IPAddress[] addresses =
                await Dns.GetHostAddressesAsync(host);

            IPAddress? address = addresses.FirstOrDefault(
                a => a.AddressFamily == AddressFamily.InterNetwork);

            if (address == null)
            {
                Console.WriteLine(
                    $"Error: could not resolve an IPv4 address for '{host}'.");
            }

            return address;
        }
        catch (SocketException)
        {
            Console.WriteLine(
                $"Error: could not resolve host '{host}'.");

            return null;
        }
    }

    static ParseResult ParseArguments(string[] args, Options options)
    {
        for (int i = 0; i < args.Length; i++)
        {
            string argument = args[i].ToLowerInvariant();

            switch (argument)
            {
                case "--help":
                case "-?":
                    return ParseResult.Help;

                case "--host":
                    if (!TryGetStringValue(
                        args,
                        ref i,
                        "--host",
                        out string hostValue))
                    {
                        return ParseResult.Error;
                    }

                    options.Host = hostValue;
                    break;

                case "--port1":
                    if (!TryGetIntValue(
                        args,
                        ref i,
                        "--port1",
                        out int port1Value))
                    {
                        return ParseResult.Error;
                    }

                    if (!ValidatePort(port1Value, "--port1"))
                    {
                        return ParseResult.Error;
                    }

                    options.Port1 = port1Value;
                    break;

                case "--port2":
                    if (!TryGetIntValue(
                        args,
                        ref i,
                        "--port2",
                        out int port2Value))
                    {
                        return ParseResult.Error;
                    }

                    if (!ValidatePort(port2Value, "--port2"))
                    {
                        return ParseResult.Error;
                    }

                    options.Port2 = port2Value;
                    break;

                case "--delay":
                    if (!TryGetIntValue(
                        args,
                        ref i,
                        "--delay",
                        out int delayValue))
                    {
                        return ParseResult.Error;
                    }

                    if (delayValue < 0)
                    {
                        Console.WriteLine(
                            "Error: --delay cannot be negative.");

                        return ParseResult.Error;
                    }

                    options.DelaySeconds = delayValue;
                    break;

                default:
                    Console.WriteLine(
                        $"Error: unknown option '{args[i]}'.");

                    Console.WriteLine(
                        "Use --help to see available options.");

                    return ParseResult.Error;
            }
        }

        return ParseResult.Success;
    }

    static bool ValidatePort(int port, string optionName)
    {
        if (port < 1 || port > 65535)
        {
            Console.WriteLine(
                $"Error: {optionName} must be between 1 and 65535.");

            return false;
        }

        return true;
    }

    static bool TryGetStringValue(
        string[] args,
        ref int index,
        string option,
        out string value)
    {
        value = string.Empty;

        if (index + 1 >= args.Length)
        {
            Console.WriteLine(
                $"Error: missing value for '{option}'.");

            return false;
        }

        string argumentValue = args[++index];

        if (string.IsNullOrWhiteSpace(argumentValue) ||
            argumentValue.StartsWith("--", StringComparison.Ordinal))
        {
            Console.WriteLine(
                $"Error: missing or invalid value for '{option}'.");

            return false;
        }

        value = argumentValue;

        return true;
    }

    static bool TryGetIntValue(
        string[] args,
        ref int index,
        string option,
        out int value)
    {
        value = 0;

        if (index + 1 >= args.Length)
        {
            Console.WriteLine(
                $"Error: missing value for '{option}'.");

            return false;
        }

        string argumentValue = args[++index];

        if (!int.TryParse(argumentValue, out value))
        {
            Console.WriteLine(
                $"Error: invalid integer value for '{option}': '{argumentValue}'.");

            return false;
        }

        return true;
    }

    static void ShowHelp()
    {
        Console.WriteLine("""
MikroTik Access Trigger

Sends two TCP connection attempts to trigger MikroTik access rules.

Author: fsdevcom2000
GitHub: https://github.com/fsdevcom2000/mikrotik-access-trigger


Usage:
  mikrotik-access.exe [options]

Options:
  --host <address>      MikroTik IP address or hostname
                        Default: 192.168.88.1

  --port1 <port>        First trigger port
                        Default: 54321

  --port2 <port>        Second trigger port
                        Default: 12345

  --delay <seconds>     Delay between triggers
                        Default: 3

  --help                Show this help message

Examples:
  mikrotik-access.exe

  mikrotik-access.exe --host 192.168.88.1

  mikrotik-access.exe --host router.example.com

  mikrotik-access.exe --host router.example.com --delay 5

  mikrotik-access.exe --host router.example.com --port1 54321 --port2 12345 --delay 5
""");
    }
}