using Godot;
using System;

public partial class VeinShaderSet : MeshInstance3D
{
	// Called when the node enters the scene tree for the first time.
	public void SetShaderPos(Vector3 vec, float pathPos)
	{
		SetInstanceShaderParameter("positionOffset",vec);
		SetInstanceShaderParameter("pathPos",pathPos);
	}
		
}
