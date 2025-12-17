using TestSearching.Entities;

namespace TestSearching.Interfaces
{
	public interface ILambdaService
	{
		public Task<ProxyResponse?> GetIpProxy();

		public Task FinishInstanceProxy();
	}
}
