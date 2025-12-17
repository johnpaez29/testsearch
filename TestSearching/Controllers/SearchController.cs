using TestSearching.Comon;
using TestSearching.Queries;
using TestSearching.Entities;
using TestSearching.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace TestSearching.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController(ISearchFactory<Company> _searchProvideFactory) : ControllerBase
    {
        /// <summary>
        /// Gets a list of companies.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A List Of companies.</returns>
        [Microsoft.AspNetCore.Mvc.HttpGet("{province}")]
        public async Task<IActionResult> Get(
            string province,
            [FromQuery] string term,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<CompanyDto> result = null;

            try
            {
                var companySearchQuery = new CompanySearchQuery()
                {
                    ProvinceCode = province,
                    SearchTerm = term
                };

                result = await Handle(companySearchQuery, cancellationToken);
                return Ok(new ApiResponse<IEnumerable<CompanyDto>>(result, true, null, null));
            }
            catch (System.Exception ex)
            {
                return NotFound(new ApiResponse<IEnumerable<CompanyDto>>(null, false, ex.Message, ex.StackTrace));
            }

        }

		private async Task<IEnumerable<CompanyDto>> Handle(CompanySearchQuery request, CancellationToken cancellationToken)
		{
			var searchProcessor = _searchProvideFactory.Create(request.ProvinceCode);
			var searchResult = await searchProcessor.SearchAsync(request.SearchTerm, cancellationToken);
			return searchResult.Select(sr => new CompanyDto 
            {
                CompanyId = sr.CompanyId,
                CompanyName = sr.CompanyName,
                Data = sr.Data,
            });
		}
	}
}
