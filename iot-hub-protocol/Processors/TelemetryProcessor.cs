using System.Text;
using iot_hub_protocol.Contracts;
using Newtonsoft.Json;

namespace iot_hub_protocol.Processors;

public static class TelemetryProcessor
{
	public static string ToJsonMessage(this Telemetry message)
	{
		return JsonConvert.SerializeObject(message, Formatting.Indented);
	}

	public static byte[] ToByteMessage(this Telemetry message)
	{
		var telemetryJson = JsonConvert.SerializeObject(message);
		
		return Encoding.ASCII.GetBytes(telemetryJson);
	}

	public static Telemetry FromByteMessage(this byte[] payload)
	{
		var message = Encoding.ASCII.GetString(payload);

		return JsonConvert.DeserializeObject<Telemetry>(message);
	}
}
