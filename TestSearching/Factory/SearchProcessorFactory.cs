using TestSearching.Entities;
using TestSearching.Interfaces;
using TestSearching.Processors;

namespace TestSearching.Factory
{
	public class SearchProcessorFactory : ISearchFactory<Company>
	{
		private readonly IServiceProvider _serviceProvider;
		public SearchProcessorFactory(IServiceProvider serviceProvider)
		{
			_serviceProvider = serviceProvider;
		}

		public ISearchProcessor<Company> Create(string provinceCode)
		{
			return provinceCode switch
			{
				"ON" => _serviceProvider.GetRequiredService<OnSearchProcessor>(),
				"BC" => _serviceProvider.GetRequiredService<BcSearchProcessor>(),
				_ => _serviceProvider.GetRequiredService<OnSearchProcessor>()
			};
		}
	}

}
