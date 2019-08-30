namespace UIWidgets
{
	using System;
	using UIWidgets.Attributes;
	using UIWidgets.Styles;
	using UnityEngine;

	/// <summary>
	/// DateTime scroller widget.
	/// </summary>
	public class DateTimeScroller : DateScroller
	{
		[SerializeField]
		bool hours = true;

		/// <summary>
		/// Display hours scroller.
		/// </summary>
		public bool Hours
		{
			get
			{
				return hours;
			}

			set
			{
				hours = value;

				if (HoursScroller != null)
				{
					HoursScroller.gameObject.SetActive(hours);
				}
			}
		}

		/// <summary>
		/// Scroller for the hour.
		/// </summary>
		[SerializeField]
		[EditorConditionBool("hours")]
		protected Scroller HoursScroller;

		/// <summary>
		/// Step to change hour.
		/// </summary>
		[SerializeField]
		[EditorConditionBool("hours")]
		public int HoursStep = 1;

		/// <summary>
		/// Format to display hours.
		/// </summary>
		[SerializeField]
		[EditorConditionBool("hours")]
		public string HoursFormat = "HH";

		/// <summary>
		/// Format to display hours.
		/// </summary>
		[SerializeField]
		[EditorConditionBool("hours")]
		public string HoursAMPMFormat = "hh";

		[SerializeField]
		bool minutes = true;

		/// <summary>
		/// Display minutes scroller.
		/// </summary>
		public bool Minutes
		{
			get
			{
				return minutes;
			}

			set
			{
				minutes = value;

				if (MinutesScroller != null)
				{
					MinutesScroller.gameObject.SetActive(minutes);
				}
			}
		}

		/// <summary>
		/// Scroller for the minute.
		/// </summary>
		[SerializeField]
		[EditorConditionBool("minutes")]
		protected Scroller MinutesScroller;

		/// <summary>
		/// Step to change minutes.
		/// </summary>
		[SerializeField]
		[EditorConditionBool("minutes")]
		public int MinutesStep = 1;

		/// <summary>
		/// Format to display minutes.
		/// </summary>
		[SerializeField]
		[EditorConditionBool("minutes")]
		public string MinutesFormat = "mm";

		[SerializeField]
		bool seconds = true;

		/// <summary>
		/// Display seconds scroller.
		/// </summary>
		public bool Seconds
		{
			get
			{
				return seconds;
			}

			set
			{
				seconds = value;

				if (SecondsScroller != null)
				{
					SecondsScroller.gameObject.SetActive(seconds);
				}
			}
		}

		/// <summary>
		/// Scroller for the second.
		/// </summary>
		[SerializeField]
		[EditorConditionBool("seconds")]
		protected Scroller SecondsScroller;

		/// <summary>
		/// Step to change seconds.
		/// </summary>
		[SerializeField]
		[EditorConditionBool("seconds")]
		public int SecondsStep = 1;

		/// <summary>
		/// Format to display seconds.
		/// </summary>
		[SerializeField]
		[EditorConditionBool("seconds")]
		public string SecondsFormat = "ss";

		[SerializeField]
		bool ampm = true;

		/// <summary>
		/// Display AM-PM scroller.
		/// </summary>
		public bool AMPM
		{
			get
			{
				return ampm;
			}

			set
			{
				ampm = value;

				if (AMPMScroller != null)
				{
					AMPMScroller.gameObject.SetActive(ampm);
				}
			}
		}

		/// <summary>
		/// Scroller for the AM-PM.
		/// </summary>
		[SerializeField]
		[EditorConditionBool("ampm")]
		protected Scroller AMPMScroller;

		/// <summary>
		/// Format to display AM-PM.
		/// </summary>
		[SerializeField]
		[EditorConditionBool("ampm")]
		public string AMPMFormat = "tt";

		/// <summary>
		/// Increment/decrement current date by hours.
		/// </summary>
		/// <param name="steps">Steps.</param>
		/// <returns>Date.</returns>
		protected virtual DateTime LoopedHours(int steps)
		{
			return LoopedHours(steps, HoursStep);
		}

		/// <summary>
		/// Increment/decrement current date by hours.
		/// </summary>
		/// <param name="steps">Steps.</param>
		/// <param name="step">Step.</param>
		/// <returns>Date.</returns>
		protected virtual DateTime LoopedHours(int steps, int step)
		{
			var value = Date;
			var increase = steps > 0;
			if (!increase)
			{
				steps = -steps;
				step = -step;
			}

			if (!IndependentScroll)
			{
				return LoopedDate(value, steps, x => x.AddHours(step));
			}

			for (int i = 0; i < steps; i++)
			{
				value = value.AddHours(step);
				value = CopyTimePeriods(value, Precision.Days);

				var new_hours = value.Hour;
				if (DateTimeEquals(Date, DateMin, Precision.Days) && (new_hours < DateMin.Hour))
				{
					new_hours = increase ? DateMax.Hour : Mathf.Max(DateMin.Hour, 24 - HoursStep);
				}

				if (DateTimeEquals(Date, DateMax, Precision.Days) && (new_hours > DateMax.Hour))
				{
					new_hours = increase ? DateMin.Hour : Mathf.Min(DateMax.Hour, 24 - HoursStep);
				}

				if (value.Hour != new_hours)
				{
					value = value.AddHours(new_hours - value.Hour);
				}
			}

			return value;
		}

		/// <summary>
		/// Increment/decrement current date by minutes.
		/// </summary>
		/// <param name="steps">Steps.</param>
		/// <returns>Date.</returns>
		protected virtual DateTime LoopedMinutes(int steps)
		{
			var value = Date;
			var step = MinutesStep;
			var increase = steps > 0;
			if (!increase)
			{
				steps = -steps;
				step = -step;
			}

			if (!IndependentScroll)
			{
				return LoopedDate(value, steps, x => x.AddMinutes(step));
			}

			for (int i = 0; i < steps; i++)
			{
				value = value.AddMinutes(step);
				value = CopyTimePeriods(value, Precision.Hours);

				var new_minutes = value.Minute;
				if (DateTimeEquals(Date, DateMin, Precision.Hours) && (new_minutes < DateMin.Minute))
				{
					new_minutes = increase ? DateMax.Minute : Mathf.Max(DateMin.Minute, 60 - MinutesStep);
				}

				if (DateTimeEquals(Date, DateMax, Precision.Hours) && (new_minutes > DateMax.Minute))
				{
					new_minutes = increase ? DateMin.Minute : Mathf.Min(DateMax.Minute, 60 - MinutesStep);
				}

				if (value.Minute != new_minutes)
				{
					value = value.AddMinutes(new_minutes - value.Minute);
				}
			}

			return value;
		}

		/// <summary>
		/// Increment/decrement current date by seconds.
		/// </summary>
		/// <param name="steps">Steps.</param>
		/// <returns>Date.</returns>
		protected virtual DateTime LoopedSeconds(int steps)
		{
			var value = Date;
			var step = SecondsStep;
			var increase = steps > 0;
			if (!increase)
			{
				steps = -steps;
				step = -step;
			}

			if (!IndependentScroll)
			{
				return LoopedDate(value, steps, x => x.AddSeconds(step));
			}

			for (int i = 0; i < steps; i++)
			{
				value = value.AddSeconds(step);
				value = CopyTimePeriods(value, Precision.Minutes);

				var new_seconds = value.Second;
				if (DateTimeEquals(Date, DateMin, Precision.Minutes) && (new_seconds < DateMin.Second))
				{
					new_seconds = increase ? DateMax.Second : Mathf.Max(DateMin.Second, 60 - SecondsStep);
				}

				if (DateTimeEquals(Date, DateMax, Precision.Minutes) && (new_seconds > DateMax.Second))
				{
					new_seconds = increase ? DateMin.Second : Mathf.Min(DateMax.Second, 60 - SecondsStep);
				}

				if (value.Second != new_seconds)
				{
					value = value.AddSeconds(new_seconds - value.Second);
				}
			}

			return value;
		}

		/// <summary>
		/// Get value for the hours at specified step.
		/// </summary>
		/// <param name="steps">Step.</param>
		/// <returns>Formatted hours.</returns>
		protected string HoursValue(int steps)
		{
			return Date.AddHours(steps * HoursStep).ToString(AMPM ? HoursAMPMFormat : HoursFormat, Culture);
		}

		/// <summary>
		/// Get value for the minutes at specified step.
		/// </summary>
		/// <param name="steps">Step.</param>
		/// <returns>Formatted minutes.</returns>
		protected string MinutesValue(int steps)
		{
			return Date.AddMinutes(steps * MinutesStep).ToString(MinutesFormat, Culture);
		}

		/// <summary>
		/// Get value for the seconds at specified step.
		/// </summary>
		/// <param name="steps">Step.</param>
		/// <returns>Formatted seconds.</returns>
		protected string SecondsValue(int steps)
		{
			return Date.AddSeconds(steps * SecondsStep).ToString(SecondsFormat, Culture);
		}

		/// <summary>
		/// Get value for the AM-PM at specified step.
		/// </summary>
		/// <param name="steps">Step.</param>
		/// <returns>Formatted AM-PM.</returns>
		protected virtual string AMPMValue(int steps)
		{
			return Date.AddHours(steps * 12).ToString(AMPMFormat, Culture);
		}

		/// <summary>
		/// Updates the calendar.
		/// Should be called only after changing culture settings.
		/// </summary>
		public override void UpdateCalendar()
		{
			base.UpdateCalendar();

			if (HoursScroller != null)
			{
				HoursScroller.SetText();
			}

			if (MinutesScroller != null)
			{
				MinutesScroller.SetText();
			}

			if (SecondsScroller != null)
			{
				SecondsScroller.SetText();
			}

			if (AMPMScroller != null)
			{
				AMPMScroller.SetText();
			}
		}

		bool isInitedTime;

		/// <summary>
		/// Init.
		/// </summary>
		protected override void Init()
		{
			base.Init();

			if (isInitedTime)
			{
				return;
			}

			isInitedTime = true;

			if (HoursScroller != null)
			{
				HoursScroller.gameObject.SetActive(Hours);

				HoursScroller.Value = HoursValue;
				HoursScroller.Decrease = () => Date = LoopedHours(-1);
				HoursScroller.Increase = () => Date = LoopedHours(+1);
				HoursScroller.IsInteractable = IsActive;

				HoursScroller.Init();
			}

			if (MinutesScroller != null)
			{
				MinutesScroller.gameObject.SetActive(Minutes);

				MinutesScroller.Value = MinutesValue;
				MinutesScroller.Decrease = () => Date = LoopedMinutes(-1);
				MinutesScroller.Increase = () => Date = LoopedMinutes(+1);
				MinutesScroller.IsInteractable = IsActive;

				MinutesScroller.Init();
			}

			if (SecondsScroller != null)
			{
				SecondsScroller.gameObject.SetActive(Seconds);

				SecondsScroller.Value = SecondsValue;
				SecondsScroller.Decrease = () => Date = LoopedSeconds(-1);
				SecondsScroller.Increase = () => Date = LoopedSeconds(+1);
				SecondsScroller.IsInteractable = IsActive;

				SecondsScroller.Init();
			}

			if (AMPMScroller != null)
			{
				AMPMScroller.gameObject.SetActive(AMPM);

				AMPMScroller.Value = AMPMValue;
				AMPMScroller.Decrease = () => Date = LoopedHours(-1, 12);
				AMPMScroller.Increase = () => Date = LoopedHours(+1, 12);
				AMPMScroller.IsInteractable = IsActive;

				AMPMScroller.Init();
			}
		}

		/// <summary>
		/// Process interactable change.
		/// </summary>
		/// <param name="interactableState">Current interactable state.</param>
		protected override void OnInteractableChange(bool interactableState)
		{
		}

#if UNITY_EDITOR
		/// <summary>
		/// Modify default values for the date time.
		/// </summary>
		protected override void OnValidate()
		{
			base.OnValidate();

			if (format == "yyyy-MM-dd")
			{
				format = "yyyy-MM-dd HH:mm:ss";
				DefaultDateMin = DateTime.MinValue.ToString(Format);
				DefaultDateMax = DateTime.MaxValue.ToString(Format);
				DefaultDate = DateTime.Now.ToString(Format);
			}
		}
#endif

		#region IStylable implementation

		/// <summary>
		/// Set the specified style.
		/// </summary>
		/// <returns><c>true</c>, if style was set for children gameobjects, <c>false</c> otherwise.</returns>
		/// <param name="style">Style data.</param>
		public override bool SetStyle(Style style)
		{
			base.SetStyle(style);

			if (HoursScroller != null)
			{
				HoursScroller.SetStyle(style);
			}

			if (MinutesScroller != null)
			{
				MinutesScroller.SetStyle(style);
			}

			if (SecondsScroller != null)
			{
				SecondsScroller.SetStyle(style);
			}

			if (AMPMScroller != null)
			{
				AMPMScroller.SetStyle(style);
			}

			return true;
		}
		#endregion
	}
}