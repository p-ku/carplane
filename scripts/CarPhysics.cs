using Godot;
using System;


public class CarPhysics : VehicleBody
{
  Vars myVars;
  HingeJoint LeftHinge;
  HingeJoint RightHinge;
  Area Gravity;
  //Car variables
  //Can't fly below this speed
  float sines;
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

  [Export] bool use_controls = true;
  [Export] bool show_settings = true;
  //These become just placeholders if presets are in use
  float MAX_ENGINE_FORCE = 100F;
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
  float aoa;
  float tangent;
  float cl;
  Vector3 lift;
  float lift_mag;
  float throttle_val;
  float thrust_mag;
  float torque_level;
  Vector3 torque_level_vec;
  Vector3 torque_roll;
  Vector3 torque_pitch;
  Vector3 torque_yaw;
  Vector3 torque_turn;

  float dotted;

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
  float up;
  float level_mag;
  float level_ax;
  float cur_level;
  float tar_level;
  Vector3 tar_level_vec;

  float bastest;
  float r_lift;
  float l_lift;
  float tail_lift;
  float GravLin;
  float GravAng;
  float LeftImp;
  float RightImp;
  //float p = myVars.LVert;
  //float r = myVars.LHori;


  // Declare member variables here. Examples:
  // private int a = 2;
  // private string b = "text";

  // Called when the node enters the scene tree for the first time.
  public override void _Ready()
  {
	myVars = (Vars)GetNode("/root/Vars");
	HingeJoint LeftHinge = (HingeJoint)GetNode("../LeftHinge");
	HingeJoint RightHinge = (HingeJoint)GetNode("../RightHinge");
	Area Gravity = (Area)GetNode("../../Gravity");

	//SetCanSleep(false);


  }
  float lerp(float firstFloat, float secondFloat, float by)
  {
	return firstFloat * (1F - by) + secondFloat * by;
  }
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
	bas = Transform;
	bastest = bas.basis.x.Length();

	myVars.car_pos = GlobalTransform.origin;
	myVars.car_norm = myVars.car_pos.Normalized();
	myVars.car_alt = myVars.car_pos.Length() - myVars.planet_radius;
	//myVars.LHori = Input.GetActionStrength("turn_right") - Input.GetActionStrength("turn_left");
	//myVars.LVert = Input.GetActionStrength("pitch_down") - Input.GetActionStrength("pitch_up");// - Mathf.Abs(myVars.LHori) / 3;

	myVars.car_basis = GlobalTransform.basis;

	tangent = myVars.car_pos.AngleTo(Transform.basis.z) - PI * 0.55F;
	up = myVars.car_pos.AngleTo(Transform.basis.y);
	aoa = LinearVelocity.AngleTo(Transform.basis.z);//#-PI*0.05;
	sines = Mathf.Abs(PI / 2 - Mathf.Abs(Transform.basis.z.AngleTo(myVars.car_pos) - PI / 2)) / (PI / 2);

	//vert = Mathf.Abs(Transform.basis.y.Dot(myVars.car_norm));
	dotted = Mathf.Abs(myVars.car_norm.Dot(Transform.basis.y));


	if (myVars.LHori == 0 && myVars.LVert == 0)
	{
	  // "up" from car
	  level_dir = myVars.car_norm.Cross(Transform.basis.y).Normalized();

	  //tar_level = -Mathf.Sin(up / 2F) * LinearVelocity.Length();
	  //level = lerp(0F, tar_level, delta);

	  var forward_level = level_dir.Dot(LinearVelocity.Normalized());
	  level_roll = forward_level * level * Transform.basis.z;
	  level_pitch = level_dir.Dot(Transform.basis.x.Normalized()) * level * Transform.basis.x * (1 - Mathf.Abs(forward_level));
	  level_mag = Transform.basis.x.Length();
	}
	else
	{
	  //tar_level_vec = Transform.basis.z.Rotated(Transform.basis.x.Normalized(), -tangent);
	  level = new Vector3();
	  level_pitch = new Vector3();
	  level_roll = new Vector3();
	}


	bank = GlobalTransform.basis.x.Dot(myVars.car_norm);
	tar_roll = myVars.car_norm.Rotated(Transform.basis.z.Normalized(), myVars.LHori * PI / 3);
	torque_roll = Transform.basis.y.Cross(tar_roll) / 2 + level_roll;
	torque_pitch = Transform.basis.x * (myVars.LVert - Mathf.Abs(Mathf.Sin(myVars.LHori * PI / 2)) / 2) + level_pitch;

	tar_yaw = myVars.car_norm.Rotated(Transform.basis.y.Normalized(), -myVars.LHori * PI);
	torque_yaw = new Vector3();//#(Transform.basis.y.cross(tar_yaw)/1.6).rotated(Transform.basis.x.Normalized(),PI/2);
	torque_yaw = -Mathf.Sin(myVars.LHori * PI / 2) * Transform.basis.y;
	AddTorque((torque_yaw + torque_roll + torque_pitch) * 500);

	steer_val = 0.0F;
	brake_val = 0.0F;

	throttle_val = Input.GetActionStrength("thrust");

	thrust = Transform.basis.Xform(new Vector3(0, 0, 100000 / myVars.car_pos.Length()));

	thrust_mag = thrust.Length();

	brake_val = Input.GetActionStrength("brake");
	steer_val = Input.GetActionStrength("turn_left") - Input.GetActionStrength("turn_right");

	EngineForce = throttle_val * MAX_ENGINE_FORCE;
	Brake = brake_val * MAX_BRAKE;

	// Using lerp for a smooth steering
	Steering = lerp(Steering, steer_val * MAX_STEERING, STEERING_SPEED * delta);
	rolling = lerp(rolling, myVars.LHori, ROLLING_SPEED * delta);
	pitching = -lerp(pitching, -myVars.LVert, PITCHING_SPEED * delta);
	//r = myVars.LHori;
	//p = myVars.LVert;

	if (myVars.flying)
	{
	  //l_lift = (LinearVelocity*LinearVelocity*sin(6*Transform.basis.z.AngleTo(LinearVelocity)-0.3*myVars.LHori)).Length()*Transform.basis.z#torque_pitch.Length()*stepify(myVars.LVert,1));
	  //r_lift = (LinearVelocity*LinearVelocity*sin(6*Transform.basis.z.AngleTo(LinearVelocity)+0.3*myVars.LHori)).Length()*Transform.basis.z#torque_pitch.Length()*stepify(myVars.LVert,1));
	  lift = (LinearVelocity * LinearVelocity * Mathf.Sin(6F * Mathf.Clamp(Transform.basis.z.AngleTo(LinearVelocity), -PI / 6F, PI / 6F))).Length() * Transform.basis.y;//torque_pitch.Length()*stepify(myVars.LVert,1));
																																										//tail_lift = (8*LinearVelocity*LinearVelocity*sin(6*Transform.basis.z.AngleTo(LinearVelocity)+0.3*(1/4+myVars.LVert))).Length()*Transform.basis.z;
	  AddForce(thrust, Transform.basis.Xform(new Vector3(0F, 0F, -1.5F)));
	  //add_force(tail_lift/3,Transform.basis.xform(Vector3(0,0,-1.5)));
	  //add_force(l_lift/3,Transform.basis.xform(Vector3(1.5,0,0)));
	  //add_force(r_lift/3,Transform.basis.xform(Vector3(-1.5,0,0)));
	  AddCentralForce(lift * 5F);


	}


	if (Input.IsActionJustPressed("wings"))
	{
	  HingeJoint LeftHinge = (HingeJoint)GetNode("../LeftHinge");
	  HingeJoint RightHinge = (HingeJoint)GetNode("../RightHinge");
	  Area Gravity = (Area)GetNode("../../Gravity");

	  myVars.flying = !myVars.flying;
	  if (myVars.flying)
	  {
		//LeftHinge.SetParam(HingeJoint.Param.MotorMaxImpulse, -100F);
		LeftHinge.Motor__targetVelocity = -100;
		RightHinge.Motor__targetVelocity = -100;
		Gravity.AngularDamp = 4;
		Gravity.LinearDamp = 4;
	  }
	  else
	  {
		LeftHinge.Motor__targetVelocity = 100;
		RightHinge.Motor__targetVelocity = 100;
		Gravity.AngularDamp = 0.1F;
		Gravity.LinearDamp = 0.1F;
	  }
	}
  }
  //$"../LeftHinge".set_param($"../LeftHinge".PARAM_MOTOR_TARGET_VELOCITY, 100)

  //  // Called every frame. 'delta' is the elapsed time since the previous frame.

}
