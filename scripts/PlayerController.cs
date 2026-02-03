using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;

//using System.Numerics;

using System.Runtime.InteropServices;
public partial class PlayerController : Node3D
{
	[Export] public Node3D PlayerObject;
	[Export] public float combatFrequency;
	[Export] public Node3D CameraTransform;
	[Export] public Controller UIController;
	[Export] public Vector3 s_OFFSET;
	[Export] public int invulnerabilityPeriod;
	[Export] public RayCast3D raycaster;
	[Export] public AudioStreamPlayer3D shoot;
	[Export] public GpuParticles3D deathVFX;
	[Export] public AudioStreamPlayer3D deathSFX;
	[Export] public AudioStreamPlayer3D shotgunSFX;
	//[Export] public float TileSize;
	[Export] public float AccelerationMultiplier;
	[Export] public float MaxSpeed;
	[Export] public GpuParticles3D particle;
	[Export] public Control deathScreen;
	[Export] public float FRICTION;
	[Export] public RigidBody3D rigidBody;
	[Export] public Skybox skybox;
	[Export] public float SKYBOXSPEED;
	[Export] public float shotgunReloadTime;
	[Export] public ShapeCast3D shotgunShapecast;
	[Export] public GpuParticles3D shotgunParticle;
	[Export] public CameraManager cameraScript;
	private float shotgunTimerStart = 0; 
	Vector3 currentPos;
	public override void _Ready()
	{
		currentPos = rigidBody.Position;
	}

	private float angle = 0;
	private int time = 0;
	private bool alive = true;
	public int Kills = 0;
	public float health = 100;
	public bool canDamage = false;
	private bool shotgunCanShoot = true;
	void UpdateRotation()
	{
		Vector2 mouseCoordinates = GetViewport().GetMousePosition();
		Vector2 gameSize = DisplayServer.WindowGetSize();

		Vector2 normalizedCoordiantes;
		normalizedCoordiantes.X = gameSize.X/2 - mouseCoordinates.X;
		normalizedCoordiantes.Y = gameSize.Y/2 - mouseCoordinates.Y;

		angle = Mathf.Atan2(normalizedCoordiantes.X, normalizedCoordiantes.Y);

		PlayerObject.Rotation = new Vector3(Mathf.Pi/2,angle,0);//Mathf.Pi/2);
		//Debug.WriteLine(gameSize);

	}

	private float movementMagic(float speedInDirection, float targetSpeed)
	{
		float multiplier; // value 0-1 to multiply acceleration by.

		multiplier = targetSpeed - speedInDirection;
		if (multiplier < 0) {return 0;} // we are going too fast already
		multiplier = multiplier / targetSpeed;
		return multiplier;
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
		
		
		Vector3 velocityInDirection = rigidBody.LinearVelocity;//.Rotated(new Vector3(0,1,0), angle - Mathf.Pi/2);
		if (moveDirection != Vector3.Zero)
		{
			moveDirection = moveDirection.Normalized();
			Vector3 globalMoveDirection = moveDirection;//.Rotated(new Vector3(0,1,0), angle - Mathf.Pi/2);
			Vector3 targetVelocity = globalMoveDirection * AccelerationMultiplier;
			
			//Vector3 velocityInDirection = rigidBody.LinearVelocity;//.Rotated(new Vector3(0,1,0), angle - Mathf.Pi/2);
			float speedInDirection = velocityInDirection.Length();
			
			float speedMultiplier = movementMagic(speedInDirection, MaxSpeed) * AccelerationMultiplier;
			Vector3 acceleration = new Vector3(
				globalMoveDirection.X * speedMultiplier,
				globalMoveDirection.Y * speedMultiplier,
				globalMoveDirection.Z * speedMultiplier
			);
			rigidBody.ApplyForce(acceleration);

		}
		//else
		//{
			//Vector3 velocityInDirection = rigidBody.LinearVelocity;//.Rotated(new Vector3(0,1,0), angle - Mathf.Pi/2);
			Vector3 friction = -velocityInDirection * FRICTION;
			if (friction.Length() > 0.05f)
			{
				rigidBody.ApplyForce(friction);
			}
		//}
	}


	void UpdateCamera()
	{
		CameraTransform.Position = rigidBody.Position + s_OFFSET;
	}

	void UpdateTiles()
	{
		skybox.SetShaderPosition(new Vector2(rigidBody.Position.X/SKYBOXSPEED, rigidBody.Position.Z/SKYBOXSPEED));
	}

	public void KillAchieved()
	{
		// when we get a kill
		Main.PlayerScore++;
		health += 5;
		if (health > 100)
		{
			health = 100;
		}
	}
	void setCombatMouse(bool status)
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
	EnemyController getEnemyFromRaycast()
	{
		if (raycaster.IsColliding())
		{
			setCombatMouse(true);
			Area3D area = (Area3D)raycaster.GetCollider();

			if (area.Name == "EnemyRaycastCollider")
			{
				EnemyController enemy = (EnemyController)area.GetParent().GetParent();
				return enemy;
			}
		}
		else
		{
			setCombatMouse(false);
		}
		return null;
	}
	List<EnemyController> getEnemyFromShapecast()
	{
		List<EnemyController> enemies = new List<EnemyController>();
		if (shotgunShapecast.IsColliding())
		{
			setCombatMouse(true);
			int count = shotgunShapecast.GetCollisionCount();
			if (count <= 0) { return null;}
			for (int i = 0; i < count; i++)
			{
				Area3D area = (Area3D)shotgunShapecast.GetCollider(i);
				if (area.Name == "EnemyRaycastCollider")
				{
					EnemyController enemy = (EnemyController)area.GetParent().GetParent();
					enemies.Add(enemy);
				}
			}	
			return enemies;
		}
		else
		{
			setCombatMouse(false);
		}
		return null;
	}
	void UpdateCombat(double delta)
	{
		
		Main.PlayerPosition = new Vector2(rigidBody.GlobalPosition.X, rigidBody.GlobalPosition.Z);
		if ((int)(time % (combatFrequency * Engine.GetFramesPerSecond())) != 0) {return;}
		Kills = Main.PlayerScore;
		if (Input.IsMouseButtonPressed(MouseButton.Left))
		{
			EnemyController enemy = getEnemyFromRaycast();
			particle.Emitting = true;
			if (!shoot.Playing)
			{
				shoot.Playing = true;
			}
			if (enemy != null)
			{
				enemy.Hit(15.0f, true);
			}
		}
		else
		{
			setCombatMouse(false);
			shoot.Playing = false;
			particle.Emitting = false;
		}
		int shotgunShootModulo = (int)((time) % (shotgunReloadTime * Engine.GetFramesPerSecond()));
		Debug.WriteLine(shotgunShootModulo);
		if (shotgunShootModulo <= 10) 
		{
			shotgunCanShoot = true; 
			Debug.WriteLine("Shotgun Can shoot!"); 
			return;
		}

		if (Input.IsKeyPressed(Key.E))
		{
			if (!shotgunCanShoot) { return;}
			

			shotgunSFX.Playing = true;

			
			//cameraScript.Explode(0.25f, 0.1f, 4,1);

			shotgunCanShoot = false;
			//shotgunTimerStart = time;
			shotgunParticle.Emitting = true;
			
			List<EnemyController> enemies = getEnemyFromShapecast();
			foreach(EnemyController enemy in enemies)
			{
				if (enemy != null)
				{
					enemy.Hit(200.0f, false);
				}
			}
		}
		else
		{
			//setCombatMouse(false);
			//shotgunSFX.Playing = false;
			shotgunParticle.Emitting = false;
		}
	}
	private async void Die()
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

	void UpdateUI()
	{
		UIController.SetHealth(health.ToString());
		UIController.SetScore(Kills.ToString());
	}
	void UpdateInvunerability()
	{
		if (canDamage == true) {return;}
		float fps = (float) Engine.GetFramesPerSecond();
		if (fps < 10)
		{
			fps = 60;
		}
		float timeToWait = invulnerabilityPeriod * fps;
		if (time >= timeToWait)
		{
			canDamage = true;
		}
	}
	public override void _PhysicsProcess(double delta)
	{
		if (!alive) {return;}
		UpdateInvunerability();
		base._PhysicsProcess(delta);
		UpdateMovement();
	}
	public override void _Process(double delta)
	{
		time++;
		if (!alive) {return;}
		if (health <= 0)
		{
			Die(); return;
		}
		//Debug.WriteLine(health);
		UpdateUI();
		UpdateCombat(delta);
		UpdateRotation();
		UpdateCamera();
		UpdateTiles();
	}

}
