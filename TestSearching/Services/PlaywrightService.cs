using Microsoft.Playwright;
using PlaywrightExtraSharp;
using PlaywrightExtraSharp.Models;
using PlaywrightExtraSharp.Plugins.ExtraStealth;
using Serilog;
using TestSearching.Entities;
using TestSearching.Interfaces;
using ILogger = Serilog.ILogger;

namespace TestSearching.Services
{
	public class PlaywrightService(ILambdaService lambdaService) : IPlaywrightService
	{
		private IBrowser _browser;
		private IBrowserContext _context;
		private ILogger _logger = Log.ForContext<PlaywrightService>();
		private IPlaywright _playwright;

		private bool _persistenceEnabled = false;
		private string _persistencePath = "state.json";

		private Proxy _proxy;

		public IPage Page { get; private set; }
		public bool IsPageCrashed => Page?.IsClosed ?? true;

		public void EnablePersistence(string path = "state.json")
		{
			_persistenceEnabled = true;
			_persistencePath = path;
		}

		public void DisablePersistence() => _persistenceEnabled = false;

		public async Task InitializeAsync()
		{
			try
			{
				await GetProxy();
				_playwright = await Playwright.CreateAsync();

				var extra = new PlaywrightExtra(BrowserTypeEnum.Chromium)
					.Use(new StealthExtraPlugin()); 

				_browser = await extra.LaunchAsync(new BrowserTypeLaunchOptions
				{
					Headless = false,
					Proxy = _proxy
				});

				await ResetPageAsync();
			}
			catch (Exception e)
			{
				Console.WriteLine("Playwright init error: " + e.Message);
				if (e.InnerException != null)
					Console.WriteLine("Inner: " + e.InnerException.Message);
			}
		}

		private async Task GetProxy()
		{
			var proxyResponse = await lambdaService.GetIpProxy();

			if (proxyResponse == null)
			{
				_logger.Error("Proxy is null");
				throw new Exception("Proxy is null");
			}

			_proxy = new Proxy
			{
				Server = $"{proxyResponse.ProxyIp}:{proxyResponse.ProxyPort}"
			};
		}
		public async Task<IPage> EnsurePageAsync()
		{
			if (Page == null || Page.IsClosed)
				await ResetPageAsync();

			return Page;
		}

		public async Task ResetPageAsync()
		{
			_context = await _browser.NewContextAsync(new BrowserNewContextOptions
			{
				Proxy = _proxy,
				BypassCSP = true
			});

			Page = await _context.NewPageAsync();

			await SimulateHumanWarmUpAsync();
		}

		public async Task<int> SimulateHumanDelay()
		{
			var rnd = new Random();
			var randomDelay = rnd.Next(500, 1500);
			await Page.Mouse.MoveAsync(rnd.Next(400, 800), rnd.Next(300, 600));
			await Task.Delay(randomDelay);
			return randomDelay;
		}

		private async Task SimulateHumanWarmUpAsync()
		{
			var rnd = new Random();
			await Page.Mouse.MoveAsync(rnd.Next(50, 400), rnd.Next(50, 300));
			await Task.Delay(rnd.Next(200, 500));
			await Page.Mouse.MoveAsync(rnd.Next(400, 800), rnd.Next(300, 600));
		}

		public async Task<IReadOnlyList<BrowserContextCookiesResult>> GetCookiesAsync()
			=> await _context.CookiesAsync();

		public async Task SetCookiesAsync(IEnumerable<Cookie> cookies)
			=> await _context.AddCookiesAsync(cookies);

		public async Task DisposeAsync()
		{
			_ = lambdaService.FinishInstanceProxy();
			if (_persistenceEnabled && _context != null)
			{
				var state = await _context.StorageStateAsync();
				await File.WriteAllTextAsync(_persistencePath, state);
			}

			try { if (Page is { IsClosed: false }) await Page.CloseAsync(); } catch { }
			try { await _context?.CloseAsync(); } catch { }
			try { await _browser?.CloseAsync(); } catch { }
			try { _playwright?.Dispose(); } catch { }
		}
	}
}
