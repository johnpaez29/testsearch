using TestSearching.Entities;
using TestSearching.Queries;

namespace TestSearching.Interfaces
{
	public interface IsearchService
	{
		Task<IEnumerable<CompanyDto>> Handle(CompanySearchQuery request, CancellationToken cancellationToken);

	}
}
