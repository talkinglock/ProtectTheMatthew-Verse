using Godot;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;
//using System.Numerics;

using System.Runtime.InteropServices;
using System.Threading.Tasks;

public partial class WeaponsHandler : Node3D
{
	private Dictionary<string, Weapon> weaponDictionary = new Dictionary<string, Weapon>();
	private ShipManager manager;
	Standard std = new Standard();
	public bool isEnemy = false;

	public void SetShipManager(ShipManager managerArg)
	{
		manager = managerArg;
	}
	public int AddWeapon(Weapon weaponToAdd, string name)
	{
		weaponToAdd.ReloadTimer.OneShot = true;
		weaponToAdd.ReloadTimer.Start();
		weaponDictionary.Add(name, weaponToAdd);
		return weaponDictionary.Count();
	}

	public bool CanFire(Weapon weapon)
	{
		if (weapon.ReloadTimer.TimeLeft != 0)
		{
			return false;
		}
		return true;
	}
	public void IsEnemy()
	{
		isEnemy = true;
	}

	async Task<bool> fire(Weapon weapon)
	{
		std.ASSERT(manager != null, "Ship manager doesnt exist!");
		if (weapon.ReloadTimer.TimeLeft != 0)
		{
			return false;
		}
		else
		{
			// restart timer
			weapon.ReloadTimer.Start();
		}
		// fire the gun

		
		if (weapon.OneShot)
		{
			weapon.ShootAudioStream.Playing = false;
			weapon.ShootAudioStream.Playing = true;

			weapon.ShootingParticles.Restart();
		}
		else
		{
			if (!weapon.ShootAudioStream.Playing)
			{
				weapon.ShootAudioStream.Playing = true;
			}
			weapon.ShootingParticles.Emitting = true;
		}



		List<ShipManager> enemies = new List<ShipManager>();
		if (weapon.IsRaycastNotShapecast)
		{
			enemies.Add(manager.getShipsFromRaycast((RayCast3D)weapon.RaycasterObject));
		}
		else
		{
			enemies = manager.getShipsFromShapecast((ShapeCast3D)weapon.RaycasterObject);
		}
		int i = 0;
		foreach (ShipManager enemy in enemies)
		{
			if (enemy == null) {continue;}
			if (weapon.ExplosionSoundPerDeath == false)
			{
				// only have one ship make explosion sound per shot
				// godot adds volume of multiple sounds, this counteracts that
				if (i == 0)
				{
					 enemy.ExplosionSoundWillPlayOnDeath = true;
				}
				else
				{
					enemy.ExplosionSoundWillPlayOnDeath = false;
				}
			}
			else
			{
				enemy.ExplosionSoundWillPlayOnDeath = true;
			}
			

			Main.DamageShip(enemy, weapon.HitPoints);
			i++;
		}
		if (weapon.OneShot)
		{
			await ToSignal(weapon.ReloadTimer, Timer.SignalName.Timeout);
			UnfireWeapon(weapon);
		}
		return true;
	}
	public void UnfireWeapon(Weapon weapon)
	{
		weapon.ShootingParticles.Emitting = false;
		weapon.ShootAudioStream.Playing = false;
	}

	public async Task<bool> TryFire(Weapon weapon)
	{
		return await fire(weapon);
	}
	public async Task<bool> TryFire(string weaponName)
	{
		Weapon weapon = FindWeapon(weaponName);
		if (weapon == null) { Debug.WriteLine("Weapon not found, try fire failed"); return false; }
		return await fire(weapon);
	}
	public Weapon FindWeapon(string name)
	{
		foreach (KeyValuePair<string, Weapon> entry in weaponDictionary)
		{
			string weaponName = entry.Key;
			Weapon currentWeapon = entry.Value;
			if (weaponName == name)
			{
				return currentWeapon;
			}
		}
		Debug.WriteLine("Weapon " + name + " not found.");
		return null;
	}

}
