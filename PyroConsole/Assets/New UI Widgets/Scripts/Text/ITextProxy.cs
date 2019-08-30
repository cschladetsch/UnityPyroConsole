namespace UIWidgets
{
	using UnityEngine;

	/// <summary>
	/// Text proxy interface.
	/// </summary>
	public interface ITextProxy
	{
		/// <summary>
		/// Gameobject.
		/// </summary>
		GameObject GameObject
		{
			get;
		}

		/// <summary>
		/// Text.
		/// </summary>
		string Text
		{
			get;
			set;
		}
	}
}