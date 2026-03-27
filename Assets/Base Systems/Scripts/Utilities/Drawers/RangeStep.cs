using System;
using System.Globalization;
using UnityEditor;
using UnityEngine;

// Use Example 1: [RangeStep(0f, 10f, 0.25f)]
// Use Example 2: [RangeStep(100, 1000, 25)]
namespace Fiber.Utilities
{
	[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
	public sealed class RangeStep : PropertyAttribute
	{
		internal readonly float min = 0f;
		internal readonly float max = 100f;
		internal readonly float step = 1f;
		internal readonly int precision;
		// Whether a increase that is not the step is allowed (Occurs when we're reaching the end)
		internal readonly bool allowNonStepReach = true;
		internal readonly bool isInt = false;

		/// <summary>
		/// Allow you to increase a float value in step, make sure the type of the variable matches the parameters
		/// </summary>
		/// <param name="min"></param>
		/// <param name="max"></param>
		/// <param name="step"></param>
		/// <param name="allowNonStepReach">Whether an increase that is not the step is allowed (Occurs when we are reaching the end)</param>
		public RangeStep(float min, float max, float step = 1f, bool allowNonStepReach = true)
		{
			this.min = min;
			this.max = max;
			this.step = step;
			precision = Precision(this.step);
			this.allowNonStepReach = allowNonStepReach;
			isInt = false;
		}

		/// <summary>
		/// Allow you to increase an int value in steps, make sure the type of the variable matches the parameters
		/// </summary>
		/// <param name="min"></param>
		/// <param name="max"></param>
		/// <param name="step"></param>
		/// <param name="allowNonStepReach"></param>
		public RangeStep(int min, int max, int step = 1, bool allowNonStepReach = true)
		{
			this.min = min;
			this.max = max;
			this.step = step;
			this.allowNonStepReach = allowNonStepReach;
			isInt = true;
		}

		private static int Precision(float f)
		{
			string str = f.ToString(CultureInfo.InvariantCulture);

			int decimalPointPos = str.IndexOf('.');

			if (decimalPointPos < 0)
			{
				return 0;
			}

			int decimalPlaces = str.Length - decimalPointPos - 1;

			return Mathf.Min(decimalPlaces, 7);
		}
	}

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(RangeStep))]
	internal sealed class RangeStepDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var rangeAttribute = (RangeStep)base.attribute;

			if (!rangeAttribute.isInt)
			{
				float rawFloat = EditorGUI.Slider(position, label, property.floatValue, rangeAttribute.min, rangeAttribute.max);
				property.floatValue = Step(rawFloat, rangeAttribute);
			}
			else
			{
				int rawInt = EditorGUI.IntSlider(position, label, property.intValue, (int)rangeAttribute.min, (int)rangeAttribute.max);
				property.intValue = Step(rawInt, rangeAttribute);
			}
		}

		internal float Step(float rawValue, RangeStep range)
		{
			var f = rawValue;

			if (range.allowNonStepReach)
			{
				// In order to ensure a reach, where the difference between rawValue and the max allowed value is less than step
				var topCap = (float)Math.Round(Mathf.Floor(range.max / range.step) * range.step, range.precision);
				var topRemaining = (float)Math.Round(range.max - topCap, range.precision);

				// If this is the special case near the top maximum
				if (topRemaining < range.step && f > topCap + topRemaining / 2)
				{
					f = range.max;
				}
				else
				{
					// Otherwise we do a regular snap
					f = (float)Math.Round(Snap(f, range.step), range.precision);
				}
			}
			else if (!range.allowNonStepReach)
			{
				f = (float)Math.Round(Snap(f, range.step), range.precision);
				// Make sure the value doesn't exceed the maximum allowed range
				if (!(f > range.max)) return f;
				f -= range.step;
				f = (float)Math.Round(f, range.precision);
			}

			return f;
		}

		internal int Step(int rawValue, RangeStep range)
		{
			int f = rawValue;

			if (range.allowNonStepReach)
			{
				// In order to ensure a reach, where the difference between rawValue and the max allowed value is less than step
				var topCap = (int)range.max / (int)range.step * (int)range.step;
				var topRemaining = (int)range.max - topCap;

				// If this is the special case near the top maximum
				if (topRemaining < range.step && f > topCap)
				{
					f = (int)range.max;
				}
				else
				{
					// Otherwise we do a regular snap
					f = (int)Snap(f, range.step);
				}
			}
			else if (!range.allowNonStepReach)
			{
				f = (int)Snap(f, range.step);
				// Make sure the value doesn't exceed the maximum allowed range
				if (f > range.max)
				{
					f -= (int)range.step;
				}
			}

			return f;
		}

		/// <summary>
		/// Snap a value to a interval
		/// </summary>
		internal static float Snap(float value, float snapInterval)
		{
			return Mathf.Round(value / snapInterval) * snapInterval;
		}
	}
#endif
}