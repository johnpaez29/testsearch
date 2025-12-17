using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace GetSearchFunction;

public class Function
{
	private readonly AmazonDynamoDBClient _ddb = new();
	private const string TABLE = "ProxyPool";
	private const int MAX_RETRIES = 10;

	public async Task<ProxyResponse?> FunctionHandler()
	{
		try
		{
			return await GetIpdEc2();
		}
		catch
		{
			return null;
		}

	}

	private async Task<ProxyResponse?> GetIpdEc2()
	{
		try
		{
			for (int attempt = 1; attempt <= MAX_RETRIES; attempt++)
			{
				var proxy = await TryBookOnceAsync();

				if (proxy != null)
					return proxy;
				await Task.Delay(Random.Shared.Next(100, 300));
			}
		}
		catch
		{
			return null;
		}

		return null;
	}

	private async Task<ProxyResponse?> TryBookOnceAsync()
	{
		var scan = await _ddb.QueryAsync(new QueryRequest
		{
			TableName = TABLE,
			IndexName = "StatusCreatedAtIndex",
			KeyConditionExpression = "#s = :available",
			ExpressionAttributeNames = new Dictionary<string, string>
			{
				["#s"] = "Status"
			},
			ExpressionAttributeValues = new Dictionary<string, AttributeValue>
			{
				[":available"] = new AttributeValue { S = "AVAILABLE" }
			},
			ScanIndexForward = true, // 🔥 OLDEST first
			Limit = 1,
			ConsistentRead = false // GSIs cannot be strongly consistent
		});

		if (!scan.Items.Any())
			return null;

		var item = scan.Items[0];



		var instanceId = item["InstanceId"].S;

		try
		{
			await _ddb.UpdateItemAsync(new UpdateItemRequest
			{
				TableName = TABLE,
				Key = new Dictionary<string, AttributeValue>
				{
					["InstanceId"] = new AttributeValue { S = instanceId }
				},
				UpdateExpression = "SET #s = :booked, BookedAt = :now",
				ConditionExpression = "#s = :available",
				ExpressionAttributeNames = new Dictionary<string, string>
				{
					["#s"] = "Status"
				},
				ExpressionAttributeValues = new Dictionary<string, AttributeValue>
				{
					[":available"] = new AttributeValue { S = "AVAILABLE" },
					[":booked"] = new AttributeValue { S = "BOOKED" },
					[":now"] = new AttributeValue
					{
						N = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()
					}
				}
			});
		}
		catch (ConditionalCheckFailedException)
		{
			return null;
		}

		return new ProxyResponse
		{
			InstanceId = instanceId,
			ProxyIp = item["PublicIp"].S,
			ProxyPort = int.Parse(item["Port"].N)
		};
	}

	public class ProxyResponse
	{
		public required string InstanceId { get; set; }
		public required string ProxyIp { get; set; }
		public required int ProxyPort { get; set; }
	}
}
