using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(WiggleProfile))]
public class WiggleProfileDrawerDrawer : PropertyDrawer
{
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		label = EditorGUI.BeginProperty(position, label, property);
			EditorGUI.indentLevel = 0;
			EditorGUIUtility.labelWidth = 50f;
			Rect fullRect = EditorGUI.PrefixLabel(position, label);
			Rect pos = new Rect(fullRect.position + new Vector2(0f, 0f), new Vector2(fullRect.width - 80 - 70 - 70 - 4, 16f));
			EditorGUIUtility.labelWidth = 36f;
			EditorGUI.PropertyField(pos, property.FindPropertyRelative("pattern"), new GUIContent("Style"));
			pos.x += pos.width + 4;

			EditorGUIUtility.labelWidth = 42f;
			pos.width = 76;
			EditorGUI.PropertyField(pos, property.FindPropertyRelative("speed"), new GUIContent("Speed"));
			pos.x += 80f;

			pos.width = 66f;
			EditorGUI.PropertyField(pos, property.FindPropertyRelative("lerp"), GUIContent.none);
			pos.x += 70;

			pos.width = 66f;
			EditorGUI.PropertyField(pos, property.FindPropertyRelative("rotateOption"), GUIContent.none);


			pos.position = fullRect.position + new Vector2(10f, 18f);

			EditorGUIUtility.labelWidth = 36f;
			pos.width = fullRect.width - 10 - 60 - 70 - 70 - 4;
			EditorGUI.PropertyField(pos, property.FindPropertyRelative("musicOption"), new GUIContent("Music"));
			pos.x += pos.width + 4;

			EditorGUIUtility.labelWidth = 32f;
			pos.width = 56;
			EditorGUI.PropertyField(pos, property.FindPropertyRelative("musicBand"), new GUIContent("Band"));
			pos.x += 60;

			EditorGUIUtility.labelWidth = 24f;
			pos.width = 66;
			EditorGUI.PropertyField(pos, property.FindPropertyRelative("volumeRatio"), new GUIContent("Vol"));
			pos.x += 70;

			EditorGUIUtility.labelWidth = 28f;
			pos.width = 66;
			EditorGUI.PropertyField(pos, property.FindPropertyRelative("minVolume"), new GUIContent("Min"));

		EditorGUI.EndProperty();
	}

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		return 34f; //2 lines
	}
}