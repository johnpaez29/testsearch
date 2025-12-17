using FluentValidation;
using TestSearching.Enums;

namespace TestSearching.Queries
{
	public class CompanySearchQueryValidator : AbstractValidator<CompanySearchQuery>
	{
		public CompanySearchQueryValidator()
		{

			RuleFor(RuleFor => RuleFor.ProvinceCode)
				.NotEmpty()
				.WithMessage("Province code is required.");

			RuleFor(RuleFor => RuleFor.ProvinceCode)
				.Must(cs => Enum.GetNames(typeof(Province)).Contains(cs))
				.WithMessage("Province code is not valid.");

			RuleFor(RuleFor => RuleFor.SearchTerm)
				.NotEmpty()
				.WithMessage("Search term is required.");

		}
	}
}
