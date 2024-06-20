using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[ExecuteAlways]
public class BlinkingButton : MonoBehaviour
{
	[SerializeField]
	protected BlinkingColorBlock m_Colors = BlinkingColorBlock.defaultColorBlock;
	public bool isBlinking { get { return m_Colors.isBlinking; } set { m_Colors.isBlinking = value; } }
	public void CommandBlink(float duration = -1, Lerp lerp = Lerp.Triangle) { m_Colors.CommandBlink(duration, lerp); }

	[Tooltip("Can this Button be interacted with?")]
	[SerializeField]
	private bool m_Interactable = true;

	public bool isToggle;
	public bool isPairedToggle;
	public bool isToggled;

	public bool isLockingDialogueAdvance;


	[Serializable]
	public class ButtonClickedEvent : UnityEvent { }

	[SerializeField]
	public ButtonClickedEvent m_OnClick = new ButtonClickedEvent();

	public ButtonClickedEvent onClick
	{
		get { return m_OnClick; }
		set { m_OnClick = value; }
	}

	private void Press()
	{
		if (!isActiveAndEnabled || !IsInteractable()) return;
		if (isToggle)
		{
			if (isPairedToggle && isToggled) { return; }
			isToggled = !isToggled;
		}
		m_OnClick.Invoke();
	}

	public void OnMouseUpAsButton()
	{
		Press();
	}

	private static bool SetStruct<T>(ref T currentValue, T newValue) where T : struct
	{
		if (EqualityComparer<T>.Default.Equals(currentValue, newValue))
			return false;

		currentValue = newValue;
		return true;
	}

	public BlinkingColorBlock colors { get { return m_Colors; } set { if (SetStruct(ref m_Colors, value)) OnSetProperty(); } }

	[NonSerialized]
	public float[] currentColorRatios = new float[4] { 1.0f, 0, 0, 0 };

	public bool interactable
	{
		get { return m_Interactable; }
		set
		{
			if (SetStruct(ref m_Interactable, value))
			{
				if (!m_Interactable && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject)
					EventSystem.current.SetSelectedGameObject(null);
				OnSetProperty();
			}
		}
	}

	protected bool isPointerInside { get; set; }
	protected bool isPointerDown { get; set; }

	public virtual bool IsInteractable()
	{
		return m_Interactable;
	}

	void OnDidApplyAnimationProperties()
	{
		OnSetProperty();
	}

	void OnEnable()
	{
		isPointerDown = false;
		SetColorState(true);
	}

	private void OnSetProperty()
	{
#if UNITY_EDITOR
		if (!Application.isPlaying)
			SetColorState(true);
		else
#endif
			SetColorState(false);
	}

	void OnDisable()
	{
		InstantClearState();
	}

#if UNITY_EDITOR
	void OnValidate()
	{
		if (m_Colors.fadeDuration < 0) { m_Colors.fadeDuration = 0; }

		// OnValidate can be called before OnEnable, this makes it unsafe to access other components
		// since they might not have been initialized yet.
		if (isActiveAndEnabled)
		{
			if (!interactable && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject)
				EventSystem.current.SetSelectedGameObject(null);

			SetColorState(true);
		}
	}

#endif // if UNITY_EDITOR

	protected ColorBlockState currentState
	{
		get
		{
			if (!IsInteractable())
				return ColorBlockState.Disabled;
			if (isToggle && isPairedToggle && isToggled)
				return ColorBlockState.Toggled;
			if (isPointerDown)
				return ColorBlockState.Pressed;
			if (isPointerInside)
				return ColorBlockState.Highlighted;
			if (isToggle && isToggled)
				return ColorBlockState.Toggled;
			return ColorBlockState.Normal;
		}
	}

	void InstantClearState()
	{
		isPointerInside = false;
		isPointerDown = false;

		SetColorState(true);
	}

	protected virtual void SetColorState(bool isInstant)
	{
		if (!gameObject.activeInHierarchy) { return; }
		m_Colors.currentState = currentState;
		float mult = (m_Colors.currentState == ColorBlockState.Highlighted || m_Colors.currentState == ColorBlockState.Pressed) ? 1.333f : 1;
	}

	protected virtual void Update()
	{
		SetColorState(false);

	}

	protected bool IsHighlighted()
	{
		if (!isActiveAndEnabled || !IsInteractable())
			return false;
		return isPointerInside && !isPointerDown;
	}

	protected bool IsPressed()
	{
		if (!isActiveAndEnabled || !IsInteractable()) return false;
		return isPointerDown;
	}

	protected virtual void EvaluateAndTransitionToSelectionState()
	{
		if (!isActiveAndEnabled || !IsInteractable()) { return; }
		SetColorState(false);
	}

	public void OnMouseDown()
	{
		EventSystem.current.SetSelectedGameObject(gameObject);
		isPointerDown = true;
		EvaluateAndTransitionToSelectionState();
	}

	public void OnMouseUp()
	{
		isPointerDown = false;
		EvaluateAndTransitionToSelectionState();
	}

	public void OnMouseEnter()
	{
		isPointerInside = true;
		EvaluateAndTransitionToSelectionState();
	}

	public void OnMouseExit()
	{
		isPointerInside = false;
		EvaluateAndTransitionToSelectionState();
	}
}
