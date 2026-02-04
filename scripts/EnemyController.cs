using Godot;
using System;
using System.Diagnostics;
using System.Security.AccessControl;
using System.Threading.Tasks;

public partial class EnemyController : Node3D
{
	[ExportGroup("Main Objects")]
	[Export] public RigidBody3D rigidbody;
	[Export] public MeshInstance3D enemyMove;
	[Export] public PlayerController player;

	[ExportGroup("VFX")]
	[Export] public GpuParticles3D deathParticles;
	[Export] public GpuParticles3D expParticles;
	[ExportGroup("Audio")]
	[Export] public AudioStreamPlayer3D hitSound;
	[Export] public AudioStreamPlayer3D explode;
	[ExportGroup("Values")]
	[Export] public float FRICTION;
	[Export] public float radius;
	[Export] public float RotationTime;
	[Export] public int moveAwayUpperRadius;
	[Export] public int moveAwayLowerRadius;

	[ExportGroup("Physics Objects")]
	[Export] public CollisionShape3D PhysicsCollider;
	
	[ExportGroup("Movement")]
	[Export] public float Acceleration;
	[Export] public float MaxSpeed;

	[ExportGroup("Combat")]
	[Export] public Weapon Automatic;
	[Export] public Timer CombatFrequencyTimer;
	[Export] public float CombatFrequencyTimerVariation;

	public Level levelScript;
	private bool alive = false;
	private int time = 0;
	private Vector3 forceOffset = Vector3.Zero;
	private float combatAddition;
	private Tween tween;
	private float adjustedAngle = -1;
	public ShipManager shipManager;
	public WeaponsHandler weaponsHandler;
	public void Initialize()
	{
		

		Random random = new Random();
		float timerVariation = random.Next((int)((100*CombatFrequencyTimerVariation) * 2)) - (100 * CombatFrequencyTimerVariation);
		CombatFrequencyTimer.WaitTime = Math.Abs(CombatFrequencyTimer.WaitTime + (timerVariation)/100); 
		Debug.WriteLine(CombatFrequencyTimer.WaitTime);

		shipManager = new ShipManager();
		weaponsHandler = new WeaponsHandler();

		weaponsHandler.SetShipManager(shipManager);
		weaponsHandler.AddWeapon(Automatic, "automatic");

		shipManager.IsEnemy = true;
		shipManager.SetRigidbody(rigidbody);
		shipManager.SetPlayerStructure(enemyMove);
		shipManager.SetMovementParameters(Acceleration, MaxSpeed, FRICTION);
		shipManager.SetHitInformation(true, hitSound);
		alive = true;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	Vector3 getVectorToPlayer()
	{
		Vector3 playerPosition = Vector3.Zero;
		playerPosition.X = player.rigidBody.Position.X - rigidbody.GlobalPosition.X;
		playerPosition.Z = player.rigidBody.Position.Z - rigidbody.GlobalPosition.Z;
		return playerPosition;//.Normalized();
	}
	void UpdatePhysics()
	{
		// go to player
		Vector3 playerPosition = getVectorToPlayer();

		if (playerPosition.Length() <= radius)
		{
			playerPosition = Vector3.Zero;
		}
		shipManager.UpdateMovement(playerPosition);

	}
	void UpdateRotation()
	{
		Vector3 bearing = getVectorToPlayer();

		float angle = Mathf.Atan2(bearing.X, bearing.Z);
		
		//lastAngle = halfwayPoint;
		shipManager.UpdateRotation(new Vector3(Mathf.Pi/2, angle, 0), RotationTime);
	}

	public async Task DeathCheck(bool explosionSound)
	{
		if (!alive) {return; }
		if (shipManager.ShouldDie())
		{
			alive = false;
			PhysicsCollider.Disabled = true;
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
		if (player.isInvulnerable == true) {return;}

		ShipManager enemyInWay = shipManager.getShipsFromRaycast((RayCast3D)Automatic.RaycasterObject);
		if (enemyInWay != null)
		{
			if (enemyInWay.IsEnemy)
			{
				Debug.WriteLine("Triggered");
				Vector3 impulseAmount = levelScript.getPositionFromRadius(Vector3.Zero, moveAwayLowerRadius, moveAwayUpperRadius);
				rigidbody.ApplyImpulse(impulseAmount/4);
			}
			else
			{
				weaponsHandler.TryFire(Automatic);
			}
		}		
	}
	public override void _PhysicsProcess(double delta)
	{
		if (!alive) {return;}
		DeathCheck(shipManager.ExplosionSoundWillPlayOnDeath);
		time++;
		if (CombatFrequencyTimer.TimeLeft == 0) 
		{
			UpdateCombat();
		}
		else
		{
			CombatFrequencyTimer.Start();
		}
		UpdateRotation();
		UpdatePhysics();
	}
}
