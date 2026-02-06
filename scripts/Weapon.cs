using Godot;
using System;
using System.Collections.Generic;

public partial class Weapon : Node
{
	// data structure that contains weapon information
	[ExportGroup("Weapon Properties")]
	[ExportSubgroup("FX")]
	[Export] public GpuParticles3D ShootingParticleA;
	[Export] public GpuParticles3D ShootingParticleB;
	[Export] public AudioStreamPlayer3D ShootAudioStream;
	[ExportSubgroup("Objects")]
	[Export] public Timer ReloadTimer;
	[Export] public Node3D RaycasterObject;
	[ExportSubgroup("Properties")]
	//[Export] public float ParticleBOffset;
	[Export] public bool IsRaycastNotShapecast;
	[Export] public bool ExplosionSoundPerDeath;
	[Export] public bool OneShot;
	[Export] public float HitPoints;
	

	

	public bool DoAttributesExist()
	{
		if(
			ShootingParticleA == null ||
			ShootingParticleB == null ||
			ShootAudioStream == null ||
			RaycasterObject == null 
		)
		{
			return false;
		}
		return true;
	}
}
