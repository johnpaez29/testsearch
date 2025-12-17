using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.SQS;
using Amazon.SQS.Model;
using Newtonsoft.Json;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace QueueTerminationProcess;

public class Function
{
	private readonly AmazonSQSClient _sqs = new();

	private const string QUEUE_URL =
		"https://sqs.us-east-1.amazonaws.com/047593684793/terminationEc2Queue";

	public async Task<APIGatewayProxyResponse> FunctionHandler(
		APIGatewayProxyRequest request,
		ILambdaContext context)
	{
		if (string.IsNullOrWhiteSpace(request.Body))
		{
			return BadRequest("Request body is required");
		}

		QueueRequest? payload;

		try
		{
			payload = JsonConvert.DeserializeObject<QueueRequest>(request.Body);
		}
		catch
		{
			return BadRequest("Invalid JSON");
		}

		if (string.IsNullOrWhiteSpace(payload?.InstanceId))
		{
			return BadRequest("InstanceId is required");
		}

		var messageBody = JsonConvert.SerializeObject(new
		{
			payload.InstanceId
		});

		await _sqs.SendMessageAsync(new SendMessageRequest
		{
			QueueUrl = QUEUE_URL,
			MessageBody = messageBody
		});

		context.Logger.LogLine($"Queued termination for {payload.InstanceId}");

		return new APIGatewayProxyResponse
		{
			StatusCode = 200,
			Body = JsonConvert.SerializeObject(new
			{
				success = true,
				message = "Termination request queued"
			}),
			Headers = new Dictionary<string, string>
			{
				["Content-Type"] = "application/json"
			}
		};
	}

	private static APIGatewayProxyResponse BadRequest(string message) =>
		new()
		{
			StatusCode = 400,
			Body = JsonConvert.SerializeObject(new
			{
				success = false,
				message
			}),
			Headers = new Dictionary<string, string>
			{
				["Content-Type"] = "application/json"
			}
		};

	public class QueueRequest
	{
		public string InstanceId { get; set; } = string.Empty;
	}
}
