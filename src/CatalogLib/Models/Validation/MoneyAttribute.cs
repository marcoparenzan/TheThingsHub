using System.ComponentModel.DataAnnotations;

namespace CatalogLib.Models.Validation;

internal class MoneyAttribute: ValidationAttribute
{
	public override bool IsValid(object? value)
	{
		if (value is decimal decimalValue)
		{ 
			return decimalValue > 0;
		}
		return false;
	}
}
