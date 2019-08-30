namespace EasyLayoutNS
{
	using System.Collections.Generic;
	using UnityEngine;

	/// <summary>
	/// Base class for EasyLayout groups.
	/// </summary>
	public abstract class EasyLayoutBaseType
	{
		/// <summary>
		/// Layuout.
		/// </summary>
		protected EasyLayout Layout;

		/// <summary>
		/// Grouped elements.
		/// </summary>
		protected List<List<LayoutElementInfo>> ElementsGroup = new List<List<LayoutElementInfo>>();

		/// <summary>
		/// Group position.
		/// </summary>
		protected static readonly List<Vector2> GroupPositions = new List<Vector2>()
		{
			new Vector2(0.0f, 1.0f), // Anchors.UpperLeft
			new Vector2(0.5f, 1.0f), // Anchors.UpperCenter
			new Vector2(1.0f, 1.0f), // Anchors.UpperRight

			new Vector2(0.0f, 0.5f), // Anchors.MiddleLeft
			new Vector2(0.5f, 0.5f), // Anchors.MiddleCenter
			new Vector2(1.0f, 0.5f), // Anchors.MiddleRight

			new Vector2(0.0f, 0.0f), // Anchors.LowerLeft
			new Vector2(0.5f, 0.0f), // Anchors.LowerCenter
			new Vector2(1.0f, 0.0f), // Anchors.LowerRight
		};

		/// <summary>
		/// Initializes a new instance of the <see cref="EasyLayoutBaseType"/> class.
		/// </summary>
		/// <param name="layout">Layout.</param>
		protected EasyLayoutBaseType(EasyLayout layout)
		{
			Layout = layout;
		}

		/// <summary>
		/// Perform layout.
		/// </summary>
		/// <param name="elements">Elements.</param>
		/// <param name="setPositions">Set elements positions.</param>
		/// <returns>Size of the group.</returns>
		public Vector2 PerformLayout(List<LayoutElementInfo> elements, bool setPositions)
		{
			SetInitialSizes(elements);
			Group(elements);
			CalculateSizes();
			var size = CalculateGroupSize();
			CalculatePositions(size);
			SetSizes();

			if (setPositions)
			{
				SetPositions();
			}

			return size;
		}

		/// <summary>
		/// Group elements.
		/// </summary>
		/// <param name="elements">Elements.</param>
		protected abstract void Group(List<LayoutElementInfo> elements);

		/// <summary>
		/// Calculate sizes of the elements.
		/// </summary>
		protected abstract void CalculateSizes();

		/// <summary>
		/// Calculate positions of the elements.
		/// </summary>
		/// <param name="size">Size.</param>
		protected abstract void CalculatePositions(Vector2 size);

		/// <summary>
		/// Calculate group size.
		/// </summary>
		/// <returns>Size.</returns>
		protected abstract Vector2 CalculateGroupSize();

		List<float> mainAxisSizes = new List<float>();

		/// <summary>
		/// Calculate size of the group.
		/// </summary>
		/// <param name="isHorizontal">ElementsGroup are in horizontal order?</param>
		/// <param name="spacing">Spacing.</param>
		/// <param name="padding">Padding,</param>
		/// <returns>Size.</returns>
		protected virtual Vector2 CalculateGroupSize(bool isHorizontal, Vector2 spacing, Vector2 padding)
		{
			var sub_axis_size = ((ElementsGroup.Count - 1) * spacing.y) + padding.y;
			for (int i = 0; i < ElementsGroup.Count; i++)
			{
				var row = ElementsGroup[i];
				var block_sub_size = 0f;
				for (var j = 0; j < row.Count; j++)
				{
					if (mainAxisSizes.Count == j)
					{
						mainAxisSizes.Add(0);
					}

					mainAxisSizes[j] = Mathf.Max(mainAxisSizes[j], isHorizontal ? row[j].Width : row[j].Height);
					block_sub_size = Mathf.Max(block_sub_size, isHorizontal ? row[j].Height : row[j].Width);
				}

				sub_axis_size += block_sub_size;
			}

			var main_axis_size = Sum(mainAxisSizes) + ((mainAxisSizes.Count - 1) * spacing.x) + padding.x;
			mainAxisSizes.Clear();

			return new Vector2(main_axis_size, sub_axis_size);
		}

		/// <summary>
		/// Set elements sizes.
		/// </summary>
		protected void SetSizes()
		{
			for (int i = 0; i < ElementsGroup.Count; i++)
			{
				var block = ElementsGroup[i];
				for (int j = 0; j < block.Count; j++)
				{
					Layout.SetElementSize(block[j]);
				}
			}
		}

		/// <summary>
		/// Set elements positions.
		/// </summary>
		protected void SetPositions()
		{
			for (int i = 0; i < ElementsGroup.Count; i++)
			{
				var block = ElementsGroup[i];
				for (int j = 0; j < block.Count; j++)
				{
					var elem = block[j];
					if (elem.IsPositionChanged)
					{
						elem.Rect.localPosition = elem.PositionPivot;
					}
				}
			}
		}

		/// <summary>
		/// Sum values of the list.
		/// </summary>
		/// <param name="list">List.</param>
		/// <returns>Sum.</returns>
		protected static float Sum(List<float> list)
		{
			var result = 0f;
			for (int i = 0; i < list.Count; i++)
			{
				result += list[i];
			}

			return result;
		}

		#region Group

		/// <summary>
		/// Reverse list.
		/// </summary>
		/// <param name="list">List.</param>
		protected static void ReverseList(List<LayoutElementInfo> list)
		{
			list.Reverse();
		}

		/// <summary>
		/// Group elements by columns in the vertical order.
		/// </summary>
		/// <param name="elements">Elements.</param>
		/// <param name="maxColumns">Maximum columns count.</param>
		protected void GroupByColumnsVertical(List<LayoutElementInfo> elements, int maxColumns)
		{
			int i = 0;
			for (int column = 0; column < maxColumns; column++)
			{
				int max_rows = Mathf.CeilToInt(((float)(elements.Count - i)) / ((float)(maxColumns - column)));
				for (int row = 0; row < max_rows; row++)
				{
					if (row == ElementsGroup.Count)
					{
						ElementsGroup.Add(new List<LayoutElementInfo>());
					}

					ElementsGroup[row].Add(elements[i]);
					i++;
				}
			}
		}

		/// <summary>
		/// Group elements by columns in the horizontal order.
		/// </summary>
		/// <param name="elements">Elements.</param>
		/// <param name="maxColumns">Maximum columns count.</param>
		protected void GroupByColumnsHorizontal(List<LayoutElementInfo> elements, int maxColumns)
		{
			for (int i = 0; i < elements.Count; i += maxColumns)
			{
				var column = new List<LayoutElementInfo>();
				var end = Mathf.Min(i + maxColumns, elements.Count);
				for (int j = i; j < end; j++)
				{
					column.Add(elements[j]);
				}

				ElementsGroup.Add(column);
			}
		}

		/// <summary>
		/// Group elements by rows in the vertical order.
		/// </summary>
		/// <param name="elements">Elements.</param>
		/// <param name="maxRows">Maximum rows count.</param>
		protected void GroupByRowsVertical(List<LayoutElementInfo> elements, int maxRows)
		{
			for (int i = 0; i < elements.Count; i++)
			{
				int row = i % maxRows;
				if (ElementsGroup.Count == row)
				{
					ElementsGroup.Add(new List<LayoutElementInfo>());
				}

				ElementsGroup[row].Add(elements[i]);
			}
		}

		/// <summary>
		/// Group elements by rows in the horizontal order.
		/// </summary>
		/// <param name="elements">Elements.</param>
		/// <param name="maxRows">Maximum rows count.</param>
		protected void GroupByRowsHorizontal(List<LayoutElementInfo> elements, int maxRows)
		{
			int i = 0;
			for (int row = 0; row < maxRows; row++)
			{
				ElementsGroup.Add(new List<LayoutElementInfo>());

				int max_columns = Mathf.CeilToInt((float)(elements.Count - i) / (float)(maxRows - row));
				for (int column = 0; column < max_columns; column++)
				{
					ElementsGroup[row].Add(elements[i]);
					i++;
				}
			}
		}

		/// <summary>
		/// Group the specified uiElements by columns.
		/// </summary>
		/// <param name="uiElements">User interface elements.</param>
		protected void GroupByColumns(List<LayoutElementInfo> uiElements)
		{
			if (Layout.IsHorizontal)
			{
				GroupByColumnsHorizontal(uiElements, Layout.ConstraintCount);
			}
			else
			{
				GroupByColumnsVertical(uiElements, Layout.ConstraintCount);
			}
		}

		/// <summary>
		/// Group the specified uiElements by rows.
		/// </summary>
		/// <param name="elements">User interface elements.</param>
		protected void GroupByRows(List<LayoutElementInfo> elements)
		{
			if (Layout.IsHorizontal)
			{
				GroupByRowsHorizontal(elements, Layout.ConstraintCount);
			}
			else
			{
				GroupByRowsVertical(elements, Layout.ConstraintCount);
			}
		}
		#endregion

		#region Sizes

		/// <summary>
		/// Resize elements.
		/// </summary>
		/// <param name="elements">Elements to resize.</param>
		protected virtual void SetInitialSizes(List<LayoutElementInfo> elements)
		{
			if (Layout.ChildrenWidth == ChildrenSize.DoNothing && Layout.ChildrenHeight == ChildrenSize.DoNothing)
			{
				return;
			}

			if (elements.Count == 0)
			{
				return;
			}

			var max_size = FindMaxSize(elements);
			for (int i = 0; i < elements.Count; i++)
			{
				SetInitialSize(elements[i], max_size);
			}
		}

		Vector2 FindMaxSize(List<LayoutElementInfo> elements)
		{
			var max_size = new Vector2(-1f, -1f);

			for (int i = 0; i < elements.Count; i++)
			{
				max_size.x = Mathf.Max(max_size.x, elements[i].PreferredWidth);
				max_size.y = Mathf.Max(max_size.y, elements[i].PreferredHeight);
			}

			if (Layout.ChildrenWidth != ChildrenSize.SetMaxFromPreferred)
			{
				max_size.x = -1f;
			}

			if (Layout.ChildrenHeight != ChildrenSize.SetMaxFromPreferred)
			{
				max_size.y = -1f;
			}

			return max_size;
		}

		void SetInitialSize(LayoutElementInfo element, Vector2 max_size)
		{
			if (Layout.ChildrenWidth != ChildrenSize.DoNothing)
			{
				element.NewWidth = (max_size.x != -1f) ? max_size.x : element.PreferredWidth;
			}

			if (Layout.ChildrenHeight != ChildrenSize.DoNothing)
			{
				element.NewHeight = (max_size.y != -1f) ? max_size.y : element.PreferredHeight;
			}
		}

		/// <summary>
		/// Resize elements width to fit.
		/// </summary>
		protected void ResizeWidthToFit()
		{
			var width = Layout.InternalSize.x;
			for (int i = 0; i < ElementsGroup.Count; i++)
			{
				ResizeToFit(width, ElementsGroup[i], Layout.Spacing.x, RectTransform.Axis.Horizontal);
			}
		}

		/// <summary>
		/// Resize specified elements to fit.
		/// </summary>
		/// <param name="size">Size.</param>
		/// <param name="elements">Elements.</param>
		/// <param name="spacing">Spacing.</param>
		/// <param name="axis">Axis to fit.</param>
		protected static void ResizeToFit(float size, List<LayoutElementInfo> elements, float spacing, RectTransform.Axis axis)
		{
			var sizes = axis == RectTransform.Axis.Horizontal ? SizesInfo.GetWidths(elements) : SizesInfo.GetHeights(elements);

			var free_space = size - sizes.TotalPreferred - ((elements.Count - 1) * spacing);
			var per_flexible = free_space / sizes.TotalFlexible;

			var minPrefLerp = 0f;
			if (sizes.TotalMin != sizes.TotalPreferred)
			{
				minPrefLerp = Mathf.Clamp01((size - sizes.TotalMin - ((elements.Count - 1) * spacing)) / (sizes.TotalPreferred - sizes.TotalMin));
			}

			for (int i = 0; i < elements.Count; i++)
			{
				var element_size = Mathf.Lerp(sizes.Sizes[i].Min, sizes.Sizes[i].Preferred, minPrefLerp) + (per_flexible * sizes.Sizes[i].Flexible);
				elements[i].SetSize(axis, element_size);
			}
		}

		/// <summary>
		/// Shrink elements width to fit.
		/// </summary>
		protected void ShrinkWidthToFit()
		{
			var width = Layout.InternalSize.x;
			for (int i = 0; i < ElementsGroup.Count; i++)
			{
				ShrinkToFit(width, ElementsGroup[i], Layout.Spacing.x, RectTransform.Axis.Horizontal);
			}
		}

		/// <summary>
		/// Resize row height to fit.
		/// </summary>
		protected void ResizeRowHeightToFit()
		{
			ResizeToFit(Layout.InternalSize.y, ElementsGroup, Layout.Spacing.y, RectTransform.Axis.Vertical);
		}

		/// <summary>
		/// Shrink row height to fit.
		/// </summary>
		protected void ShrinkRowHeightToFit()
		{
			ShrinkToFit(Layout.InternalSize.y, ElementsGroup, Layout.Spacing.y, RectTransform.Axis.Vertical);
		}

		/// <summary>
		/// Shrink specified elements size to fit.
		/// </summary>
		/// <param name="size">Size.</param>
		/// <param name="elements">Elements.</param>
		/// <param name="spacing">Spacing.</param>
		/// <param name="axis">Axis to fit.</param>
		protected static void ShrinkToFit(float size, List<LayoutElementInfo> elements, float spacing, RectTransform.Axis axis)
		{
			var sizes = axis == RectTransform.Axis.Horizontal ? SizesInfo.GetWidths(elements) : SizesInfo.GetHeights(elements);

			float free_space = size - sizes.TotalPreferred - ((elements.Count - 1) * spacing);
			if (free_space > 0f)
			{
				return;
			}

			var per_flexible = (free_space > 0f)
				? Mathf.Max(0f, free_space / sizes.TotalFlexible)
				: -Mathf.Max(0f, -free_space / sizes.TotalFlexible);

			var minPrefLerp = 0f;
			if (sizes.TotalMin != sizes.TotalPreferred)
			{
				minPrefLerp = Mathf.Clamp01((size - sizes.TotalMin - ((elements.Count - 1) * spacing)) / (sizes.TotalPreferred - sizes.TotalMin));
			}

			for (int i = 0; i < elements.Count; i++)
			{
				var element_size = Mathf.Lerp(sizes.Sizes[i].Min, sizes.Sizes[i].Preferred, minPrefLerp) + (per_flexible * sizes.Sizes[i].Flexible);
				elements[i].SetSize(axis, element_size);
			}
		}

		/// <summary>
		/// Resize specified elements to fit.
		/// </summary>
		/// <param name="size">Size.</param>
		/// <param name="elements">Elements.</param>
		/// <param name="spacing">Spacing.</param>
		/// <param name="axis">Axis to fit.</param>
		protected static void ResizeToFit(float size, List<List<LayoutElementInfo>> elements, float spacing, RectTransform.Axis axis)
		{
			var sizes = axis == RectTransform.Axis.Horizontal ? SizesInfo.GetWidths(elements) : SizesInfo.GetHeights(elements);

			var free_space = size - sizes.TotalPreferred - ((elements.Count - 1) * spacing);
			var per_flexible = (free_space > 0f)
				? Mathf.Max(0f, free_space / sizes.TotalFlexible)
				: -Mathf.Max(0f, -free_space / sizes.TotalFlexible);

			var minPrefLerp = 0f;
			if (sizes.TotalMin != sizes.TotalPreferred)
			{
				minPrefLerp = Mathf.Clamp01((size - sizes.TotalMin - ((elements.Count - 1) * spacing)) / (sizes.TotalPreferred - sizes.TotalMin));
			}

			for (int i = 0; i < elements.Count; i++)
			{
				var element_size = Mathf.Lerp(sizes.Sizes[i].Min, sizes.Sizes[i].Preferred, minPrefLerp) + (per_flexible * sizes.Sizes[i].Flexible);
				var row = elements[i];
				for (int j = 0; j < row.Count; j++)
				{
					row[j].SetSize(axis, element_size);
				}
			}
		}

		/// <summary>
		/// Shrink specified elements to fit.
		/// </summary>
		/// <param name="size">Size.</param>
		/// <param name="elements">Elements.</param>
		/// <param name="spacing">Spacing.</param>
		/// <param name="axis">Axis to fit.</param>
		protected static void ShrinkToFit(float size, List<List<LayoutElementInfo>> elements, float spacing, RectTransform.Axis axis)
		{
			var sizes = axis == RectTransform.Axis.Horizontal ? SizesInfo.GetWidths(elements) : SizesInfo.GetHeights(elements);

			var free_space = size - sizes.TotalPreferred - ((elements.Count - 1) * spacing);
			if (free_space > 0f)
			{
				return;
			}

			var per_flexible = free_space / sizes.TotalFlexible;

			var minPrefLerp = 0f;
			if (sizes.TotalMin != sizes.TotalPreferred)
			{
				minPrefLerp = Mathf.Clamp01((size - sizes.TotalMin - ((elements.Count - 1) * spacing)) / (sizes.TotalPreferred - sizes.TotalMin));
			}

			for (int i = 0; i < elements.Count; i++)
			{
				var element_size = Mathf.Lerp(sizes.Sizes[i].Min, sizes.Sizes[i].Preferred, minPrefLerp) + (per_flexible * sizes.Sizes[i].Flexible);
				var row = elements[i];
				for (int j = 0; j < row.Count; j++)
				{
					row[j].SetSize(axis, element_size);
				}
			}
		}
		#endregion
	}
}