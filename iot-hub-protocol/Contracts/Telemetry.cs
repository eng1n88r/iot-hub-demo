namespace iot_hub_protocol.Contracts;

public class Telemetry
{
	public int SegmentId { get; set; }

	public string Message { get; set; }

	public StatusType Status { get; set; }
}

