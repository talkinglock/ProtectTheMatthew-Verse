using Godot;
using System;

public partial class Controller : Control
{
	// Called when the node enters the scene tree for the first time.
	public void SetScore(string score)
	{
		GetNode<TextEdit>("TextEdit").Text = "Score: " + score;
	}
	public void SetHealth(string score)
	{
		GetNode<TextEdit>("Health").Text = "Health: " + score;
	}
}
