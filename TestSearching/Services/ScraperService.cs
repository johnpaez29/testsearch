using Microsoft.Playwright;
using Polly;
using Polly.Retry;
using Serilog;
using TestSearching.Interfaces;
using ILogger = Serilog.ILogger;

namespace TestSearching.Services
{
	public class ScraperService : IScraperService
	{
		private readonly IPlaywrightService _playwrightService;
		private readonly ILogger _logger;
		private readonly AsyncRetryPolicy _retryPolicy;
		private readonly ILambdaService _lambdaService;
		public ScraperService(IPlaywrightService playwrightService, ILambdaService lambdaService)
		{
			_lambdaService = lambdaService;
			_playwrightService = playwrightService;
			_logger = Log.ForContext<ScraperService>();

			_retryPolicy = Policy
				.Handle<Exception>()
				.WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(10),
					(exception, timeSpan, retryCount, context) =>
					{
						_logger.Warning("BR Search:  Retry {RetryCount} after {TimeSpan} due to {Exception}", retryCount, timeSpan, exception.Message);
					});
			_lambdaService = lambdaService;
		}


		public async ValueTask<string> GetPdfReceiptAsync(string transactionNumber, string receiptUrl)
		{
			var page = await _playwrightService.EnsurePageAsync();

			var downloadFolder = Path.Combine(Directory.GetCurrentDirectory(), Constants.DOWNLOAD_RECEIPT_DIRECTORY);
			var filePath = Path.Combine(downloadFolder, $"{transactionNumber}.pdf");

			try
			{
				_logger.Information("BR ON - Payment Receipt: Step 3: Navigating to download page");

				await _retryPolicy.ExecuteAsync(async () =>
				{
					await page.GotoAsync(receiptUrl, new PageGotoOptions() { Timeout = 60000 });
					await page.WaitForSelectorAsync(".appAttrDocumentDownloadLink", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible, Timeout = 30000 });

					_logger.Information("BR ON - Payment Receipt: Step 4: Navigation Completed");
				});

				var download = await page.RunAndWaitForDownloadAsync(async () =>
				{
					await page.ClickAsync(".appAttrDocumentDownloadLink");
				});

				await download.SaveAsAsync(filePath);

				_logger.Information("BR ON - Payment Receipt: Step 5: Pdf receipt was obtained : {FilePath} Transaction Id: {TransactionNumber} ", filePath, transactionNumber);
			}
			catch (Exception ex)
			{
				var bodyHtml = await page.Locator("body").InnerHTMLAsync();
				_logger.Error(ex, "BR ON - Payment Receipt:  Error occurred while scraping data for Transaction Id: {TransactionId}. Page Content: {PageContent}", transactionNumber, bodyHtml);

				var containsBotDetection = bodyHtml?.Contains("data-sitekey=\"ae73173b-7003-44e0-bc87-654d0dab8b75\"");

				if (containsBotDetection is true)
				{
					_logger.Error("BR ON - Payment Receipt: Possible Bot detection was found.");
				}
			}

			await _playwrightService.DisposeAsync();
			return filePath;
		}


		public async ValueTask<(string, string)> GetBusinessInformationAsync(string companyName)
		{
			string pageUrl = null;
			string pageContent = null;

			var page = await _playwrightService.EnsurePageAsync();

			try
			{
				await _retryPolicy.ExecuteAsync(async () =>
				{
					await page.GotoAsync(Constants.OBR_SEARCH_URL, new PageGotoOptions { Timeout = 30000 });
					await AddDelaytoPage();
				});

				// Expects page to have a heading with the name of Installation.
				_logger.Information("BR Search:  Searching {CompanyName}", companyName);
				await page.GetByLabel("Search For Entity Name or OCN/BIN").FillAsync(companyName);
				await AddDelaytoPage();

				await page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync();

				var noResults = await page.Locator(".appSearchNoResults").IsVisibleAsync();

				if (noResults)
				{
					_logger.Warning("BR Search:  No results found for {CompanyName}", companyName);
					return (string.Empty, string.Empty);
				}


				await page.WaitForSelectorAsync(".appRepeaterRowContent", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
				await AddDelaytoPage();

				pageUrl = page.Url;
				pageContent = await page.Locator(".registerItemSearch-results-page > .appRepeaterContent").InnerHTMLAsync();

				_logger.Information("BR Search: Raw results obtained for query {CompanyName}", companyName);
			}
			catch (Exception ex)
			{
				var bodyHtml = await page.Locator("body").InnerHTMLAsync();
				_logger.Error(ex, "BR Search:  Error occurred while scraping data for {CompanyName}. Exception: {Exception}. Page Content: {PageContent}", companyName, ex.Message, bodyHtml);

				var containsBotDetection = bodyHtml?.Contains("data-sitekey=\"ae73173b-7003-44e0-bc87-654d0dab8b75\"");

				if (containsBotDetection is true)
				{
					_logger.Error("BR Search: Possible Bot detection was found.");
				}
			}

			await _playwrightService.DisposeAsync();
			return (pageUrl, pageContent);
		}

		private async Task AddDelaytoPage()
		{
			var delay = await _playwrightService.SimulateHumanDelay();
			_logger.Information("BR Search: Adding {Delay} ms to execution.", delay);
		}
	}

}
