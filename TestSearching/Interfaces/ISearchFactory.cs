namespace TestSearching.Interfaces
{
	public interface ISearchFactory<T> where T : class
	{
		ISearchProcessor<T> Create(string provinceCode);
	}
}
