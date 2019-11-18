namespace UIWidgets
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Threading;
	using EasyLayoutNS;
	using UIWidgets.Attributes;
	using UIWidgets.Styles;
	using UnityEngine;
	using UnityEngine.Events;
	using UnityEngine.EventSystems;
	using UnityEngine.Serialization;
	using UnityEngine.UI;

	/// <summary>
	/// Base class for custom ListViews.
	/// </summary>
	/// <typeparam name="TComponent">Type of DefaultItem component.</typeparam>
	/// <typeparam name="TItem">Type of item.</typeparam>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Reviewed.")]
	[DataBindSupport]
	public partial class ListViewCustom<TComponent, TItem> : ListViewBase
		where TComponent : ListViewItem
	{
		/// <summary>
		/// ListView display type.
		/// </summary>
		[SerializeField]
		protected ListViewType listType = ListViewType.ListViewWithFixedSize;

		/// <summary>
		/// ListView display type.
		/// </summary>
		public ListViewType ListType
		{
			get
			{
				return listType;
			}

			set
			{
				listType = value;

				listRenderer = null;

				if (isListViewCustomInited)
				{
					SetDefaultItem(defaultItem);
				}
			}
		}

		/// <summary>
		/// The items.
		/// </summary>
		[SerializeField]
		protected List<TItem> customItems = new List<TItem>();

		/// <summary>
		/// Data source.
		/// </summary>
		protected ObservableList<TItem> dataSource;

		/// <summary>
		/// Gets or sets the data source.
		/// </summary>
		/// <value>The data source.</value>
		[DataBindField]
		public virtual ObservableList<TItem> DataSource
		{
			get
			{
				if (dataSource == null)
				{
					#pragma warning disable 0618
					dataSource = new ObservableList<TItem>(customItems);
					dataSource.OnChange += UpdateItems;
					customItems = null;
					#pragma warning restore 0618
				}

				if (!isListViewCustomInited)
				{
					Init();
				}

				return dataSource;
			}

			set
			{
				if (!isListViewCustomInited)
				{
					Init();
				}

				SetNewItems(value, IsMainThread);

				if (IsMainThread)
				{
					SetScrollValue(0f);
				}
				else
				{
					DataSourceSetted = true;
				}
			}
		}

		/// <summary>
		/// If data source setted?
		/// </summary>
		protected bool DataSourceSetted = false;

		/// <summary>
		/// Is data source changed?
		/// </summary>
		protected bool IsDataSourceChanged = false;

		[SerializeField]
		[FormerlySerializedAs("DefaultItem")]
		TComponent defaultItem;

		/// <summary>
		/// The default item template.
		/// </summary>
		public TComponent DefaultItem
		{
			get
			{
				return defaultItem;
			}

			set
			{
				SetDefaultItem(value);
			}
		}

		#region ComponentPool fields

		/// <summary>
		/// The components list.
		/// </summary>
		[SerializeField]
		[HideInInspector]
		protected List<TComponent> Components = new List<TComponent>();

		/// <summary>
		/// The components cache list.
		/// </summary>
		[SerializeField]
		[HideInInspector]
		protected List<TComponent> ComponentsCache = new List<TComponent>();

		/// <summary>
		/// The components displayed indices.
		/// </summary>
		[SerializeField]
		[HideInInspector]
		protected List<int> ComponentsDisplayedIndices = new List<int>();

		ListViewComponentPool<TComponent, TItem> componentsPool;

		/// <summary>
		/// The components pool.
		/// Constructor with lists needed to avoid lost connections when instantiated copy of the inited ListView.
		/// </summary>
		protected ListViewComponentPool<TComponent, TItem> ComponentsPool
		{
			get
			{
				if (componentsPool == null)
				{
					componentsPool = new ListViewComponentPool<TComponent, TItem>(Components, ComponentsCache, ComponentsDisplayedIndices)
					{
						Owner = this,
						Container = Container,
						CallbackAdd = AddCallback,
						CallbackRemove = RemoveCallback,
						Template = DefaultItem,
					};
				}

				return componentsPool;
			}
		}

		#endregion

		/// <summary>
		/// The displayed indices.
		/// </summary>
		protected List<int> DisplayedIndices = new List<int>();

		/// <summary>
		/// Gets the first displayed index.
		/// </summary>
		/// <value>The first displayed index.</value>
		public int DisplayedIndicesFirst
		{
			get
			{
				return DisplayedIndices.Count > 0 ? DisplayedIndices[0] : -1;
			}
		}

		/// <summary>
		/// Gets the last displayed index.
		/// </summary>
		/// <value>The last displayed index.</value>
		public int DisplayedIndicesLast
		{
			get
			{
				return DisplayedIndices.Count > 0 ? DisplayedIndices[DisplayedIndices.Count - 1] : -1;
			}
		}

		/// <summary>
		/// Gets the selected item.
		/// </summary>
		/// <value>The selected item.</value>
		[DataBindField]
		public TItem SelectedItem
		{
			get
			{
				if (SelectedIndex == -1)
				{
					return default(TItem);
				}

				return DataSource[SelectedIndex];
			}
		}

		/// <summary>
		/// Gets the selected items.
		/// </summary>
		/// <value>The selected items.</value>
		[DataBindField]
		public List<TItem> SelectedItems
		{
			get
			{
				return SelectedIndices.Convert<int, TItem>(GetDataItem);
			}
		}

		/// <summary>
		/// If enabled scroll limited to last item.
		/// </summary>
		[SerializeField]
		[Obsolete("Use ScrollRect.MovementType = Clamped instead.")]
		public bool LimitScrollValue = false;

		[SerializeField]
		[FormerlySerializedAs("Sort")]
		bool sort = true;

		/// <summary>
		/// Sort items.
		/// Deprecated. Replaced with DataSource.Comparison.
		/// </summary>
		public bool Sort
		{
			get
			{
				return sort;
			}

			set
			{
				sort = value;
				if (sort && isListViewCustomInited)
				{
					UpdateItems();
				}
			}
		}

		Func<IEnumerable<TItem>, IEnumerable<TItem>> sortFunc;

		/// <summary>
		/// Sort function.
		/// Deprecated. Replaced with DataSource.Comparison.
		/// </summary>
		public Func<IEnumerable<TItem>, IEnumerable<TItem>> SortFunc
		{
			get
			{
				return sortFunc;
			}

			set
			{
				sortFunc = value;
				if (Sort && isListViewCustomInited)
				{
					UpdateItems();
				}
			}
		}

		/// <summary>
		/// What to do when the object selected.
		/// </summary>
		[DataBindEvent("SelectedItem", "SelectedItems")]
		[SerializeField]
		public ListViewCustomEvent OnSelectObject = new ListViewCustomEvent();

		/// <summary>
		/// What to do when the object deselected.
		/// </summary>
		[DataBindEvent("SelectedItem", "SelectedItems")]
		[SerializeField]
		public ListViewCustomEvent OnDeselectObject = new ListViewCustomEvent();

		/// <summary>
		/// What to do when the event system send a pointer enter Event.
		/// </summary>
		[SerializeField]
		public ListViewCustomEvent OnPointerEnterObject = new ListViewCustomEvent();

		/// <summary>
		/// What to do when the event system send a pointer exit Event.
		/// </summary>
		[SerializeField]
		public ListViewCustomEvent OnPointerExitObject = new ListViewCustomEvent();

		/// <summary>
		/// Callback after UpdateView() call.
		/// </summary>
		[SerializeField]
		public UnityEvent OnUpdateView = new UnityEvent();

		#region Coloring fields

		[SerializeField]
		bool allowColoring = true;

		/// <summary>
		/// Allow items coloring.
		/// </summary>
		public bool AllowColoring
		{
			get
			{
				return allowColoring;
			}

			set
			{
				if (allowColoring != value)
				{
					allowColoring = value;
					ComponentsColoring(true);
				}
			}
		}

		[SerializeField]
		Color defaultBackgroundColor = Color.white;

		[SerializeField]
		Color defaultColor = Color.black;

		/// <summary>
		/// Default background color.
		/// </summary>
		public Color DefaultBackgroundColor
		{
			get
			{
				return defaultBackgroundColor;
			}

			set
			{
				defaultBackgroundColor = value;
				ComponentsColoring(true);
			}
		}

		/// <summary>
		/// Default text color.
		/// </summary>
		public Color DefaultColor
		{
			get
			{
				return defaultColor;
			}

			set
			{
				defaultColor = value;
				ComponentsColoring(true);
			}
		}

		[SerializeField]
		[FormerlySerializedAs("HighlightedBackgroundColor")]
		Color highlightedBackgroundColor = new Color(203, 230, 244, 255);

		/// <summary>
		/// Color of background on pointer over.
		/// </summary>
		public Color HighlightedBackgroundColor
		{
			get
			{
				return highlightedBackgroundColor;
			}

			set
			{
				highlightedBackgroundColor = value;
				ComponentsHighlightedColoring();
			}
		}

		/// <summary>
		/// Color of text on pointer text.
		/// </summary>
		[SerializeField]
		[FormerlySerializedAs("HighlightedColor")]
		Color highlightedColor = Color.black;

		/// <summary>
		/// Color of background on pointer over.
		/// </summary>
		public Color HighlightedColor
		{
			get
			{
				return highlightedColor;
			}

			set
			{
				highlightedColor = value;
				ComponentsHighlightedColoring();
			}
		}

		[SerializeField]
		Color selectedBackgroundColor = new Color(53, 83, 227, 255);

		[SerializeField]
		Color selectedColor = Color.black;

		/// <summary>
		/// Background color of selected item.
		/// </summary>
		public Color SelectedBackgroundColor
		{
			get
			{
				return selectedBackgroundColor;
			}

			set
			{
				selectedBackgroundColor = value;
				ComponentsColoring(true);
			}
		}

		/// <summary>
		/// Text color of selected item.
		/// </summary>
		public Color SelectedColor
		{
			get
			{
				return selectedColor;
			}

			set
			{
				selectedColor = value;
				ComponentsColoring(true);
			}
		}

		/// <summary>
		/// How long a color transition should take.
		/// </summary>
		[SerializeField]
		public float FadeDuration = 0f;
		#endregion

		/// <summary>
		/// The ScrollRect.
		/// </summary>
		[SerializeField]
		protected ScrollRect scrollRect;

		/// <summary>
		/// Gets or sets the ScrollRect.
		/// </summary>
		/// <value>The ScrollRect.</value>
		public ScrollRect ScrollRect
		{
			get
			{
				return scrollRect;
			}

			set
			{
				if (scrollRect != null)
				{
					var r = scrollRect.GetComponent<ResizeListener>();
					if (r != null)
					{
						r.OnResize.RemoveListener(SetNeedResize);
					}

					scrollRect.onValueChanged.RemoveListener(OnScrollRectUpdate);
				}

				scrollRect = value;

				if (scrollRect != null)
				{
					var resizeListener = Utilites.GetOrAddComponent<ResizeListener>(scrollRect);
					resizeListener.OnResize.AddListener(SetNeedResize);

					scrollRect.onValueChanged.AddListener(OnScrollRectUpdate);

					UpdateScrollRectSize();
				}
			}
		}

		#region ListRenderer fields

		/// <summary>
		/// The DefaultItem layout group.
		/// </summary>
		[SerializeField]
		[HideInInspector]
		protected LayoutGroup DefaultItemLayoutGroup;

		/// <summary>
		/// The DefaultItem layout group.
		/// </summary>
		[SerializeField]
		[HideInInspector]
		protected LayoutGroup DefaultItemLayout;

		/// <summary>
		/// The layout elements of the DefaultItem.
		/// </summary>
		[SerializeField]
		[HideInInspector]
		protected List<ILayoutElement> LayoutElements = new List<ILayoutElement>();

		/// <summary>
		/// The DefaultItem nested layout groups.
		/// </summary>
		[SerializeField]
		[HideInInspector]
		protected LayoutGroup[] DefaultItemNestedLayouts;

		[SerializeField]
		[HideInInspector]
		TComponent defaultItemCopy;

		/// <summary>
		/// Gets the default item copy.
		/// </summary>
		/// <value>The default item copy.</value>
		protected TComponent DefaultItemCopy
		{
			get
			{
				if (defaultItemCopy == null)
				{
					defaultItemCopy = Compatibility.Instantiate(DefaultItem);
					defaultItemCopy.transform.SetParent(DefaultItem.transform.parent, false);
					defaultItemCopy.gameObject.name = "DefaultItemCopy";
					defaultItemCopy.gameObject.SetActive(false);

					Utilites.FixInstantiated(DefaultItem, defaultItemCopy);
				}

				return defaultItemCopy;
			}
		}

		RectTransform defaultItemCopyRect;

		/// <summary>
		/// Gets the RectTransform of DefaultItemCopy.
		/// </summary>
		/// <value>RectTransform.</value>
		protected RectTransform DefaultItemCopyRect
		{
			get
			{
				if (defaultItemCopyRect == null)
				{
					defaultItemCopyRect = defaultItemCopy.transform as RectTransform;
				}

				return defaultItemCopyRect;
			}
		}
		#endregion

		[SerializeField]
		[HideInInspector]
		ListViewTypeBase listRenderer;

		/// <summary>
		/// ListView renderer.
		/// </summary>
		protected ListViewTypeBase ListRenderer
		{
			get
			{
				if (listRenderer == null)
				{
					listRenderer = GetRenderer(ListType);
				}

				return listRenderer;
			}

			set
			{
				listRenderer = value;
			}
		}

		/// <summary>
		/// Maximal count of the visible items.
		/// </summary>
		public int MaxVisibleItems
		{
			get
			{
				Init();

				return ListRenderer.MaxVisibleItems;
			}
		}

		/// <summary>
		/// The size of the DefaultItem.
		/// </summary>
		protected Vector2 ItemSize;

		/// <summary>
		/// The size of the ScrollRect.
		/// </summary>
		protected Vector2 ScrollRectSize;

		/// <summary>
		/// The direction.
		/// </summary>
		[SerializeField]
		protected ListViewDirection direction = ListViewDirection.Vertical;

		/// <summary>
		/// Set content size fitter settings?
		/// </summary>
		[SerializeField]
		[FormerlySerializedAs("_setContentSizeFitter")]
		protected bool setContentSizeFitter = true;

		/// <summary>
		/// The set ContentSizeFitter parameters according direction.
		/// </summary>
		public bool SetContentSizeFitter
		{
			get
			{
				return setContentSizeFitter;
			}

			set
			{
				setContentSizeFitter = value;
				if (LayoutBridge != null)
				{
					LayoutBridge.UpdateContentSizeFitter = value;
				}
			}
		}

		/// <summary>
		/// Gets or sets the direction.
		/// </summary>
		/// <value>The direction.</value>
		public ListViewDirection Direction
		{
			get
			{
				return direction;
			}

			set
			{
				SetDirection(value);
			}
		}

		[NonSerialized]
		bool isListViewCustomInited = false;

		/// <summary>
		/// The layout.
		/// </summary>
		protected EasyLayout layout;

		/// <summary>
		/// Gets the layout.
		/// </summary>
		/// <value>The layout.</value>
		public EasyLayout Layout
		{
			get
			{
				if (layout == null)
				{
					layout = Container.GetComponent<EasyLayout>();
				}

				return layout;
			}
		}

		/// <summary>
		/// Selected items cache (to keep valid selected indices with updates).
		/// </summary>
		protected HashSet<TItem> SelectedItemsCache = new HashSet<TItem>();

		ILayoutBridge layoutBridge;

		/// <summary>
		/// Scroll use unscaled time.
		/// </summary>
		[SerializeField]
		public bool ScrollUnscaledTime = true;

		/// <summary>
		/// Scroll movement curve.
		/// </summary>
		[SerializeField]
		[Tooltip("Requirements: start value should be less than end value; Recommended start value = 0; end value = 1;")]
		public AnimationCurve ScrollMovement = AnimationCurve.EaseInOut(0, 0, 0.25f, 1);

		/// <summary>
		/// The scroll coroutine.
		/// </summary>
		protected IEnumerator ScrollCoroutine;

		/// <summary>
		/// LayoutBridge.
		/// </summary>
		protected ILayoutBridge LayoutBridge
		{
			get
			{
				if ((layoutBridge == null) && IsVirtualizationSupported())
				{
					if (Layout != null)
					{
						layoutBridge = new EasyLayoutBridge(Layout, DefaultItem.transform as RectTransform, setContentSizeFitter)
						{
							IsHorizontal = IsHorizontal(),
						};
						ListRenderer.DirectionChanged();
					}
					else
					{
						var hv_layout = Container.GetComponent<HorizontalOrVerticalLayoutGroup>();
						if (hv_layout != null)
						{
							layoutBridge = new StandardLayoutBridge(hv_layout, DefaultItem.transform as RectTransform, setContentSizeFitter);
						}
					}
				}

				return layoutBridge;
			}
		}

		/// <summary>
		/// The main thread.
		/// </summary>
		protected Thread MainThread;

		/// <summary>
		/// Gets a value indicating whether this instance is executed in main thread.
		/// </summary>
		/// <value><c>true</c> if this instance is executed in main thread; otherwise, <c>false</c>.</value>
		protected bool IsMainThread
		{
			get
			{
				return MainThread != null && MainThread.Equals(Thread.CurrentThread);
			}
		}

		/// <summary>
		/// Is DefaultItem implements IViewData{TItem}.
		/// </summary>
		protected bool CanSetData;

		/// <summary>
		/// Center the list items if all items visible.
		/// </summary>
		[SerializeField]
		[Tooltip("Center the list items if all items visible.")]
		protected bool centerTheItems;

		/// <summary>
		/// Center the list items if all items visible.
		/// </summary>
		public virtual bool CenterTheItems
		{
			get
			{
				return centerTheItems;
			}

			set
			{
				centerTheItems = value;
				UpdateView();
			}
		}

		/// <summary>
		/// List should be looped.
		/// </summary>
		[SerializeField]
		protected bool loopedList = false;

		/// <summary>
		/// List can be looped.
		/// </summary>
		/// <value><c>true</c> if list can be looped; otherwise, <c>false</c>.</value>
		public virtual bool LoopedList
		{
			get
			{
				return loopedList;
			}

			set
			{
				loopedList = value;
			}
		}

		/// <summary>
		/// List can be looped and items is enough to make looped list.
		/// </summary>
		/// <value><c>true</c> if looped list available; otherwise, <c>false</c>.</value>
		public virtual bool LoopedListAvailable
		{
			get
			{
				return LoopedList && !ListRenderer.IsTileView && IsVirtualizationSupported() && (GetScrollRectSize() < ListSize());
			}
		}

		/// <summary>
		/// Precalculate item sizes.
		/// Disabling this option increase performance with huge lists of items with variable sizes and decrease scroll precision.
		/// </summary>
		[SerializeField]
		public bool PrecalculateItemSizes = true;

		/// <summary>
		/// Init this instance.
		/// </summary>
		public override void Init()
		{
			if (isListViewCustomInited)
			{
				return;
			}

			isListViewCustomInited = true;

			MainThread = Thread.CurrentThread;

			base.Init();
			base.Items = new List<ListViewItem>();

			SelectedItemsCache.Clear();
			SelectedItems.ForEach(x => SelectedItemsCache.Add(x));

			SetItemIndices = false;

			DestroyGameObjects = false;

			CanSetData = DefaultItem is IViewData<TItem>;

			ComponentsPool.Template = defaultItem;

			DefaultItem.gameObject.SetActive(true);

			if (IsVirtualizationSupported())
			{
				ScrollRect = scrollRect;
				CalculateItemSize();
			}

			SetContentSizeFitter = setContentSizeFitter;

			DefaultItem.gameObject.SetActive(false);

			SetDirection(direction);

			UpdateItems();
		}

		/// <summary>
		/// Update ScrollRect size.
		/// </summary>
		protected void UpdateScrollRectSize()
		{
			ScrollRectSize = (scrollRect.transform as RectTransform).rect.size;
			ScrollRectSize.x = float.IsNaN(ScrollRectSize.x) ? 1f : Mathf.Max(ScrollRectSize.x, 1f);
			ScrollRectSize.y = float.IsNaN(ScrollRectSize.y) ? 1f : Mathf.Max(ScrollRectSize.y, 1f);
		}

		/// <summary>
		/// Get the rendered of the specified ListView type.
		/// </summary>
		/// <param name="type">ListView type</param>
		/// <returns>Renderer.</returns>
		protected virtual ListViewTypeBase GetRenderer(ListViewType type)
		{
			switch (type)
			{
				case ListViewType.ListViewWithFixedSize:
					return new ListViewTypeFixed(this);
				case ListViewType.ListViewWithVariableSize:
					return new ListViewTypeSize(this);
				case ListViewType.TileViewWithFixedSize:
					return new TileViewTypeFixed(this);
				case ListViewType.TileViewWithVariableSize:
					return new TileViewTypeSize(this);
				case ListViewType.TileViewStaggered:
					return new TileViewStaggered(this);
				default:
					throw new NotSupportedException("Unknown ListView type: " + type);
			}
		}

		/// <summary>
		/// Sets the default item.
		/// </summary>
		/// <param name="newDefaultItem">New default item.</param>
		protected virtual void SetDefaultItem(TComponent newDefaultItem)
		{
			if (newDefaultItem == null)
			{
				throw new ArgumentNullException("newDefaultItem");
			}

			if (defaultItemCopy != null)
			{
				Destroy(defaultItemCopy.gameObject);
				defaultItemCopy = null;
				defaultItemCopyRect = null;
			}

			defaultItem = newDefaultItem;

			if (!isListViewCustomInited)
			{
				return;
			}

			defaultItem.gameObject.SetActive(true);
			CalculateItemSize(true);

			CanSetData = defaultItem is IViewData<TItem>;

			ComponentsPool.Template = defaultItem;

			CalculateMaxVisibleItems();

			UpdateView();

			if (scrollRect != null)
			{
				var resizeListener = scrollRect.GetComponent<ResizeListener>();
				if (resizeListener != null)
				{
					resizeListener.OnResize.Invoke();
				}
			}
		}

		#region ComponentsHighlightedColoring

		List<RaycastResult> raycastResults = new List<RaycastResult>();

		/// <summary>
		/// Apply changed highlighted colors.
		/// </summary>
		protected void ComponentsHighlightedColoring()
		{
			if (!isListViewCustomInited)
			{
				return;
			}

			if (!allowColoring)
			{
				return;
			}

			var item_under_navigation = FindItem(EventSystem.current.currentSelectedGameObject, Container);
			if (item_under_navigation != null)
			{
				HighlightColoring(item_under_navigation);
			}

			var item_under_pointer = FindItem();
			if (item_under_pointer != null)
			{
				HighlightColoring(item_under_pointer);
			}
		}

		ListViewItem FindItem()
		{
			raycastResults.Clear();

			Vector2 position;
			var root = Utilites.FindTopmostCanvas(transform).transform as RectTransform;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(root, Input.mousePosition, null, out position);
			position.x += root.rect.width * root.pivot.x;
			position.y += root.rect.height * root.pivot.y;

			var event_data = new PointerEventData(EventSystem.current)
			{
				position = position,
			};

			EventSystem.current.RaycastAll(event_data, raycastResults);

			foreach (var raycastResult in raycastResults)
			{
				if (!raycastResult.isValid)
				{
					continue;
				}

				var item = raycastResult.gameObject.GetComponent<ListViewItem>();
				if ((item != null) && (item.Owner.GetInstanceID() == GetInstanceID()))
				{
					return item;
				}
			}

			return null;
		}

		static ListViewItem FindItem(GameObject go, Transform parent)
		{
			if (go == null)
			{
				return null;
			}

			if (!go.transform.IsChildOf(parent))
			{
				return null;
			}

			var t = go.transform;
			while ((t.parent != null) && (t.parent.GetInstanceID() != parent.GetInstanceID()))
			{
				t = t.parent;
			}

			return (t.parent == null) ? null : t.GetComponent<ListViewItem>();
		}
		#endregion

		/// <summary>
		/// Destroy the component.
		/// </summary>
		/// <param name="component">Component.</param>
		protected void DestroyComponent(TComponent component)
		{
			Destroy(component.gameObject);
		}

		/// <summary>
		/// Gets the layout margin.
		/// </summary>
		/// <returns>The layout margin.</returns>
		public override Vector4 GetLayoutMargin()
		{
			return LayoutBridge.GetMarginSize();
		}

		/// <summary>
		/// Sets the direction.
		/// </summary>
		/// <param name="newDirection">New direction.</param>
		protected virtual void SetDirection(ListViewDirection newDirection)
		{
			direction = newDirection;

			if (scrollRect != null)
			{
				scrollRect.horizontal = IsHorizontal();
				scrollRect.vertical = !IsHorizontal();
				scrollRect.StopMovement();
				scrollRect.content.anchoredPosition = Vector2.zero;
			}

			(Container as RectTransform).anchoredPosition = Vector2.zero;

			if (IsVirtualizationSupported())
			{
				LayoutBridge.IsHorizontal = IsHorizontal();
				ListRenderer.DirectionChanged();

				CalculateMaxVisibleItems();
			}

			UpdateView();
		}

		/// <summary>
		/// Determines whether is sort enabled.
		/// </summary>
		/// <returns><c>true</c> if is sort enabled; otherwise, <c>false</c>.</returns>
		public bool IsSortEnabled()
		{
			if (DataSource.Comparison != null)
			{
				return true;
			}

			return Sort && SortFunc != null;
		}

		/// <summary>
		/// Gets the index of the nearest item.
		/// </summary>
		/// <returns>The nearest index.</returns>
		/// <param name="eventData">Event data.</param>
		public override int GetNearestIndex(PointerEventData eventData)
		{
			if (IsSortEnabled())
			{
				return -1;
			}

			Vector2 point;
			var rectTransform = Container as RectTransform;
			if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out point))
			{
				return DataSource.Count;
			}

			var rect = rectTransform.rect;
			if (!rect.Contains(point))
			{
				return DataSource.Count;
			}

			return GetNearestIndex(point);
		}

		/// <summary>
		/// Gets the index of the nearest item.
		/// </summary>
		/// <returns>The nearest item index.</returns>
		/// <param name="point">Point.</param>
		public override int GetNearestIndex(Vector2 point)
		{
			return ListRenderer.GetNearestIndex(point);
		}

		/// <summary>
		/// Gets the spacing between items.
		/// </summary>
		/// <returns>The item spacing.</returns>
		public override float GetItemSpacing()
		{
			return LayoutBridge.GetSpacing();
		}

		/// <summary>
		/// Gets the horizontal spacing between items.
		/// </summary>
		/// <returns>The item spacing.</returns>
		public override float GetItemSpacingX()
		{
			return LayoutBridge.GetSpacingX();
		}

		/// <summary>
		/// Gets the vertical spacing between items.
		/// </summary>
		/// <returns>The item spacing.</returns>
		public override float GetItemSpacingY()
		{
			return LayoutBridge.GetSpacingY();
		}

		/// <summary>
		/// Gets the item.
		/// </summary>
		/// <returns>The item.</returns>
		/// <param name="index">Index.</param>
		protected TItem GetDataItem(int index)
		{
			return DataSource[index];
		}

		/// <summary>
		/// Calculates the size of the item.
		/// </summary>
		/// <param name="reset">Reset item size.</param>
		protected virtual void CalculateItemSize(bool reset = false)
		{
			ItemSize = ListRenderer.GetItemSize(reset);
		}

		/// <summary>
		/// Determines whether this instance is horizontal.
		/// </summary>
		/// <returns><c>true</c> if this instance is horizontal; otherwise, <c>false</c>.</returns>
		public override bool IsHorizontal()
		{
			return direction == ListViewDirection.Horizontal;
		}

		/// <summary>
		/// Calculates the max count of visible items.
		/// </summary>
		protected virtual void CalculateMaxVisibleItems()
		{
			if (!isListViewCustomInited)
			{
				return;
			}

			ListRenderer.CalculateMaxVisibleItems();

			ListRenderer.ValidateContentSize();
		}

		/// <summary>
		/// Resize this instance.
		/// </summary>
		public virtual void Resize()
		{
			ListRenderer.CalculateItemsSizes(DataSource, false);

			NeedResize = false;

			UpdateScrollRectSize();

			CalculateItemSize(true);
			CalculateMaxVisibleItems();
			UpdateView();
		}

		/// <summary>
		/// Determines whether this instance can optimize.
		/// </summary>
		/// <returns><c>true</c> if this instance can optimize; otherwise, <c>false</c>.</returns>
		protected virtual bool IsVirtualizationSupported()
		{
			return ListRenderer.IsVirtualizationSupported();
		}

		/// <summary>
		/// Invokes the select event.
		/// </summary>
		/// <param name="index">Index.</param>
		protected override void InvokeSelect(int index)
		{
			if (!IsValid(index))
			{
				Debug.LogWarning("Incorrect index: " + index, this);
			}

			var component = GetComponent(index);
			var item = DataSource[index];

			base.InvokeSelect(index);

			SelectedItemsCache.Add(item);
			OnSelectObject.Invoke(index);

			SelectColoring(component);
			if (component != null)
			{
				component.StateSelected();
			}
		}

		/// <summary>
		/// Invokes the deselect event.
		/// </summary>
		/// <param name="index">Index.</param>
		protected override void InvokeDeselect(int index)
		{
			if (!IsValid(index))
			{
				Debug.LogWarning("Incorrect index: " + index, this);
			}

			var component = GetComponent(index);
			var item = DataSource[index];

			base.InvokeDeselect(index);

			SelectedItemsCache.Remove(item);
			OnDeselectObject.Invoke(index);

			DefaultColoring(component);
			if (component != null)
			{
				component.StateDefault();
			}
		}

		/// <summary>
		/// Raises the pointer enter callback event.
		/// </summary>
		/// <param name="item">Item.</param>
		void OnPointerEnterCallback(ListViewItem item)
		{
			OnPointerEnterObject.Invoke(item.Index);

			if (!IsSelected(item.Index))
			{
				HighlightColoring(item);
				item.StateHighlighted();
			}
		}

		/// <summary>
		/// Raises the pointer exit callback event.
		/// </summary>
		/// <param name="item">Item.</param>
		void OnPointerExitCallback(ListViewItem item)
		{
			OnPointerExitObject.Invoke(item.Index);

			if (!IsSelected(item.Index))
			{
				DefaultColoring(item);
				item.StateDefault();
			}
		}

		/// <summary>
		/// Set flag to update view when data source changed.
		/// </summary>
		public override void UpdateItems()
		{
			SetNewItems(DataSource, IsMainThread);
			IsDataSourceChanged = !IsMainThread;
		}

		/// <summary>
		/// Clear items of this instance.
		/// </summary>
		public override void Clear()
		{
			DataSource.Clear();
			SetScrollValue(0f);
		}

		/// <summary>
		/// Add the specified item.
		/// </summary>
		/// <param name="item">Item.</param>
		/// <returns>Index of added item.</returns>
		public virtual int Add(TItem item)
		{
			DataSource.Add(item);

			return DataSource.IndexOf(item);
		}

		/// <summary>
		/// Remove the specified item.
		/// </summary>
		/// <param name="item">Item.</param>
		/// <returns>Index of removed TItem.</returns>
		public virtual int Remove(TItem item)
		{
			var index = DataSource.IndexOf(item);
			if (index == -1)
			{
				return index;
			}

			DataSource.RemoveAt(index);

			return index;
		}

		/// <summary>
		/// Remove item by the specified index.
		/// </summary>
		/// <param name="index">Index.</param>
		public virtual void Remove(int index)
		{
			DataSource.RemoveAt(index);
		}

		/// <summary>
		/// Sets the scroll value.
		/// </summary>
		/// <param name="value">Value.</param>
		/// <param name="callScrollUpdate">Call ScrollUpdate() if position changed.</param>
		protected void SetScrollValue(float value, bool callScrollUpdate = true)
		{
			if (scrollRect.content == null)
			{
				return;
			}

			var current_position = scrollRect.content.anchoredPosition;
			var new_position = IsHorizontal()
				? new Vector2(-value, current_position.y)
				: new Vector2(current_position.x, value);

			SetScrollValue(new_position, callScrollUpdate);
		}

		/// <summary>
		/// Sets the scroll value.
		/// </summary>
		/// <param name="newPosition">Value.</param>
		/// <param name="callScrollUpdate">Call ScrollUpdate() if position changed.</param>
		protected void SetScrollValue(Vector2 newPosition, bool callScrollUpdate = true)
		{
			if (scrollRect.content == null)
			{
				return;
			}

			var current_position = scrollRect.content.anchoredPosition;
			const float delta = 0.01f;
			var diff = (IsHorizontal() && Mathf.Abs(current_position.x - newPosition.x) != delta)
					|| (!IsHorizontal() && Mathf.Abs(current_position.y - newPosition.y) != delta);

			scrollRect.StopMovement();

			if (diff)
			{
				scrollRect.content.anchoredPosition = newPosition;
				if (callScrollUpdate)
				{
					ScrollUpdate();
				}
			}
		}

		/// <summary>
		/// Gets the scroll value in ListView direction.
		/// </summary>
		/// <returns>The scroll value.</returns>
		protected float GetScrollValue()
		{
			var pos = scrollRect.content.anchoredPosition;
			var result = IsHorizontal() ? -pos.x : pos.y;
			if (LoopedListAvailable)
			{
				return result;
			}

			return float.IsNaN(result) ? 0f : result;
		}

		/// <summary>
		/// Scrolls to specified item immediately.
		/// </summary>
		/// <param name="item">Item.</param>
		public virtual void ScrollTo(TItem item)
		{
			var index = DataSource.IndexOf(item);
			if (index > -1)
			{
				ScrollTo(index);
			}
		}

		/// <summary>
		/// Scroll to the specified item with animation.
		/// </summary>
		/// <param name="item">Item.</param>
		public virtual void ScrollToAnimated(TItem item)
		{
			var index = DataSource.IndexOf(item);
			if (index > -1)
			{
				ScrollToAnimated(index);
			}
		}

		/// <summary>
		/// Scrolls to item with specified index.
		/// </summary>
		/// <param name="index">Index.</param>
		public override void ScrollTo(int index)
		{
			if (!IsVirtualizationSupported())
			{
				return;
			}

			SetScrollValue(GetScrollPosition(index));
		}

		/// <summary>
		/// Scrolls to specified position.
		/// </summary>
		/// <param name="position">Position.</param>
		public override void ScrollToPosition(float position)
		{
			if (!IsVirtualizationSupported())
			{
				return;
			}

			SetScrollValue(position);
		}

		/// <summary>
		/// Scrolls to specified position.
		/// </summary>
		/// <param name="position">Position.</param>
		public override void ScrollToPosition(Vector2 position)
		{
			if (!IsVirtualizationSupported())
			{
				return;
			}

			SetScrollValue(position);
		}

		/// <summary>
		/// Is visible item with specified index.
		/// </summary>
		/// <param name="index">Index.</param>
		/// <returns>true if item visible; false otherwise.</returns>
		public override bool IsVisible(int index)
		{
			if (!IsVirtualizationSupported())
			{
				return false;
			}

			var viewport_top = GetScrollValue() + LayoutBridge.GetMargin();
			var viewport_bottom = viewport_top + GetScrollRectSize() - LayoutBridge.GetMargin();

			var border_top = GetItemPosition(index);
			var border_top_visible = (viewport_top <= border_top) && (border_top < viewport_bottom);
			if (border_top_visible)
			{
				return true;
			}

			var border_bottom = GetItemPositionBorderEnd(index);
			var border_bottom_visible = (viewport_top <= border_bottom) && (border_bottom < viewport_bottom);

			return border_bottom_visible;
		}

		/// <summary>
		/// Starts the scroll coroutine.
		/// </summary>
		/// <param name="coroutine">Coroutine.</param>
		protected virtual void StartScrollCoroutine(IEnumerator coroutine)
		{
			StopScrollCoroutine();
			ScrollCoroutine = coroutine;
			StartCoroutine(ScrollCoroutine);
		}

		/// <summary>
		/// Stops the scroll coroutine.
		/// </summary>
		protected virtual void StopScrollCoroutine()
		{
			if (ScrollCoroutine != null)
			{
				StopCoroutine(ScrollCoroutine);
			}
		}

		/// <summary>
		/// Stop scrolling.
		/// </summary>
		public override void ScrollStop()
		{
			StopScrollCoroutine();
		}

		/// <summary>
		/// Scroll to specified index with time.
		/// </summary>
		/// <param name="index">Index.</param>
		public override void ScrollToAnimated(int index)
		{
			StartScrollCoroutine(ScrollToAnimatedCoroutine(index, ScrollUnscaledTime));
		}

		/// <summary>
		/// Scrolls to specified position with time.
		/// </summary>
		/// <param name="target">Position.</param>
		public override void ScrollToPositionAnimated(float target)
		{
			var current_position = ScrollRect.content.anchoredPosition;
			var position = IsHorizontal()
				? new Vector2(-target, current_position.y)
				: new Vector2(current_position.x, target);

			StartScrollCoroutine(ScrollToAnimatedCoroutine(() => position, ScrollUnscaledTime));
		}

		/// <summary>
		/// Scrolls to specified position with time.
		/// </summary>
		/// <param name="target">Position.</param>
		public override void ScrollToPositionAnimated(Vector2 target)
		{
			StartScrollCoroutine(ScrollToAnimatedCoroutine(() => target, ScrollUnscaledTime));
		}

		/// <summary>
		/// Get secondary scroll position (for the cross direction).
		/// </summary>
		/// <param name="index">Index.</param>
		/// <returns>Secondary scroll position.</returns>
		protected virtual float GetScrollPositionSecondary(int index)
		{
			var current_position = ScrollRect.content.anchoredPosition;

			return IsHorizontal() ? current_position.y : current_position.x;
		}

		/// <summary>
		/// Get scroll position for the specified index.
		/// </summary>
		/// <param name="index">Index.</param>
		/// <returns>Scroll position</returns>
		protected virtual Vector2 GetScrollPosition(int index)
		{
			var scroll_main = GetScrollValue();

			var item_starts = GetItemPosition(index);
			var item_ends = GetItemPositionBottom(index);

			if (item_starts < scroll_main)
			{
				scroll_main = item_starts;
			}
			else if (item_ends > scroll_main)
			{
				scroll_main = item_ends;
			}

			var scroll_secondary = GetScrollPositionSecondary(index);

			var position = IsHorizontal()
				? new Vector2(-scroll_main, scroll_secondary)
				: new Vector2(scroll_secondary, scroll_main);

			return position;
		}

		/// <summary>
		/// Scroll to specified index with time coroutine.
		/// </summary>
		/// <returns>The scroll to index with time coroutine.</returns>
		/// <param name="index">Index.</param>
		/// <param name="unscaledTime">Use unscaled time.</param>
		protected virtual IEnumerator ScrollToAnimatedCoroutine(int index, bool unscaledTime)
		{
			return ScrollToAnimatedCoroutine(() => GetScrollPosition(index), unscaledTime);
		}

		/// <summary>
		/// Scroll to specified position with time coroutine.
		/// </summary>
		/// <returns>The scroll to index with time coroutine.</returns>
		/// <param name="targetPosition">Target position.</param>
		/// <param name="unscaledTime">Use unscaled time.</param>
		protected virtual IEnumerator ScrollToAnimatedCoroutine(Func<Vector2> targetPosition, bool unscaledTime)
		{
			var base_position = ScrollRect.content.anchoredPosition;

			float delta;
			var animationLength = ScrollMovement.keys[ScrollMovement.keys.Length - 1].time;
			var startTime = Utilites.GetTime(unscaledTime);

			do
			{
				delta = Utilites.GetTime(unscaledTime) - startTime;
				var value = ScrollMovement.Evaluate(delta);

				var pos = base_position + ((targetPosition() - base_position) * value);

				SetScrollValue(pos);

				yield return null;
			}
			while (delta < animationLength);

			SetScrollValue(targetPosition());

			yield return null;

			SetScrollValue(targetPosition());
		}

		/// <summary>
		/// Gets the item position by index.
		/// </summary>
		/// <returns>The item position.</returns>
		/// <param name="index">Index.</param>
		public override float GetItemPosition(int index)
		{
			return ListRenderer.GetItemPosition(index);
		}

		/// <summary>
		/// Gets the item position by index.
		/// </summary>
		/// <returns>The item position.</returns>
		/// <param name="index">Index.</param>
		public virtual float GetItemPositionBorderEnd(int index)
		{
			return ListRenderer.GetItemPositionBorderEnd(index);
		}

		/// <summary>
		/// Gets the item middle position by index.
		/// </summary>
		/// <returns>The item middle position.</returns>
		/// <param name="index">Index.</param>
		public virtual float GetItemPositionMiddle(int index)
		{
			return ListRenderer.GetItemPositionMiddle(index);
		}

		/// <summary>
		/// Gets the item bottom position by index.
		/// </summary>
		/// <returns>The item bottom position.</returns>
		/// <param name="index">Index.</param>
		public virtual float GetItemPositionBottom(int index)
		{
			return ListRenderer.GetItemPositionBottom(index);
		}

		/// <summary>
		/// Adds the callback.
		/// </summary>
		/// <param name="item">Item.</param>
		protected virtual void AddCallback(ListViewItem item)
		{
			ListRenderer.AddCallback(item);

			item.onPointerEnterItem.AddListener(OnPointerEnterCallback);
			item.onPointerExitItem.AddListener(OnPointerExitCallback);
		}

		/// <summary>
		/// Removes the callback.
		/// </summary>
		/// <param name="item">Item.</param>
		protected virtual void RemoveCallback(ListViewItem item)
		{
			if (item == null)
			{
				return;
			}

			ListRenderer.RemoveCallback(item);

			item.onPointerEnterItem.RemoveListener(OnPointerEnterCallback);
			item.onPointerExitItem.RemoveListener(OnPointerExitCallback);
		}

		/// <summary>
		/// Set the specified item.
		/// </summary>
		/// <param name="item">Item.</param>
		/// <param name="allowDuplicate">If set to <c>true</c> allow duplicate.</param>
		/// <returns>Index of item.</returns>
		public int Set(TItem item, bool allowDuplicate = true)
		{
			int index;

			if (!allowDuplicate)
			{
				index = DataSource.IndexOf(item);
				if (index == -1)
				{
					index = Add(item);
				}
			}
			else
			{
				index = Add(item);
			}

			Select(index);

			return index;
		}

		/// <summary>
		/// Updates the component layout.
		/// </summary>
		/// <param name="component">Component.</param>
		protected virtual void UpdateComponentLayout(TComponent component)
		{
			LayoutUtilites.UpdateLayoutsRecursive(component);
		}

		/// <summary>
		/// Sets component data with specified item.
		/// </summary>
		/// <param name="component">Component.</param>
		/// <param name="item">Item.</param>
		protected virtual void SetData(TComponent component, TItem item)
		{
			if (CanSetData)
			{
				(component as IViewData<TItem>).SetData(item);
			}
		}

		/// <summary>
		/// Gets the default width of the item.
		/// </summary>
		/// <returns>The default item width.</returns>
		public override float GetDefaultItemWidth()
		{
			return ItemSize.x;
		}

		/// <summary>
		/// Gets the default height of the item.
		/// </summary>
		/// <returns>The default item height.</returns>
		public override float GetDefaultItemHeight()
		{
			return ItemSize.y;
		}

		/// <summary>
		/// Gets the size of the scroll.
		/// </summary>
		/// <returns>The scroll size.</returns>
		protected float GetScrollRectSize()
		{
			return IsHorizontal() ? ScrollRectSize.x : ScrollRectSize.y;
		}

		/// <summary>
		/// Refresh the scroll position.
		/// </summary>
		protected virtual void RefreshScrollPosition()
		{
			var scroll = GetScrollValue();
			var list_size = ListSize();
			if (scroll > list_size)
			{
				SetScrollValue(scroll - list_size);
			}
			else if (scroll < 0)
			{
				SetScrollValue(scroll + list_size);
			}
		}

		/// <summary>
		/// On ScrollUpdate.
		/// </summary>
		protected virtual void ScrollUpdate()
		{
			if (LoopedListAvailable)
			{
				RefreshScrollPosition();
			}

			if (!ListRenderer.UpdateDisplayedIndices())
			{
				return;
			}

			SetDisplayedIndices(ListRenderer.IsTileView);
		}

		/// <summary>
		/// Sets the displayed indices.
		/// </summary>
		/// <param name="isNewData">Is new data?</param>
		protected virtual void SetDisplayedIndices(bool isNewData = true)
		{
			if (isNewData)
			{
				ComponentsPool.DisplayedIndicesSet(DisplayedIndices, ComponentSetData);
			}
			else
			{
				ComponentsPool.DisplayedIndicesUpdate(DisplayedIndices, ComponentSetData);
			}

			UpdateLayoutBridge();
		}

		/// <summary>
		/// Process the ScrollRect update event.
		/// </summary>
		/// <param name="position">Position.</param>
		protected virtual void OnScrollRectUpdate(Vector2 position)
		{
			StartScrolling();
			ScrollUpdate();
		}

		/// <summary>
		/// Set data to component.
		/// </summary>
		/// <param name="component">Component.</param>
		protected virtual void ComponentSetData(TComponent component)
		{
			SetData(component, DataSource[component.Index]);
			Coloring(component as ListViewItem);

			if (IsSelected(component.Index))
			{
				component.StateSelected();
			}
			else
			{
				component.StateDefault();
			}
		}

		/// <summary>
		/// Updates the view.
		/// </summary>
		protected void UpdateView()
		{
			if (!isListViewCustomInited)
			{
				return;
			}

			ListRenderer.UpdateDisplayedIndices();

			SetDisplayedIndices();

			OnUpdateView.Invoke();
		}

		/// <summary>
		/// Updates the layout bridge.
		/// </summary>
		protected virtual void UpdateLayoutBridge()
		{
			if (LayoutBridge == null)
			{
				return;
			}

			if (IsRequiredCenterTheItems())
			{
				var filler = (GetScrollRectSize() - ListSize() - LayoutBridge.GetFullMargin()) / 2;
				LayoutBridge.SetFiller(filler, 0f);
			}
			else
			{
				var top = CalculateTopFillerSize();
				var bottom = CalculateBottomFillerSize();
				LayoutBridge.SetFiller(top, bottom);
			}

			LayoutBridge.UpdateLayout();
		}

		/// <summary>
		/// Validate scroll value.
		/// </summary>
		[Obsolete("Not more used since LimitScrollValue is obsolete.")]
		protected virtual void ValidateScrollValue()
		{
		}

		/// <summary>
		/// Determines whether is required center the list items.
		/// </summary>
		/// <returns><c>true</c> if required center the list items; otherwise, <c>false</c>.</returns>
		protected virtual bool IsRequiredCenterTheItems()
		{
			if (!CenterTheItems)
			{
				return false;
			}

			return GetScrollRectSize() > ListSize();
		}

		/// <summary>
		/// Get the size of the ListView.
		/// </summary>
		/// <returns>The size.</returns>
		protected virtual float ListSize()
		{
			return ListRenderer.ListSize();
		}

		/// <summary>
		/// Keep selected items on items update.
		/// </summary>
		[SerializeField]
		protected bool KeepSelection = true;

		/// <summary>
		/// Updates the items.
		/// </summary>
		/// <param name="newItems">New items.</param>
		/// <param name="updateView">Update view.</param>
		protected virtual void SetNewItems(ObservableList<TItem> newItems, bool updateView = true)
		{
			ListRenderer.CalculateItemsSizes(newItems, false);

			lock (DataSource)
			{
				DataSource.OnChange -= UpdateItems;

				if (Sort && SortFunc != null)
				{
					newItems.BeginUpdate();

					var sorted = new List<TItem>(SortFunc(newItems));

					newItems.Clear();
					newItems.AddRange(sorted);

					newItems.EndUpdate();
				}

				SilentDeselect(SelectedIndices);
				var new_selected_indices = RecalculateSelectedIndices(newItems);

				dataSource = newItems;

				CalculateMaxVisibleItems();

				if (KeepSelection)
				{
					SilentSelect(new_selected_indices);
				}

				SelectedItemsCache.Clear();
				SelectedItems.ForEach(x => SelectedItemsCache.Add(x));

				if (updateView)
				{
					UpdateView();
				}

				DataSource.OnChange += UpdateItems;
			}
		}

		/// <summary>
		/// Recalculates the selected indices.
		/// </summary>
		/// <returns>The selected indices.</returns>
		/// <param name="newItems">New items.</param>
		protected virtual List<int> RecalculateSelectedIndices(ObservableList<TItem> newItems)
		{
			var new_selected_indices = new List<int>();

			foreach (var item in SelectedItemsCache)
			{
				var new_index = newItems.IndexOf(item);
				if (new_index != -1)
				{
					new_selected_indices.Add(new_index);
				}
			}

			return new_selected_indices;
		}

		/// <summary>
		/// Calculates the size of the top filler.
		/// </summary>
		/// <returns>The top filler size.</returns>
		protected virtual float CalculateTopFillerSize()
		{
			return ListRenderer.CalculateTopFillerSize();
		}

		/// <summary>
		/// Calculates the size of the bottom filler.
		/// </summary>
		/// <returns>The bottom filler size.</returns>
		protected virtual float CalculateBottomFillerSize()
		{
			return ListRenderer.CalculateBottomFillerSize();
		}

		/// <summary>
		/// Determines if item exists with the specified index.
		/// </summary>
		/// <returns><c>true</c> if item exists with the specified index; otherwise, <c>false</c>.</returns>
		/// <param name="index">Index.</param>
		public override bool IsValid(int index)
		{
			return (index >= 0) && (index < DataSource.Count);
		}

		/// <summary>
		/// Process the item move event.
		/// </summary>
		/// <param name="eventData">Event data.</param>
		/// <param name="item">Item.</param>
		protected override void OnItemMove(AxisEventData eventData, ListViewItem item)
		{
			ListRenderer.OnItemMove(eventData, item);
		}

		/// <summary>
		/// Coloring the specified component.
		/// </summary>
		/// <param name="component">Component.</param>
		protected override void Coloring(ListViewItem component)
		{
			if (component == null)
			{
				return;
			}

			if (IsSelected(component.Index))
			{
				SelectColoring(component);
			}
			else
			{
				DefaultColoring(component);
			}
		}

		/// <summary>
		/// Set highlights colors of specified component.
		/// </summary>
		/// <param name="component">Component.</param>
		protected override void HighlightColoring(ListViewItem component)
		{
			if (component == null)
			{
				return;
			}

			if (IsSelected(component.Index))
			{
				return;
			}

			HighlightColoring(component as TComponent);
		}

		/// <summary>
		/// Set highlights colors of specified component.
		/// </summary>
		/// <param name="component">Component.</param>
		protected virtual void HighlightColoring(TComponent component)
		{
			if (component == null)
			{
				return;
			}

			if (!allowColoring)
			{
				return;
			}

			if (IsSelected(component.Index))
			{
				return;
			}

			component.GraphicsColoring(HighlightedColor, HighlightedBackgroundColor, FadeDuration);
		}

		/// <summary>
		/// Set select colors of specified component.
		/// </summary>
		/// <param name="component">Component.</param>
		protected virtual void SelectColoring(ListViewItem component)
		{
			if (component == null)
			{
				return;
			}

			SelectColoring(component as TComponent);
		}

		/// <summary>
		/// Set select colors of specified component.
		/// </summary>
		/// <param name="component">Component.</param>
		protected virtual void SelectColoring(TComponent component)
		{
			if (component == null)
			{
				return;
			}

			if (!allowColoring)
			{
				return;
			}

			if (IsInteractable())
			{
				component.GraphicsColoring(SelectedColor, SelectedBackgroundColor, FadeDuration);
			}
			else
			{
				component.GraphicsColoring(SelectedColor * DisabledColor, SelectedBackgroundColor * DisabledColor, FadeDuration);
			}
		}

		/// <summary>
		/// Set default colors of specified component.
		/// </summary>
		/// <param name="component">Component.</param>
		protected virtual void DefaultColoring(ListViewItem component)
		{
			if (component == null)
			{
				return;
			}

			DefaultColoring(component as TComponent);
		}

		/// <summary>
		/// Set default colors of specified component.
		/// </summary>
		/// <param name="component">Component.</param>
		protected virtual void DefaultColoring(TComponent component)
		{
			if (component == null)
			{
				return;
			}

			if (!allowColoring)
			{
				return;
			}

			if (IsInteractable())
			{
				component.GraphicsColoring(DefaultColor, DefaultBackgroundColor, FadeDuration);
			}
			else
			{
				component.GraphicsColoring(DefaultColor * DisabledColor, DefaultBackgroundColor * DisabledColor, FadeDuration);
			}
		}

		/// <summary>
		/// Updates the colors.
		/// </summary>
		/// <param name="instant">Is should be instant color update?</param>
		public override void ComponentsColoring(bool instant = false)
		{
			if (!allowColoring && instant)
			{
				ComponentsPool.ForEach(x => DefaultColoring(x as ListViewItem));
				return;
			}

			if (instant)
			{
				var old_duration = FadeDuration;
				FadeDuration = 0f;
				ComponentsPool.ForEach(x => Coloring(x as ListViewItem));
				FadeDuration = old_duration;
			}
			else
			{
				ComponentsPool.ForEach(x => Coloring(x as ListViewItem));
			}
		}

		/// <summary>
		/// This function is called when the MonoBehaviour will be destroyed.
		/// </summary>
		protected override void OnDestroy()
		{
			layout = null;
			layoutBridge = null;

			ScrollRect = null;

			ComponentsPool.Template = null;

			base.OnDestroy();
		}

		/// <summary>
		/// Calls the specified action for each component.
		/// </summary>
		/// <param name="func">Action.</param>
		public override void ForEachComponent(Action<ListViewItem> func)
		{
			base.ForEachComponent(func);

			func(DefaultItem);

			if (defaultItemCopy != null)
			{
				func(DefaultItemCopy);
			}

			ComponentsPool.ForEachCache(x => func(x as ListViewItem));
		}

		/// <summary>
		/// Calls the specified action for each component.
		/// </summary>
		/// <param name="func">Action.</param>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1061:DoNotHideBaseClassMethods", Justification = "Reviewed.")]
		public virtual void ForEachComponent(Action<TComponent> func)
		{
			base.ForEachComponent<TComponent>(func);
			func(DefaultItem);
			ComponentsPool.ForEachCache(func);
		}

		/// <summary>
		/// Determines whether item visible.
		/// </summary>
		/// <returns><c>true</c> if item visible; otherwise, <c>false</c>.</returns>
		/// <param name="index">Index.</param>
		public override bool IsItemVisible(int index)
		{
			return DisplayedIndices.Contains(index);
		}

		/// <summary>
		/// Gets the visible indices.
		/// </summary>
		/// <returns>The visible indices.</returns>
		public List<int> GetVisibleIndices()
		{
			return new List<int>(DisplayedIndices);
		}

		/// <summary>
		/// Gets the visible components.
		/// </summary>
		/// <returns>The visible components.</returns>
		public List<TComponent> GetVisibleComponents()
		{
			return ComponentsPool.List();
		}

		/// <summary>
		/// Gets the item component.
		/// </summary>
		/// <returns>The item component.</returns>
		/// <param name="index">Index.</param>
		public TComponent GetItemComponent(int index)
		{
			return GetComponent(index) as TComponent;
		}

		/// <summary>
		/// OnStartScrolling event.
		/// </summary>
		public UnityEvent OnStartScrolling = new UnityEvent();

		/// <summary>
		/// OnEndScrolling event.
		/// </summary>
		public UnityEvent OnEndScrolling = new UnityEvent();

		/// <summary>
		/// Time before raise OnEndScrolling event since last OnScrollRectUpdate event raised.
		/// </summary>
		public float EndScrollDelay = 0.3f;

		/// <summary>
		/// Is ScrollRect now on scrolling state.
		/// </summary>
		protected bool Scrolling;

		/// <summary>
		/// When last scroll event happen?
		/// </summary>
		protected float LastScrollingTime;

		/// <summary>
		/// Update this instance.
		/// </summary>
		protected virtual void Update()
		{
			if (DataSourceSetted || IsDataSourceChanged)
			{
				var reset_scroll = DataSourceSetted;

				DataSourceSetted = false;
				IsDataSourceChanged = false;

				lock (DataSource)
				{
					CalculateMaxVisibleItems();
					UpdateView();
				}

				if (reset_scroll)
				{
					SetScrollValue(0f);
				}
			}

			if (NeedResize)
			{
				Resize();
			}

			if (IsStopScrolling())
			{
				EndScrolling();
			}
		}

		/// <summary>
		/// This function is called when the object becomes enabled and active.
		/// </summary>
		protected override void OnEnable()
		{
			base.OnEnable();

			StartCoroutine(ForceRebuild());

			var old = FadeDuration;
			FadeDuration = 0f;
			ComponentsPool.ForEach(Coloring);
			FadeDuration = old;
		}

		IEnumerator ForceRebuild()
		{
			yield return null;
			ForEachComponent(MarkLayoutForRebuild);
		}

		void MarkLayoutForRebuild(ListViewItem item)
		{
			if (item != null)
			{
				LayoutRebuilder.MarkLayoutForRebuild(item.transform as RectTransform);
			}
		}

		/// <summary>
		/// Start to track scrolling event.
		/// </summary>
		protected virtual void StartScrolling()
		{
			LastScrollingTime = Utilites.GetUnscaledTime();
			if (Scrolling)
			{
				return;
			}

			Scrolling = true;
			OnStartScrolling.Invoke();
		}

		/// <summary>
		/// Determines whether ScrollRect is stop scrolling.
		/// </summary>
		/// <returns><c>true</c> if ScrollRect is stop scrolling; otherwise, <c>false</c>.</returns>
		protected virtual bool IsStopScrolling()
		{
			if (!Scrolling)
			{
				return false;
			}

			return (LastScrollingTime + EndScrollDelay) <= Utilites.GetUnscaledTime();
		}

		/// <summary>
		/// Raise OnEndScrolling event.
		/// </summary>
		protected virtual void EndScrolling()
		{
			Scrolling = false;
			OnEndScrolling.Invoke();
		}

		/// <summary>
		/// Is need to handle resize event?
		/// </summary>
		protected bool NeedResize;

		/// <summary>
		/// Sets the need resize.
		/// </summary>
		protected virtual void SetNeedResize()
		{
			if (!IsVirtualizationSupported())
			{
				return;
			}

			NeedResize = true;
		}

		#region ListViewPaginator support

		/// <summary>
		/// Gets the ScrollRect.
		/// </summary>
		/// <returns>The ScrollRect.</returns>
		public override ScrollRect GetScrollRect()
		{
			return ScrollRect;
		}

		/// <summary>
		/// Gets the items count.
		/// </summary>
		/// <returns>The items count.</returns>
		public override int GetItemsCount()
		{
			return DataSource.Count;
		}

		/// <summary>
		/// Gets the items per block count.
		/// </summary>
		/// <returns>The items per block.</returns>
		public override int GetItemsPerBlock()
		{
			return ListRenderer.GetItemsPerBlock();
		}

		/// <summary>
		/// Gets the index of the nearest item.
		/// </summary>
		/// <returns>The nearest item index.</returns>
		public override int GetNearestItemIndex()
		{
			return ListRenderer.GetNearestItemIndex();
		}
		#endregion

		#region Obsolete

		/// <summary>
		/// Gets the visible indices.
		/// </summary>
		/// <returns>The visible indices.</returns>
		[Obsolete("Use GetVisibleIndices()")]
		public List<int> GetVisibleIndicies()
		{
			return GetVisibleIndices();
		}

		/// <summary>
		/// Gets or sets the items.
		/// </summary>
		/// <value>Items.</value>
		[Obsolete("Use DataSource instead.")]
		public new List<TItem> Items
		{
			get
			{
				return new List<TItem>(DataSource);
			}

			set
			{
				DataSource = new ObservableList<TItem>(value);
			}
		}
		#endregion

		#region IStylable implementation

		/// <summary>
		/// Set the specified style.
		/// </summary>
		/// <param name="style">Style data.</param>
		protected virtual void SetStyleDefaultItem(Style style)
		{
			if (defaultItem != null)
			{
				defaultItem.Owner = this;
				defaultItem.SetStyle(style.Collections.DefaultItemBackground, style.Collections.DefaultItemText, style);
			}

			if (ComponentsPool != null)
			{
				ComponentsPool.ForEachAll(x => x.SetStyle(style.Collections.DefaultItemBackground, style.Collections.DefaultItemText, style));
			}
		}

		/// <summary>
		/// Sets the style colors.
		/// </summary>
		/// <param name="style">Style.</param>
		protected virtual void SetStyleColors(Style style)
		{
			defaultBackgroundColor = style.Collections.DefaultBackgroundColor;
			defaultColor = style.Collections.DefaultColor;
			HighlightedBackgroundColor = style.Collections.HighlightedBackgroundColor;
			HighlightedColor = style.Collections.HighlightedColor;
			selectedBackgroundColor = style.Collections.SelectedBackgroundColor;
			selectedColor = style.Collections.SelectedColor;
		}

		/// <summary>
		/// Sets the ScrollRect style.
		/// </summary>
		/// <param name="style">Style.</param>
		protected virtual void SetStyleScrollRect(Style style)
		{
			#if UNITY_5_3 || UNITY_5_3_OR_NEWER
			var viewport = scrollRect.viewport != null ? scrollRect.viewport : Container.parent;
			#else
			var viewport = Container.parent;
			#endif
			style.Collections.Viewport.ApplyTo(viewport.GetComponent<Image>());

			style.HorizontalScrollbar.ApplyTo(scrollRect.horizontalScrollbar);
			style.VerticalScrollbar.ApplyTo(scrollRect.verticalScrollbar);
		}

		/// <summary>
		/// Set the specified style.
		/// </summary>
		/// <returns><c>true</c>, if style was set for children gameobjects, <c>false</c> otherwise.</returns>
		/// <param name="style">Style data.</param>
		public override bool SetStyle(Style style)
		{
			SetStyleDefaultItem(style);

			SetStyleColors(style);

			SetStyleScrollRect(style);

			style.Collections.MainBackground.ApplyTo(GetComponent<Image>());

			if (IsTable)
			{
				var image = Utilites.GetOrAddComponent<Image>(Container);
				image.sprite = null;
				image.color = DefaultColor;

				var mask_image = Utilites.GetOrAddComponent<Image>(Container.parent);
				mask_image.sprite = null;

				var mask = Utilites.GetOrAddComponent<Mask>(Container.parent);
				mask.showMaskGraphic = false;

				defaultBackgroundColor = style.Table.Background.Color;
			}

			if (ComponentsPool != null)
			{
				ComponentsColoring(true);
			}

			style.ApplyTo(transform.Find("Header"));

			return true;
		}
		#endregion
	}
}