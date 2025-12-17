using Serilog;
using System.Text.Json;
using TestSearching.Comon;
using TestSearching.Interfaces;
using ILogger = Serilog.ILogger;

namespace TestSearching.Services
{
	public class BcRegistryService : IBcRegistryService
	{
		private readonly HttpClient _httpClient;
		private readonly ILogger _logger;

		public BcRegistryService(HttpClient httpClient)
		{
			_httpClient = httpClient;
			_logger = Log.ForContext<BcRegistryService>();
		}

		public async ValueTask<IEnumerable<ResultItem>> GetBusinessInformationListAsync(QueryRequest queryRequest)
		{
			_logger.Information("BR Search: Credentials {AccountId}", _httpClient.DefaultRequestHeaders);

			if (string.IsNullOrWhiteSpace(queryRequest.Query.Value))
			{
				_logger.Warning("BR Search: Received query is empty.");
				return Enumerable.Empty<ResultItem>();
			}

			var content = JsonSerializer.Serialize(queryRequest);

			var request = new HttpRequestMessage(HttpMethod.Post, "/registry-search/api/v2/search/businesses")
			{
				Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json")
			};

			var apiResponse = await _httpClient.SendAsync(request);
			apiResponse.EnsureSuccessStatusCode();
			var responseContent = await GetCompanyListAsync(apiResponse);

			return responseContent;
		}

		private async ValueTask<IEnumerable<ResultItem>> GetCompanyListAsync(HttpResponseMessage message)
		{
			var jsonContent = await message.Content.ReadAsStringAsync();

			_logger.Information("BR Search: Raw Result {RawResult}.", jsonContent);

			using var doc = JsonDocument.Parse(jsonContent);

			var resultsJson = doc.RootElement.GetProperty("searchResults").GetProperty("results").GetRawText();
			return JsonSerializer.Deserialize<List<ResultItem>>(resultsJson);
		}
	}

}
