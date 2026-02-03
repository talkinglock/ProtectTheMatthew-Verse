using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;

public partial class Level : Node3D
{
	[Export] public Node3D playerNode;
	[Export] public Node3D enemyTemplate;
	[Export] public Node enemiesFolder;
	[Export] public int spawnRadiusLower;
	[Export] public int spawnRadiusUpper;
	[Export] public int EnemyMinimum;
	[Export] public int EnemySpawnAmount;	
	public int enemyCount = 0;
	
	private Random rng = new Random();

	public Vector3 getPositionFromRadius(Vector3 startPosition, int radiusLower, int radiusUpper)
	{
		int spawnDistance = rng.Next(radiusLower + radiusUpper) - radiusLower;
		Vector3 rawSpawnPos = new Vector3(
			rng.Next(50)-25,
			0,
			rng.Next(50)-25
		);
		Vector3 normalizedPos = rawSpawnPos.Normalized();
		normalizedPos *= spawnDistance;

		return startPosition + normalizedPos;
	}

	void SpawnEnemies(int spawns)
	{
		void Spawn()
		{
			Node3D newEnemy = (Node3D) enemyTemplate.Duplicate(7); // the 7 flag means clone all data like scripts and colliders
			EnemyController enemyScript = (EnemyController) newEnemy;
			enemyScript.player = (PlayerController) playerNode;
			enemyScript.levelScript = (Level) this;

			Vector3 spawnPosition = getPositionFromRadius(((RigidBody3D)playerNode.GetNode("PlayerRigidbody")).Position, spawnRadiusLower, spawnRadiusUpper);
			newEnemy.Position = spawnPosition;

			enemiesFolder.AddChild(newEnemy);

			enemyCount++;

			enemyScript.Initialize();
		}

		for (int i = 0; i < spawns; ++i)
		{
			Spawn();
		}
	}
	void UpdateEnemies()
	{
		if (enemyCount <= EnemySpawnAmount)
		{
			SpawnEnemies(EnemySpawnAmount);
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		UpdateEnemies();
	}
}
