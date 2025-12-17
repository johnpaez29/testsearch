namespace TestSearching.Entities
{
	public class Company
	{
		public required string CompanyId { get; init; }
		public required string CompanyName { get; init; }
		public object? Data { get; init; }
	}
}
