namespace UIWidgets
{
	using System;
	using System.Collections.Generic;
	using EasyLayoutNS;
	using UIWidgets.Styles;
	using UnityEngine;
	using UnityEngine.EventSystems;
	using UnityEngine.UI;

	/// <content>
	/// Base class for custom ListViews.
	/// </content>
	public partial class ListViewCustom<TComponent, TItem> : ListViewBase, IStylable
		where TComponent : ListViewItem
	{
		/// <summary>
		/// Base class for the ListView renderer.
		/// </summary>
		protected abstract class ListViewTypeBase
		{
			/// <summary>
			/// Visibility data.
			/// </summary>
			protected struct Visibility
			{
				/// <summary>
				/// First visible index.
				/// </summary>
				public int FirstVisible;

				/// <summary>
				/// Count of the visible items.
				/// </summary>
				public int Items;

				/// <summary>
				/// Last visible index.
				/// </summary>
				public int LastVisible
				{
					get
					{
						return FirstVisible + Items;
					}
				}

				/// <summary>
				/// Determines whether the specified object is equal to the current object.
				/// </summary>
				/// <param name="obj">The object to compare with the current object.</param>
				/// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
				public override bool Equals(object obj)
				{
					if (obj is Visibility)
					{
						return Equals((Visibility)obj);
					}

					return false;
				}

				/// <summary>
				/// Determines whether the specified object is equal to the current object.
				/// </summary>
				/// <param name="other">The object to compare with the current object.</param>
				/// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
				public bool Equals(Visibility other)
				{
					return FirstVisible == other.FirstVisible && Items == other.Items;
				}

				/// <summary>
				/// Hash function.
				/// </summary>
				/// <returns>A hash code for the current object.</returns>
				public override int GetHashCode()
				{
					return FirstVisible ^ Items;
				}

				/// <summary>
				/// Compare specified visibility data.
				/// </summary>
				/// <param name="obj1">First data.</param>
				/// <param name="obj2">Second data.</param>
				/// <returns>true if the data are equal; otherwise, false.</returns>
				public static bool operator ==(Visibility obj1, Visibility obj2)
				{
					return Equals(obj1, obj2);
				}

				/// <summary>
				/// Compare specified visibility data.
				/// </summary>
				/// <param name="obj1">First data.</param>
				/// <param name="obj2">Second data.</param>
				/// <returns>true if the data not equal; otherwise, false.</returns>
				public static bool operator !=(Visibility obj1, Visibility obj2)
				{
					return !Equals(obj1, obj2);
				}
			}

			/// <summary>
			/// Minimal count of the visible items.
			/// </summary>
			protected const int MinVisibleItems = 2;

			/// <summary>
			/// Maximal count of the visible items.
			/// </summary>
			public int MaxVisibleItems
			{
				get;
				protected set;
			}

			/// <summary>
			/// Visibility info.
			/// </summary>
			protected Visibility Visible;

			/// <summary>
			/// Owner.
			/// </summary>
			protected ListViewCustom<TComponent, TItem> Owner;

			/// <summary>
			/// Initializes a new instance of the <see cref="ListViewTypeBase"/> class.
			/// </summary>
			/// <param name="owner">Owner.</param>
			protected ListViewTypeBase(ListViewCustom<TComponent, TItem> owner)
			{
				Owner = owner;
			}

			/// <summary>
			/// Is looped list allowed?
			/// </summary>
			/// <returns>True if looped list allowed; otherwise false.</returns>
			public virtual bool IsTileView
			{
				get
				{
					return false;
				}
			}

			/// <summary>
			/// Calculates the maximum count of the visible items.
			/// </summary>
			public abstract void CalculateMaxVisibleItems();

			/// <summary>
			/// Calculates the size of the item.
			/// </summary>
			/// <param name="reset">Reset item size.</param>
			/// <returns>Item size.</returns>
			public virtual Vector2 GetItemSize(bool reset = false)
			{
				Owner.DefaultItem.gameObject.SetActive(true);

				var rt = Owner.DefaultItem.transform as RectTransform;

				Owner.LayoutElements.Clear();
				Compatibility.GetComponents<ILayoutElement>(Owner.DefaultItem.gameObject, Owner.LayoutElements);
				Owner.LayoutElements.Sort((x, y) => -x.layoutPriority.CompareTo(y.layoutPriority));

				var size = Owner.ItemSize;

				if ((size.x == 0f) || reset)
				{
					size.x = Mathf.Max(PreferredWidth(Owner.LayoutElements), rt.rect.width, 1f);
					if (float.IsNaN(size.x))
					{
						size.x = 1f;
					}
				}

				if ((size.y == 0f) || reset)
				{
					size.y = Mathf.Max(PreferredHeight(Owner.LayoutElements), rt.rect.height, 1f);
					if (float.IsNaN(size.y))
					{
						size.y = 1f;
					}
				}

				Owner.DefaultItem.gameObject.SetActive(false);

				return size;
			}

			static float PreferredHeight(List<ILayoutElement> elems)
			{
				if (elems.Count == 0)
				{
					return 0f;
				}

				var priority = elems[0].layoutPriority;
				var result = -1f;
				for (int i = 0; i < elems.Count; i++)
				{
					if ((result > -1f) && (elems[i].layoutPriority < priority))
					{
						break;
					}

					result = Mathf.Max(result, elems[i].minHeight, elems[i].preferredHeight);
					priority = elems[i].layoutPriority;
				}

				return result;
			}

			static float PreferredWidth(List<ILayoutElement> elems)
			{
				if (elems.Count == 0)
				{
					return 0f;
				}

				var priority = elems[0].layoutPriority;
				var result = -1f;
				for (int i = 0; i < elems.Count; i++)
				{
					if ((result > -1f) && (elems[i].layoutPriority < priority))
					{
						break;
					}

					result = Mathf.Max(result, elems[i].minWidth, elems[i].preferredWidth);
					priority = elems[i].layoutPriority;
				}

				return result;
			}

			/// <summary>
			/// Calculates the size of the top filler.
			/// </summary>
			/// <returns>The top filler size.</returns>
			public abstract float CalculateTopFillerSize();

			/// <summary>
			/// Calculates the size of the bottom filler.
			/// </summary>
			/// <returns>The bottom filler size.</returns>
			public abstract float CalculateBottomFillerSize();

			/// <summary>
			/// Gets the first index of the visible.
			/// </summary>
			/// <returns>The first visible index.</returns>
			/// <param name="strict">If set to <c>true</c> strict.</param>
			public abstract int GetFirstVisibleIndex(bool strict = false);

			/// <summary>
			/// Gets the last visible index.
			/// </summary>
			/// <returns>The last visible index.</returns>
			/// <param name="strict">If set to <c>true</c> strict.</param>
			public abstract int GetLastVisibleIndex(bool strict = false);

			/// <summary>
			/// Gets the position of the start border of the item.
			/// </summary>
			/// <returns>The position.</returns>
			/// <param name="index">Index.</param>
			public abstract float GetItemPosition(int index);

			/// <summary>
			/// Gets the position of the end border of the item.
			/// </summary>
			/// <returns>The position.</returns>
			/// <param name="index">Index.</param>
			public abstract float GetItemPositionBorderEnd(int index);

			/// <summary>
			/// Gets the position to display item at the center of the ScrollRect viewport.
			/// </summary>
			/// <returns>The position.</returns>
			/// <param name="index">Index.</param>
			public abstract float GetItemPositionMiddle(int index);

			/// <summary>
			/// Gets the position to display item at the bottom of the ScrollRect viewport.
			/// </summary>
			/// <returns>The position.</returns>
			/// <param name="index">Index.</param>
			public abstract float GetItemPositionBottom(int index);

			/// <summary>
			/// Calculate and sets the sizes of the items.
			/// </summary>
			/// <param name="items">Items.</param>
			/// <param name="forceUpdate">If set to <c>true</c> force update.</param>
			public virtual void CalculateItemsSizes(ObservableList<TItem> items, bool forceUpdate = true)
			{
			}

			/// <summary>
			/// Calculates the size of the component for the specified item.
			/// </summary>
			/// <returns>The component size.</returns>
			/// <param name="item">Item.</param>
			protected virtual Vector2 CalculateComponentSize(TItem item)
			{
				if (Owner.DefaultItemLayout == null)
				{
					return Owner.ItemSize;
				}

				Owner.SetData(Owner.DefaultItemCopy, item);

				Owner.DefaultItemNestedLayouts.ForEach(LayoutUtilites.UpdateLayout);

				LayoutUtilites.UpdateLayout(Owner.DefaultItemLayout);

				var size = new Vector2(
					LayoutUtility.GetPreferredWidth(Owner.DefaultItemCopyRect),
					LayoutUtility.GetPreferredHeight(Owner.DefaultItemCopyRect));

				return size;
			}

			/// <summary>
			/// Calculates the size of the component for the item with the specified index.
			/// </summary>
			/// <returns>The component size.</returns>
			/// <param name="index">Index.</param>
			public virtual Vector2 CalculateSize(int index)
			{
				Owner.DefaultItemCopy.gameObject.SetActive(true);

				if (Owner.DefaultItemLayout == null)
				{
					Owner.DefaultItemLayout = Owner.DefaultItemCopy.GetComponent<LayoutGroup>();

					Owner.DefaultItemNestedLayouts = Owner.DefaultItemCopy.GetComponentsInChildren<LayoutGroup>();
					Array.Reverse(Owner.DefaultItemNestedLayouts);
				}

				var size = CalculateComponentSize(Owner.DataSource[index]);

				Owner.DefaultItemCopy.gameObject.SetActive(false);

				return size;
			}

			/// <summary>
			/// Adds the callback.
			/// </summary>
			/// <param name="item">Item.</param>
			public virtual void AddCallback(ListViewItem item)
			{
			}

			/// <summary>
			/// Removes the callback.
			/// </summary>
			/// <param name="item">Item.</param>
			public virtual void RemoveCallback(ListViewItem item)
			{
			}

			/// <summary>
			/// Gets the index of the nearest item.
			/// </summary>
			/// <returns>The nearest item index.</returns>
			/// <param name="point">Point.</param>
			public abstract int GetNearestIndex(Vector2 point);

			/// <summary>
			/// Gets the index of the nearest item.
			/// </summary>
			/// <returns>The nearest item index.</returns>
			public abstract int GetNearestItemIndex();

			/// <summary>
			/// Get the size of the ListView.
			/// </summary>
			/// <returns>The size.</returns>
			public abstract float ListSize();

			/// <summary>
			/// Get block index by item index.
			/// </summary>
			/// <param name="index">Item index.</param>
			/// <returns>Block index.</returns>
			protected virtual int GetBlockIndex(int index)
			{
				return index;
			}

			/// <summary>
			/// Gets the items per block count.
			/// </summary>
			/// <returns>The items per block.</returns>
			public virtual int GetItemsPerBlock()
			{
				return 1;
			}

			/// <summary>
			/// Determines whether this instance can be virtualized.
			/// </summary>
			/// <returns><c>true</c> if this instance can virtualized; otherwise, <c>false</c>.</returns>
			public virtual bool IsVirtualizationSupported()
			{
				var scrollRectSpecified = Owner.scrollRect != null;
				var containerSpecified = Owner.Container != null;
				var currentLayout = containerSpecified ? ((Owner.layout != null) ? Owner.layout : Owner.Container.GetComponent<LayoutGroup>()) : null;
				var validLayout = currentLayout ? ((currentLayout is EasyLayout) || (currentLayout is HorizontalOrVerticalLayoutGroup)) : false;

				return scrollRectSpecified && validLayout;
			}

			/// <summary>
			/// Process the item move event.
			/// </summary>
			/// <param name="eventData">Event data.</param>
			/// <param name="item">Item.</param>
			public virtual void OnItemMove(AxisEventData eventData, ListViewItem item)
			{
				if (!Owner.Navigation)
				{
					return;
				}

				switch (eventData.moveDir)
				{
					case MoveDirection.Left:
						break;
					case MoveDirection.Right:
						break;
					case MoveDirection.Up:
						if (item.Index > 0)
						{
							Owner.SelectComponentByIndex(item.Index - 1);
						}

						break;
					case MoveDirection.Down:
						if (Owner.IsValid(item.Index + 1))
						{
							Owner.SelectComponentByIndex(item.Index + 1);
						}

						break;
				}
			}

			/// <summary>
			/// Validates the content size and item size.
			/// </summary>
			public virtual void ValidateContentSize()
			{
			}

			/// <summary>
			/// Get visibility data.
			/// </summary>
			/// <returns>Visibility data.</returns>
			protected virtual Visibility VisibilityData()
			{
				var visible = default(Visibility);

				visible.FirstVisible = GetFirstVisibleIndex();

				if (Owner.LoopedListAvailable)
				{
					visible.Items = Mathf.Min(MaxVisibleItems, Owner.DataSource.Count);
				}
				else if (IsVirtualizationSupported() && (Owner.DataSource.Count > 0))
				{
					visible.Items = Mathf.Min(MaxVisibleItems, Owner.DataSource.Count);

					if ((visible.FirstVisible + visible.Items) > Owner.DataSource.Count)
					{
						visible.Items = Owner.DataSource.Count - visible.FirstVisible;
						if (visible.Items < GetItemsPerBlock())
						{
							visible.Items = Mathf.Min(Owner.DataSource.Count, visible.Items + GetItemsPerBlock());
							visible.FirstVisible = Owner.DataSource.Count - visible.Items;
						}
					}
				}
				else
				{
					visible.FirstVisible = 0;
					visible.Items = Owner.DataSource.Count;
				}

				return visible;
			}

			/// <summary>
			/// Update displayed indices.
			/// </summary>
			/// <returns>true if displayed indices changed; otherwise false.</returns>
			public virtual bool UpdateDisplayedIndices()
			{
				var new_visible = VisibilityData();
				if (new_visible == Visible)
				{
					return false;
				}

				Visible = new_visible;

				Owner.DisplayedIndices.Clear();
				for (int i = Visible.FirstVisible; i < Visible.LastVisible; i++)
				{
					Owner.DisplayedIndices.Add(VisibleIndex2ItemIndex(i));
				}

				return true;
			}

			/// <summary>
			/// Convert visible index to item index.
			/// </summary>
			/// <returns>The item index.</returns>
			/// <param name="index">Visible index.</param>
			protected virtual int VisibleIndex2ItemIndex(int index)
			{
				return index % Owner.DataSource.Count;
			}

			/// <summary>
			/// Process ListView direction changed.
			/// </summary>
			public virtual void DirectionChanged()
			{
				if (Owner.Layout != null)
				{
					Owner.Layout.MainAxis = !Owner.IsHorizontal() ? Axis.Horizontal : Axis.Vertical;
				}
			}
		}
	}
}