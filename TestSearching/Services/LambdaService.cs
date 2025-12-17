using TestSearching.Entities;
using TestSearching.Interfaces;

namespace TestSearching.Services
{
	public class LambdaService(HttpClient httpClient) : ILambdaService
	{
		private string instanceId = string.Empty;

		public class TerminateRequest
		{
			public string InstanceId { get; set; }
		}

		public async Task FinishInstanceProxy()
		{
			try
			{
				var payload = new { };

				var response = await httpClient.PostAsJsonAsync(
					"https://1gpkahyxm6.execute-api.us-east-1.amazonaws.com/default/queueFinishEc2",
					new TerminateRequest { InstanceId = instanceId });

				if (response.IsSuccessStatusCode)
				{
					var content = await response.Content.ReadFromJsonAsync<ProxyResponse>();

					instanceId = content?.InstanceId ?? string.Empty;
				}
			}
			catch
			{
				return;
			}
		}

		public async Task<ProxyResponse?> GetIpProxy()
		{

			for (int index = 0; index < 30; index++)
			{
				try
				{
					var proxy = await GetProxyRest();

					if (proxy == null)
					{
						await Task.Delay(Random.Shared.Next(1000, 3000));
						continue;
					}
					return proxy;
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.Message);
					return null;
				}

			}

			return null;
		}

		private async Task<ProxyResponse?> GetProxyRest()
		{
			var response = await httpClient.GetAsync("https://v2u9lq0sgk.execute-api.us-east-1.amazonaws.com/default/getEc2Proxy");
			if (response.IsSuccessStatusCode)
			{
				var content = await response.Content.ReadFromJsonAsync<ProxyResponse>();

				instanceId = content?.InstanceId ?? string.Empty;

				return content;
			}
			else
			{
				return null;
			}
		}
	}
}
