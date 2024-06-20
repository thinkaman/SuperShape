using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SuperShapeMesh))]
[CanEditMultipleObjects]
public class SuperShapeMeshEditor : Editor
{
	SerializedProperty proxyMaskMeshProp, mainColliderProp, tailColliderProp, isCachedProp, isLimitingOverdrawingProp;

	void OnEnable()
	{
		proxyMaskMeshProp = serializedObject.FindProperty("_proxyMaskMesh");
		mainColliderProp = serializedObject.FindProperty("myMainCollider");
		tailColliderProp = serializedObject.FindProperty("myTailCollider");
		isCachedProp = serializedObject.FindProperty("isCached");
		isLimitingOverdrawingProp = serializedObject.FindProperty("isLimitingOverdrawing");
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();
		EditorGUILayout.PropertyField(proxyMaskMeshProp);
		EditorGUILayout.PropertyField(mainColliderProp);
		EditorGUILayout.PropertyField(tailColliderProp);
		EditorGUILayout.PropertyField(isCachedProp);
		EditorGUILayout.PropertyField(isLimitingOverdrawingProp);
		serializedObject.ApplyModifiedProperties();
	}
}
