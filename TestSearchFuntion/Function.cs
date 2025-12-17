using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Newtonsoft.Json;
using System.Text;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace TestSearchFuntion
{
	public class Function
	{
		private readonly AmazonEC2Client _ec2 = new(
			new AmazonEC2Config
			{
				RegionEndpoint = Amazon.RegionEndpoint.USEast1
			}
		);

		private readonly AmazonDynamoDBClient _ddb = new();


		private const string AMI_ID = "ami-068c0051b15cdb816";
		private const string SECURITY_GROUP = "sg-023b507c46d2fd2d1";
		private const string USER_DATA = @"#!/bin/bash
yum update -y
yum install -y squid
systemctl enable squid
sed -i 's/http_access deny all/http_access allow all/' /etc/squid/squid.conf
systemctl restart squid
";

		public async Task<APIGatewayHttpApiV2ProxyResponse> FunctionHandler(
		  APIGatewayHttpApiV2ProxyRequest request,
		  ILambdaContext context)
		{
			var input = JsonConvert.DeserializeObject<CreatePoolRequest>(request.Body ?? "{}");
			int count = Math.Clamp(input?.Count ?? 1, 1, 50);

			// 1) Create EC2 instances
			var run = await _ec2.RunInstancesAsync(new RunInstancesRequest
			{
				ImageId = AMI_ID,
				InstanceType = InstanceType.T3Micro,
				MinCount = count,
				MaxCount = count,
				SecurityGroupIds = [SECURITY_GROUP],
				UserData = Convert.ToBase64String(Encoding.UTF8.GetBytes(USER_DATA)),
				TagSpecifications =
				[
					new TagSpecification
					{
						ResourceType = ResourceType.Instance,
						Tags = [ new Amazon.EC2.Model.Tag("Role", "Proxy") ]
					}
				]
			});

			var instanceIds = run.Reservation.Instances
				.Select(i => i.InstanceId)
				.ToList();

			context.Logger.LogLine($"Instances created: {string.Join(',', instanceIds)}");

			await WaitUntilRunningAsync(instanceIds);

			DescribeInstancesResponse describe;
			while (true)
			{
				describe = await _ec2.DescribeInstancesAsync(new DescribeInstancesRequest
				{
					InstanceIds = instanceIds
				});

				var withIp = describe.Reservations
					.SelectMany(r => r.Instances)
					.Where(i => !string.IsNullOrEmpty(i.PublicIpAddress))
					.ToList();

				if (withIp.Count == instanceIds.Count)
					break;

				await Task.Delay(2000);
			}

			var writeRequests = MapRequests(describe);
			await SaveEc2Instances(writeRequests);

			var response = new ProxyResponse
			{
				CountCreated = instanceIds.Count,
				Instances = instanceIds.ToArray()
			};

			return new APIGatewayHttpApiV2ProxyResponse
			{
				StatusCode = 201,
				Body = JsonConvert.SerializeObject(response),
				Headers = new Dictionary<string, string>
				{
					["Content-Type"] = "application/json"
				}
			};
		}
		private async Task SaveEc2Instances(IEnumerable<WriteRequest> writeRequests)
		{
			foreach (var chunk in writeRequests.Chunk(100))
			{
				var response = await _ddb.BatchWriteItemAsync(new BatchWriteItemRequest
				{
					RequestItems = new Dictionary<string, List<WriteRequest>>
					{
						["ProxyPool"] = [.. chunk]
					}
				});
				while (response.UnprocessedItems.Count != 0)
				{
					response = await _ddb.BatchWriteItemAsync(new BatchWriteItemRequest
					{
						RequestItems = response.UnprocessedItems
					});
				}
			}
		}

		private static IEnumerable<WriteRequest> MapRequests(DescribeInstancesResponse describe)
		{
			return describe.Reservations
					.SelectMany(r => r.Instances)
					.Where(i => !string.IsNullOrEmpty(i.PublicIpAddress))
					.Select(i => new WriteRequest
					{
						PutRequest = new PutRequest
						{
							Item = new Dictionary<string, AttributeValue>
							{
								["InstanceId"] = new AttributeValue { S = i.InstanceId },
								["PublicIp"] = new AttributeValue { S = i.PublicIpAddress },
								["Port"] = new AttributeValue { N = "3128" },
								["Status"] = new AttributeValue { S = "AVAILABLE" },
								["CreatedAt"] = new AttributeValue
								{
									N = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()
								}
							}
						}
					});
		}

		// ---------------------- WAITERS ----------------------


		private async Task WaitUntilRunningAsync(List<string> instanceIds)
		{
			var remaining = new HashSet<string>(instanceIds);

			while (remaining.Count > 0)
			{
				try
				{
					var res = await _ec2.DescribeInstancesAsync(
						new DescribeInstancesRequest
						{
							InstanceIds = remaining.ToList()
						});

					foreach (var instance in res.Reservations.SelectMany(r => r.Instances))
					{
						if (instance.State.Name == InstanceStateName.Running)
						{
							remaining.Remove(instance.InstanceId);
						}
					}
				}
				catch (AmazonEC2Exception ex) when
					(ex.ErrorCode == "InvalidInstanceID.NotFound")
				{
					// 🔁 EC2 eventual consistency — WAIT and retry
				}

				await Task.Delay(2000);
			}
		}
	}

	public class CreatePoolRequest
	{
		public int Count { get; set; }
	}
	public class ProxyResponse
	{
		public int CountCreated { get; set; }
		public string[] Instances { get; set; }
	}
}
