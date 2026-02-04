using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;
using System.Linq.Expressions;

//using System.Numerics;


using System.Runtime.InteropServices;
using System.Threading.Tasks;
public partial class PlayerController : Node3D
{
	[ExportGroup("Main Objects")]

	[Export] public Node3D PlayerObject;
	[Export] public RigidBody3D rigidBody;

	[Export] public Node3D CameraTransform;
	[Export] public Controller UIController;
	[Export] public CameraManager cameraScript;
	[Export] public Control deathScreen;

	[ExportGroup("VFX")]
	[Export] public GpuParticles3D deathVFX;

	[ExportGroup("Audio")]
	[Export] public AudioStreamPlayer3D deathSFX;
	[Export] public AudioStreamPlayer3D shotgunSFX;

	[ExportGroup("Values")]
	[Export] public Vector3 CameraOffset;
	[Export] public int invulnerabilityPeriod;
	
	//[Export] public float TileSize;
	[Export] public float AccelerationMultiplier;
	[Export] public float MaxSpeed;
	[Export] public float FRICTION;
	[Export] public float SKYBOXSPEED;
	[Export] public float HealthGainOnKill;
	[ExportGroup("Misc Objects")]
	[Export] public Skybox skybox;
	[Export] public Timer InvulnerabilityTimer;
	[ExportGroup("Combat")]
	[Export] public Weapon Shotgun;
	[Export] public Weapon Automatic;

	private WeaponsHandler weaponsHandler;
	public ShipManager shipManager;

	
	private float angle = 0;
	private int time = 0;
	private bool alive = false;
	public int Score = 0;
	public bool isInvulnerable = false;
	private Standard std = new Standard();

	public void KillAchieved()
	{
		// when we get a kill
		Score++;
		shipManager.Heal(HealthGainOnKill);
	}
	void UpdateMovement()
	{
		Vector3 moveDirection = Vector3.Zero;
		if (Input.IsKeyPressed(Key.W))
		{
			moveDirection.Z = 1;
		}
		else if (Input.IsKeyPressed(Key.S))
		{
			moveDirection.Z = -1;
		}
		if (Input.IsKeyPressed(Key.A))
		{
			moveDirection.X = 1;
		}
		else if (Input.IsKeyPressed(Key.D))
		{
			moveDirection.X = -1;
		}
		shipManager.UpdateMovement(moveDirection);
	}


	void UpdateCamera()
	{
		CameraTransform.Position = rigidBody.Position + CameraOffset;
	}

	void UpdateSkybox()
	{
		skybox.SetShaderPosition(new Vector2(rigidBody.Position.X/SKYBOXSPEED, rigidBody.Position.Z/SKYBOXSPEED));
	}
	
	void UpdateCombat(double delta)
	{
		if (Input.IsMouseButtonPressed(MouseButton.Left))
		{
			weaponsHandler.TryFire(Automatic);
		}
		else
		{
			weaponsHandler.UnfireWeapon(Automatic);
		}
		if (Input.IsKeyPressed(Key.E))
		{
			weaponsHandler.TryFire(Shotgun);
		}
	}

	void UpdateUI()
	{
		UIController.SetHealth(shipManager.GetHealth().ToString());
		UIController.SetScore(Score.ToString());
	}
	private async Task UpdateInvunerability()
	{
		if (InvulnerabilityTimer.TimeLeft == 0)
		{
			isInvulnerable = false;
		}
		else
		{
			isInvulnerable = true;
		}
	}
	public override void _PhysicsProcess(double delta)
	{
		if (!alive) {return;}
		UpdateInvunerability();
		base._PhysicsProcess(delta);
		UpdateMovement();
	}

	void UpdateRotation()
	{
		Vector2 mouseCoordinates = GetViewport().GetMousePosition();
		Vector2 gameSize = DisplayServer.WindowGetSize();

		Vector2 normalizedCoordiantes;
		normalizedCoordiantes.X = gameSize.X/2 - mouseCoordinates.X;
		normalizedCoordiantes.Y = gameSize.Y/2 - mouseCoordinates.Y;

		angle = Mathf.Atan2(normalizedCoordiantes.X, normalizedCoordiantes.Y);

		shipManager.UpdateRotation(new Vector3(0, angle,0));
	}

	private async Task UpdateDeath()
	{
		if (shipManager.ShouldDie())
		{
			alive = false;
			deathVFX.Emitting = true;
			PlayerObject.QueueFree();

			deathSFX.Playing = true;
			
			CameraManager cam = (CameraManager)GetNode("PlayerCam");
			cam.Explode(3.5f, 3.5f, 2,2);
				
			await ToSignal(deathSFX, AudioStreamPlayer3D.SignalName.Finished);
			deathScreen.Visible = true;
			QueueFree();
		}
	}
	public override void _Process(double delta)
	{
		time++;
		UpdateDeath();
		if (!alive) {return;}
		UpdateUI();
		UpdateCombat(delta);
		UpdateRotation();
		UpdateCamera();
	}
	public override void _Ready()
	{
		weaponsHandler = new WeaponsHandler();
		shipManager = new ShipManager();

		weaponsHandler.SetShipManager(shipManager);

		weaponsHandler.AddWeapon(Shotgun, "shotgun");
		weaponsHandler.AddWeapon(Automatic, "automatic");

		shipManager.SetRigidbody(rigidBody);
		shipManager.SetPlayerStructure(PlayerObject);
		shipManager.SetMovementParameters(AccelerationMultiplier, MaxSpeed, FRICTION);
		alive = true;
	}
}
