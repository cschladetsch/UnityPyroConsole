namespace UIWidgets
{
	using UnityEngine;

	/// <summary>
	/// Text null.
	/// Used when not found any text component.
	/// </summary>
	public class TextNull : ITextProxy
	{
		/// <summary>
		/// GameObject.
		/// </summary>
		public GameObject GameObject
		{
			get
			{
				return null;
			}
		}

		/// <summary>
		/// Text.
		/// </summary>
		public string Text
		{
			get
			{
				return null;
			}

			set
			{
			}
		}
	}
}