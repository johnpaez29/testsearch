namespace TestSearching
{
	public static class Constants
	{
		public const string OBR_SEARCH_URL = "https://www.appmybizaccount.gov.on.ca/onbis/master/entry.pub?applicationCode=onbis-master&businessService=registerItemSearch";
		public const string BC_REGISTRY_ACCOUNT_ID_HEADER = "Account-Id";
		public const string BC_REGISTRY_API_KEY_HEADER = "X-Apikey";
		public static readonly string FED_BASE_URL = "https://ised-isde.canada.ca/cc/lgcy/fdrlCrpSrch.html";
		public const string FED_DOMAIN_URL = "https://ised-isde.canada.ca/cc/lgcy/";
		public const int FED_SEARCH_LIMIT = 1;

		public const string DOWNLOAD_RECEIPT_DIRECTORY = "ReceiptDownloads";
		public const string ORDER_REGEX = "Client Reference \\/ Référence du client\\s+(\\d+)";
	}
}
