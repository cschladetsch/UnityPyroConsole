namespace UIWidgets
{
	using System;
	using UnityEngine;

	/// <summary>
	/// Scroller item.
	/// </summary>
	public class ScrollerItem : MonoBehaviour
	{
		/// <summary>
		/// Index of the item.
		/// </summary>
		[NonSerialized]
		public int Index;

		/// <summary>
		/// Text adapter.
		/// </summary>
		[SerializeField]
		public TextAdapter Text;

		/// <summary>
		/// Owner.
		/// </summary>
		[NonSerialized]
		public Scroller Owner;
	}
}