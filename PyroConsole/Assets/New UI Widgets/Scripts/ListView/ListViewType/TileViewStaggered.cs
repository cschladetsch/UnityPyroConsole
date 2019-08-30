namespace UIWidgets
{
	using System.Collections.Generic;
	using EasyLayoutNS;
	using UIWidgets.Styles;
	using UnityEngine;
	using UnityEngine.UI;

	/// <content>
	/// Base class for custom ListViews.
	/// </content>
	public partial class ListViewCustom<TComponent, TItem> : ListViewBase, IStylable
		where TComponent : ListViewItem
	{
		/// <summary>
		/// ListView renderer with staggered layout.
		/// </summary>
		protected class TileViewStaggered : ListViewTypeSize
		{
			class ItemPositionComparer : IComparer<TileViewStaggeredItemPosition>
			{
				public int Compare(TileViewStaggeredItemPosition x, TileViewStaggeredItemPosition y)
				{
					var pos_compare = x.Position.CompareTo(y.Position);
					if (pos_compare != 0)
					{
						return pos_compare;
					}

					return x.Block.CompareTo(y.Block);
				}
			}

			/// <summary>
			/// Block.
			/// Contains list of indices in the block.
			/// </summary>
			class Block : List<int>
			{
			}

			/// <summary>
			/// List of blocks.
			/// </summary>
			List<Block> BlocksIndices = new List<Block>();

			/// <summary>
			/// Blocks count.
			/// </summary>
			int Blocks;

			List<float> BlocksFullSizes = new List<float>();

			List<float> LayoutPaddingStart;

			List<float> LayoutPaddingEnd;

			SortedList<TileViewStaggeredItemPosition, int> OrderedIndices = new SortedList<TileViewStaggeredItemPosition, int>(new ItemPositionComparer());

			/// <summary>
			/// Initializes a new instance of the <see cref="TileViewStaggered"/> class.
			/// </summary>
			/// <param name="owner">Owner.</param>
			public TileViewStaggered(ListViewCustom<TComponent, TItem> owner)
				: base(owner)
			{
				if (Owner.Layout == null)
				{
					Debug.LogWarning("TileViewStaggered requires Container.EasyLayout component.", Owner);
					return;
				}

				Owner.Layout.LayoutType = LayoutTypes.Staggered;
				LayoutPaddingStart = Owner.Layout.StaggeredSettings.PaddingInnerStart;
				LayoutPaddingEnd = Owner.Layout.StaggeredSettings.PaddingInnerEnd;
			}

			/// <summary>
			/// Process ListView direction changed.
			/// </summary>
			public override void DirectionChanged()
			{
				Owner.Layout.MainAxis = Owner.IsHorizontal() ? Axis.Horizontal : Axis.Vertical;
			}

			/// <summary>
			/// Is looped list allowed?
			/// </summary>
			/// <returns>True if looped list allowed; otherwise false.</returns>
			public override bool IsTileView
			{
				get
				{
					return true;
				}
			}

			/// <summary>
			/// Determines whether this instance can be virtualized.
			/// </summary>
			/// <returns><c>true</c> if this instance can be virtualized; otherwise, <c>false</c>.</returns>
			public override bool IsVirtualizationSupported()
			{
				var scrollRectSpecified = Owner.scrollRect != null;
				var containerSpecified = Owner.Container != null;
				var currentLayout = containerSpecified ? ((Owner.layout != null) ? Owner.layout : Owner.Container.GetComponent<LayoutGroup>()) : null;
				var validLayout = currentLayout is EasyLayout;

				return scrollRectSpecified && validLayout;
			}

			/// <summary>
			/// Calculates the maximum count of the visible items.
			/// </summary>
			public override void CalculateMaxVisibleItems()
			{
				CalculateItemsSizes(Owner.DataSource, false);

				MaxVisibleItems = CalculateMaxVisibleItems(Owner.DataSource);
			}

			/// <summary>
			/// Calculates the maximum count of the visible items.
			/// </summary>
			/// <param name="items">Items.</param>
			/// <returns>Maximum count of the visible items.</returns>
			protected override int CalculateMaxVisibleItems(ObservableList<TItem> items)
			{
				var spacing_x = Owner.GetItemSpacingX();
				var spacing_y = Owner.GetItemSpacingY();

				var height = Owner.ScrollRectSize.y + spacing_y - Owner.LayoutBridge.GetFullMarginY();
				var width = Owner.ScrollRectSize.x + spacing_x - Owner.LayoutBridge.GetFullMarginX();

				Blocks = Owner.IsHorizontal()
					? Mathf.FloorToInt(height / (Owner.ItemSize.y + spacing_y))
					: Mathf.FloorToInt(width / (Owner.ItemSize.x + spacing_x));

				Blocks = Mathf.Max(1, Blocks);
				Blocks = Owner.LayoutBridge.BlocksConstraint(Blocks);

				CalculateBlocksIndices();

				var max_items_per_block = Owner.IsHorizontal()
					? Mathf.FloorToInt(height / (LowestHeight() + spacing_y)) + 1
					: Mathf.FloorToInt(width / (LowestWidth() + spacing_x)) + 1;
				max_items_per_block = Mathf.Max(max_items_per_block, MinVisibleItems);

				return Blocks * max_items_per_block;
			}

			float LowestWidth()
			{
				if (ItemSizes.Count == 0)
				{
					return 1f;
				}

				var result = 0f;
				var is_first = true;

				foreach (var size in ItemSizes.Values)
				{
					if (is_first)
					{
						result = size.x;
						is_first = false;
					}
					else
					{
						result = Mathf.Min(result, size.x);
					}
				}

				return Mathf.Max(1f, result);
			}

			float LowestHeight()
			{
				if (ItemSizes.Count == 0)
				{
					return 1f;
				}

				var result = 0f;
				var is_first = true;

				foreach (var size in ItemSizes.Values)
				{
					if (is_first)
					{
						result = size.y;
						is_first = false;
					}
					else
					{
						result = Mathf.Min(result, size.y);
					}
				}

				return Mathf.Max(1f, result);
			}

			static void EnsureListSize<T>(List<T> list, int size)
			{
				for (int i = list.Count; i < size; i++)
				{
					list.Add(default(T));
				}
			}

			int NextBlockIndex()
			{
				var index = 0;
				var min_size = BlocksFullSizes[0];

				for (int i = 1; i < BlocksFullSizes.Count; i++)
				{
					if (BlocksFullSizes[i] < min_size)
					{
						index = i;
						min_size = BlocksFullSizes[i];
					}
				}

				return index;
			}

			void InsertToBlock(int block_index, int index)
			{
				var spacing = Owner.LayoutBridge.GetSpacing();

				for (int i = BlocksIndices.Count; i <= block_index; i++)
				{
					BlocksIndices.Add(new Block());
				}

				var row = BlocksIndices[block_index];
				row.Add(index);

				if (row.Count > 1)
				{
					BlocksFullSizes[block_index] += spacing;
				}

				OrderedIndices.Add(new TileViewStaggeredItemPosition(block_index, BlocksFullSizes[block_index]), index);

				BlocksFullSizes[block_index] += GetItemSize(index);
			}

			/// <summary>
			/// Calculate block indices.
			/// </summary>
			protected void CalculateBlocksIndices()
			{
				BlocksFullSizes.Clear();

				EnsureListSize(BlocksFullSizes, Blocks);

				BlocksIndices.Clear();
				OrderedIndices.Clear();

				for (int i = 0; i < Owner.DataSource.Count; i++)
				{
					InsertToBlock(NextBlockIndex(), i);
				}
			}

			/// <summary>
			/// Calculates the size of the top filler.
			/// </summary>
			/// <returns>The top filler size.</returns>
			public override float CalculateTopFillerSize()
			{
				Owner.Layout.StaggeredSettings.PaddingInnerStart = LayoutPaddingStart;

				return 0f;
			}

			/// <summary>
			/// Calculates the size of the bottom filler.
			/// </summary>
			/// <returns>The bottom filler size.</returns>
			public override float CalculateBottomFillerSize()
			{
				Owner.Layout.StaggeredSettings.PaddingInnerEnd = LayoutPaddingEnd;

				return 0f;
			}

			/// <summary>
			/// Get block index by item index.
			/// </summary>
			/// <param name="index">Item index.</param>
			/// <returns>Block index.</returns>
			protected override int GetBlockIndex(int index)
			{
				for (int i = 0; i < BlocksIndices.Count; i++)
				{
					if (BlocksIndices[i].Contains(index))
					{
						return i;
					}
				}

				return 0;
			}

			/// <summary>
			/// Gets the position of the start border of the item.
			/// </summary>
			/// <returns>The position.</returns>
			/// <param name="index">Index.</param>
			public override float GetItemPosition(int index)
			{
				var block = BlocksIndices[GetBlockIndex(index)];

				var size = 0f;
				var spacing = Owner.LayoutBridge.GetSpacing();
				for (int i = 0; i < block.Count; i++)
				{
					if (block[i] == index)
					{
						break;
					}

					size += GetItemSize(block[i]) + spacing;
				}

				if (size > 0)
				{
					size -= spacing;
				}

				return size + Owner.LayoutBridge.GetMargin();
			}

			/// <summary>
			/// Gets the position to display item at the center of the ScrollRect viewport.
			/// </summary>
			/// <returns>The position.</returns>
			/// <param name="index">Index.</param>
			public override float GetItemPositionMiddle(int index)
			{
				var start = GetItemPosition(index);
				var end = GetItemPositionBottom(index);
				return start + ((end - start) / 2);
			}

			/// <summary>
			/// Gets the position to display item at the bottom of the ScrollRect viewport.
			/// </summary>
			/// <returns>The position.</returns>
			/// <param name="index">Index.</param>
			public override float GetItemPositionBottom(int index)
			{
				var block = BlocksIndices[GetBlockIndex(index)];

				var size = 0f;
				var spacing = Owner.LayoutBridge.GetSpacing();
				for (int i = 0; i < block.Count; i++)
				{
					size += GetItemSize(block[i]) + spacing;

					if (block[i] == index)
					{
						break;
					}
				}

				if (size > 0)
				{
					size -= spacing;
				}

				return size + Owner.LayoutBridge.GetMargin() - Owner.GetScrollRectSize();
			}

			int GetIndexAtBlock(float size, int blockIndex)
			{
				var spacing = Owner.LayoutBridge.GetSpacing();
				var block = BlocksIndices[blockIndex];
				var index = block[0];
				size -= GetItemSize(index);

				if (size >= 0f)
				{
					for (var i = 1; i < block.Count; i++)
					{
						size -= GetItemSize(block[i]) + spacing;

						if (size < 0)
						{
							break;
						}

						index = block[i];
					}
				}

				return index;
			}

			/// <summary>
			/// Gets the first index of the visible.
			/// </summary>
			/// <returns>The first visible index.</returns>
			/// <param name="strict">If set to <c>true</c> strict.</param>
			public override int GetFirstVisibleIndex(bool strict = false)
			{
				return GetIndexAtBlock(Owner.GetScrollValue(), 0);
			}

			/// <summary>
			/// Gets the last index of the visible.
			/// </summary>
			/// <returns>The last visible index.</returns>
			/// <param name="strict">If set to <c>true</c> strict.</param>
			public override int GetLastVisibleIndex(bool strict = false)
			{
				var last_visible_index = GetIndexAtBlock(Owner.GetScrollValue() + Owner.GetScrollRectSize(), BlocksIndices.Count - 1);

				return last_visible_index;
			}

			List<int> newDisplayedIndices = new List<int>();

			/// <summary>
			/// Update displayed indices.
			/// </summary>
			/// <returns>true if displayed indices changed; otherwise false.</returns>
			public override bool UpdateDisplayedIndices()
			{
				newDisplayedIndices.Clear();

				LayoutPaddingStart.Clear();
				LayoutPaddingEnd.Clear();

				EnsureListSize(LayoutPaddingStart, Blocks);
				EnsureListSize(LayoutPaddingEnd, Blocks);

				var start = Owner.GetScrollValue();
				var end = Owner.GetScrollValue() + Owner.GetScrollRectSize();
				var spacing = Owner.LayoutBridge.GetSpacing();

				foreach (var kv in OrderedIndices)
				{
					var info = kv.Key;
					var index = kv.Value;
					var size = GetItemSize(index);
					var item_end = info.Position + size;

					if (item_end < start)
					{
						LayoutPaddingStart[info.Block] = info.Position + size + spacing;
					}
					else if (info.Position > end)
					{
						LayoutPaddingEnd[info.Block] += size + spacing;
					}
					else
					{
						newDisplayedIndices.Add(index);
					}
				}

				if (Owner.DisplayedIndices == newDisplayedIndices)
				{
					return false;
				}

				Owner.DisplayedIndices.Clear();
				Owner.DisplayedIndices.AddRange(newDisplayedIndices);
				Owner.Layout.NeedUpdateLayout();

				return true;
			}

			static string ListToString<T>(List<T> list)
			{
				var result = string.Empty;
				for (int i = 0; i < list.Count; i++)
				{
					result += i + ": " + list[i] + "; ";
				}

				return result;
			}

			/// <summary>
			/// Gets the index of the nearest item.
			/// </summary>
			/// <returns>The nearest item index.</returns>
			/// <param name="point">Point.</param>
			public override int GetNearestIndex(Vector2 point)
			{
				if (Owner.IsSortEnabled())
				{
					return -1;
				}

				var spacing = Owner.IsHorizontal() ? Owner.LayoutBridge.GetSpacingY() : Owner.LayoutBridge.GetSpacingX();
				var size = Owner.IsHorizontal() ? Owner.ItemSize.y : Owner.ItemSize.x;

				var block_pos = Owner.IsHorizontal() ? -point.y : point.x;
				var item_pos = Owner.IsHorizontal() ? point.x : -point.y;
				var block_index = (block_pos < size) ? 0 : Mathf.FloorToInt((block_pos - size) / (size + spacing)) + 1;

				return GetIndexAtBlock(item_pos, block_index);
			}

			/// <summary>
			/// Count of items the per block.
			/// </summary>
			/// <returns>The per block.</returns>
			public override int GetItemsPerBlock()
			{
				return Blocks;
			}

			/// <summary>
			/// Get the size of the ListView.
			/// </summary>
			/// <returns>The size.</returns>
			public override float ListSize()
			{
				if (Owner.DataSource.Count == 0)
				{
					return 0;
				}

				var max = BlocksFullSizes[0];
				for (int i = 1; i < BlocksFullSizes.Count; i++)
				{
					max = Mathf.Max(max, BlocksFullSizes[i]);
				}

				return max;
			}
		}
	}
}