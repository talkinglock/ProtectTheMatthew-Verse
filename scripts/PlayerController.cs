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
	[Export] public AnimationPlayer IdleAnimation;

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
	/*[Export] public Path3D VeinPath;
	[Export] public PathFollow3D VeinFollower;
	[Export] public float InterVeinDistance;
	[Export] public ShaderMaterial VeinShaderMaterial;
	*/
	[ExportGroup("Combat")]
	[Export] public Weapon Shotgun;
	[Export] public Weapon Automatic;
	[Export] public Weapon Dash;
	[Export] public float DashDistance;
	[Export] public float DashTime;
	[Export] public float EndDashSpeed;
	

	private WeaponsHandler weaponsHandler;
	public ShipManager shipManager;

	
	private float angle = 0;
	private int time = 0;
	private bool alive = false;
	private bool canMove = true;
	public int Score = 0;
	public bool isInvulnerable = false;
	private Standard std = new Standard();

	public void KillAchieved()
	{
		// when we get a kill
		Score++;
		shipManager.Heal(HealthGainOnKill);
	}
	async Task UpdateMovement()
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
		if (canMove)
		{
			shipManager.UpdateMovement(moveDirection);
		}
	}


	void UpdateCamera()
	{
		Vector2 resolution = DisplayServer.WindowGetSize();
		Vector2 mousePosUIRelative = GetViewport().GetMousePosition();
		Vector2 mousePosShipRelative = new Vector2(
			2 * ((resolution.X/2) - mousePosUIRelative.X)/resolution.X,
			2* ((resolution.Y/2) - mousePosUIRelative.Y )/resolution.Y
		);
		Vector3 mousePosShipRelative3 = new Vector3(
			mousePosShipRelative.X,
			0,
			mousePosShipRelative.Y
		); 

		Debug.WriteLine(mousePosShipRelative3);
		CameraTransform.Position = PlayerObject.GlobalPosition + CameraOffset + mousePosShipRelative3;
	}

	void UpdateSkybox()
	{
		skybox.SetShaderPosition(new Vector2(rigidBody.Position.X/SKYBOXSPEED, rigidBody.Position.Z/SKYBOXSPEED));
	}
	
	async Task FireDash()
	{
		weaponsHandler.TryFire(Dash);
		canMove = false;
		Vector3 forwardVector = shipManager.GetForwardVector(angle);
		Vector3 endPosShipRelative = forwardVector * DashDistance;
		Vector3 endPosWorldRelative = endPosShipRelative + rigidBody.GlobalPosition;

		// tween to position
		Tween tweener = GetTree().CreateTween();
		tweener.SetTrans(Tween.TransitionType.Linear);
		tweener.TweenProperty(rigidBody, "position", endPosWorldRelative, DashTime);

		await ToSignal(tweener, Tween.SignalName.Finished);

		// apply impulse to immediately move us to max speed
		float forceMag = rigidBody.Mass * EndDashSpeed; // force for instant impulse
		Vector3 force = forwardVector * forceMag;
		rigidBody.ApplyImpulse(force);

		canMove = true;
	}
	void UpdateCombat(double delta)
	{
		if (Input.IsMouseButtonPressed(MouseButton.Left))
		{
			if (weaponsHandler.CanFire(Automatic))
			{
				IdleAnimation.PlayWithCapture("Auto");
				weaponsHandler.TryFire(Automatic);
			}
		}
		else
		{
			weaponsHandler.UnfireWeapon(Automatic);
			IdleAnimation.PlayWithCapture("new_animation");
		}

		if (Input.IsKeyPressed(Key.Space) && weaponsHandler.CanFire(Dash))
		{
			FireDash();
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

		IdleAnimation.PlayWithCapture("new_animation");

		alive = true;
	}
}
