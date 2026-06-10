using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

using CrestronHomeDriverFeedInstaller.Core.Abstractions;
using CrestronHomeDriverFeedInstaller.Core.Models;

namespace CrestronHomeDriverFeedInstaller.Core.Services;

public sealed class ProcessorDiscoveryService : IProcessorDiscoveryService
	{
	private const int CIP_PORT = 41794;
	private const int TOOLBOX_PACKET_SIZE = 266;
	private const int SOCKET_RECEIVE_TIMEOUT_MS = 200;

	public async Task<ProcessorDiscoveryResult> DiscoverAsync (TimeSpan timeout, CancellationToken cancellationToken = default)
		{
		var diagnostics = new List<string>
			{
			$"Discovery timeout: {timeout.TotalMilliseconds:N0} ms"
			};
		var results = new List<DiscoveredProcessorInfo> ();
		var seen = new HashSet<string> (StringComparer.OrdinalIgnoreCase);
		var probe = BuildProbe ();
		var sockets = new List<DiscoverySocket> ();

		try
			{
			foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces ())
				{
				if (networkInterface.OperationalStatus != OperationalStatus.Up)
					{
					continue;
					}

				if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
					{
					continue;
					}

				foreach (var unicastAddress in networkInterface.GetIPProperties ().UnicastAddresses)
					{
					if (unicastAddress.Address.AddressFamily != AddressFamily.InterNetwork)
						{
						continue;
						}

					if (unicastAddress.IPv4Mask is null)
						{
						continue;
						}

					var broadcastAddress = GetBroadcastAddress (unicastAddress.Address, unicastAddress.IPv4Mask);
					if (broadcastAddress is null)
						{
						continue;
						}

					try
						{
						var socket = new Socket (AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
						socket.SetSocketOption (SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
						socket.SetSocketOption (SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
						socket.ReceiveTimeout = SOCKET_RECEIVE_TIMEOUT_MS;
						socket.Bind (new IPEndPoint (unicastAddress.Address, CIP_PORT));

						var destination = new IPEndPoint (broadcastAddress, CIP_PORT);
						sockets.Add (new DiscoverySocket (socket, destination));
						diagnostics.Add ($"Interface {unicastAddress.Address}:{((IPEndPoint)socket.LocalEndPoint!).Port} -> broadcast {destination.Address}:{destination.Port}");
						}
					catch (Exception ex)
						{
						diagnostics.Add ($"Socket error on {unicastAddress.Address}: {ex.Message}");
						}
					}
				}

			if (sockets.Count == 0)
				{
				diagnostics.Add ("No active IPv4 interfaces were available for discovery.");
				return new ProcessorDiscoveryResult
					{
					Processors = Array.Empty<DiscoveredProcessorInfo> (),
					Diagnostics = diagnostics.ToArray ()
					};
				}

			diagnostics.Add ($"Probe size: {probe.Length} bytes");

			foreach (var (socket, destination) in sockets)
				{
				cancellationToken.ThrowIfCancellationRequested ();
				socket.SendTo (probe, destination);
				diagnostics.Add ($"Sent {probe.Length}-byte probe to {destination}");
				}

			var deadline = DateTime.UtcNow.Add (timeout);
			var buffer = new byte[512];

			while (DateTime.UtcNow < deadline)
				{
				cancellationToken.ThrowIfCancellationRequested ();

				foreach (var (socket, _) in sockets)
					{
					try
						{
						EndPoint remoteEndPoint = new IPEndPoint (IPAddress.Any, 0);
						var receivedLength = socket.ReceiveFrom (buffer, ref remoteEndPoint);
						var senderAddress = ((IPEndPoint)remoteEndPoint).Address;

						if (buffer[0] == 0x14)
							{
							diagnostics.Add ($"Ignored echo probe from {senderAddress}");
							continue;
							}

						var processor = ParseResponse (buffer, receivedLength, senderAddress);
						if (processor is null)
							{
							diagnostics.Add ($"Ignored response from {senderAddress}: len={receivedLength}, first=0x{buffer[0]:X2}, preview={ToHexPreview (buffer, receivedLength, 24)}, ascii={ToAsciiPreview (buffer, receivedLength, 24)}");
							continue;
							}

						if (!seen.Add (senderAddress.ToString ()))
							{
							diagnostics.Add ($"Ignored duplicate valid response from {senderAddress}");
							continue;
							}

						results.Add (processor);
						diagnostics.Add ($"Found: {processor}");
						}
					catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
						{
						}
					}

				await Task.Delay (50, cancellationToken);
				}
			}
		finally
			{
			foreach (var (socket, _) in sockets)
				{
				socket.Dispose ();
				}
			}

		if (results.Count == 0)
			{
			diagnostics.Add ("No valid discovery responses were received.");
			}

		return new ProcessorDiscoveryResult
			{
			Processors = results
				.OrderBy (processor => processor.Hostname)
				.ThenBy (processor => processor.IPAddress.ToString ())
				.ToArray (),
			Diagnostics = diagnostics.ToArray ()
			};
		}

	private static byte[] BuildProbe ()
		{
		var packet = new byte[TOOLBOX_PACKET_SIZE];
		packet[0] = 0x14;
		packet[1] = 0x00;
		packet[2] = 0x00;
		packet[3] = 0x00;
		packet[4] = 0x01;
		packet[5] = 0x04;
		packet[6] = 0x00;
		packet[7] = 0x03;
		packet[8] = 0x00;
		packet[9] = 0x00;

		var hostnameBytes = Encoding.ASCII.GetBytes (Dns.GetHostName ().ToUpperInvariant ());
		var copyLength = Math.Min (hostnameBytes.Length, 255);
		Buffer.BlockCopy (hostnameBytes, 0, packet, 10, copyLength);
		return packet;
		}

	private static DiscoveredProcessorInfo? ParseResponse (byte[] data, int length, IPAddress address)
		{
		if (length < 7)
			{
			return null;
			}

		string hostname;
		var versionString = string.Empty;

		switch (data[0])
			{
			case 0x15:
				hostname = ExtractNullTerminatedAscii (data, 10, Math.Max (0, Math.Min (256, length - 10)));
				if (length > 266)
					{
					versionString = ExtractNullTerminatedAscii (data, 266, Math.Max (0, Math.Min (128, length - 266)));
					}
				break;

			case 0x16:
				hostname = ExtractNullTerminatedAscii (data, 6, Math.Max (0, length - 6));
				if (string.IsNullOrWhiteSpace (hostname) && length > 10)
					{
					hostname = ExtractNullTerminatedAscii (data, 10, Math.Max (0, length - 10));
					}
				if (length > 266)
					{
					versionString = ExtractNullTerminatedAscii (data, 266, Math.Max (0, Math.Min (128, length - 266)));
					}
				break;

			default:
				return null;
			}

		if (string.IsNullOrWhiteSpace (hostname))
			{
			return null;
			}

		return new DiscoveredProcessorInfo
			{
			IPAddress = address,
			Hostname = hostname,
			VersionString = versionString
			};
		}

	private static string ExtractNullTerminatedAscii (byte[] data, int offset, int maxLength)
		{
		var limit = Math.Min (offset + maxLength, data.Length);
		var end = offset;
		while (end < limit && data[end] != 0x00)
			{
			end++;
			}

		return Encoding.ASCII.GetString (data, offset, end - offset).Trim ();
		}

	private static string ToHexPreview (byte[] data, int length, int count)
		{
		var previewLength = Math.Min (Math.Max (length, 0), Math.Min (count, data.Length));
		return BitConverter.ToString (data, 0, previewLength);
		}

	private static string ToAsciiPreview (byte[] data, int length, int count)
		{
		var previewLength = Math.Min (Math.Max (length, 0), Math.Min (count, data.Length));
		var builder = new StringBuilder (previewLength);
		for (var index = 0; index < previewLength; index++)
			{
			var value = data[index];
			builder.Append (value is >= 32 and <= 126 ? (char)value : '.');
			}

		return builder.ToString ();
		}

	private static IPAddress? GetBroadcastAddress (IPAddress address, IPAddress subnetMask)
		{
		var addressBytes = address.GetAddressBytes ();
		var maskBytes = subnetMask.GetAddressBytes ();
		if (addressBytes.Length != maskBytes.Length)
			{
			return null;
			}

		var broadcastBytes = new byte[addressBytes.Length];
		for (var index = 0; index < addressBytes.Length; index++)
			{
			broadcastBytes[index] = (byte)(addressBytes[index] | ~maskBytes[index]);
			}

		return new IPAddress (broadcastBytes);
		}

	private sealed class DiscoverySocket
		{
		public DiscoverySocket (Socket socket, IPEndPoint destination)
			{
			Socket = socket;
			Destination = destination;
			}

		public Socket Socket
			{
			get;
			}

		public IPEndPoint Destination
			{
			get;
			}

		public void Deconstruct (out Socket socket, out IPEndPoint destination)
			{
			socket = Socket;
			destination = Destination;
			}
		}
	}
