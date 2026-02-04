
using System;
using System.Diagnostics;
using Godot;

public class Standard
{
	public void ASSERT(bool condition, string info)
	{
		if (condition) { return; }
		throw new Exception("[STD] ASSERT CHECK FAILED! INFO: " + info);
	}
	
	public bool SOFTASSERT(bool condition, string info)
	{
		if (condition) { return true; }
		Debug.WriteLine("[STD] SOFT ASSERT CHECK FAILED! PROGRAM WILL CONTINUE! INFO: " + info);
		return false;
	}

	public void setCombatMouse(bool status)
	{
		if (status)
		{
			Input.SetCustomMouseCursor(ResourceLoader.Load("res://res/images/crosshairStandard.png"));
		}
		else
		{
			Input.SetCustomMouseCursor(ResourceLoader.Load("res://res/images/crosshairStandardHit.png"));
		}
	}
}
