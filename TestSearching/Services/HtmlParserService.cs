using HtmlAgilityPack;
using Serilog;
using System.Web;
using System.Xml;
using TestSearching.Entities;
using TestSearching.Interfaces;
using ILogger = Serilog.ILogger;

namespace TestSearching.Services
{
	public class HtmlParserService : IHtmlParserService
	{
		private readonly ILogger _logger;

		public HtmlParserService()
		{
			_logger = Log.ForContext<HtmlParserService>();
		}

		public IEnumerable<Company> GetBusinessInformationList(string queryId, string rawHtml)
		{
			if (string.IsNullOrWhiteSpace(rawHtml))
			{
				_logger.Warning("BR Search: Received empty or null HTML content.");
				return Enumerable.Empty<Company>();
			}

			var htmlDoc = new HtmlDocument();
			htmlDoc.LoadHtml(rawHtml.Trim());
			var companyNodes = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'appRepeaterRowContent')]");

			if (companyNodes == null || !companyNodes.Any())
			{
				_logger.Warning("BR Search: No company nodes found in the provided HTML.");
				return Enumerable.Empty<Company>();
			}

			var parsedResult = companyNodes
				.Select(node => ParseCompanyNode(queryId, node))
				.Where(company => company != null && !((OnCompanyDetails)company.Data).IsArchived && !string.IsNullOrEmpty(((OnCompanyDetails)company.Data).Status));

			_logger.Information("BR Search: Formatted Search Result: {@ParsedResult}", parsedResult);

			return parsedResult;
		}

		private Company ParseCompanyNode(string queryId, HtmlNode node)
		{
			var companyData = node.SelectSingleNode(".//a[contains(@class, 'registerItemSearch-results-page-line-ItemBox-resultLeft-viewMenu')] | .//div[contains(@class, 'entityTypeTLabel')]")?.InnerText;
			var previousNames = node.SelectNodes(".//div[contains(@class, 'Name')]//span[contains(@class, 'appMinimalValue')]");

			return new Company
			{
				CompanyId = ExtractIdFromCompanyData(companyData),
				CompanyName = HttpUtility.HtmlDecode(ExtractValueFromCompanyData(companyData)),
				Data = new OnCompanyDetails
				{
					Type = node.SelectSingleNode(".//div[contains(@class, 'appRawText')]")?.InnerText?.Trim(),
					Address = node.SelectSingleNode(".//div[contains(@class, 'appAttrValue')]")?.InnerText?.Trim(),
					Status = node.SelectSingleNode(".//div[contains(@class, 'Status')]//span[contains(@class, 'appMinimalValue')]")?.InnerText?.Trim(),
					DateIncorporated = node.SelectSingleNode(".//div[contains(@class, 'RegistrationDate')]//span[contains(@class, 'appMinimalValue')]")?.InnerText?.Trim(),
					PreviousNames = previousNames?.Select(x => HttpUtility.HtmlDecode(x.InnerText)).ToList(),
					IsArchived = node.SelectSingleNode(".//div[contains(@class, 'registerItemSearch-results-page-line-ItemBox-resultLeft-archivedRecordText')]")?.InnerText?.Contains("archived") ?? false,
					QueryId = queryId
				}
			};
		}

		private static string ExtractIdFromCompanyData(string companyData)
		{
			if (string.IsNullOrEmpty(companyData)) return null;
			var startIndex = companyData.LastIndexOf("(") + 1;
			var length = companyData.LastIndexOf(")") - startIndex;
			return length > 0 ? companyData.Substring(startIndex, length).Trim() : null;
		}

		private static string ExtractValueFromCompanyData(string companyData)
		{
			if (string.IsNullOrEmpty(companyData)) return null;
			var endIndex = companyData.LastIndexOf("(");
			return endIndex > 0 ? companyData.Substring(0, endIndex).Trim() : null;
		}
	}

}
