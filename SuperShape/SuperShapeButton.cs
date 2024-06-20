using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[ExecuteAlways]
public class SuperShapeButton : BlinkingButton
{
    [SerializeField]
    private SuperShape m_TargetSuperShape;

    void Awake()
    {
        if (m_TargetSuperShape == null)
            m_TargetSuperShape = GetComponent<SuperShape>();
    }

#if UNITY_EDITOR
    void Reset()
    {
        m_TargetSuperShape = GetComponent<SuperShape>();
    }
#endif // if UNITY_EDITOR

    protected override void SetColorState(bool isInstant)
    {
		if (!gameObject.activeInHierarchy) { return; }
		m_Colors.currentState = currentState;
		float mult = (m_Colors.currentState == ColorBlockState.Highlighted || m_Colors.currentState == ColorBlockState.Pressed) ? 1.333f : 1;
		m_Colors.Update(isInstant ? 9999 : m_TargetSuperShape.deltaTime * mult);
        m_TargetSuperShape.layerColors[0] = m_Colors.GetColor();
        if (m_TargetSuperShape.layerCount >= 3)
        {
            m_TargetSuperShape.layerColors[2] = m_TargetSuperShape.layerColors[0];
        }
    }

    protected override  void EvaluateAndTransitionToSelectionState()
    {
		if (!isActiveAndEnabled || !IsInteractable()) { return; }

        m_TargetSuperShape.isClickedGutterDescending = isPointerDown && isPointerInside && !(isToggle && isPairedToggle && isToggled);
        SetColorState(false);
    }
}
