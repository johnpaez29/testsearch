using TestSearching.Comon;
using TestSearching.Entities;
using TestSearching.Interfaces;
using TestSearching.Services;

namespace TestSearching.Processors
{
	public class OnSearchProcessor : ISearchProcessor<Company>
	{
		private readonly IScraperService _scraperService;
		private readonly IHtmlParserService _htmlParserService;

		public OnSearchProcessor(IScraperService scraperService, IHtmlParserService htmlParserService)
		{
			_scraperService = scraperService;
			_htmlParserService = htmlParserService;
		}

		public async Task GetPadfAsync(string transaction, string url)
		{
			await _scraperService.GetPdfReceiptAsync(transaction, url);
		}

		public async Task<IEnumerable<Company>> SearchAsync(string searchTerm, CancellationToken cancellationToken)
		{
			var queryId = System.Guid.NewGuid().ToString();

			var htmlResult = await _scraperService.GetBusinessInformationAsync(searchTerm);
			var parsedResult = _htmlParserService.GetBusinessInformationList(queryId, htmlResult.Item2);

			if (parsedResult == null || !parsedResult.Any())
			{
				return Enumerable.Empty<Company>();
			}

			// Save Result In Redis
			CompanyQueryResult queryResult = new CompanyQueryResult()
			{
				Id = queryId,
				Query = searchTerm,
				ResultUrl = htmlResult.Item1
			};

			// Return Query Id and companies result
			return [.. parsedResult];
		}
	}

}
