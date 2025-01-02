using System.Text;
using Microsoft.Azure.Devices;
using Newtonsoft.Json;

var cancellationTokenSource = new CancellationTokenSource();
var token = cancellationTokenSource.Token;
var hubConnectionString = "HUB_CONNECTION_STRING";
var serviceClient = ServiceClient.CreateFromConnectionString(hubConnectionString);
var registryManager = RegistryManager.CreateFromConnectionString(hubConnectionString);
_ = ReceiveFeedback(serviceClient);

while (!token.IsCancellationRequested)
{
	Console.WriteLine("Which device are you sending message to? ");
	Console.Write(">");

	var deviceId = Console.ReadLine();

	await SendCloudToDeviceMessage(serviceClient, deviceId);
	await CallDirectMethod(serviceClient, deviceId);
	await UpdateDeviceFirmwareMethod(registryManager, deviceId);
}

async Task UpdateDeviceFirmwareMethod(RegistryManager manager, string? deviceId)
{
	var deviceTwin = await manager.GetTwinAsync(deviceId);

	var twinPatch = new
	{
		properties = new
		{
			desired = new
			{
				firmwareVersion = "2.0"
			}
		}
	};

	var twinPatchJson = JsonConvert.SerializeObject(twinPatch);

	await manager.UpdateTwinAsync(deviceId, twinPatchJson, deviceTwin.ETag);

	Console.WriteLine("Firmware update sent to device!");
}

async Task CallDirectMethod(ServiceClient client, string? deviceId)
{
	var method = new CloudToDeviceMethod("showMessage");

	method.SetPayloadJson("'Hello from C#!!!'");

	var response = await client.InvokeDeviceMethodAsync(deviceId, method);

	Console.WriteLine($"Response status: {response.Status}, payload: {response.GetPayloadAsJson()}");
}

async Task SendCloudToDeviceMessage(ServiceClient client, string? deviceId)
{
	Console.WriteLine("Which message are you sending? ");
	Console.Write(">");

	var message = Console.ReadLine();

	var commandMessage = new Message(Encoding.ASCII.GetBytes(message!))
	{
		MessageId = Guid.NewGuid().ToString(),
		Ack = DeliveryAcknowledgement.Full
	};

	await client.SendAsync(deviceId, commandMessage);
}

async Task ReceiveFeedback(ServiceClient client)
{
	var feedbackReceiver = client.GetFeedbackReceiver();

	while (!token.IsCancellationRequested)
	{
		var feedbackBatch = await feedbackReceiver.ReceiveAsync(token);

		if (feedbackBatch == null) continue;

		foreach (var record in feedbackBatch.Records)
		{
			var messageId = record.OriginalMessageId;
			var statusCode = record.StatusCode;

			Console.WriteLine($"Feedback from device received {messageId} with status {statusCode}");
		}

		await feedbackReceiver.CompleteAsync(feedbackBatch, token);
	}
}

