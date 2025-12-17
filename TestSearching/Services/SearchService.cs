using TestSearching.Entities;
using TestSearching.Interfaces;
using TestSearching.Queries;

namespace TestSearching.Services
{
	public class SearchService(ISearchFactory<Company> _searchProvideFactory)
	{
		public async Task<IEnumerable<CompanyDto>> Handle(CompanySearchQuery request, CancellationToken cancellationToken)
		{
			var searchProcessor = _searchProvideFactory.Create(request.ProvinceCode);
			var searchResult = await searchProcessor.SearchAsync(request.SearchTerm, cancellationToken);
			return searchResult.Select(sr => new CompanyDto
			{
				CompanyId = sr.CompanyId,
				CompanyName = sr.CompanyName,
				Data = sr.Data,
			});
		}
	}
}
