using AutoMapper;
using System.IO;
using System.Text.Json.Serialization;
using TestSearching.Entities;

namespace TestSearching.Comon
{
	public record QueryRequest
	{
		public QueryRequest(string query, int rows = 10, int start = 0)
		{
			Query = new Query { Value = query };
			Rows = rows;
			Start = start;
		}

		[JsonPropertyName("query")]
		public Query? Query { get; set; }
		[JsonPropertyName("rows")]
		public int? Rows { get; set; }
		[JsonPropertyName("start")]
		public int? Start { get; set; }
	}

	public record Query
	{
		[JsonPropertyName("value")]
		public string? Value { get; set; }
	}

	public record ResultItem
	{
		[JsonPropertyName("bn")]
		public string Bn { get; set; }
		[JsonPropertyName("identifier")]
		public string Identifier { get; set; }
		[JsonPropertyName("legalType")]
		public string LegalType { get; set; }
		[JsonPropertyName("name")]
		public string Name { get; set; }
		[JsonPropertyName("parties")]
		public List<Party> Parties { get; set; }
		[JsonPropertyName("score")]
		public double Score { get; set; }
		[JsonPropertyName("status")]
		public string Status { get; set; }

	}


	public record Party
	{
		[JsonPropertyName("partyName")]
		public string Name { get; set; }
		[JsonPropertyName("partyRoles")]
		public List<string> Roles { get; set; }
		[JsonPropertyName("partyType")]
		public string Type { get; set; }
		[JsonPropertyName("score")]
		public double Score { get; set; }
	}
}
