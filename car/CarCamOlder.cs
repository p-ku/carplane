/* using Godot;
using System;

public class CarCam : Camera
{
  internal float OriginLength;
  internal bool Snap;
  Globals vars;
  DirectionalLight sun;
  Transform systemTransform, rotTransform;
  StaticBody carAnchor;
  RigidBody camAnchor;
  float input, prevInput, lerpVal = 0.07f, inDiff = 0, angDiff, horizonAng, MaxCamTurnDeg, maSum;
  float amount1 = 0.5f, amount2 = 0.1f, camAngle = 0f, camAngleRad, carAnchorOriginDist = 1f;
  internal Transform PrevGlobalTransform, VelocityTransform;
  Vector3 horizon, camVec, offset1, offset2, carAnchorNormal = new Vector3(1f, 1f, 1f);
  OpenSimplexNoise noise = new OpenSimplexNoise();
  int noise_y = 0, curDir = 0;
  Car car;
  Tween camTween;
  VelocityMesh velm;
  System.Collections.Generic.Queue<float> maBuffer; // For moving average of position.

  public override void _Ready()
  {
	vars = (Globals)GetTree().Root.FindNode("Globals", true, false);
	velm = (VelocityMesh)GetTree().Root.FindNode("VelocityMesh", true, false);

	car = (Car)GetTree().Root.FindNode("Car", true, false);
	carAnchor = (StaticBody)GetTree().Root.FindNode("CarAnchor", true, false);
	camAnchor = (RigidBody)GetTree().Root.FindNode("CamAnchor", true, false);
	sun = (DirectionalLight)GetTree().Root.FindNode("Sun", true, false);

	MaxCamTurnDeg = vars.FovDeg.x;
	lerpVal = MaxCamTurnDeg / 400f;

	rotTransform = GlobalTransform;

	camTween = new Tween();
	AddChild(camTween);

	GD.Randomize();
	noise.Seed = (int)GD.Randi();
	noise.Period = 4f;
	noise.Octaves = 2;
  }

  public override void _Process(float delta)
  {
	//velm.mat.SetShaderParam("cam_xform", new Basis());

	carAnchorNormal = carAnchor.GlobalTransform.origin.Normalized();

	OriginLength = GlobalTransform.origin.Length(); // From center of planet
	Far = OriginLength + vars.AtmoRadius;

	carAnchorOriginDist = GlobalTransform.origin.Length();

	amount1 = Mathf.Pow(2f, car.LinearDamp) / 2000f;
	amount2 = amount1 / 5f;

	systemTransform.origin = car.GlobalTransform.origin * 1.05f;
	systemTransform.basis = GlobalTransform.basis;
	GlobalTransform = systemTransform;

	carAnchor.LookAt(Vector3.Zero, GlobalTransform.basis.x);

	horizonAng = Mathf.Asin(vars.planet_radius / carAnchorOriginDist);
	camVec = camAnchor.GlobalTransform.origin.DirectionTo(carAnchor.GlobalTransform.origin);

	horizon = camVec.Rotated(carAnchorNormal, Mathf.Pi / 2f);
	horizon = carAnchorNormal.Rotated(horizon, horizonAng);
	horizon = 1.4f * vars.planet_radius * horizon;
	camAngle = Mathf.Wrap(camAngle, -180, 180);

	if (Input.IsActionPressed("CamReverse"))
	{
	  horizon = horizon.Rotated(carAnchorNormal, Mathf.Pi);
	}
	else
	{
	  if (Input.IsActionPressed("CamUp") | Input.IsActionPressed("CamDown") | Input.IsActionPressed("CamLeft") | Input.IsActionPressed("CamRight"))
	  {
		input = Mathf.Rad2Deg(Input.GetVector("CamDown", "CamUp", "CamRight", "CamLeft").Angle());
		inDiff = Mathf.Wrap(prevInput - input, -180, 180);
		float absInDiff = Mathf.Abs(inDiff);
		if (absInDiff < MaxCamTurnDeg)
		{
		  // Determine if clockwise or counter clockwise rotation.
		  if (curDir == 0)
			curDir = (sbyte)inDiff;
		  else if (absInDiff > 2f)
			curDir = -curDir;
		  angDiff = Mathf.Wrap(camAngle - input, -180, 180);
		  camAngleRad = Mathf.Deg2Rad(camAngle);
		  horizon = horizon.Rotated(carAnchorNormal, camAngleRad);
		}
		else
		{
		  camAngle = input;
		  curDir = 0;
		  inDiff = 0;
		}
		prevInput = input;
	  }
	  else if (Math.Abs(camAngle) < MaxCamTurnDeg)
	  {
		angDiff = Mathf.Wrap(camAngle, -180f, 180f);
		camAngleRad = Mathf.Deg2Rad(camAngle);
		horizon = horizon.Rotated(carAnchorNormal, camAngleRad);
		inDiff = 0;
		prevInput = 0;
	  }
	  else
	  {

		prevInput = 0;
		camAngle = 0f;
		input = 0f;
		camTween.Stop(this);
		curDir = 0;
		inDiff = 0;
	  }
	  camTween.Start();
	  camTween.InterpolateProperty(this, "camAngle", camAngle, camAngle - angDiff, lerpVal,
	Tween.TransitionType.Expo, Tween.EaseType.Out);
	}
	rotTransform.origin = GlobalTransform.origin + horizon.Rotated(carAnchorNormal, Mathf.Pi) / 10f;
	GlobalTransform = rotTransform;
	LookAt(horizon, GlobalTransform.origin);

	noise_y += 1;
	offset1.x = amount1 * noise.GetNoise2d(noise.Seed, noise_y);
	offset1.y = amount1 * noise.GetNoise2d(noise.Seed * 2, noise_y);
	offset1.z = amount1 * noise.GetNoise2d(noise.Seed * 3, noise_y);
	offset2.x = amount2 * noise.GetNoise2d(noise.Seed * 4, noise_y);
	offset2.y = amount2 * noise.GetNoise2d(noise.Seed * 5, noise_y);
	offset2.z = amount2 * noise.GetNoise2d(noise.Seed * 6, noise_y);

	Rotation = Rotation + offset1;
	Translation = Translation + offset2;
	PrevGlobalTransform = GlobalTransform;
  }

  public override void _PhysicsProcess(float delta)
  {
	sun.RotateY(0.01f * delta);
	//	Environment.FogDepthBegin = Mathf.Min(vars.cam_alt, vars.planet_radius);
	// Environment.BackgroundSkyRotation = sun.Rotation;
  }
  float movingAverage(System.Collections.Generic.Queue<float> buffer, float current, float sum)
  {
	buffer.Enqueue(current);
	sum += current;
	float ma = sum / buffer.Count;
	sum -= buffer.Dequeue();
	return ma;
  }
}
 */