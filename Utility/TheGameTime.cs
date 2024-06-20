using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TheGameTime : MonoBehaviour
{
	private const float MAXIMUM_DELTA_TIME = 0.1f;

	public static TheGameTime instance;

	private bool _isPaused;
	private float _timeGlobalScale;
	private float _time;
	private float _deltaTime;

	private bool _isMenuPaused;
	private float _menuTimeGlobalScale;
	private float _menuTime;
	private float _menuDeltaTime;

	public static bool isPaused { get { return instance._isPaused; } set { instance._isPaused = value; } }
	public static float timeGlobalScale { get { return instance._timeGlobalScale; } set { instance._timeGlobalScale = value; } }
	public static float time { get { return instance == null ? Time.time : instance._time; } }
	public static float deltaTime { get { return instance == null ? Time.deltaTime : instance._deltaTime; } }

	public static bool isMenuPaused { get { return instance._isMenuPaused; } set { instance._isMenuPaused = value; } }
	public static float menuTimeGlobalScale { get { return instance._menuTimeGlobalScale; } set { instance._menuTimeGlobalScale = value; } }
	public static float menuTime { get { return instance == null ? Time.time : instance._menuTime; } }
	public static float menuDeltaTime { get { return instance == null ? Time.deltaTime : instance._menuDeltaTime; } }


	void Awake()
	{
		if (instance != null)
		{
			Destroy(gameObject);
			return;
		}
		instance = this;

		_isPaused = false;
		_timeGlobalScale = 1;
		_time = Time.time;
		_deltaTime = Time.deltaTime;

		_isMenuPaused = false;
		_menuTimeGlobalScale = 1;
		_menuTime = Time.time;
		_menuDeltaTime = Time.deltaTime;
	}

	void Update()
	{
		_deltaTime = Mathf.Clamp(_isPaused ? 0 : Time.deltaTime * _timeGlobalScale, -MAXIMUM_DELTA_TIME, MAXIMUM_DELTA_TIME);
		_time += _deltaTime;

		_menuDeltaTime = Mathf.Clamp(_isMenuPaused ? 0 : Time.deltaTime * _menuTimeGlobalScale, -MAXIMUM_DELTA_TIME, MAXIMUM_DELTA_TIME);
		_menuTime += _menuDeltaTime;
	}
}
