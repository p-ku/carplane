using Godot;
using System;

public class StickCam : Camera
{
  Vars vars;
  float speed = 20F;
  Vector3 target;
  Vector3 look;
  Vector3 prevLook;
  RigidBody carAnchor;
  RigidBody camAnchor;

  Vector3 rotator;
  float camCheck;
  float smallAng;
  float stickAng;
  float prevAng;
  float lerpVal = 0.3f;

  float stickHyp;
  float camRad = 3f;
  Vector3 camRadVec = new Vector3(0f, 3f, 0f);

  Vector3 rotPos;
  Vector2 input;
  Vector2 prevInput;

  Vector3 stick3;
  float angDiff;
  float tanLen;
  float tanAng;
  Vector2 step = new Vector2(0.1f, 0.1f);
  Vector3 tanVec;

  Transform rotTransform;

  public override void _Ready()
  {
	vars = (Vars)GetNode("/root/Vars");

	camAnchor = (RigidBody)GetNode("../../CamAnchor");
	carAnchor = (RigidBody)GetNode("../../CarAnchor");
	look = vars.car_pos;
	prevLook = vars.car_pos;
	rotTransform = GlobalTransform;
  }
  public override void _PhysicsProcess(float delta)
  {

	input = Input.GetVector("CamDown", "CamUp", "CamRight", "CamLeft");

	vars.stick2 = input;
	if (input.x == 0f)
	{
	  input.x = 1f;
	}
	input = (input + prevInput) / 2f;

	stickAng = input.Angle();
	angDiff = stickAng - prevAng;

	vars.stickAng = angDiff;


	//if (Math.Abs(angDiff) > Mathf.Pi)
	//{

	angDiff = stickAng - prevAng;
	if (Mathf.Abs(angDiff) > Mathf.Pi)
	{
	  /*       if (Mathf.Abs(stickAng) > Mathf.Abs(angDiff))
	  {
	  if (angDiff > 0)
	  {
	  stickAng = prevAng - (Mathf.Pi - angDiff * lerpVal);
	  }
	  else
	  {
	  stickAng = prevAng + (Mathf.Pi + angDiff * lerpVal);
	  }
	  }
	  else
	  { */
	  smallAng = 2f * Mathf.Pi - Mathf.Abs(angDiff);
	  camCheck = Mathf.Abs(stickAng) + lerpVal * smallAng;

	  if (stickAng > 0)
	  {
		//if (camCheck > Mathf.Pi - Mathf.Abs(prevAng))
		if (camCheck > Mathf.Pi)
		{
		  //stickAng = stickAng + (Mathf.Pi - angDiff * (1f - lerpVal));
		  stickAng = prevAng - smallAng * lerpVal;

		}
		else
		{
		  //stickAng = prevAng + (Mathf.Pi - angDiff * lerpVal);
		  stickAng = stickAng + smallAng * (1f - lerpVal);
		}
	  }
	  else
	  {
		//if (camCheck > Mathf.Pi - Mathf.Abs(stickAng))
		if (camCheck > Mathf.Pi)
		{
		  //stickAng = prevAng - (Mathf.Pi + angDiff * (1f - lerpVal));
		  stickAng = prevAng + smallAng * lerpVal;
		}
		else
		{
		  // stickAng = stickAng - (Mathf.Pi + angDiff * lerpVal);
		  stickAng = stickAng - smallAng * (1f - lerpVal);
		}
	  }
	  //  }
	}
	else
	{
	  stickAng = prevAng + angDiff * lerpVal;
	}

	tanLen = Mathf.Sqrt(Mathf.Pow(2, vars.cam_alt) - Mathf.Pow(2, vars.planet_radius));
	tanAng = Mathf.Pi / 2f - Mathf.Asin(vars.planet_radius / GlobalTransform.origin.Length());
	tanAng = Mathf.Asin(vars.planet_radius / GlobalTransform.origin.Length());

	//tanAng = Mathf.Pi - Mathf.Asin(vars.planet_radius / GlobalTransform.origin.Length());

	tanVec = 1.3f * vars.planet_radius * vars.car_norm.Rotated(GlobalTransform.origin.Cross(carAnchor.GlobalTransform.origin).Normalized(), tanAng);
	//tanVec = (GlobalTransform.origin - carAnchor.GlobalTransform.origin).Rotated(carAnchor.GlobalTransform.origin.Cross(GlobalTransform.origin).Normalized(), -tanAng);

	//tanVec = prevLook.LinearInterpolate(tanVec, 0.8f);

	rotPos = camAnchor.GlobalTransform.origin.Rotated(vars.car_norm, stickAng);
	rotPos = GlobalTransform.origin + (rotPos - GlobalTransform.origin) * lerpVal;
	//GlobalTransform = GlobalTransform.InterpolateWith(rotTransform, lerpVal);
	//GlobalTransform = rotTransform;

	//look = prevLook.LinearInterpolate(vars.car_pos + vars.LinVel.LengthSquared() * vars.LinVel.Normalized() / 100f, 0.1f);
	LookAtFromPosition(rotPos, tanVec, GlobalTransform.origin);

	prevLook = tanVec;
	prevAng = stickAng;
	prevInput = input;
	//rotTransform.basis = GlobalTransform.basis;


	vars.cam_pos = GlobalTransform.origin;
	vars.cam_alt = GlobalTransform.origin.Length() - vars.planet_radius;

  }

}
