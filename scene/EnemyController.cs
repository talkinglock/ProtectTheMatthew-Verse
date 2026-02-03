using Godot;
using System;
using System.Diagnostics;
using System.Security.AccessControl;
using System.Threading.Tasks;

public partial class EnemyController : Node3D
{
	[Export] public RigidBody3D rigidbody;
	[Export] public AudioStreamPlayer3D hitSound;
	[Export] public AudioStreamPlayer3D shootSound;
	[Export] public float speed;
	[Export] public float radius;
	[Export] public float ROTATION_TIME;
	[Export] public MeshInstance3D enemyMove;
	[Export] public GpuParticles3D shootParticle;
	[Export] public float combatFrequency;
	[Export] public float HitDamage;
	[Export] public RayCast3D raycaster;
	
	[Export] public AudioStreamPlayer3D explode;
	[Export] public float FRICTION;
	[Export] public GpuParticles3D deathParticles;
	[Export] public GpuParticles3D expParticles;
	[Export] public PlayerController player;
	[Export] public int moveAwayUpperRadius;
	[Export] public int moveAwayLowerRadius;
	public Level levelScript;
	// Called when the node enters the scene tree for the first time.
	[Export]public float health = 100.0f;
	private bool alive = false;
	private int time = 0;
	private Vector3 forceOffset = Vector3.Zero;
	private float combatAddition;
	private Tween tween;
	private float adjustedAngle = -1;
	public void Initialize()
	{
		alive = true;
		Random random = new Random();
		combatAddition = (float) random.Next(6)/3;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	private float movementMagic(float speedInDirection, float targetSpeed)
	{
		float multiplier; // value 0-1 to multiply acceleration by.

		multiplier = targetSpeed - speedInDirection;
		if (multiplier < 0) {return 0;} // we are going too fast already
		multiplier = multiplier / targetSpeed;
		return multiplier;
	}
	void UpdatePhysics()
	{
		Vector3 friction = -rigidbody.LinearVelocity * FRICTION;
		if (friction.Length() > 0.05f)
		{
			rigidbody.ApplyForce(friction);
		}

		// go to player

		Vector3 playerPosition = Vector3.Zero;

		playerPosition.X = Main.PlayerPosition.X - rigidbody.GlobalPosition.X;
		playerPosition.Z = Main.PlayerPosition.Y - rigidbody.GlobalPosition.Z;

		
		if (playerPosition.Length() >= radius)
		{
			playerPosition.Normalized();
			rigidbody.ApplyForce(playerPosition * movementMagic(rigidbody.LinearVelocity.Length(), speed));
		}
		

	}
	void UpdateRotation()
	{
		
		Vector2 playerPosition;

		playerPosition.X = Main.PlayerPosition.X - rigidbody.GlobalPosition.X;
		playerPosition.Y = Main.PlayerPosition.Y - rigidbody.GlobalPosition.Z;

		float angle = Mathf.Atan2(playerPosition.X, playerPosition.Y);
		
		//lastAngle = halfwayPoint;
		float lerpResult = Mathf.LerpAngle(adjustedAngle, angle, ROTATION_TIME/(float)Engine.GetFramesPerSecond());
		if (adjustedAngle == -1) { lerpResult = angle;}
		rigidbody.Rotation = new Vector3(0, lerpResult, 0);
		adjustedAngle = lerpResult;
	}

	public async Task Hit(float hp, bool explosionSound)
	{
		if (!alive) {return; }
		health -= hp;
		hitSound.Playing = false;
		hitSound.Playing = true;
		if (health <= 0)
		{
			//Debug.WriteLine("death");
			alive = false;
			levelScript.enemyCount--;
			deathParticles.Emitting = true;
			enemyMove.QueueFree();
			expParticles.Emitting = true;
			
			player.KillAchieved();
			explode.Playing = explosionSound;
			if (player == null)
			{
				QueueFree();
			}
			CameraManager camera = (CameraManager)player.GetNode("PlayerCam");
			camera.Explode(0.4f, 1.5f, 2,1);
			
			await ToSignal(expParticles, GpuParticles3D.SignalName.Finished);
			QueueFree(); // disposes of the entire enemy tree
		}
	}

	void UpdateCombat()
	{
		if (raycaster.IsColliding() && player.canDamage == true)
		{
			RigidBody3D colliderRigid = (RigidBody3D)((Area3D)raycaster.GetCollider()).GetParent();
			//Debug.WriteLine(colliderRigid.Name);
			if (colliderRigid.Name == "Enemy")
			{
				// pick a direction to move away from it 
				Debug.WriteLine("Triggered");
				Vector3 impulseAmount = levelScript.getPositionFromRadius(Vector3.Zero, moveAwayLowerRadius, moveAwayUpperRadius);
				rigidbody.ApplyImpulse(impulseAmount * 2);
			}
			else if (colliderRigid.Name == "PlayerRigidbody")
			{
				shootSound.Playing = true;
				shootParticle.Emitting = true;
				player.health -= HitDamage;
			}
	
		}
	}
	private int combatCheck; 
	public override void _PhysicsProcess(double delta)
	{

		if (!alive) {return;}
		

		time++;
		combatCheck = (int)((combatFrequency + combatAddition) * (int)Engine.GetFramesPerSecond());
		if (combatCheck <= 0) {combatCheck = 2;}
		if (time % combatCheck == 0) { UpdateCombat();}
		

		UpdateRotation();
		UpdatePhysics();
	}
}
