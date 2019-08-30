﻿namespace UIWidgets
{
	using System;
	using UIWidgets.Styles;
	using UnityEngine;

	/// <summary>
	/// DatePicker.
	/// </summary>
	public class TimePicker : Picker<TimeSpan, TimePicker>
	{
		/// <summary>
		/// ListView.
		/// </summary>
		[SerializeField]
		public TimeBase Time;

		/// <summary>
		/// Value.
		/// </summary>
		protected TimeSpan Value;

		/// <summary>
		/// Prepare picker to open.
		/// </summary>
		/// <param name="defaultValue">Default value.</param>
		public override void BeforeOpen(TimeSpan defaultValue)
		{
			Value = defaultValue;
			Time.Time = defaultValue;

			Time.OnTimeChanged.AddListener(TimeSelected);
		}

		/// <summary>
		/// Process selected time.
		/// </summary>
		/// <param name="time">Time.</param>
		protected void TimeSelected(TimeSpan time)
		{
			Value = time;
		}

		/// <summary>
		/// Pick the selected time.
		/// </summary>
		public void Ok()
		{
			Selected(Value);
		}

		/// <summary>
		/// Prepare picker to close.
		/// </summary>
		public override void BeforeClose()
		{
			Time.OnTimeChanged.RemoveListener(TimeSelected);
		}

		#region IStylable implementation

		/// <summary>
		/// Set the specified style.
		/// </summary>
		/// <returns><c>true</c>, if style was set for children gameobjects, <c>false</c> otherwise.</returns>
		/// <param name="style">Style data.</param>
		public override bool SetStyle(Style style)
		{
			base.SetStyle(style);

			Time.SetStyle(style);

			style.Dialog.Button.ApplyTo(transform.Find("Buttons/Cancel"));
			style.Dialog.Button.ApplyTo(transform.Find("Buttons/OK"));

			return true;
		}
		#endregion
	}
}