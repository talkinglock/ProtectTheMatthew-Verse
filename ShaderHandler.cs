using Godot;
using System;

public partial class ShaderHandler : MeshInstance3D
{
	// Called when the node enters the scene tree for the first time.
	public void SetPos(Vector2 vector)
	{
		SetInstanceShaderParameter("TilingOffset", vector);	
	}

}
