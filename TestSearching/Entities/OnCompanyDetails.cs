using System.Text.Json.Serialization;

namespace TestSearching.Entities
{
	public class OnCompanyDetails
	{
		public string? Status { get; init; }
		public string? Address { get; init; }
		public string? DateIncorporated { get; init; }
		public string? Type { get; init; }
		[JsonIgnore]
		public bool IsArchived { get; init; }
		public IReadOnlyCollection<string>? PreviousNames { get; init; }
		public string? QueryId { get; init; }
	}
}
