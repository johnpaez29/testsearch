namespace TestSearching.Interfaces
{
	public interface ISearchProcessor<T> where T : class
	{
		Task GetPadfAsync(string transaction, string url);
		Task<IEnumerable<T>> SearchAsync(string searchTerm, CancellationToken cancellationToken);
	}
}
