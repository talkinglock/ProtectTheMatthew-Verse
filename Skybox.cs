using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Threading.Tasks.Dataflow;

public partial class Skybox : Node3D
{
	//private Dictionary<(int x, int y), Node3D> grid = new();
	[Export] public ShaderHandler shader;
	public override void _Ready()
	{
		
	}

	//public Dictionary<(int x, int y), Node3D> getGrid()
	//{
		//return grid;
	//}
	public void SetShaderPosition(Vector2 vector)
	{
		shader.SetPos(vector);
	}
	/*public void GenerateTile(Vector2I coords)
	{
		
			coords are where to clone the skybox part for tiling
			uniform passed into shader should reflect to the change to avoid shader tiling and mismatch across borders
		
		if (grid.TryGetValue((coords.X, coords.Y), out var obj)) {
			Debug.WriteLine("[ERROR!] Skybox tile already generated at Coordinates: " + coords.ToString());
			return;
		}
		MeshInstance3D tile = (MeshInstance3D)BaseTile.Duplicate();
		GetParent().AddChild(tile);
		tile.Position = new Vector3(BaseTile.Scale.X * (coords.X * 2), -20, BaseTile.Scale.Z * (coords.Y * 2));
		grid[(coords.X, coords.Y)] = tile;
		Debug.WriteLine("[SKYBOX] Tile constructed, relative coords: " + coords.ToString());
		Debug.WriteLine(tile.Position);
	}
	public void DestroyTile(Vector2I coords)
	{
		if (grid.TryGetValue((coords.X, coords.Y), out var obj)) {
			obj.Dispose();
			grid.Remove((coords.X, coords.Y));
		}
		else
		{
			Debug.WriteLine("[ERROR!] Skybox tile not generated at Coordinates, cannot destroy!: " + coords.ToString());
			return;
		}
	}
	*/
}
