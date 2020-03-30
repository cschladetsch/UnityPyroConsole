#if UIWIDGETS_TMPRO_SUPPORT
namespace UIWidgets
{
	using UnityEngine;

	/// <summary>
	/// TextTMPro.
	/// </summary>
	public class TextTMPro : ITextProxy
	{
		/// <summary>
		/// Text component.
		/// </summary>
		protected TMPro.TextMeshProUGUI Component;

		/// <summary>
		/// GameObject.
		/// </summary>
		public GameObject GameObject
		{
			get
			{
				return Component.gameObject;
			}
		}

		/// <summary>
		/// Text.
		/// </summary>
		public string Text
		{
			get
			{
				return Component.text;
			}

			set
			{
				Component.text = value;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TextTMPro"/> class.
		/// </summary>
		/// <param name="component">Component.</param>
		public TextTMPro(TMPro.TextMeshProUGUI component)
		{
			Component = component;
		}
	}
}
#endif