using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;

using System.Runtime.InteropServices;
public partial class ShipManager
{
    private RigidBody3D rigidbody;
    private Node3D playerStruct;
    private GpuParticles3D explosionParticles, expParticles;
    private AudioStreamPlayer3D HitSound;
    private Standard std = new Standard();
    private float currentAngle = 0, accel, maxSpeed, friction; 
    private float health = 100;
    private bool alive = true;
    public bool ExplosionSoundWillPlayOnDeath = false;
    public bool IsEnemy = false;
    private bool PlayHitSound = false;


    // script:    

    public float GetHealth()
    {
        return health;
    }
    public bool ShouldDie()
    {
        return !alive;
    }
    public void Heal(float healthGain)
    {
        health = Mathf.Clamp(health + healthGain, 0, 100);
    }
    public void TakeDamage(float hitpoints)
    {
        if (!alive) {return;}
        health -= hitpoints;
        if (PlayHitSound && ExplosionSoundWillPlayOnDeath)
        {
            HitSound.Playing = false;
            HitSound.Playing = true;
        }
        if (health <= 0)
        {
            alive = false;
        }    
    }

    public void SetRigidbody(RigidBody3D rigid) { rigidbody = rigid; }
    public void SetPlayerStructure(Node3D structure) { playerStruct = structure; }
    public void SetMovementParameters(float acceleration, float maximumSpeed, float frictionPercentage) {accel = acceleration; maxSpeed = maximumSpeed; friction = frictionPercentage;}
    public void SetHitInformation(bool playHitSound, AudioStreamPlayer3D hitSound){PlayHitSound = playHitSound; HitSound = hitSound;}

    public ShipManager getShipsFromRaycast(RayCast3D raycast)
	{
        if (!alive) {return null;}
		if (raycast.IsColliding())
		{
			Area3D area = (Area3D)raycast.GetCollider();
            std.SOFTASSERT(area != null, "getShipsFromRaycast Area check failed");
            return getShipFromArea(area);
		}
		return null;
	}
	public List<ShipManager> getShipsFromShapecast(ShapeCast3D shapecast)
	{
        if (!alive) {return null;}
		List<ShipManager> enemies = new List<ShipManager>();
		if (shapecast.IsColliding())
		{
			int count = shapecast.GetCollisionCount();
			bool check = std.SOFTASSERT(count >= 0, "getEnemyFromShapecast no colliders found");
            if (!check) {return null;}
			for (int i = 0; i < count; i++)
			{
				Area3D area = (Area3D)shapecast.GetCollider(i);
                ShipManager enemy = getShipFromArea(area);
                if (enemy != null)
                {
                    enemies.Add(enemy);
                }
			}	
			return enemies;
		}
		return null;
	}
    public void UpdateMovement(Vector3 moveDirection)
    {
        if (!alive) {return;}
        std.ASSERT(rigidbody != null, "Update Movement Rigidbody Check");
        std.ASSERT(maxSpeed != 0, "Update Movement MaxSpeed Check ");
        std.ASSERT(accel != 0, "Update Rotation Acceleration Check");
        std.ASSERT(friction != 0, "Update Rotation Friction Check");

        Vector3 velocityInDirection = rigidbody.LinearVelocity;
		if (moveDirection != Vector3.Zero)
		{
			moveDirection = moveDirection.Normalized();
			Vector3 globalMoveDirection = moveDirection;
			
			float speedInDirection = velocityInDirection.Length();
			
			float speedMultiplier = getAccelerationFromTargetSpeed(speedInDirection, maxSpeed) * accel;
			Vector3 acceleration = new Vector3(
				globalMoveDirection.X * speedMultiplier,
				globalMoveDirection.Y * speedMultiplier,
				globalMoveDirection.Z * speedMultiplier
			);
			rigidbody.ApplyForce(acceleration);
		}

		Vector3 frictionForce = -velocityInDirection * friction;
		if (frictionForce.Length() > 0.05f)
		{
			rigidbody.ApplyForce(frictionForce);
        }
    }
    public void UpdateRotation(Vector3 rotationVector,float tweenTime)
	{
        if (!alive) {return;}
        std.ASSERT(playerStruct != null, "Dynamic Update Rotation PlayerStructure Check");

        if (playerStruct == null) { 
            Debug.WriteLine("[SM] Tweened Update Rotation PlayerStructure Check Failed!"); 
            return;
        }

		float lerpResult = Mathf.LerpAngle(currentAngle, rotationVector.Y, tweenTime/(float)Engine.GetFramesPerSecond());

		if (currentAngle == -1) { lerpResult = rotationVector.Y;}
		playerStruct.Rotation = rotationVector;
		currentAngle = lerpResult;
	}
    public void UpdateRotation(Vector3 rotationVector)
	{
        if (!alive) {return;}
        std.ASSERT(playerStruct != null, "Update Rotation PlayerStructure Check");

		playerStruct.Rotation = new Vector3(Mathf.Pi/2,rotationVector.Y,0);
        currentAngle = rotationVector.Y;
	}    
    private ShipManager getShipFromArea(Area3D area)
    {
     
        ShipManager enemy;
		if (area.Name == "EnemyRaycastCollider")
		{
			enemy = ((EnemyController)area.GetParent().GetParent()).shipManager;
		}
        else if (area.Name == "PlayerRaycastCollider")
        {
            enemy = ((PlayerController)area.GetParent().GetParent()).shipManager;
        }
        else
        {
            enemy = null;
        }
        if (enemy == this){enemy = null;}

		return enemy;
    }
    private float getAccelerationFromTargetSpeed(float speedInDirection, float targetSpeed)
	{
        if (!alive) {return 0;}
		float multiplier; // value 0-1 to multiply acceleration by.

		multiplier = targetSpeed - speedInDirection;
		if (multiplier < 0) {return 0;} // we are going too fast already
		multiplier = multiplier / targetSpeed;
		return multiplier;
	}
    
   
}