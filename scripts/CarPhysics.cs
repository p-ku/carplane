using Godot;
using System;


public class CarPhysics : VehicleBody
{
  Vars vars;

  Area Gravity;
  float PI = Mathf.Pi;
  Vector3 tailPos = new Vector3(0F, 0F, -2F);
  Vector3 liftPos = new Vector3(0F, 0F, -0.3F);
  float MAX_ENGINE_FORCE = 100F;
  Vector3 levelTorque;
  float MAX_BRAKE = 5F;
  float MAX_STEERING = 0.5F;
  float STEERING_SPEED = 7F;
  Transform windFrame;
  Plane longPlane;
  float ROLLING_SPEED = 3F;

  float PITCHING_SPEED = 10F;

  float rolling;
  float pitching;
  float AoA;
  float yawAng;

  Vector3 lift;
  float throttle_val;
  float thrust_mag;

  Vector3 torque_roll;
  Vector3 torque_pitch;
  Vector3 torque_yaw;

  float Clift;
  //float dotted;

  float bank;
  Vector3 tar_roll;
  Vector3 tar_yaw;

  float steer_val;
  float brake_val;

  Vector3 thrust;
  Vector3 level_dir;
  //Vector3 vert;
  Vector3 level;

  Vector3 level_pitch;
  Vector3 level_roll;
  //float up;
  float level_mag;
  float level_ax;
  float cur_level;
  float tar_level;


  float liftClamp1;
  float liftClamp2;
  float xAng;
  float yAng;
  float zAng;

  float CliftConst = Mathf.Pi / 6f;

  Vector3 localAngVel;
  Vector3 localLinVel;
  float liftConst = 0.5F * 1.229F;

  Vector3 thrustBase = new Vector3(0f, 0f, 50000000f);
  HingeJoint LeftHinge;
  HingeJoint RightHinge;
  RayCast rayLift;
  RayCast rayTorque;
  RayCast rayThrust;
  RayCast rayVel;


  public override void _Ready()
  {
	vars = (Vars)GetNode("/root/Vars");
	LeftHinge = (HingeJoint)GetNode("LeftWing/LeftHinge");
	RightHinge = (HingeJoint)GetNode("RightWing/RightHinge");
	Gravity = (Area)GetNode("../Gravity");
	rayLift = (RayCast)GetNode("rayLift");
	rayTorque = (RayCast)GetNode("rayTorque");
	rayThrust = (RayCast)GetNode("rayThrust");
	rayVel = (RayCast)GetNode("rayVel");
	rayLift.Translation = liftPos;
	rayThrust.Translation = tailPos;

	liftClamp1 = -PI / 30f;
	liftClamp2 = PI / 10f;
  }

  float lerp(float firstFloat, float secondFloat, float by)
  { return firstFloat * (1F - by) + secondFloat * by; }
  Vector3 Lerp(Vector3 firstVector, Vector3 secondVector, float by)
  {
	float retX = lerp(firstVector.x, secondVector.x, by);
	float retY = lerp(firstVector.y, secondVector.y, by);
	float retZ = lerp(firstVector.z, secondVector.z, by);
	return new Vector3(retX, retY, retZ);
  }
  public override void _PhysicsProcess(float delta)
  {
	vars.car_pos = GlobalTransform.origin;
	vars.car_norm = vars.car_pos.Normalized();
	vars.car_alt = vars.car_pos.Length() - vars.planet_radius;
	vars.car_basis = GlobalTransform.basis;
	vars.car_transform = GlobalTransform;

	if (vars.LHori == 0 & vars.LVert == 0)
	{
	  level_dir = vars.car_norm.Cross(Transform.basis.y).Normalized();

	  var forward_level = level_dir.Dot(LinearVelocity.Normalized());
	  level_roll = forward_level * level * Transform.basis.z;
	  level_pitch = level_dir.Dot(Transform.basis.x.Normalized()) * level * Transform.basis.x * (1f - Mathf.Abs(forward_level));
	  level_mag = Transform.basis.x.Length();
	}
	else
	{
	  level = Vector3.Zero;
	  level_pitch = Vector3.Zero;
	  level_roll = Vector3.Zero;
	}
	vars.AngDamp = AngularDamp;

	bank = GlobalTransform.basis.x.Dot(vars.car_norm);
	tar_roll = vars.car_norm.Rotated(Transform.basis.z.Normalized(), vars.LHori * PI / 3f);
	torque_roll = Transform.basis.y.Cross(tar_roll) / 2f + level_roll;
	torque_pitch = Transform.basis.x * (vars.LVert - Mathf.Abs(Mathf.Sin(vars.LHori * PI / 2f)) / 2f) + level_pitch;

	tar_yaw = vars.car_norm.Rotated(Transform.basis.y.Normalized(), -vars.LHori * PI);
	torque_yaw = -Mathf.Sin(vars.LHori * PI / 2f) * Transform.basis.y;


	AddTorque((torque_yaw + torque_roll + torque_pitch) * 100000f * delta * AngularDamp);
	//AddTorque((vars.LVert * Transform.basis.x.Normalized() + vars.LHori * Transform.basis.z.Normalized()) * 500 + AngDamp);
	localAngVel = Transform.basis.XformInv(AngularVelocity);
	localLinVel = Transform.basis.XformInv(LinearVelocity);


	vars.LocLinVel = localLinVel;
	vars.LinVel = LinearVelocity;

	steer_val = 0.0F;
	brake_val = 0.0F;

	throttle_val = Input.GetActionStrength("thrust");

	thrust = thrustBase * delta / vars.car_pos.Length();

	thrust_mag = thrust.Length();

	brake_val = Input.GetActionStrength("brake");
	steer_val = Input.GetActionStrength("turn_left") - Input.GetActionStrength("turn_right");

	EngineForce = throttle_val * MAX_ENGINE_FORCE;
	Brake = brake_val * MAX_BRAKE;

	// Using lerp for a smooth steering
	Steering = lerp(Steering, steer_val * MAX_STEERING, STEERING_SPEED * delta);
	rolling = lerp(rolling, vars.LHori, ROLLING_SPEED * delta);
	pitching = -lerp(pitching, -vars.LVert, PITCHING_SPEED * delta);




	/* 	if (vars.flying)
	  { */
	yawAng = GlobalTransform.basis.z.SignedAngleTo(LinearVelocity, -GlobalTransform.basis.y);
	//AoA = GlobalTransform.basis.z.SignedAngleTo(LinearVelocity, GlobalTransform.basis.x);
	xAng = GlobalTransform.basis.x.AngleTo(LinearVelocity);
	yAng = GlobalTransform.basis.y.AngleTo(LinearVelocity);
	zAng = GlobalTransform.basis.z.AngleTo(LinearVelocity);

	longPlane.Normal = vars.car_basis.x;

	windFrame.basis.x = LinearVelocity;
	windFrame.basis.z = longPlane.Project(LinearVelocity).Rotated(vars.car_basis.x, -PI / 2f);
	windFrame.basis.y = windFrame.basis.z.Cross(windFrame.basis.x);
	windFrame.Orthonormalized();
	AoA = PI / 2f - windFrame.basis.z.AngleTo(GlobalTransform.basis.z);
	//windFrame.basis.z = vars.car_norm.Rotated(LinearVelocity.Cross(vars.car_norm).Normalized(), PI / 2f);
	vars.windFrame = windFrame.basis;

	vars.AoA = AoA;
	//Clift = Mathf.Sin(15f * Mathf.Clamp(AoA, liftClamp1, liftClamp2)) + 1f;
	Clift = Mathf.Sin(15f * Mathf.Clamp(yAng, liftClamp1 - PI / 2f, liftClamp2 - PI / 2f)) + 1f;
	Clift = Mathf.Sin(2f * AoA);
	Clift = delta * Clift * liftConst * LinearVelocity.Project(GlobalTransform.basis.z).LengthSquared();
	// lift = Clift * LinearVelocity.Normalized();
	lift = Clift * windFrame.basis.z.Normalized();
	//lift = lift.Rotated(LinearVelocity.Cross(vars.car_norm).Normalized(), PI / 2f);
	//lift = lift.Rotated(vars.car_basis.x, PI / 2f);

	//lift = lift.Rotated(LinearVelocity.Cross(vars.car_norm).Normalized(), PI / 2f);
	//if (yAng < PI / 2f & zAng > PI / 2f) { lift = -lift; }
	//else { lift = lift.Rotated(LinearVelocity.Cross(vars.car_norm).Normalized(), PI / 2f); }


	//Clift = Mathf.Sin(15f * Mathf.Clamp(AoA, liftClamp1, liftClamp2)) + 1f;

	if (vars.flying)
	{
	  AddForce(Transform.basis.Xform(thrust), Transform.basis.Xform(tailPos));
	}
	//AddCentralForce(Transform.basis.Xform(lift));
	AddCentralForce(lift);
	vars.liftAng = lift.AngleTo(LinearVelocity);
	vars.Lift = lift;
	vars.Clift = Clift;
	//rayLift.CastTo = vars.car_pos + lift;
	//rayLift.CastTo = Transform.basis.XformInv(lift) + ToLocal(vars.car_pos);
	rayLift.CastTo = 10f * Transform.basis.XformInv(lift);
	//rayLift.CastTo = new Vector3(1f, 0f, 0f);

	//}
	//levelTorque = 500f * Mathf.Sin(AoA) * Mathf.Sin(yawAng) * Basis.Identity.x;
	//AddTorque(Transform.basis.Xform(levelTorque) * LinearVelocity.LengthSquared());
	rayTorque.CastTo = levelTorque / 10f;

	rayThrust.CastTo = thrust / 10f;
	rayVel.CastTo = localLinVel * 10000f;


	//	LinearDamp = LinearVelocity.LengthSquared() / vars.car_pos.LengthSquared();
	// AngularDamp = LinearVelocity.LengthSquared() / vars.car_pos.LengthSquared();

	LinearDamp = LinearVelocity.LengthSquared() / vars.car_pos.Length();
	AngularDamp = AngularVelocity.LengthSquared() / vars.car_pos.Length() + LinearDamp;

  }

  public override void _Input(InputEvent @event)
  {
	if (Input.IsActionJustPressed("wings"))
	{
	  vars.flying = !vars.flying;
	  if (vars.flying)
	  {
		LeftHinge.Motor__targetVelocity = -100f;
		RightHinge.Motor__targetVelocity = -100f;
	  }
	  else
	  {
		LeftHinge.Motor__targetVelocity = 100f;
		RightHinge.Motor__targetVelocity = 100f;
		lift = Vector3.Zero;
	  }
	}
  }
}
