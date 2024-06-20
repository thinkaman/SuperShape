using UnityEditor;

[CustomEditor(typeof(SuperShapeButton), true)]
[CanEditMultipleObjects]
public class SuperShapeButtonEditor : Editor
{
    SerializedProperty m_InteractableProperty;
    SerializedProperty m_ColorBlockProperty;
    SerializedProperty isToggleProperty;
    SerializedProperty isPairedToggleProperty;
    SerializedProperty isToggledProperty;

    SerializedProperty m_OnClickProperty;

    protected void OnEnable()
    {
        m_InteractableProperty = serializedObject.FindProperty("m_Interactable");
        m_ColorBlockProperty = serializedObject.FindProperty("m_Colors");
        isToggleProperty = serializedObject.FindProperty("isToggle");
        isPairedToggleProperty = serializedObject.FindProperty("isPairedToggle");
        isToggledProperty = serializedObject.FindProperty("isToggled");

        m_OnClickProperty = serializedObject.FindProperty("m_OnClick");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        SuperShapeButton button = (SuperShapeButton)serializedObject.targetObject;

        EditorGUILayout.PropertyField(m_InteractableProperty);
        EditorGUILayout.PropertyField(m_ColorBlockProperty);
        EditorGUILayout.PropertyField(isToggleProperty);
        if(button.isToggle)
        {
            EditorGUILayout.PropertyField(isPairedToggleProperty);
            EditorGUILayout.PropertyField(isToggledProperty);
        }

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(m_OnClickProperty);
        serializedObject.ApplyModifiedProperties();
    }
}