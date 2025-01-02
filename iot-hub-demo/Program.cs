using System.Text;
using iot_hub_protocol.Contracts;
using iot_hub_protocol.Processors;
using Microsoft.Azure.Devices.Client;

const string deviceConnectionString = "DEVICE_CONNECTION_STRING";

Console.WriteLine("Initializing Device Connection...");

var device = DeviceClient.CreateFromConnectionString(deviceConnectionString);

await device.OpenAsync();

Console.WriteLine($"Device is connected!");

_ = Task.Run(() => ReceiveEvents(device));

await device.SetMethodDefaultHandlerAsync(DefaultMethodHandler, null);
await device.SetMethodHandlerAsync("showMessage", ShowMethodHandler, null);

Console.WriteLine("Press a key to submit feedback (h: Happy, u: Unhappy, e: Emergency, q: Quit):");

var quit = false;
var segmentId = 0;
var status = StatusType.NotSpecified;

while (!quit)
{
	Console.Write("Action:");

	var input = Console.ReadKey();

	Console.WriteLine();

	switch (char.ToLower(input.KeyChar))
	{
		case 'q':
			quit = true;
			break;
		case 'h':
			status = StatusType.Happy;
			break;
		case 'u':
			status = StatusType.Unhappy;
			break;
		case 'e':
			status = StatusType.Emergency;
			break;
		default:
			continue;
	}

	if (quit) continue;

	segmentId++;

	var telemetry = new Telemetry
	{
		SegmentId = segmentId,
		Message = $"Telemetry Segment with ID {segmentId}",
		Status = status
	};

	var message = new Message(telemetry.ToByteMessage());

	await device.SendEventAsync(message);

	Console.WriteLine($"Message with Id {telemetry.SegmentId} was sent!");
}

return;


async Task ReceiveEvents(DeviceClient client)
{
	while (true)
	{
		var message = await client.ReceiveAsync();

		if (message == null) continue;

		var messageBody = message.GetBytes();
		var payoad = Encoding.ASCII.GetString(messageBody);

		Console.WriteLine($"{payoad} received!");

		await client.CompleteAsync(message);
	}
}

Task<MethodResponse> ShowMethodHandler(MethodRequest methodRequest, object userContext)
{
	Console.WriteLine("****MESSAGE RECEIVED****");
	Console.WriteLine(methodRequest.DataAsJson);

	var responsePayload = Encoding.ASCII.GetBytes("{\"response\": \"Message shown!\"}");

	return Task.FromResult(new MethodResponse(responsePayload, 200));
}

Task<MethodResponse> DefaultMethodHandler(MethodRequest methodRequest, object userContext)
{
	Console.WriteLine("****OTHER DEVICE METHOD RECEIVED****");
	Console.WriteLine($"Method: {methodRequest.Name}");
	Console.WriteLine($"Payload: {methodRequest.DataAsJson}");

	var responsePayload = Encoding.ASCII.GetBytes("{\"response\": \"This method doesn't exist!\"}");

	return Task.FromResult(new MethodResponse(responsePayload, 404));
}