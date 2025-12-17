using TestSearching.Entities;

namespace TestSearching.Interfaces
{
	public interface IHtmlParserService
	{
		IEnumerable<Company> GetBusinessInformationList(string queryId, string rawHtml);
	}
}
