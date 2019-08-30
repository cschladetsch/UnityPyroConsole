namespace UIWidgets
{
	using System;
	using System.Collections.Generic;
	using UIWidgets.Styles;
	using UnityEngine;
	using UnityEngine.EventSystems;
	using UnityEngine.UI;

	/// <summary>
	/// Scroller. Show list of the string.
	/// </summary>
	[RequireComponent(typeof(EasyLayoutNS.EasyLayout))]
	[RequireComponent(typeof(Mask))]
	public class Scroller : UIBehaviourConditional, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler, IStylable
	{
		/// <summary>
		/// Do nothing.
		/// </summary>
		protected static void DoNothing()
		{
		}

		/// <summary>
		/// Default function to get value.
		/// </summary>
		/// <param name="step">Step.</param>
		/// <returns>Value.</returns>
		protected static string DefaultValue(int step)
		{
			return "Index: " + step;
		}

		/// <summary>
		/// Default function to check is scroller interactable.
		/// </summary>
		/// <returns>true if scroller is interactable; otherwise false.</returns>
		protected static bool DefaultInteractable()
		{
			return true;
		}

		/// <summary>
		/// Action to increase the value.
		/// </summary>
		public Action Increase = DoNothing;

		/// <summary>
		/// Action to decrease the value.
		/// </summary>
		public Action Decrease = DoNothing;

		/// <summary>
		/// Convert index to the displayed string.
		/// </summary>
		public Func<int, string> Value = DefaultValue;

		/// <summary>
		/// Is scroller interactable?
		/// </summary>
		public Func<bool> IsInteractable = DefaultInteractable;

		/// <summary>
		/// Size of the DefaultItem.
		/// </summary>
		[NonSerialized]
		protected Vector2 DefaultItemSize;

		[SerializeField]
		ScrollerItem defaultItem;

		/// <summary>
		/// DefaultItem.
		/// </summary>
		public ScrollerItem DefaultItem
		{
			get
			{
				return defaultItem;
			}

			set
			{
				if (defaultItem != value)
				{
					defaultItem = value;

					UpdateLayout();
					Resize();
				}
			}
		}

		/// <summary>
		/// Layout.
		/// </summary>
		[NonSerialized]
		protected EasyLayoutBridge Layout;

		/// <summary>
		/// Used instances of the DefaultItem.
		/// </summary>
		[NonSerialized]
		protected List<ScrollerItem> Components = new List<ScrollerItem>();

		/// <summary>
		/// Unused instances of the DefaultItem.
		/// </summary>
		[NonSerialized]
		protected List<ScrollerItem> ComponentsCache = new List<ScrollerItem>();

		/// <summary>
		/// Is horizontal scroll.
		/// Not supported yet.
		/// </summary>
		[SerializeField]
		[HideInInspector]
		protected bool IsHorizontal;

		/// <summary>
		/// Scroll sensitivity.
		/// </summary>
		[SerializeField]
		public float ScrollSensitivity = 15f;

		/// <summary>
		/// Layout internal padding.
		/// </summary>
		protected float Padding
		{
			get
			{
				return Layout.GetFiller().x;
			}

			set
			{
				var padding = ClampPadding(value);
				Layout.SetFiller(padding, 0f);
			}
		}

		/// <summary>
		/// Animate inertia scroll with unscaled time.
		/// </summary>
		[SerializeField]
		public bool UnscaledTime = true;

		/// <summary>
		/// Time to stop.
		/// </summary>
		[SerializeField]
		public float TimeToStop = 0.5f;

		/// <summary>
		/// Velocity.
		/// </summary>
		[NonSerialized]
		protected float ScrollVelocity;

		/// <summary>
		/// Inertia velocity.
		/// </summary>
		[NonSerialized]
		protected float IntertiaVelocity;

		/// <summary>
		/// Current deceleration rate.
		/// </summary>
		[NonSerialized]
		protected float CurrentDecelerationRate;

		/// <summary>
		/// Inertia distance.
		/// </summary>
		[NonSerialized]
		protected float InertiaDistance;

		/// <summary>
		/// Is drag event occurring?
		/// </summary>
		[NonSerialized]
		protected bool IsDragging;

		/// <summary>
		/// Is scrolling occurring?
		/// </summary>
		[NonSerialized]
		protected bool IsScrolling;

		/// <summary>
		/// Previous scroll value.
		/// </summary>
		[NonSerialized]
		protected float PrevScrollValue;

		/// <summary>
		/// Current scroll value.
		/// </summary>
		[NonSerialized]
		protected float CurrentScrollValue;

		RectTransform rectTransform;

		/// <summary>
		/// Current RectTransformn.
		/// </summary>
		protected RectTransform RectTransform
		{
			get
			{
				if (rectTransform == null)
				{
					rectTransform = transform as RectTransform;
				}

				return rectTransform;
			}
		}

		/// <summary>
		/// Median index of the components.
		/// </summary>
		protected int ComponentsMedian
		{
			get
			{
				return Components.Count / 2;
			}
		}

		bool isInited;

		/// <summary>
		/// Start this instance.
		/// </summary>
		protected override void Start()
		{
			Init();
		}

		/// <summary>
		/// Init this instance.
		/// </summary>
		public void Init()
		{
			if (isInited)
			{
				return;
			}

			isInited = true;

			UpdateLayout();

			var resizer = Utilites.GetOrAddComponent<ResizeListener>(this);
			resizer.OnResize.AddListener(Resize);

			Resize();

			AlignComponents();
		}

		/// <summary>
		/// Clamp padding value.
		/// </summary>
		/// <param name="padding">Padding.</param>
		/// <returns>Clamped value.</returns>
		protected float ClampPadding(float padding)
		{
			var size = ItemFullSize();
			var center = GetCenter();
			if (Mathf.Round(padding) > (-size))
			{
				var n = Mathf.FloorToInt((Mathf.Round(padding) - center) / size);
				if (n > 0)
				{
					padding -= n * size;
					for (int i = 0; i < n; i++)
					{
						Decrease();
					}

					SetText();
				}
			}
			else if (Mathf.Round(padding) < (-size))
			{
				var n = Mathf.FloorToInt((Mathf.Round(-padding) + center) / size);
				if (n > 0)
				{
					padding += n * size;
					for (int i = 0; i < n; i++)
					{
						Increase();
					}

					SetText();
				}
			}

			return padding;
		}

		/// <summary>
		/// Update the layout.
		/// </summary>
		protected void UpdateLayout()
		{
			Layout = new EasyLayoutBridge(GetComponent<EasyLayoutNS.EasyLayout>(), DefaultItem.transform as RectTransform, false, false);
			Layout.IsHorizontal = IsHorizontal;

			DefaultItemSize = Layout.GetItemSize();
			DefaultItem.gameObject.SetActive(false);
		}

		/// <summary>
		/// Container size.
		/// </summary>
		/// <returns>Size.</returns>
		protected float ContainerSize()
		{
			return (IsHorizontal ? RectTransform.rect.width : RectTransform.rect.height) - Layout.GetFullMargin();
		}

		/// <summary>
		/// Content size.
		/// </summary>
		/// <returns>Size.</returns>
		protected float ContentSize()
		{
			return (ItemFullSize() * Components.Count) - Layout.GetSpacing();
		}

		/// <summary>
		/// Item size.
		/// </summary>
		/// <returns>Size.</returns>
		protected float ItemSize()
		{
			return IsHorizontal ? DefaultItemSize.x : DefaultItemSize.y;
		}

		/// <summary>
		/// Item size with spacing.
		/// </summary>
		/// <returns>Size.</returns>
		protected float ItemFullSize()
		{
			return ItemSize() + Layout.GetSpacing();
		}

		/// <summary>
		/// Calculate the maximum count of the visible components.
		/// </summary>
		/// <returns>Maximum count of the visible components.</returns>
		protected int CalculateMax()
		{
			var result = Mathf.CeilToInt((ContainerSize() + Layout.GetSpacing()) / ItemFullSize()) + 1;

			if ((result % 2) == 0)
			{
				result += 1;
			}

			return result;
		}

		/// <summary>
		/// Is component is null?
		/// </summary>
		/// <param name="component">Component.</param>
		/// <returns>true if component is null; otherwise, false.</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Reviewed.")]
		protected bool IsNullComponent(ScrollerItem component)
		{
			return component == null;
		}

		/// <summary>
		/// Set component count.
		/// </summary>
		/// <param name="count">Count.</param>
		protected void SetComponentCount(int count)
		{
			if (count == Components.Count)
			{
				return;
			}

			if (Components.Count < count)
			{
				ComponentsCache.RemoveAll(IsNullComponent);

				for (int i = Components.Count; i < count; i++)
				{
					Components.Add(CreateComponent());
				}
			}
			else
			{
				for (int i = count; i < Components.Count; i++)
				{
					ComponentsCache.Add(Components[i]);
				}

				Components.RemoveRange(count, Components.Count - count);
			}
		}

		/// <summary>
		/// Process RectTransform resize.
		/// </summary>
		protected void Resize()
		{
			var max = CalculateMax();

			if (max == Components.Count)
			{
				return;
			}

			SetComponentCount(max);

			var median = ComponentsMedian;
			for (int i = 0; i < Components.Count; i++)
			{
				Components[i].Index = i - median;
				Components[i].Owner = this;
				Components[i].transform.SetAsLastSibling();
			}

			SetText();
		}

		/// <summary>
		/// Create component instance.
		/// </summary>
		/// <returns>Component instance.</returns>
		protected ScrollerItem CreateComponent()
		{
			ScrollerItem component;

			if (ComponentsCache.Count > 0)
			{
				component = ComponentsCache[ComponentsCache.Count - 1];
				ComponentsCache.RemoveAt(ComponentsCache.Count - 1);
			}
			else
			{
				component = Compatibility.Instantiate(DefaultItem);
				component.transform.SetParent(transform, false);
				Utilites.FixInstantiated(DefaultItem, component);
				component.Owner = this;
			}

			component.gameObject.SetActive(true);

			return component;
		}

		/// <summary>
		/// Set text of the specified component.
		/// </summary>
		/// <param name="component">Component.</param>
		protected void SetComponentText(ScrollerItem component)
		{
			component.Text.Value = Value(component.Index);
		}

		/// <summary>
		/// Set text.
		/// </summary>
		public void SetText()
		{
			Components.ForEach(SetComponentText);
		}

		/// <summary>
		/// Returns true if the GameObject and the Component are active.
		/// </summary>
		/// <returns>true if the GameObject and the Component are active; otherwise false.</returns>
		public override bool IsActive()
		{
			return base.IsActive() && isInited && IsInteractable();
		}

		/// <summary>
		/// Process the begin drag event.
		/// </summary>
		/// <param name="eventData">Event data.</param>
		public virtual void OnBeginDrag(PointerEventData eventData)
		{
			if (eventData.button != PointerEventData.InputButton.Left)
			{
				return;
			}

			if (!IsActive())
			{
				return;
			}

			IsDragging = true;

			PrevScrollValue = Padding;
			CurrentScrollValue = Padding;

			StopInertia();
		}

		/// <summary>
		/// Process the drag event.
		/// </summary>
		/// <param name="eventData">Event data.</param>
		public virtual void OnDrag(PointerEventData eventData)
		{
			if (!IsDragging)
			{
				return;
			}

			if (eventData.button != PointerEventData.InputButton.Left)
			{
				return;
			}

			if (!IsActive())
			{
				return;
			}

			StopInertia();
			var scroll_delta = IsHorizontal ? eventData.delta.x : -eventData.delta.y;
			Scroll(scroll_delta);
		}

		/// <summary>
		/// Process scroll event.
		/// </summary>
		/// <param name="eventData">Event data.</param>
		public void OnScroll(PointerEventData eventData)
		{
			if (!IsActive())
			{
				return;
			}

			IsScrolling = true;
			var scroll_delta = IsHorizontal ? eventData.scrollDelta.x : -eventData.scrollDelta.y;
			Scroll(scroll_delta * ScrollSensitivity);
		}

		/// <summary>
		/// Scroll.
		/// </summary>
		/// <param name="delta">Delta.</param>
		protected virtual void Scroll(float delta)
		{
			Padding += delta;

			CurrentScrollValue += delta;
			var time_delta = Utilites.DefaultGetDeltaTime(UnscaledTime);
			var new_velocity = (PrevScrollValue - CurrentScrollValue) / time_delta;
			ScrollVelocity = Mathf.Lerp(ScrollVelocity, new_velocity, time_delta * 10);
			PrevScrollValue = CurrentScrollValue;
		}

		/// <summary>
		/// Process the end drag event.
		/// </summary>
		/// <param name="eventData">Event data.</param>
		public virtual void OnEndDrag(PointerEventData eventData)
		{
			if (!IsDragging)
			{
				return;
			}

			if (eventData.button != PointerEventData.InputButton.Left)
			{
				return;
			}

			IsDragging = false;
			InitIntertia();
		}

		/// <summary>
		/// Init inertia.
		/// </summary>
		protected virtual void InitIntertia()
		{
			IntertiaVelocity = -ScrollVelocity;
			CurrentDecelerationRate = -IntertiaVelocity / TimeToStop;

			var direction = Mathf.Sign(IntertiaVelocity);
			var time_to_stop_sq = Mathf.Pow(TimeToStop, 2f);
			var distance = ((-Mathf.Abs(CurrentDecelerationRate) * time_to_stop_sq) / 2f) + (Mathf.Abs(IntertiaVelocity) * TimeToStop);
			InertiaDistance = ClampDistance(distance, direction);
			IntertiaVelocity = (InertiaDistance - (-Mathf.Abs(CurrentDecelerationRate) * (TimeToStop * TimeToStop) / 2f)) / TimeToStop;
			IntertiaVelocity *= direction;
		}

		/// <summary>
		/// Late update.
		/// </summary>
		protected virtual void LateUpdate()
		{
			if (IsScrolling)
			{
				IsScrolling = false;
				InitIntertia();
			}
			else if (!IsDragging && (InertiaDistance > 0f))
			{
				var delta = Utilites.DefaultGetDeltaTime(UnscaledTime);
				var distance = IntertiaVelocity > 0f
					? Mathf.Min(InertiaDistance, IntertiaVelocity * delta)
					: Mathf.Max(-InertiaDistance, IntertiaVelocity * delta);

				Padding += distance;
				InertiaDistance -= Mathf.Abs(distance);

				if (InertiaDistance > 0f)
				{
					IntertiaVelocity += CurrentDecelerationRate * delta;
					ScrollVelocity = -IntertiaVelocity;
				}
				else
				{
					StopInertia();
				}
			}
		}

		/// <summary>
		/// Stop inertia.
		/// </summary>
		protected void StopInertia()
		{
			CurrentDecelerationRate = 0f;
			InertiaDistance = 0f;
		}

		/// <summary>
		/// Clamp distance to stop right at value.
		/// </summary>
		/// <param name="distance">Distance.</param>
		/// <param name="direction">Scroll direction.</param>
		/// <returns>Clamped distance.</returns>
		protected float ClampDistance(float distance, float direction)
		{
			var extra = (GetCenter() - Padding) * direction;
			var steps = Mathf.Round((Mathf.Abs(distance) - extra) / ItemFullSize());
			var new_distance = (steps * ItemFullSize()) + extra;
			return new_distance;
		}

		/// <summary>
		/// Get center.
		/// </summary>
		/// <returns>Center.</returns>
		protected float GetCenter()
		{
			return -(ContentSize() - ContainerSize()) / 2f;
		}

		/// <summary>
		/// Align components.
		/// </summary>
		protected void AlignComponents()
		{
			Padding = GetCenter();
		}

		/// <summary>
		/// Process the destroy event.
		/// </summary>
		protected override void OnDestroy()
		{
			var resizer = GetComponent<ResizeListener>();
			if (resizer != null)
			{
				resizer.OnResize.RemoveListener(Resize);
			}
		}

		#region IStylable implementation

		/// <summary>
		/// Set the specified style.
		/// </summary>
		/// <returns><c>true</c>, if style was set for children gameobjects, <c>false</c> otherwise.</returns>
		/// <param name="style">Style data.</param>
		public bool SetStyle(Style style)
		{
			if (DefaultItem.Text != null)
			{
				style.Scroller.Text.ApplyTo(DefaultItem.Text.GameObject);

				if (isInited)
				{
					Components.ForEach(x => style.Scroller.Text.ApplyTo(x.Text.GameObject));
					ComponentsCache.ForEach(x => style.Scroller.Text.ApplyTo(x.Text.GameObject));
				}
			}

			return true;
		}
		#endregion
	}
}