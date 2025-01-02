using System.Collections.Concurrent;
using System.Diagnostics;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Processor;
using Azure.Storage.Blobs;
using iot_hub_protocol.Processors;

var hubName = "iothub-demo-hub";
var iotHubConnectionString = "IOT_HUB_CONNECTION_STRING";

var storageConnectionString = "STORAGE_CONNECTION_STRING";
var storageContainerName = "iot-message-processor-host";

var consumerGroupName = EventHubConsumerClient.DefaultConsumerGroupName;
var storageClient = new BlobContainerClient(storageConnectionString, storageContainerName);
var processor = new EventProcessorClient(storageClient, consumerGroupName, iotHubConnectionString, hubName);
var partitionEventCount = new ConcurrentDictionary<string, int>();

async Task ProcessEventHandler(ProcessEventArgs args)
{
	try
	{
		if (args.CancellationToken.IsCancellationRequested)
		{
			return;
		}

		var partition = args.Partition.PartitionId;
		var eventBody = args.Data.EventBody.ToArray();
		var telemetry = eventBody.FromByteMessage();
		var deviceId = args.Data.SystemProperties["iothub-connection-device-id"];

		Console.WriteLine($"Message with ID {telemetry.SegmentId} was received from {deviceId}:");
		Console.WriteLine(telemetry.ToJsonMessage());

		var eventsSinceLastCheckpoint = partitionEventCount.AddOrUpdate(
			key: partition,
			addValue: 1,
			updateValueFactory: (_, currentCount) => currentCount + 1);

		if (eventsSinceLastCheckpoint >= 50)
		{
			await args.UpdateCheckpointAsync();
			
			partitionEventCount[partition] = 0;
		}
	}
	catch (Exception ex)
	{
		Console.WriteLine(ex.Message);
	}
}

Task ProcessErrorHandler(ProcessErrorEventArgs args)
{
	try
	{
		Debug.WriteLine("Error in the EventProcessorClient");
		Debug.WriteLine($"\tOperation: {args.Operation}");
		Debug.WriteLine($"\tException: {args.Exception}");
	}
	catch (Exception ex)
	{
		Console.WriteLine(ex.Message);
	}

	return Task.CompletedTask;
}

try
{
	using var cancellationTokenSource = new CancellationTokenSource();
	cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(60));

	processor.ProcessEventAsync += ProcessEventHandler;
	processor.ProcessErrorAsync += ProcessErrorHandler;

	try
	{
		await processor.StartProcessingAsync(cancellationTokenSource.Token);
		await Task.Delay(Timeout.Infinite, cancellationTokenSource.Token);
	}
	catch (TaskCanceledException)
	{

	}
	finally
	{
		await processor.StopProcessingAsync();
	}
}
catch(Exception ex)
{
	Console.WriteLine(ex.Message);
}
finally
{
	processor.ProcessEventAsync -= ProcessEventHandler;
	processor.ProcessErrorAsync -= ProcessErrorHandler;
}