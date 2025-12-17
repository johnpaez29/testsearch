using TestSearching.Comon;
using TestSearching.Entities;
using TestSearching.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace TestSearching.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class PdfController(ISearchFactory<Company> _searchProvideFactory) : ControllerBase
	{
		/// <summary>
		/// Gets a list of companies.
		/// </summary>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A List Of companies.</returns>
		[Microsoft.AspNetCore.Mvc.HttpGet()]
		public async Task<IActionResult> Get([FromQuery] string transaction,
			CancellationToken cancellationToken = default)
		{

			try
			{
				await Handle(transaction, cancellationToken);
				return Ok(new ApiResponse<string>("pdf stored succesfully", true, null, null));
			}
			catch (System.Exception ex)
			{
				return NotFound(new ApiResponse<IEnumerable<CompanyDto>>(null, false, ex.Message, ex.StackTrace));
			}

		}

		private async Task Handle(string request, CancellationToken cancellationToken)
		{
			var searchProcessor = _searchProvideFactory.Create(request);

			await searchProcessor.GetPadfAsync(request, "https://www.obrpartner.mgcs.gov.on.ca/onbis/payment/viewInstance/view.pub?id=8ebd952f903c51456e608b33ecacfd26c9ce67e8a0182329&_timestamp=1828605121415693&targetAppCode=onbis-payment");
		}
	}
}
