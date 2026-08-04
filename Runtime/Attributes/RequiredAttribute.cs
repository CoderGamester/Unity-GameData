using System;

namespace GameLovers.GameData
{
	/// <summary>
	/// Requires a non-null value; an empty string also fails.
	/// </summary>
	public class RequiredAttribute : ValidationAttribute
	{
		/// <inheritdoc />
		public override bool IsValid(object value, out string message)
		{
			if (value == null)
			{
				message = "Value is required";
				return false;
			}

			if (value is string s && string.IsNullOrEmpty(s))
			{
				message = "String value is required";
				return false;
			}

			message = null;
			return true;
		}
	}
}
