using System;
using System.Collections;

namespace GameLovers.GameData
{
	/// <summary>
	/// Requires a string or collection of at least the given length.
	/// </summary>
	public class MinLengthAttribute : ValidationAttribute
	{
		private readonly int _minLength;

		public MinLengthAttribute(int minLength)
		{
			_minLength = minLength;
		}

		/// <inheritdoc />
		public override bool IsValid(object value, out string message)
		{
			if (value == null)
			{
				if (_minLength > 0)
				{
					message = $"Value is null but minimum length {_minLength} is required";
					return false;
				}
				message = null;
				return true;
			}

			int length = 0;
			if (value is string s) length = s.Length;
			else if (value is ICollection collection) length = collection.Count;
			else if (value is IEnumerable enumerable)
			{
				var enumerator = enumerable.GetEnumerator();
				while (enumerator.MoveNext()) length++;
			}

			if (length < _minLength)
			{
				message = $"Length {length} is less than minimum length {_minLength}";
				return false;
			}

			message = null;
			return true;
		}
	}
}
