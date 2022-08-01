using Godot;
using System;
// using System.Collections.Generic;
public class CarCam : Camera
{
  internal float OriginLength;
  float overCarLength, lerpVal, horizonAng = 0f;
  float tanDistAtmo;
  ShaderMaterial atmoMat, velMat;
  Globals vars;
  float camAngle = 0f, MaxCamTurn;
  internal Transform PrevGlobalTransform = Transform.Identity;
  internal Transform PrevTransform;
  internal float Altitude;
  Vector3 blurAngle;
  OpenSimplexNoise noise = new OpenSimplexNoise();
  int noise_y = 0, curDir = 0;
  Car car;
  Tween camTween, stickTween;
  Vector3 desiredCamPos, Normal = Vector3.Forward, originLerp;
  internal Plane camPlane;
  Vector3 overCar = Vector3.Forward;
  Transform targetTransform = Transform.Identity;
  Quat slerpQuat = Quat.Identity;
  float camThreshold = 0.035f, camXangle = 0f, camYangle = 0f, camZangle = 0f;
  const float ninetyRad = Mathf.Pi / 2f;
  public override void _Ready()
  {
	vars = (Globals)GetTree().Root.FindNode("Globals", true, false);
	camTween = (Tween)GetTree().Root.FindNode("CamTween", true, false);
	stickTween = (Tween)GetTree().Root.FindNode("StickTween", true, false);
	car = (Car)GetTree().Root.FindNode("Car", true, false);
	//MeshInstance velMesh = (MeshInstance)GetTree().Root.FindNode("VelocityMesh", true, false);
	MaxCamTurn = ninetyRad;
	lerpVal = MaxCamTurn / 400f;

	noise.Seed = (int)GD.Randi();
	noise.Period = 4f;
	noise.Octaves = 2;

	Fov = vars.FovDeg.y;
	desiredCamPos = GlobalTransform.origin;
	originLerp = GlobalTransform.origin + Vector3.One;
	overCar = car.GlobalTransform.origin;
	targetTransform = GlobalTransform;
  }
  public override void _Process(float delta)
  {
	PrevGlobalTransform = GlobalTransform;
	PrevTransform = Transform;

	// Transform prevTargetTransform = targetTransform;
	Vector3 prevOverCar = overCar;
	overCarLength = car.OriginLength + 4f;
	overCar = car.Normal * overCarLength;

	Vector3 posDelta = overCar - prevOverCar;

	camPlane = new Plane(car.Normal, overCarLength);
	//desiredCamPos = camPlane.Project(desiredCamPos - posDelta / (overCarLength * overCarLength));
	float upsideDown = car.Normal.Dot(car.GlobalTransform.basis.y) * 0.5f + 0.5f;
	// desiredCamPos = camPlane.Project(desiredCamPos - posDelta * upsideDown / (overCarLength * overCarLength));
	desiredCamPos = camPlane.Project(desiredCamPos);
	Vector3 toAnchor = (desiredCamPos - overCar).Normalized();
	desiredCamPos = overCar + toAnchor * 5f;

	horizonAng = Mathf.Acos(vars.PlanetRadius / overCarLength);
	Normal = overCar.Normalized();

	Vector3 lookTarget = toAnchor.Rotated(Normal, -ninetyRad);
	lookTarget = Normal.Rotated(lookTarget, horizonAng);
	lookTarget = vars.PlanetRadius * lookTarget;

	targetTransform = new Transform(targetTransform.basis, desiredCamPos);
	targetTransform = targetTransform.LookingAt(lookTarget, desiredCamPos);

	// Quat a = slerpQuat;
	Quat b = targetTransform.basis.Quat();
	slerpQuat = slerpQuat.Slerp(b, 0.25f).Normalized(); // find halfway point between a and b
	camTween.InterpolateProperty(this, "originLerp",
	originLerp, targetTransform.origin, 4f * delta, Tween.TransitionType.Quad, Tween.EaseType.Out);

	camTween.Start();

	// Vector3 rotNormal = car.Normal.Rotated(targetTransform.origin.Cross(originLerp).Normalized(), originLerp.AngleTo(targetTransform.origin));

	if (Input.IsActionPressed("CamReverse"))
	{
	  camAngle = Mathf.Pi;
	}
	else if (Input.IsActionPressed("CamUp") | Input.IsActionPressed("CamDown") |
	Input.IsActionPressed("CamLeft") | Input.IsActionPressed("CamRight"))
	{
	  float inputAngle = Input.GetVector("CamDown", "CamUp", "CamRight", "CamLeft").Angle();
	  float angDiff = Mathf.Wrap(camAngle - inputAngle, -Mathf.Pi, Mathf.Pi);

	  int inputDir = Mathf.Sign(angDiff);

	  if (Mathf.Abs(angDiff) > camThreshold)
	  {
		if (Mathf.Abs(angDiff) > MaxCamTurn)
		{
		  camAngle = inputAngle;
		  stickTween.Stop(this);
		}
		else
		  curDir = inputDir;
	  }
	  if (curDir == inputDir & !Input.IsActionJustReleased("CamReverse"))
	  {
		stickTween.InterpolateProperty(this, "camAngle", camAngle, camAngle - angDiff, 0.1f, Tween.TransitionType.Quad, Tween.EaseType.Out);
		stickTween.Start();
	  }
	}
	else
	{
	  if (Mathf.Abs(Mathf.Wrap(camAngle, -Mathf.Pi, Mathf.Pi)) > MaxCamTurn)
	  {
		camAngle = 0;
		stickTween.Stop(this);
	  }
	  else
	  {
		curDir = 0;
		stickTween.InterpolateProperty(this, "camAngle", camAngle, 0f, 0.1f, Tween.TransitionType.Quad, Tween.EaseType.Out);
		stickTween.Start();
	  }
	}


	// Useful for troubleshooting motion blur.
	PrevGlobalTransform = GlobalTransform;

	GlobalTransform = new Transform(targetTransform.basis.Rotated(car.Normal, camAngle), targetTransform.origin.Rotated(car.Normal, camAngle));
	//  if ((float)Time.GetTicksMsec() % 1000 < 500)
	//    GlobalTransform = GlobalTransform.Rotated(car.Normal, (float)Time.GetTicksMsec() / 110);
	//  else
	//    GlobalTransform = GlobalTransform.Rotated(car.Normal, (float)Time.GetTicksMsec() / -110);

	// GlobalTransform = GlobalTransform.Rotated(car.Normal, Mathf.Sin((float)Time.GetTicksMsec() / -50));

	OriginLength = GlobalTransform.origin.Length();
	Altitude = OriginLength - vars.PlanetRadius;

  }

  void cameraShake(float rotFactor, float transFactor)
  {
	noise_y += 1;
	float rotationOffsetX = rotFactor * noise.GetNoise2d(noise.Seed, noise_y);
	float rotationOffsetY = rotFactor * noise.GetNoise2d(noise.Seed * 2, noise_y);
	float rotationOffsetZ = rotFactor * noise.GetNoise2d(noise.Seed * 3, noise_y);
	float translationOffsetX = transFactor * noise.GetNoise2d(noise.Seed * 4, noise_y);
	float translationOffsetY = transFactor * noise.GetNoise2d(noise.Seed * 5, noise_y);
	float translationOffsetZ = transFactor * noise.GetNoise2d(noise.Seed * 6, noise_y);
	Rotation = Rotation + new Vector3(rotationOffsetX, rotationOffsetY, rotationOffsetZ);
	Translation = Translation + new Vector3(translationOffsetX, translationOffsetY, translationOffsetZ);
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
