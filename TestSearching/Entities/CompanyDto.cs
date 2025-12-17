using AutoMapper;

namespace TestSearching.Entities
{
	public record CompanyDto
	{
		public string CompanyId { get; init; }
		public string CompanyName { get; init; }
		public object? Data { get; init; }

	}

}
