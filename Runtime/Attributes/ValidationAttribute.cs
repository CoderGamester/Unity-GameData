using System;

namespace GameLovers.GameData
{
	/// <summary>
	/// Base class for config-field validation attributes evaluated by the Config Browser's
	/// validation pass.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
	public abstract class ValidationAttribute : Attribute
	{
		/// <summary>
		/// Returns whether <paramref name="value"/> passes, and the message to show when it does not.
		/// </summary>
		public abstract bool IsValid(object value, out string message);
	}
}
