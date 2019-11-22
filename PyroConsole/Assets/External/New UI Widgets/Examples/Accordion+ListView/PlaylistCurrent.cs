namespace UIWidgets.Examples
{
	using UnityEngine;

	/// <summary>
	/// Index of the currently playing item in the play-list.
	/// </summary>
	public class PlaylistCurrent : MonoBehaviour
	{
		int index = -1;

		/// <summary>
		/// Index.
		/// </summary>
		public int Index
		{
			get
			{
				return index;
			}

			set
			{
				if (index != value)
				{
					index = value;

					OnChange.Invoke(index);
				}
			}
		}

		/// <summary>
		/// OnChange event.
		/// </summary>
		public PlaylistCurrentEvent OnChange = new PlaylistCurrentEvent();
	}
}