using Godot;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Net.Security;


public partial class CameraManager : Camera3D
{
	[Export] public Node3D cameraTransform;
	public float shakeMultiplier = 0.01f;
	public float shakeSpeed = 0.01f;
	public float shakeDenominator = 1f;
	
	public float time = 0;
	public Vector3 offset;
	// Called when the node enters the scene tree for the first time.
	private RandomNumberGenerator rng = new RandomNumberGenerator();
	private FastNoiseLite noise = new FastNoiseLite();

	private bool busy = false;
	
	private float explosionMultiplier, explosionShakeDenominator, timeStart, explosionTime, explosionDecayExponent;

	void ShakeFormula()
	{
		Vector2 points = new Vector2(Mathf.Sin(time * shakeDenominator), Mathf.Cos(time * shakeDenominator)) * 3;  
		float noiseValue = noise.GetNoise2D(points.X, points.Y) * 3f;
		float noiseValue2 = noise.GetNoise2D(points.Y, points.X) * 3f;
		offset = new Vector3(
			noiseValue2, //+ MathF.Sin(time * shakeSpeed)/shakeDenominator,
			0.0f,
			noiseValue
		);
	}

	void UpdateCamera()
	{
		ShakeFormula();
		Position = cameraTransform.Position + offset * shakeMultiplier;
	}
	public void Explode(float em, float esd, float decayExponent,float et)
	{
		explosionMultiplier = em;
		explosionShakeDenominator = esd;
		explosionDecayExponent = decayExponent;
		explosionTime = et * (float)Engine.GetFramesPerSecond();
		timeStart = time;
		busy = true;
	}

	void ZeroOut()
	{
		busy = false;
		explosionDecayExponent = 0;
		shakeDenominator = 1;
		shakeMultiplier = 0;
		explosionMultiplier = 0;
		explosionShakeDenominator = 1;
		timeStart = 0;
		explosionTime = 0;
	}
	private void ExplodeHandler()
	{
		float normalizedTime = (time - timeStart)/explosionTime;

		if (normalizedTime >= 1) {Debug.WriteLine("Aborting"); ZeroOut(); return;}
		Debug.Write("Exploding");
		float inversedTime = Math.Abs(Mathf.Pow(normalizedTime, explosionDecayExponent) - 1f);

		shakeMultiplier = inversedTime * explosionMultiplier;
		shakeDenominator = inversedTime * explosionShakeDenominator;

	}
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		time++;
		UpdateCamera();
		if (busy)
		{
			ExplodeHandler();
		}
	}
}
