namespace TestSearching.Comon
{
	public record CompanyQueryResult
	{
		public string Id { get; set; }
		public string Query { get; set; }
		public string ResultUrl { get; set; }
	}
}
