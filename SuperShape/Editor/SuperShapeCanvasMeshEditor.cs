using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SuperShapeCanvasMesh))]
[CanEditMultipleObjects]
public class SuperShapeCanvasMeshEditor : Editor
{
	SerializedProperty proxyMaskCanvasMeshProp, mainColliderProp, tailColliderProp, isCachedProp, isLimitingOverdrawingProp;

	void OnEnable()
	{
		proxyMaskCanvasMeshProp = serializedObject.FindProperty("proxyMaskCanvasMesh");
		mainColliderProp = serializedObject.FindProperty("myMainCollider");
		tailColliderProp = serializedObject.FindProperty("myTailCollider");
		isCachedProp = serializedObject.FindProperty("isCached");
		isLimitingOverdrawingProp = serializedObject.FindProperty("isLimitingOverdrawing");
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();
		EditorGUILayout.PropertyField(proxyMaskCanvasMeshProp);
		EditorGUILayout.PropertyField(mainColliderProp);
		EditorGUILayout.PropertyField(tailColliderProp);
		EditorGUILayout.PropertyField(isCachedProp);
		EditorGUILayout.PropertyField(isLimitingOverdrawingProp);
		serializedObject.ApplyModifiedProperties();
	}
}
