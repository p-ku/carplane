/* using Godot;
using System;

public class CarCam : Camera
{
  internal float OriginLength;
  Globals vars;
  DirectionalLight sun;
  Transform systemTransform, rotTransform;
  StaticBody carAnchor;
  RigidBody camAnchor;
  float input, prevInput, lerpVal = 0.07f, inDiff = 0, angDiff, horizonAng = 0f, MaxCamTurnDeg;
  float amount1 = 0.5f, amount2 = 0.1f, camAngle = 0f, camAngleRad, carOriginLength = 1f;
  internal Transform PrevTransform, VelocityTransform;
  Vector3 horizon, camVec, offset1, offset2, carNormal = Vector3.Up, ma, delaySum;

  OpenSimplexNoise noise = new OpenSimplexNoise();
  int noise_y = 0, curDir = 0;
  Car car;
  Tween tween;
  VelocityMesh velm;

  //System.Collections.Generic.Queue<Vector3>
  //maQ = new System.Collections.Generic.Queue<Vector3>(maQsize + 2);
  //System.Collections.Generic.Queue<Vector3>
  //delayQ = new System.Collections.Generic.Queue<Vector3>(delayQsize + 2);
  Plane carPlane;
  Vector3 velProj, negVelProj;
  Vector3 camProj;
  Vector3 camOri, anchorOri;

  Plane horiPlane, vertSlice = new Plane();
  Vector3 PrevLinVel;
  Transform maTransform;
  ColorCam colorCam;
  Vector3 lookie, lookieProj;
  Vector3 PrevNorm = Vector3.Up;
  Vector3 lookDir = Vector3.Up;
  Transform camTran = Transform.Identity, anchorTran = Transform.Identity;
  Vector3 camTrail;
  Vector3 camNormal = Vector3.Up.Normalized();
  Vector3 anchorVec = Vector3.Up.Normalized();

  Vector3 anchNormal = Vector3.Up.Normalized();
  RayCast anchRay;

  // StaticBody camCounter;
  float horiDist;
  public override void _Ready()
  {
	vars = (Globals)GetTree().Root.FindNode("Globals", true, false);
	velm = (VelocityMesh)GetTree().Root.FindNode("VelocityMesh", true, false);
	colorCam = (ColorCam)GetTree().Root.FindNode("ColorCam", true, false);

	car = (Car)GetTree().Root.FindNode("Car", true, false);
	carAnchor = (StaticBody)GetTree().Root.FindNode("CarAnchor", true, false);
	anchRay = (RayCast)GetTree().Root.FindNode("AnchRay", true, false);

	camAnchor = (RigidBody)GetTree().Root.FindNode("CamAnchor", true, false);
	sun = (DirectionalLight)GetTree().Root.FindNode("Sun", true, false);

	MaxCamTurnDeg = vars.FovDeg.x;
	lerpVal = MaxCamTurnDeg / 400f;

	rotTransform = GlobalTransform;

	tween = new Tween();
	AddChild(tween);
	GD.Randomize();
	noise.Seed = (int)GD.Randi();
	noise.Period = 4f;
	noise.Octaves = 2;
	carNormal = car.GlobalTransform.origin.Normalized();
	carOriginLength = car.GlobalTransform.origin.Length();
	carPlane = new Plane(carNormal, vars.planet_radius);
	anchorTran = carAnchor.GlobalTransform;

	LookAt(car.GlobalTransform.origin, GlobalTransform.origin);
	horizonAng = Mathf.Asin(vars.planet_radius / GlobalTransform.origin.Length());
	GlobalTransform = camAnchor.GlobalTransform;
  }
  public override void _PhysicsProcess(float delta)
  {
	carNormal = car.GlobalTransform.origin.Normalized();
	carOriginLength = car.GlobalTransform.origin.Length();

	Vector3 carAnchorOrigin = 1.4f * carOriginLength * carNormal;
	carAnchor.GlobalTransform = new Transform(Basis.Identity, 1.4f * carOriginLength * carNormal);
	carAnchor.LookAt(Vector3.Zero, GlobalTransform.basis.x);

	sun.RotateY(0.01f * delta);

	carPlane = new Plane(carNormal, vars.planet_radius);

	float tester = 2f * car.Altitude - (carPlane.DistanceTo(car.LinearVelocity + carNormal * vars.planet_radius));
	anchorVec = carAnchor.GlobalTransform.origin - camAnchor.GlobalTransform.origin;
	//anchorVec = -camAnchor.GlobalTransform.origin.DirectionTo(carAnchor.GlobalTransform.origin);

	vertSlice = new Plane(camAnchor.GlobalTransform.origin, carAnchor.GlobalTransform.origin, Vector3.Zero);
	horiPlane = new Plane(camAnchor.GlobalTransform.origin, carAnchor.GlobalTransform.origin, Vector3.Zero);
	Plane anchorPlane = new Plane(anchorVec, 0f);

	anchNormal = anchorVec.Normalized();
	anchRay.CastTo = camAnchor.GlobalTransform.origin;

	anchRay.LookAt(lookie, GlobalTransform.origin);
  }
  public override void _Process(float delta)
  {
	GlobalTransform = camAnchor.GlobalTransform;

	horizonAng = Mathf.Asin(vars.planet_radius / GlobalTransform.origin.Length());
	horiDist = vars.planet_radius / Mathf.Tan(horizonAng);
	camNormal = GlobalTransform.origin.Normalized();
	//lookie = horiDist * camNormal.Rotated(anchNormal, Mathf.Pi - horizonAng);
	//lookie = lookie.Rotated(camNormal, -Mathf.Pi * 0.5f);
	//lookie += GlobalTransform.origin;
	Vector3 lookie1 = anchNormal.Rotated(carNormal, Mathf.Pi / 2f);
	Vector3 lookie2 = carNormal.Rotated(lookie1, horizonAng);
	lookie = vars.planet_radius * lookie2;


	camProj = carPlane.Project(GlobalTransform.origin);

	OriginLength = GlobalTransform.origin.Length(); // From center of planet
	Far = OriginLength + vars.AtmoRadius;

	amount1 = Mathf.Pow(2f, car.LinearDamp) / 2000f;
	amount2 = amount1 / 5f;

	camVec = GlobalTransform.origin.DirectionTo(car.GlobalTransform.origin);
	Vector3 localCamVec = ToLocal(camVec);


	//	tween.InterpolateProperty(this, "lookDir", lookDir, lookie, 0.06f,
	//	Tween.TransitionType.Expo, Tween.EaseType.InOut);
	//
	//	tween.Start();

	GlobalTransform = camAnchor.GlobalTransform;
	if (lookie.AngleTo(GlobalTransform.origin) > 0)
	{
	  LookAt(lookie, GlobalTransform.origin);
	}

	colorCam.GlobalTransform = GlobalTransform;
	//  if (car.GlobalTransform.origin.AngleTo(GlobalTransform.origin) > 0)
	//    LookAt(car.GlobalTransform.origin, GlobalTransform.origin);

	//   horizon = camVec.Rotated(carNormal, Mathf.Pi / 2f);
	//   horizon = carNormal.Rotated(horizon, horizonAng);
	//   horizon = 1.4f * vars.planet_radius * horizon;
	//   camAngle = Mathf.Wrap(camAngle, -180, 180);

	//  if (Input.IsActionPressed("CamReverse"))
	//  {
	//    horizon = horizon.Rotated(carNormal, Mathf.Pi);
	//  }
	//  else
	//  {
	//    if (Input.IsActionPressed("CamUp") | Input.IsActionPressed("CamDown") |
	//    Input.IsActionPressed("CamLeft") | Input.IsActionPressed("CamRight"))
	//    {
	//      input = Mathf.Rad2Deg(Input.GetVector("CamDown", "CamUp", "CamRight", "CamLeft").Angle());
	//      inDiff = Mathf.Wrap(prevInput - input, -180, 180);
	//      float absInDiff = Mathf.Abs(inDiff);
	//      if (absInDiff < MaxCamTurnDeg)
	//      {
	//        // Determine if clockwise or counter clockwise rotation.
	//        if (curDir == 0)
	//          curDir = (sbyte)inDiff;
	//        else if (absInDiff > 2f)
	//          curDir = -curDir;
	//        angDiff = Mathf.Wrap(camAngle - input, -180, 180);
	//        camAngleRad = Mathf.Deg2Rad(camAngle);
	//        horizon = horizon.Rotated(carNormal, camAngleRad);
	//      }
	//      else
	//      {
	//        camAngle = input;
	//        curDir = 0;
	//        inDiff = 0;
	//      }
	//      prevInput = input;
	//    }
	//    else if (Math.Abs(camAngle) < MaxCamTurnDeg)
	//    {
	//      angDiff = Mathf.Wrap(camAngle, -180f, 180f);
	//      camAngleRad = Mathf.Deg2Rad(camAngle);
	//      horizon = horizon.Rotated(carNormal, camAngleRad);
	//      inDiff = 0;
	//      prevInput = 0;
	//    }
	//    else
	//    {
	//
	//      prevInput = 0;
	//      camAngle = 0f;
	//      input = 0f;
	//      rotTween.Stop(this);
	//      curDir = 0;
	//      inDiff = 0;
	//    }
	//    rotTween.Start();
	//    rotTween.InterpolateProperty(this, "camAngle", camAngle, camAngle - angDiff, lerpVal,
	//  Tween.TransitionType.Expo, Tween.EaseType.Out);
	//  }
	//
	//
	//
	//  noise_y += 1;
	//  offset1.x = amount1 * noise.GetNoise2d(noise.Seed, noise_y);
	//  offset1.y = amount1 * noise.GetNoise2d(noise.Seed * 2, noise_y);
	//  offset1.z = amount1 * noise.GetNoise2d(noise.Seed * 3, noise_y);
	//  offset2.x = amount2 * noise.GetNoise2d(noise.Seed * 4, noise_y);
	//  offset2.y = amount2 * noise.GetNoise2d(noise.Seed * 5, noise_y);
	//  offset2.z = amount2 * noise.GetNoise2d(noise.Seed * 6, noise_y);
	//
	//  Rotation = Rotation + offset1;
	//  Translation = Translation + offset2;
  }


  //  Vector3 movingAverageVec(System.Collections.Generic.Queue<Vector3> buffer, Vector3 current, Vector3 sum)
  //  {
  //	buffer.Enqueue(current);
  //	sum += current;
  //	if (buffer.Count >= 1000)
  //	  sum -= sum + current - buffer.Dequeue();
  //	//GD.Print(buffer.Count);
  //	//GD.Print(sum);
  //
  //	return sum / buffer.Count;
  //  }
  //  float movingAverage(System.Collections.Generic.Queue<float> buffer, float current, float sum)
  //  {
  //	buffer.Enqueue(current);
  //	sum += current;
  //	if (buffer.Count >= 1000)
  //	  sum -= sum + current - buffer.Dequeue();
  //	//GD.Print(buffer.Count);
  //	return sum / buffer.Count;
  //  }
}
 */
