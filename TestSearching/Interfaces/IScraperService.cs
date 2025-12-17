namespace TestSearching.Interfaces
{
	public interface IScraperService
	{
		ValueTask<string> GetPdfReceiptAsync(string transactionNumber, string receiptUrl);
		ValueTask<(string, string)> GetBusinessInformationAsync(string companyName);
	}
}
