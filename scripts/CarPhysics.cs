using Godot;
using System;


public class CarPhysics : VehicleBody
{
  Vars myVars;

  //Car variables
  //Can't fly below this speed
  //float sines;
  Area Gravity;
  float min_flight_speed = 10F;
  float max_flight_speed = 30F;
  float turn_speed = 0.75F;
  float pitch_speed = 0.5F;
  float level_speed = 3F;
  float throttle_delta = 30F;
  float acceleration = 6F;
  float altitude;
  float forward_speed;
  float target_speed;
  bool grounded;
  float last_turn;
  float smooth_input;
  float PI = Mathf.Pi;
  Vector3 tailPos = new Vector3(0F, 0F, -2F);
  Vector3 liftPos = new Vector3(0F, 0F, -0.3F);

  [Export] bool use_controls = true;
  [Export] bool show_settings = true;
  //These become just placeholders if presets are in use
  float MAX_ENGINE_FORCE = 100F;
  Vector3 levelTorque;
  float MAX_BRAKE = 5F;
  float MAX_STEERING = 0.5F;
  float STEERING_SPEED = 7F;

  float ROLLING_SPEED = 3F;

  float PITCHING_SPEED = 10F;

  float linvel;
  [Export] float base_gravity = 15F;
  float grav;
  float gravit3;
  Transform bas;
  float rolling;
  float pitching;
  float AoA;
  float yawAng;
  //float tangent;
  float cl;
  Vector3 lift;
  float throttle_val;
  float thrust_mag;
  float torque_level;
  Vector3 torque_level_vec;
  Vector3 torque_roll;
  Vector3 torque_pitch;
  Vector3 torque_yaw;
  Vector3 torque_turn;
  float Clift;
  //float dotted;

  float bank;
  Vector3 tar_roll;
  Vector3 tar_yaw;

  float steer_val;
  float brake_val;
  float density = 10F;
  Vector3 thrust;
  Vector3 level_dir;
  //Vector3 vert;
  Vector3 level;
  Vector3 roll_level;
  Vector3 pitch_level;
  Vector3 yaw_level;
  Vector3 level_pitch;
  Vector3 level_roll;
  //float up;
  float level_mag;
  float level_ax;
  float cur_level;
  float tar_level;
  Vector3 tar_level_vec;

  float bastest;
  float r_lift;
  float l_lift;
  Vector3 tail_lift;
  float liftClamp1;
  float liftClamp2;
  float CliftConst = Mathf.Pi / 6;

  float GravLin;
  float GravAng;
  float LeftImp;
  float RightImp;
  Vector3 localAngVel;
  Vector3 localLinVel;
  float liftConst = 0.5F * 1.229F;

  Vector3 thrustBase = new Vector3(0, 0, 12000000f);

  HingeJoint LeftHinge;
  HingeJoint RightHinge;
  RayCast rayLift;
  RayCast rayTorque;
  RayCast rayThrust;
  RayCast rayVel;

  //float p = myVars.LVert;
  //float r = myVars.LHori;


  // Declare member variables here. Examples:
  // private int a = 2;
  // private string b = "text";

  // Called when the node enters the scene tree for the first time.
  public override void _Ready()
  {
	myVars = (Vars)GetNode("/root/Vars");
	LeftHinge = (HingeJoint)GetNode("LeftWing/LeftHinge");
	RightHinge = (HingeJoint)GetNode("RightWing/RightHinge");
	Gravity = (Area)GetNode("../Gravity");
	rayLift = (RayCast)GetNode("rayLift");
	rayTorque = (RayCast)GetNode("rayTorque");
	rayThrust = (RayCast)GetNode("rayThrust");
	rayVel = (RayCast)GetNode("rayVel");
	rayLift.Translation = liftPos;
	rayThrust.Translation = tailPos;
	//Transform.basis.Rotated(Transform.basis.x, Mathf.Pi / 4).Orthonormalized();
	//LeftHinge.Transform.basis.Rotated(Transform.basis.x, Mathf.Pi / 4).Orthonormalized();
	//RightHinge.Transform.basis.Rotated(Transform.basis.x, Mathf.Pi / 4).Orthonormalized();
	/* 	liftClamp1 = (1F / 15F) * (-2F * Mathf.Pi + Mathf.Pi + Mathf.Asin(Mathf.Pi / 6F));
	  liftClamp2 = (1F / 15F) * (Mathf.Pi + Mathf.Asin(Mathf.Pi / 6F)); */
	liftClamp1 = -Mathf.Pi / 30f;
	liftClamp2 = Mathf.Pi / 10f;
	//SetCanSleep(false);


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
	//var up = Transform(myVars.car_norm.rotated(GlobalTransform.basis.z),myVars.car_norm,Vector3(),Vector3())
	//bas = Transform;
	//bastest = bas.basis.x.Length();

	myVars.car_pos = GlobalTransform.origin;
	myVars.car_norm = myVars.car_pos.Normalized();
	myVars.car_alt = myVars.car_pos.Length() - myVars.planet_radius;
	//myVars.LHori = Input.GetActionStrength("turn_right") - Input.GetActionStrength("turn_left");
	//myVars.LVert = Input.GetActionStrength("pitch_down") - Input.GetActionStrength("pitch_up");// - Mathf.Abs(myVars.LHori) / 3;

	myVars.car_basis = GlobalTransform.basis;
	myVars.car_transform = GlobalTransform;

	//tangent = myVars.car_pos.AngleTo(Transform.basis.z) - PI * 0.55F;
	//up = myVars.car_pos.AngleTo(Transform.basis.y);
	//aoa = LinearVelocity.AngleTo(Transform.basis.z);//#-PI*0.05;
	//sines = Mathf.Abs(PI / 2f - Mathf.Abs(Transform.basis.z.AngleTo(myVars.car_pos) - PI / 2f)) / (PI / 2f);

	//vert = Mathf.Abs(Transform.basis.y.Dot(myVars.car_norm));
	//dotted = Mathf.Abs(myVars.car_norm.Dot(Transform.basis.y));


	if (myVars.LHori == 0 & myVars.LVert == 0)
	{
	  // "up" from car
	  level_dir = myVars.car_norm.Cross(Transform.basis.y).Normalized();

	  //tar_level = -Mathf.Sin(up / 2F) * LinearVelocity.Length();
	  //level = lerp(0F, tar_level, delta);

	  var forward_level = level_dir.Dot(LinearVelocity.Normalized());
	  level_roll = forward_level * level * Transform.basis.z;
	  level_pitch = level_dir.Dot(Transform.basis.x.Normalized()) * level * Transform.basis.x * (1f - Mathf.Abs(forward_level));
	  level_mag = Transform.basis.x.Length();
	}
	else
	{
	  //tar_level_vec = Transform.basis.z.Rotated(Transform.basis.x.Normalized(), -tangent);
	  level = Vector3.Zero;
	  level_pitch = Vector3.Zero;
	  level_roll = Vector3.Zero;
	}


	bank = GlobalTransform.basis.x.Dot(myVars.car_norm);
	tar_roll = myVars.car_norm.Rotated(Transform.basis.z.Normalized(), myVars.LHori * PI / 3f);
	torque_roll = Transform.basis.y.Cross(tar_roll) / 2 + level_roll;
	torque_pitch = Transform.basis.x * (myVars.LVert - Mathf.Abs(Mathf.Sin(myVars.LHori * PI / 2f)) / 2f) + level_pitch;

	tar_yaw = myVars.car_norm.Rotated(Transform.basis.y.Normalized(), -myVars.LHori * PI);
	//#(Transform.basis.y.cross(tar_yaw)/1.6).rotated(Transform.basis.x.Normalized(),PI/2);
	torque_yaw = -Mathf.Sin(myVars.LHori * PI / 2f) * Transform.basis.y;


	AddTorque((torque_yaw + torque_roll + torque_pitch) * 500f * delta);
	//AddTorque((myVars.LVert * Transform.basis.x.Normalized() + myVars.LHori * Transform.basis.z.Normalized()) * 500 + AngDamp);
	localAngVel = Transform.basis.XformInv(AngularVelocity);
	localLinVel = Transform.basis.XformInv(LinearVelocity);

	//localLinVel = GlobalTransform.basis.XformInv(LinearVelocity);
	//localLinVel = GlobalTransform.XformInv(LinearVelocity);
	//localLinVel = Transform.basis.XformInv(LinearVelocity);
	//localLinVel = Transform.XformInv(LinearVelocity);
	//localLinVel = GlobalTransform.basis.Xform(LinearVelocity);
	//localLinVel = GlobalTransform.XformInv(LinearVelocity);
	//localLinVel = Transform.basis.Xform(LinearVelocity);
	//localLinVel = Transform.Xform(LinearVelocity);





	myVars.LocLinVel = localLinVel;
	myVars.LinVel = LinearVelocity;

	steer_val = 0.0F;
	brake_val = 0.0F;

	throttle_val = Input.GetActionStrength("thrust");

	thrust = thrustBase * delta / myVars.car_pos.Length();

	thrust_mag = thrust.Length();

	brake_val = Input.GetActionStrength("brake");
	steer_val = Input.GetActionStrength("turn_left") - Input.GetActionStrength("turn_right");

	EngineForce = throttle_val * MAX_ENGINE_FORCE;
	Brake = brake_val * MAX_BRAKE;

	// Using lerp for a smooth steering
	Steering = lerp(Steering, steer_val * MAX_STEERING, STEERING_SPEED * delta);
	rolling = lerp(rolling, myVars.LHori, ROLLING_SPEED * delta);
	pitching = -lerp(pitching, -myVars.LVert, PITCHING_SPEED * delta);

	AoA = GlobalTransform.basis.z.SignedAngleTo(LinearVelocity, GlobalTransform.basis.x);
	yawAng = Transform.basis.z.SignedAngleTo(LinearVelocity, Transform.basis.y);

	myVars.AoA = AoA;

	if (myVars.flying)
	{

	  //lift = 5 * (LinearVelocity * LinearVelocity * Mathf.Sin(6F * Mathf.Clamp(Transform.basis.z.AngleTo(LinearVelocity), -PI / 6F, PI / 6F))).Length() * Transform.basis.y;//torque_pitch.Length()*stepify(myVars.LVert,1));

	  Clift = Mathf.Sin(15f * Mathf.Clamp(AoA, liftClamp1, liftClamp2)) + 1f;

	  lift = Clift * liftConst * LinearVelocity.LengthSquared() * Basis.Identity.y.Rotated(Basis.Identity.x, AoA);

	  AddForce(Transform.basis.Xform(thrust), Transform.basis.Xform(tailPos));
	  //AddCentralForce(lift);
	  //AddForce(Transform.basis.Xform(-1000F * lift), Transform.basis.Xform(liftPos));
	  AddCentralForce(Transform.basis.Xform(lift) * delta);//, Transform.basis.Xform(tailPos));



	  myVars.Lift = lift;
	  myVars.Clift = Clift;



	}
	levelTorque = 500f * Mathf.Sin(AoA) * Mathf.Sin(yawAng) * Basis.Identity.x;
	//AddTorque(Transform.basis.Xform(levelTorque) * LinearVelocity.LengthSquared());
	rayTorque.CastTo = levelTorque / 10f;

	rayThrust.CastTo = thrust / 10f;
	rayVel.CastTo = localLinVel * 10000f;
	rayLift.CastTo = lift / 10f;
	LinearDamp = LinearVelocity.LengthSquared() / myVars.car_pos.LengthSquared();

	myVars.dampy = LinearDamp;
  }
  //$"../LeftHinge".set_param($"../LeftHinge".PARAM_MOTOR_TARGET_VELOCITY, 100)

  //  // Called every frame. 'delta' is the elapsed time since the previous frame.
  public override void _Input(InputEvent @event)
  {
	if (Input.IsActionJustPressed("wings"))
	{
	  myVars.flying = !myVars.flying;
	  if (myVars.flying)
	  {
		//LeftHinge.SetParam(HingeJoint.Param.MotorMaxImpulse, -100F);
		LeftHinge.Motor__targetVelocity = -100f;
		RightHinge.Motor__targetVelocity = -100f;
		/* 		Gravity.AngularDamp = 4f;
		  Gravity.LinearDamp = 4f; */
	  }
	  else
	  {
		LeftHinge.Motor__targetVelocity = 100f;
		RightHinge.Motor__targetVelocity = 100f;
		lift = Vector3.Zero;
		/* 		Gravity.AngularDamp = 0.1f;
		  Gravity.LinearDamp = 0.1f; */
	  }

	}
	// base._Input(@event); 
  }
}
