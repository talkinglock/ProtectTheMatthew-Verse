using Godot;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

public partial class Main : Node
{
	public void _Process()
	{
		//Debug.WriteLine("Running");
	}


	public static async Task DamageShip(ShipManager enemy, float hitPoints)
	{
		//Debug.WriteLine("Damaging");
		enemy.TakeDamage(hitPoints);
	}
}
