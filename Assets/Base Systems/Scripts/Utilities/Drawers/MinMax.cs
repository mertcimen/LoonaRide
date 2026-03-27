using UnityEditor;
using UnityEngine;

// Use Example 1: [RangeStep(0f, 10f)] Vector2 minMax;
// Use Example 2: [RangeStep(100, 1000)] Vector2Int minMax;
namespace Fiber.Utilities
{
	public sealed class MinMax : PropertyAttribute
	{
		public float MinLimit = 0;
		public float MaxLimit = 1;
		public bool ShowEditRange;
		public bool ShowDebugValues;

		public MinMax(int min, int max)
		{
			MinLimit = min;
			MaxLimit = max;
		}
	}

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(MinMax))]
	public class MinMaxDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			// cast the attribute to make life easier
			var minMax = (MinMax)attribute;

			// This only works on a vector2 and vector2Int! ignore on any other property type (we should probably draw an error message instead!)
			if (property.propertyType == SerializedPropertyType.Vector2)
			{
				// if we're flagged to draw in a special mode, let us modify the drawing rectangle to draw only one line at a time
				if (minMax.ShowDebugValues || minMax.ShowEditRange)
				{
					position = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
				}

				// pull out a bunch of helpful min/max values....
				var minValue = property.vector2Value.x; // the currently set minimum and maximum value
				var maxValue = property.vector2Value.y;
				var minLimit = minMax.MinLimit; // the limit for both min and max, min cant go lower than minLimit and maax cant top maxLimit
				var maxLimit = minMax.MaxLimit;

				// and ask unity to draw them all nice for us!
				EditorGUI.MinMaxSlider(position, label, ref minValue, ref maxValue, minLimit, maxLimit);

				var vec = Vector2.zero; // save the results into the property!
				vec.x = minValue;
				vec.y = maxValue;

				property.vector2Value = vec;

				// Do we have a special mode flagged? time to draw lines!
				if (minMax.ShowDebugValues || minMax.ShowEditRange)
				{
					var isEditable = minMax.ShowEditRange;

					if (!isEditable)
						GUI.enabled = false; // if were just in debug mode and not edit mode, make sure all the UI is read only!

					// move the draw rect on by one line
					position.y += EditorGUIUtility.singleLineHeight;

					var vals = new float[] { minLimit, minValue, maxValue, maxLimit }; // shove the values and limits into a vector4 and draw them all at once
					EditorGUI.MultiFloatField(position, new GUIContent("Range"),
						new GUIContent[] { new GUIContent("MinLimit"), new GUIContent("MinVal"), new GUIContent("MaxVal"), new GUIContent("MaxLimit") }, vals);

					GUI.enabled = false; // the range part is always read-only
					position.y += EditorGUIUtility.singleLineHeight;
					EditorGUI.FloatField(position, "Selected Range", maxValue - minValue);
					GUI.enabled = true; // remember to make the UI editable again!

					if (isEditable)
						property.vector2Value = new Vector2(vals[1], vals[2]); // save off any change to the value~
				}
			}
			else if (property.propertyType == SerializedPropertyType.Vector2Int)
			{
				// if we are flagged to draw in a special mode, lets modify the drawing rectangle to draw only one line at a time
				if (minMax.ShowDebugValues || minMax.ShowEditRange)
					position = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

				// pull out a bunch of helpful min/max values....
				float minValue = property.vector2IntValue.x; // the currently set minimum and maximum value
				float maxValue = property.vector2IntValue.y;
				var minLimit = Mathf.RoundToInt(minMax.MinLimit); // the limit for both min and max, min cant go lower than minLimit and maax cant top maxLimit
				var maxLimit = Mathf.RoundToInt(minMax.MaxLimit);

				// and ask unity to draw them all nice for us!
				EditorGUI.MinMaxSlider(position, label, ref minValue, ref maxValue, minLimit, maxLimit);

				var vec = Vector2Int.zero; // save the results into the property!
				vec.x = Mathf.RoundToInt(minValue);
				vec.y = Mathf.RoundToInt(maxValue);

				property.vector2IntValue = vec;

				// Do we have a special mode flagged? time to draw lines!
				if (!minMax.ShowDebugValues && !minMax.ShowEditRange) return;
				
				var isEditable = minMax.ShowEditRange;

				if (!isEditable)
					GUI.enabled = false; // if were just in debug mode and not edit mode, make sure all the UI is read-only!

				// move the draw rect on by one line
				position.y += EditorGUIUtility.singleLineHeight;

				var vals = new float[] { minLimit, minValue, maxValue, maxLimit }; // shove the values and limits into a vector4 and draw them all at once
				EditorGUI.MultiFloatField(position, new GUIContent("Range"),
					new GUIContent[] { new GUIContent("MinLimit"), new GUIContent("MinVal"), new GUIContent("MaxVal"), new GUIContent("MaxLimit") }, vals);

				GUI.enabled = false; // the range part is always read-only
				position.y += EditorGUIUtility.singleLineHeight;
				EditorGUI.FloatField(position, "Selected Range", maxValue - minValue);
				GUI.enabled = true; // remember to make the UI editable again!

				if (isEditable)
					property.vector2IntValue = new Vector2Int(Mathf.RoundToInt(vals[1]), Mathf.RoundToInt(vals[2])); // save off any change to the value~
			}
		}

		// This method lets unity know how big to draw the property. We need to override this because it could end up being more than one line big
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var minMax = (MinMax)attribute;

			// By default, just return the standard line height
			var size = EditorGUIUtility.singleLineHeight;

			// If we have a special mode, add two extra lines!
			if (minMax.ShowEditRange || minMax.ShowDebugValues)
				size += EditorGUIUtility.singleLineHeight * 2;

			return size;
		}
	}
#endif
}