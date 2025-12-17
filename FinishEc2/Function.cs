using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.Lambda;
using Amazon.Lambda.Core;
using Amazon.Lambda.Model;
using Amazon.Lambda.SQSEvents;
using Newtonsoft.Json;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace FinishEc2;

public class Function
{
	private readonly AmazonEC2Client _ec2 = new(new AmazonEC2Config
	{
		RegionEndpoint = Amazon.RegionEndpoint.USEast1
	});

	private readonly AmazonLambdaClient _lambda = new();
	private readonly AmazonDynamoDBClient _ddb = new();

	private const string TABLE = "ProxyPool";
	private const string ADD_EC2_LAMBDA = "addEc2";

	public async Task FunctionHandler(SQSEvent evnt, ILambdaContext context)
	{
		foreach (var record in evnt.Records)
		{
			ReplaceRequest? message;

			try
			{
				message = JsonConvert.DeserializeObject<ReplaceRequest>(record.Body);
				if (string.IsNullOrWhiteSpace(message?.InstanceId))
				{
					context.Logger.LogLine("Message missing InstanceId");
					continue;
				}
			}
			catch (Exception ex)
			{
				context.Logger.LogLine($"Invalid message format: {ex.Message}");
				continue;
			}

			var instanceId = message.InstanceId;
			await SafeExecute(
					() => TerminateInstanceAsync(instanceId, context),
					context,
					$"Terminate EC2 {instanceId}"
				);

			var tasks = new List<Task>
			{
				SafeExecute(
					() => DeleteFromDynamoAsync(instanceId, context),
					context,
					$"Delete DynamoDB {instanceId}"
				),
				SafeExecute(
					() => InvokeCreateEc2Async(context),
					context,
					"Invoke addEc2 Lambda"
				)
			};

			await Task.WhenAll(tasks);

			context.Logger.LogLine($"Replacement flow completed for {instanceId}");
		}
	}

	/// <summary>
	/// Safe wrapper
	/// </summary>
	/// <param name="action">the action method</param>
	/// <param name="context">context lambda</param>
	/// <param name="operation">Operation Name</param>
	/// <returns></returns>
	private static async Task SafeExecute(
		Func<Task> action,
		ILambdaContext context,
		string operation)
	{
		try
		{
			await action();
		}
		catch (Exception ex)
		{
			context.Logger.LogLine($"❌ {operation} failed: {ex.Message}");
		}
	}

	// ---------------- OPERATIONS ----------------

	private async Task TerminateInstanceAsync(string instanceId, ILambdaContext context)
	{
		await _ec2.TerminateInstancesAsync(new TerminateInstancesRequest
		{
			InstanceIds = [instanceId]
		});

		await WaitUntilTerminatedAsync(instanceId, context);


		context.Logger.LogLine($"EC2 termination requested: {instanceId}");
	}

	private async Task DeleteFromDynamoAsync(string instanceId, ILambdaContext context)
	{
		await _ddb.DeleteItemAsync(new DeleteItemRequest
		{
			TableName = TABLE,
			Key = new Dictionary<string, AttributeValue>
			{
				["InstanceId"] = new AttributeValue { S = instanceId }
			}
		});

		context.Logger.LogLine($"DynamoDB item deleted: {instanceId}");
	}

	private async Task InvokeCreateEc2Async(ILambdaContext context)
	{
		await _lambda.InvokeAsync(new InvokeRequest
		{
			FunctionName = ADD_EC2_LAMBDA,
			InvocationType = InvocationType.Event,
			Payload = JsonConvert.SerializeObject(new { Count = 1 })
		});

		context.Logger.LogLine("addEc2 Lambda invoked");
	}

	private async Task WaitUntilTerminatedAsync(string instanceId, ILambdaContext context)
	{
		while (true)
		{
			try
			{
				var res = await _ec2.DescribeInstancesAsync(new DescribeInstancesRequest
				{
					InstanceIds = [instanceId]
				});

				var state = res.Reservations
					.SelectMany(r => r.Instances)
					.FirstOrDefault()
					?.State?.Name;

				if (state == InstanceStateName.Terminated)
				{
					context.Logger.LogLine($"Instance {instanceId} fully terminated");
					return;
				}
			}
			catch (AmazonEC2Exception ex) when
				(ex.ErrorCode == "InvalidInstanceID.NotFound")
			{
				context.Logger.LogLine($"Instance {instanceId} no longer exists");
				return;
			}

			await Task.Delay(3000);
		}
	}

	public class ReplaceRequest
	{
		public string InstanceId { get; set; } = string.Empty;
	}
}
