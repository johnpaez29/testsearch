using AutoMapper;
using TestSearching.Comon;
using TestSearching.Entities;
using TestSearching.Interfaces;

namespace TestSearching.Processors
{
	public class BcSearchProcessor : ISearchProcessor<Company>
	{
		private readonly IBcRegistryService _bcRegistryService;

		public BcSearchProcessor(IBcRegistryService bcRegistryService)
		{
			_bcRegistryService = bcRegistryService;
		}

		public Task GetPadfAsync(string transaction, string url)
		{
			throw new NotImplementedException();
		}

		public async Task<IEnumerable<Company>> SearchAsync(string searchTerm, CancellationToken cancellationToken)
		{
			var queryRequest = new QueryRequest(searchTerm);
			var queryResult = await _bcRegistryService.GetBusinessInformationListAsync(queryRequest);
			var companyResult = queryResult.Select(qr => new Company 
			{	CompanyId = qr.Identifier,
				CompanyName = qr.Name,
				Data = new BcCompanyDetails 
				{
					LegalType = qr.LegalType,
					Bn = qr.Bn,
					Score = qr.Score,
					Status = qr.Status
				}
			});
			return companyResult;
		}
	}

}
