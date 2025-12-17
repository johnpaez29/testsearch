using TestSearching.Comon;

namespace TestSearching.Interfaces
{
	public interface IBcRegistryService
	{
		ValueTask<IEnumerable<ResultItem>> GetBusinessInformationListAsync(QueryRequest queryRequest);
	}
}
