namespace UIWidgets
{
	using System;
	using UIWidgets.Attributes;
	using UIWidgets.Styles;
	using UnityEngine;
	using UnityEngine.UI;

	/// <summary>
	/// Time widget for 24 hour format.
	/// </summary>
	public class TimeScroller : TimeBase
	{
		/// <summary>
		/// Time comparison precision.
		/// </summary>
		protected enum Precision
		{
			/// <summary>
			/// Days.
			/// </summary>
			Days = 0,

			/// <summary>
			/// Hours.
			/// </summary>
			Hours,

			/// <summary>
			/// Minutes.
			/// </summary>
			Minutes,

			/// <summary>
			/// Seconds.
			/// </summary>
			Seconds,

			/// <summary>
			/// Milliseconds.
			/// </summary>
			Milliseconds,

			/// <summary>
			/// Ticks.
			/// </summary>
			Ticks,
		}

		/// <summary>
		/// If enabled any time period changes will not change other time periods.
		/// </summary>
		[SerializeField]
		[Tooltip("If enabled any time period changes will not change other time periods.")]
		public bool IndependentScroll;

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
		/// Looped clamp the specified time.
		/// </summary>
		/// <param name="value">Time.</param>
		/// <param name="step">Step.</param>
		/// <returns>Clamped time.</returns>
		protected virtual TimeSpan LoopedClamp(TimeSpan value, TimeSpan step)
		{
			value += step;
			if (value.Ticks < 0)
			{
				value += new TimeSpan(-value.Days, 0, 0, 0);
			}

			if (value < timeMin)
			{
				value = timeMax;
			}

			if (value > timeMax)
			{
				value = timeMin;
			}

			return value;
		}

		/// <summary>
		/// Looped increment/decrement for the time.
		/// </summary>
		/// <param name="value">Start time.</param>
		/// <param name="steps">Steps.</param>
		/// <param name="step">Function increment/decrement for the time.</param>
		/// <returns>Resulted time.</returns>
		protected virtual TimeSpan LoopedTime(TimeSpan value, int steps, TimeSpan step)
		{
			for (int i = 0; i < steps; i++)
			{
				value = LoopedClamp(value, step);
			}

			return value;
		}

		/// <summary>
		/// Increment/decrement current date by hours.
		/// </summary>
		/// <param name="steps">Steps.</param>
		/// <returns>Date.</returns>
		protected virtual TimeSpan LoopedHours(int steps)
		{
			return LoopedHours(steps, HoursStep);
		}

		/// <summary>
		/// Increment/decrement current date by hours.
		/// </summary>
		/// <param name="steps">Steps.</param>
		/// <param name="hoursStep">Step.</param>
		/// <returns>Date.</returns>
		protected virtual TimeSpan LoopedHours(int steps, int hoursStep)
		{
			var value = Time;
			var step = new TimeSpan(hoursStep, 0, 0);
			var increase = steps > 0;
			if (!increase)
			{
				steps = -steps;
				step = -step;
			}

			if (!IndependentScroll)
			{
				return LoopedTime(value, steps, step);
			}

			for (int i = 0; i < steps; i++)
			{
				value += step;
				value = CopyTimePeriods(value, Precision.Days);

				var new_hours = value.Hours;
				if (TimeEquals(Time, TimeMin, Precision.Days) && (new_hours < TimeMin.Hours))
				{
					new_hours = increase ? TimeMax.Hours : Mathf.Max(TimeMin.Hours, 24 - HoursStep);
				}

				if (TimeEquals(Time, TimeMax, Precision.Days) && (new_hours > TimeMax.Hours))
				{
					new_hours = increase ? TimeMin.Hours : Mathf.Min(TimeMax.Hours, 24 - HoursStep);
				}

				if (value.Hours != new_hours)
				{
					value += new TimeSpan(new_hours - value.Hours, 0, 0);
				}
			}

			return value;
		}

		/// <summary>
		/// Increment/decrement current date by minutes.
		/// </summary>
		/// <param name="steps">Steps.</param>
		/// <returns>Date.</returns>
		protected virtual TimeSpan LoopedMinutes(int steps)
		{
			var value = Time;
			var step = new TimeSpan(0, MinutesStep, 0);
			var increase = steps > 0;
			if (!increase)
			{
				steps = -steps;
				step = -step;
			}

			if (!IndependentScroll)
			{
				return LoopedTime(value, steps, step);
			}

			for (int i = 0; i < steps; i++)
			{
				value += step;
				value = CopyTimePeriods(value, Precision.Hours);

				var new_minutes = value.Minutes;
				if (TimeEquals(Time, TimeMin, Precision.Hours) && (new_minutes < TimeMin.Minutes))
				{
					new_minutes = increase ? TimeMin.Minutes : Mathf.Max(TimeMin.Minutes, 60 - MinutesStep);
				}

				if (TimeEquals(Time, TimeMax, Precision.Hours) && (new_minutes > TimeMax.Minutes))
				{
					new_minutes = increase ? 0 : Mathf.Min(TimeMax.Minutes, 60 - MinutesStep);
				}

				if (value.Minutes != new_minutes)
				{
					value += new TimeSpan(0, new_minutes - value.Minutes, 0);
				}
			}

			return value;
		}

		/// <summary>
		/// Increment/decrement current date by seconds.
		/// </summary>
		/// <param name="steps">Steps.</param>
		/// <returns>Date.</returns>
		protected virtual TimeSpan LoopedSeconds(int steps)
		{
			var value = Time;
			var step = new TimeSpan(0, 0, SecondsStep);
			var increase = steps > 0;
			if (!increase)
			{
				steps = -steps;
				step = -step;
			}

			if (!IndependentScroll)
			{
				return LoopedTime(value, steps, step);
			}

			for (int i = 0; i < steps; i++)
			{
				value += step;
				value = CopyTimePeriods(value, Precision.Minutes);

				var new_seconds = value.Seconds;
				if (TimeEquals(Time, TimeMin, Precision.Minutes) && (new_seconds < TimeMin.Seconds))
				{
					new_seconds = increase ? TimeMin.Seconds : Mathf.Max(TimeMin.Seconds, 60 - SecondsStep);
				}

				if (TimeEquals(Time, TimeMax, Precision.Minutes) && (new_seconds > TimeMax.Seconds))
				{
					new_seconds = increase ? 0 : Mathf.Min(TimeMax.Seconds, 60 - SecondsStep);
				}

				if (value.Seconds != new_seconds)
				{
					value += new TimeSpan(0, 0, new_seconds - value.Seconds);
				}
			}

			return value;
		}

		/// <summary>
		/// Copy time periods from current time to the specified time with precision.
		/// </summary>
		/// <param name="value">Initial time.</param>
		/// <param name="precision">Copy precision.</param>
		/// <returns>Time.</returns>
		protected TimeSpan CopyTimePeriods(TimeSpan value, Precision precision)
		{
			if (value.Ticks < 0)
			{
				value += new TimeSpan(1, 0, 0, 0);
			}

			if (value.Days > 0)
			{
				value -= new TimeSpan(value.Days, 0, 0, 0);
			}

			switch (precision)
			{
				case Precision.Days:
					return value;
				case Precision.Hours:
					return value + new TimeSpan(Time.Days - value.Days, Time.Hours - value.Hours, 0, 0);
				case Precision.Minutes:
					return value + new TimeSpan(Time.Days - value.Days, Time.Hours - value.Hours, Time.Minutes - value.Minutes, 0);
				case Precision.Seconds:
					return value + new TimeSpan(Time.Days - value.Days, Time.Hours - value.Hours, Time.Minutes - value.Minutes, Time.Seconds - value.Seconds);
				case Precision.Milliseconds:
					return value + new TimeSpan(Time.Days - value.Days, Time.Hours - value.Hours, Time.Minutes - value.Minutes, Time.Seconds - value.Seconds, Time.Milliseconds - value.Milliseconds);
				case Precision.Ticks:
					return Time;
				default:
					throw new NotSupportedException("Unknown precision: " + precision);
			}
		}

		/// <summary>
		/// Compare time with specified precision.
		/// </summary>
		/// <param name="time1">Time 1.</param>
		/// <param name="time2">Time 2.</param>
		/// <param name="precision">Compare precision.</param>
		/// <returns>true if time equals; otherwise false.</returns>
		protected static bool TimeEquals(TimeSpan time1, TimeSpan time2, Precision precision)
		{
			switch (precision)
			{
				case Precision.Days:
					return time1.Days == time2.Days;
				case Precision.Hours:
					return (time1.Days == time2.Days) && (time1.Hours == time2.Hours);
				case Precision.Minutes:
					return (time1.Days == time2.Days) && (time1.Hours == time2.Hours) && (time1.Minutes == time2.Minutes);
				case Precision.Seconds:
					return (time1.Days == time2.Days) && (time1.Hours == time2.Hours) && (time1.Minutes == time2.Minutes) && (time1.Seconds == time2.Seconds);
				case Precision.Milliseconds:
					return (time1.Days == time2.Days) && (time1.Hours == time2.Hours) && (time1.Minutes == time2.Minutes) && (time1.Seconds == time2.Seconds) && (time1.Milliseconds == time2.Milliseconds);
				case Precision.Ticks:
					return time1 == time2;
				default:
					throw new NotSupportedException("Unknown precision: " + precision);
			}
		}

		/// <summary>
		/// Get value for the hours at specified step.
		/// </summary>
		/// <param name="steps">Step.</param>
		/// <returns>Formatted hours.</returns>
		protected virtual string HoursValue(int steps)
		{
			var time = LoopedHours(steps);
			if (AMPM)
			{
				var date = new DateTime(2019, 1, 2);
				date += time - date.TimeOfDay;

				return date.ToString("hh");
			}

			return time.Hours.ToString("D2");
		}

		/// <summary>
		/// Get value for the minutes at specified step.
		/// </summary>
		/// <param name="steps">Step.</param>
		/// <returns>Formatted minutes.</returns>
		protected virtual string MinutesValue(int steps)
		{
			return LoopedMinutes(steps).Minutes.ToString("D2");
		}

		/// <summary>
		/// Get value for the seconds at specified step.
		/// </summary>
		/// <param name="steps">Step.</param>
		/// <returns>Formatted seconds.</returns>
		protected virtual string SecondsValue(int steps)
		{
			return LoopedSeconds(steps).Seconds.ToString("D2");
		}

		/// <summary>
		/// Get value for the AM-PM at specified step.
		/// </summary>
		/// <param name="steps">Step.</param>
		/// <returns>Formatted AM-PM.</returns>
		protected virtual string AMPMValue(int steps)
		{
			var date = new DateTime(2019, 1, 2);
			var step = new TimeSpan(12, 0, 0);
			if (steps < 0)
			{
				steps = -steps;
				step = -step;
			}

			date += LoopedTime(Time, steps, step) - date.TimeOfDay;

			return date.ToString("tt");
		}

		/// <summary>
		/// Init the input.
		/// </summary>
		protected override void InitInput()
		{
			if (HoursScroller != null)
			{
				HoursScroller.gameObject.SetActive(Hours);

				HoursScroller.Value = HoursValue;
				HoursScroller.Decrease = () => Time = LoopedHours(-1);
				HoursScroller.Increase = () => Time = LoopedHours(+1);
				HoursScroller.IsInteractable = IsActive;

				HoursScroller.Init();
			}

			if (MinutesScroller != null)
			{
				MinutesScroller.gameObject.SetActive(Minutes);

				MinutesScroller.Value = MinutesValue;
				MinutesScroller.Decrease = () => Time = LoopedMinutes(-1);
				MinutesScroller.Increase = () => Time = LoopedMinutes(+1);
				MinutesScroller.IsInteractable = IsActive;

				MinutesScroller.Init();
			}

			if (SecondsScroller != null)
			{
				SecondsScroller.gameObject.SetActive(Seconds);

				SecondsScroller.Value = SecondsValue;
				SecondsScroller.Decrease = () => Time = LoopedSeconds(-1);
				SecondsScroller.Increase = () => Time = LoopedSeconds(+1);
				SecondsScroller.IsInteractable = IsActive;

				SecondsScroller.Init();
			}

			if (AMPMScroller != null)
			{
				AMPMScroller.gameObject.SetActive(AMPM);

				AMPMScroller.Value = AMPMValue;
				AMPMScroller.Decrease = () => Time = LoopedHours(-1, 12);
				AMPMScroller.Increase = () => Time = LoopedHours(+1, 12);
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

		/// <summary>
		/// Add the listeners.
		/// </summary>
		protected override void AddListeners()
		{
		}

		/// <summary>
		/// Removes the listeners.
		/// </summary>
		protected override void RemoveListeners()
		{
		}

		/// <summary>
		/// Updates the inputs.
		/// </summary>
		public override void UpdateInputs()
		{
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

		#region IStylable implementation

		/// <summary>
		/// Set the specified style.
		/// </summary>
		/// <returns><c>true</c>, if style was set for children gameobjects, <c>false</c> otherwise.</returns>
		/// <param name="style">Style data.</param>
		public override bool SetStyle(Style style)
		{
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

			style.Scroller.Highlight.ApplyTo(transform.Find("Highlight"));
			style.Scroller.Background.ApplyTo(GetComponent<Image>());

			return true;
		}
		#endregion
	}
}