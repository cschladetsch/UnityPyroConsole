namespace EasyLayoutNS
{
	using System;
	using System.Collections.Generic;
	using UnityEditor;
	using UnityEngine;

	/// <summary>
	/// EasyLayout editor.
	/// </summary>
	[CustomEditor(typeof(EasyLayout), true)]
	[CanEditMultipleObjects]
	public class EasyLayoutEditor : Editor
	{
		Dictionary<string, SerializedProperty> sProperties = new Dictionary<string, SerializedProperty>();

		string[] properties = new string[]
		{
			"groupPosition",
			"mainAxis",
			"layoutType",
			"compactConstraint",
			"compactConstraintCount",
			"gridConstraint",
			"gridConstraintCount",

			"rowAlign",
			"innerAlign",
			"cellAlign",
			"spacing",
			"symmetric",
			"margin",
			"marginTop",
			"marginBottom",
			"marginLeft",
			"marginRight",
			"topToBottom",
			"rightToLeft",
			"skipInactive",
			/*
			"ControlWidth",
			"MaxWidth",
			"ControlHeight",
			"MaxHeight",
			*/
			"childrenWidth",
			"childrenHeight",

			"SettingsChanged",
			"flexSettings",
			"staggeredSettings",
		};

		/// <summary>
		/// Init.
		/// </summary>
		protected virtual void OnEnable()
		{
			Array.ForEach(targets, x =>
			{
				var l = x as EasyLayout;
				if (l != null)
				{
					l.Upgrade();
				}
			});
			sProperties.Clear();

			Array.ForEach(properties, x =>
			{
				var p = serializedObject.FindProperty(x);
				if (p != null)
				{
					sProperties.Add(x, p);
				}
			});
		}

		/// <summary>
		/// Draw inspector GUI.
		/// </summary>
		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.PropertyField(sProperties["mainAxis"], true);
			EditorGUILayout.PropertyField(sProperties["layoutType"], true);

			EditorGUI.indentLevel++;

			switch ((LayoutTypes)sProperties["layoutType"].enumValueIndex)
			{
				case LayoutTypes.Compact:
					EditorGUILayout.PropertyField(sProperties["groupPosition"], true);
					EditorGUILayout.PropertyField(sProperties["rowAlign"], true);
					EditorGUILayout.PropertyField(sProperties["innerAlign"], true);

					EditorGUILayout.PropertyField(sProperties["compactConstraint"], true);

					// not flexible
					if (sProperties["compactConstraint"].enumValueIndex != 0)
					{
						EditorGUI.indentLevel++;
						EditorGUILayout.PropertyField(sProperties["compactConstraintCount"], true);
						EditorGUI.indentLevel--;
					}

					break;
				case LayoutTypes.Grid:
					EditorGUILayout.PropertyField(sProperties["groupPosition"], true);
					EditorGUILayout.PropertyField(sProperties["cellAlign"], true);

					EditorGUILayout.PropertyField(sProperties["gridConstraint"], true);

					// not flexible
					if (sProperties["gridConstraint"].enumValueIndex != 0)
					{
						EditorGUI.indentLevel++;
						EditorGUILayout.PropertyField(sProperties["gridConstraintCount"], true);
						EditorGUI.indentLevel--;
					}

					break;
				case LayoutTypes.Flex:
					EditorGUILayout.PropertyField(sProperties["flexSettings"], true);
					break;
				case LayoutTypes.Staggered:
					EditorGUILayout.PropertyField(sProperties["staggeredSettings"], true);
					break;
			}

			EditorGUI.indentLevel--;

			EditorGUILayout.PropertyField(sProperties["spacing"], true);
			EditorGUILayout.PropertyField(sProperties["symmetric"], true);

			if (sProperties["symmetric"].boolValue)
			{
				EditorGUILayout.PropertyField(sProperties["margin"], true);
			}
			else
			{
				EditorGUILayout.PropertyField(sProperties["marginTop"], true);
				EditorGUILayout.PropertyField(sProperties["marginBottom"], true);
				EditorGUILayout.PropertyField(sProperties["marginLeft"], true);
				EditorGUILayout.PropertyField(sProperties["marginRight"], true);
			}

			EditorGUILayout.PropertyField(sProperties["skipInactive"], true);
			EditorGUILayout.PropertyField(sProperties["rightToLeft"], true);
			EditorGUILayout.PropertyField(sProperties["topToBottom"], true);

			EditorGUILayout.PropertyField(sProperties["childrenWidth"], true);
			EditorGUILayout.PropertyField(sProperties["childrenHeight"], true);

			EditorGUILayout.PropertyField(sProperties["SettingsChanged"], true);

			if (targets.Length == 1)
			{
				var script = (EasyLayout)target;

				EditorGUILayout.LabelField("Block size", string.Format("{0}x{1}", script.BlockSize.x, script.BlockSize.y));
				EditorGUILayout.LabelField("UI size", string.Format("{0}x{1}", script.UISize.x, script.UISize.y));
				EditorGUILayout.LabelField("Size", string.Format("{0}x{1}", script.Size.x, script.Size.y));
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
}