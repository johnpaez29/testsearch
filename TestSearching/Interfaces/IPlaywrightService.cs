using Microsoft.Playwright;

namespace TestSearching.Interfaces
{
	public interface IPlaywrightService
	{
		Task InitializeAsync();
		Task<IPage> EnsurePageAsync();
		Task<int> SimulateHumanDelay();
		Task ResetPageAsync();
		Task DisposeAsync();
		void EnablePersistence(string path = "state.json");
		void DisablePersistence();
		Task<IReadOnlyList<BrowserContextCookiesResult>> GetCookiesAsync();
		Task SetCookiesAsync(IEnumerable<Cookie> cookies);
		IPage Page { get; }
		bool IsPageCrashed { get; }
	}
}
