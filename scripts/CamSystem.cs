using Godot;
using System;

public class CamSystem : StaticBody
{
  Vars vars;
  Transform systemTransform;
  Vector3 look;
  Vector3 prevLook;
  StaticBody carAnchor;
  RigidBody camAnchor;

  float camCheck;
  float smallAng;
  float stickAng;
  float prevAng;
  float lerpVal = 0.3f;


  Vector3 rotPos;
  Vector2 input;
  float prevInput;

  float angDiff;
  float tanLen;
  Vector3 debug;
  float horizonAng;
  Vector3 horizon;
  Transform rotTransform;
  Vector3 max_offset = new Vector3(0.1f, 0.1f, 0.1f);  // Maximum hor/ver shake in pixels.
  float amount = 0.01f;
  Vector3 offset;

  OpenSimplexNoise noise = new OpenSimplexNoise();
  int noise_y = 0;
  RigidBody car;
  Camera cam;


  public override void _Ready()
  {
	vars = (Vars)GetNode("/root/Vars");
	car = (RigidBody)GetNode("../../Car");
	//carAnchor = (StaticBody)GetNode("../CarAnchor");

	camAnchor = (RigidBody)GetNode("../CamAnchor");
	cam = (Camera)GetNode("../CamAnchor/Camera");

	rotTransform = cam.GlobalTransform;

	systemTransform = GlobalTransform;

	systemTransform.origin = vars.car_pos;

	/* 	  systemTransform.basis.z = vars.car_norm;
	  systemTransform.basis.y = systemTransform.origin.DirectionTo(camAnchor.GlobalTransform.origin);
	  systemTransform.basis.x = systemTransform.basis.y.Cross(systemTransform.basis.z).Normalized(); */
	GlobalTransform = systemTransform;
	GD.Randomize();
	noise.Seed = (int)GD.Randi();
	noise.Period = 4f;
	noise.Octaves = 2;
  }


  public override void _Process(float delta)
  {
	amount = car.LinearDamp / 100f;

	cam.Fov = 80f + vars.LinVel.Length();



	/* 	systemTransform.origin = vars.car_pos * 1.15f;
	  systemTransform.basis.z = vars.car_norm;
	  systemTransform.basis.y = systemTransform.origin.DirectionTo(camAnchor.GlobalTransform.origin);
	  systemTransform.basis.x = systemTransform.basis.z.Cross(systemTransform.basis.y).Normalized(); */
	systemTransform.origin = vars.car_pos * 1.05f;

	systemTransform.basis = GlobalTransform.basis;

	//GlobalTransform = systemTransform.Orthonormalized();

	GlobalTransform = systemTransform;
	LookAt(vars.car_pos, GlobalTransform.basis.x);


	horizonAng = Mathf.Asin(vars.planet_radius / GlobalTransform.origin.Length());
	horizon = camAnchor.GlobalTransform.origin.DirectionTo(GlobalTransform.origin);

	horizon = horizon.Rotated(vars.car_norm, Mathf.Pi / 2f);

	horizon = vars.car_norm.Rotated(horizon, horizonAng);
	horizon = 1.4f * vars.planet_radius * horizon;


	if (Input.IsActionPressed("CamReverse"))
	{
	  lerpVal = 1f;
	  stickAng = Mathf.Pi;
	  horizon = horizon.Rotated(vars.car_norm, stickAng);

	}
	else
	{
	  if (Input.IsActionPressed("CamUp") | Input.IsActionPressed("CamDown") | Input.IsActionPressed("CamLeft") | Input.IsActionPressed("CamRight"))
	  {
		input = Input.GetVector("CamDown", "CamUp", "CamRight", "CamLeft");
		stickAng = input.Angle();
		horizon = horizon.Rotated(vars.car_norm, stickAng);
	  }
	}

	lerpVal = 0.3f;


	//rotTransform.origin = GlobalTransform.origin - horizon.Rotated(vars.car_norm, stickAng) / 15f;
	//rotTransform.origin = GlobalTransform.origin - horizon / 5f;



	//rotTransform.origin = cam.GlobalTransform.origin + (rotTransform.origin - cam.GlobalTransform.origin) * lerpVal;

	rotTransform.origin = GlobalTransform.origin + horizon.Rotated(vars.car_norm, Mathf.Pi) / 10f;
	vars.debug = rotTransform.origin;

	cam.GlobalTransform = rotTransform;

	//cam.LookAtFromPosition(GlobalTransform.origin + vars.car_norm, horizon, cam.GlobalTransform.origin);
	cam.LookAt(horizon, GlobalTransform.origin);

	vars.cam_pos = cam.GlobalTransform.origin;
	vars.cam_alt = cam.GlobalTransform.origin.Length() - vars.planet_radius;


	noise_y += 1;
	offset.x = max_offset.x * amount * noise.GetNoise2d(noise.Seed, noise_y);
	offset.y = max_offset.y * amount * noise.GetNoise2d(noise.Seed * 2, noise_y);
	offset.z = max_offset.z * amount * noise.GetNoise2d(noise.Seed * 3, noise_y);

	Rotation = Rotation + offset;


  }

}
