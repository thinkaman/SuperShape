using UnityEngine;
using System;
using System.Collections;

public class Call
{
	Action callbackMethodA;
	Action<object[]> callbackMethodB;
	object[] args;
	int mutexCount;
	public float timer;
	bool isRealtime = false;

	public Call(Action callbackMethod, params object[] args)
	{
		this.callbackMethodA = callbackMethod;
		this.args = args;
		timer = 0;
		mutexCount = 0;
	}

	public Call(Action<object[]> callbackMethod, params object[] args)
	{
		this.callbackMethodB = callbackMethod;
		this.args = args;
		timer = 0;
		mutexCount = 0;
	}

	public Call(float delay, Action callbackMethod, params object[] args)
	{
		this.callbackMethodA = callbackMethod;
		this.args = args;
		timer = delay;
		mutexCount = 0;
	}

	public Call(float delay, Action<object[]> callbackMethod, params object[] args)
	{
		this.callbackMethodB = callbackMethod;
		this.args = args;
		timer = delay;
		mutexCount = 0;
	}

	public Call SetRealtime() { isRealtime = true; return this; }

	public Action<object[]> backs
	{
		get
		{
			mutexCount++;
			return back;
		}
	}

	public void back(object[] argsOverride = null)
	{
		if (timer > 0)
		{
			TheGameTime.instance.StartCoroutine(DelayCoroutine());
		}
		else
		{
			Execute();
		}
	}

	public void UpdateTarget(Action callbackMethod, params object[] args)
	{
		this.callbackMethodA = callbackMethod;
		this.args = args;
	}

	public void UpdateTarget(Action<object[]> callbackMethod, params object[] args)
	{
		this.callbackMethodB = callbackMethod;
		this.args = args;
	}

	private void Execute(object[] argsOverride = null)
	{
		mutexCount--;
		if (mutexCount > 0) { return; }
		if (callbackMethodA != null)
		{
			callbackMethodA();
		}
		else
		{
			callbackMethodB(argsOverride == null ? args : argsOverride);
		}
	}

	private IEnumerator DelayCoroutine()
	{
		if (isRealtime)
		{
			yield return new WaitForSecondsRealtime(timer);
		}
		else
		{
			yield return new WaitForSeconds(timer);
		}
		timer = 0;
		back();
	}

	public override string ToString()
	{
		return callbackMethodA.ToString() + (mutexCount > 0 ? " " + mutexCount.ToString() : "");
	}
}
