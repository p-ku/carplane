using Godot;

public class Car : VehicleBody
{
  Globals vars;

  internal Vector3 Normal = Vector3.Forward;
  internal float OriginLength, Altitude;
  internal float angDampSq;
  internal float linVelLen, linVelLenSq, angVelLenSq;
  internal float angVelLen;

  bool flying;
  const float pi = Mathf.Pi;
  Vector3 tailPos = new Vector3(0F, 0F, -2F);
  Vector3 liftPos = new Vector3(0F, 0F, -0.3F);
  float MAX_ENGINE_FORCE = 1000F;
  Vector3 levelTorque;
  float MAX_BRAKE = 50F;
  float MAX_STEERING = -Mathf.Pi / 4f;
  float STEERING_SPEED = 7F;
  Transform windFrame;
  Plane longPlane;
  float ROLLING_SPEED = 3F;

  float PITCHING_SPEED = 10F;
  float lInput;


  float AoA;
  float yawAng;

  public Vector3 lift;


  Vector3 torque_roll;
  Vector3 torque_pitch;
  Vector3 torque_yaw;

  float Clift;
  //float dotted;

  float bank;
  Vector3 tar_roll;
  Vector3 tar_yaw;

  float steer_val;


  Vector3 thrust;
  Vector3 level_dir;
  //Vector3 vert;
  Vector3 level;

  Vector3 level_pitch;
  Vector3 level_roll;
  //float up;
  //float level_mag;
  float level_ax;
  float cur_level;
  float tar_level;


  float liftClamp1;
  float liftClamp2;
  float xAng;
  float yAng;
  float zAng;

  float CliftConst = pi / 6f;

  Vector3 localAngVel;
  Vector3 localLinVel;
  float forward_level;

  float liftConst = 0.5F * 1.229F;

  Vector3 thrustBase = new Vector3(0f, 0f, 60000000f);
  HingeJoint LeftHinge;
  HingeJoint RightHinge;
  RayCast rayLift;
  RayCast rayTorque;
  RayCast rayThrust;
  RayCast rayVel;
  Tween steer;
  public Vector3 wind;

  Tween thrustTween;

  //  StaticBody CarAnchor;
  //  RigidBody CamAnchor;
  //  Camera CarCam;
  //  HingeJoint CamJoint;


  public override void _Ready()
  {
	try { } catch { }
	vars = (Globals)GetTree().Root.FindNode("Globals", true, false);
	// LeftHinge = (HingeJoint)GetNode("LeftWing/LeftHinge");
	// RightHinge = (HingeJoint)GetNode("RightWing/RightHinge");
	// rayLift = (RayCast)GetNode("rayLift");
	// rayTorque = (RayCast)GetNode("rayTorque");
	// rayThrust = (RayCast)GetNode("rayThrust");
	// rayVel = (RayCast)GetNode("rayVel");
	// rayLift.Translation = liftPos;
	// rayThrust.Translation = tailPos;
	thrustTween = (Tween)GetTree().Root.FindNode("ThrustTween", true, false);

	LeftHinge = (HingeJoint)GetTree().Root.FindNode("LeftHinge", true, false);

	RightHinge = (HingeJoint)GetTree().Root.FindNode("RightHinge", true, false);
	rayLift = (RayCast)GetTree().Root.FindNode("rayLift", true, false);
	rayTorque = (RayCast)GetTree().Root.FindNode("rayTorque", true, false);
	rayThrust = (RayCast)GetTree().Root.FindNode("rayThrust", true, false);
	rayVel = (RayCast)GetTree().Root.FindNode("rayVel", true, false);
	rayLift.Translation = liftPos;
	rayThrust.Translation = tailPos;
	liftClamp1 = -pi / 30f;
	liftClamp2 = pi / 10f;
	steer = new Tween();
	//	CarAnchor = new StaticBody();
	//	CamAnchor = new RigidBody();
	//	CarCam = new Camera();
	//	CamJoint = new HingeJoint();
	AddChild(steer);

	//	CarAnchor.InputRayPickable = false;
	//	CarAnchor.Translate(new Vector3(0, 4, 0));
	//	CarAnchor.Rotate(Vector3.Right, pi / 2);
	//	CarAnchor.Visible = false;
	//	AddChild(CarAnchor);
	//
	//	CamAnchor.InputRayPickable = false;
	//	CamAnchor.LinearDamp = 100;
	//	CamAnchor.AngularDamp = 0;
	//	CamAnchor.Translate(new Vector3(0, 4, 4));
	//	CamAnchor.Rotate(Vector3.Right, pi / 2);
	//	CamAnchor.Visible = false;
	//	AddChild(CamAnchor);
	//
	//	CamJoint.Nodes__nodeA = CarAnchor.GetPath();
	//	CamJoint.Nodes__nodeB = CamAnchor.GetPath();
	//	CamJoint.Params__bias = 0.99f;
	//	CamJoint.Translate(new Vector3(0, 4, 0));
	//	CamJoint.Rotate(Vector3.Right, pi / 2);
	//	CamJoint.Visible = false;
	//	AddChild(CamJoint);
	//	ulong CarAnchorId = CarAnchor.GetInstanceId();
	//
	//	CarAnchor = (StaticBody)GD.InstanceFromId(CarAnchorId);
	//
	//
	//	ulong CarCamId = CarCam.GetInstanceId();
	//	CarCam.SetScript(ResourceLoader.Load("res://vehicles/player/CarCam.cs"));
	//	CarCam = (Camera)GD.InstanceFromId(CarCamId);
	//	AddChild(CarCam);
	//	CarCam.SetPhysicsProcess(true);
	//	CarCam.SetProcess(true);
  }

  /*   float lerp(float firstFloat, float secondFloat, float by)
	{ return firstFloat * (1F - by) + secondFloat * by; } */
  /*   Vector3 Lerp(Vector3 firstVector, Vector3 secondVector, float by)
	{
	  float retX = lerp(firstVector.x, secondVector.x, by);
	  float retY = lerp(firstVector.y, secondVector.y, by);
	  float retZ = lerp(firstVector.z, secondVector.z, by);
	  return new Vector3(retX, retY, retZ);
	} */
  public override void _PhysicsProcess(float delta)
  {
	Normal = GlobalTransform.origin.Normalized();
	OriginLength = GlobalTransform.origin.Length();
	Altitude = OriginLength - vars.PlanetRadius;
	//wind.z = -normal.x;
	//wind.x = normal.z;
	//wind.Normalized();
	//wind *= Mathf.Sin(normal.AngleTo(Vector3.Up)) * vars.planet_radius * 0.01f;

	if (vars.lHori == 0f & vars.lVert == 0f)
	{
	  //level_dir = normal.Cross(Transform.basis.y).Normalized();

	  //forward_level = level_dir.Dot(LinearVelocity.Normalized());
	  //level_roll = forward_level * level * GlobalTransform.basis.z;
	  //level_pitch = level_dir.Dot(Transform.basis.x.Normalized()) * level * GlobalTransform.basis.x * (1f - Mathf.Abs(forward_level));
	}
	else
	{
	  //level = Vector3.Zero;
	  level_pitch = Vector3.Zero;
	  level_roll = Vector3.Zero;
	}
	vars.AngDamp = AngularDamp;

	//bank = GlobalTransform.basis.x.Dot(normal);
	tar_roll = Normal.Rotated(Transform.basis.z, vars.lHori * pi / 3f);
	torque_roll = GlobalTransform.basis.y.Cross(tar_roll) / 2f;// + level_roll;
	torque_pitch = GlobalTransform.basis.x * (vars.lVert - Mathf.Abs(Mathf.Sin(vars.lHori * pi / 2f)) / 2f);// + level_pitch;
																											//tar_yaw = normal.Rotated(Transform.basis.y, -vars.lHori * pi);
	torque_yaw = -Mathf.Sin(vars.lHori * pi / 2f) * GlobalTransform.basis.y;

	torque_roll = GlobalTransform.basis.z * vars.lHori;// + level_roll;



	//AddTorque((vars.lVert * GlobalTransform.basis.x.Normalized() + vars.lHori * GlobalTransform.basis.z.Normalized()) * 500 + AngDamp);
	localAngVel = GlobalTransform.basis.XformInv(AngularVelocity);
	localLinVel = GlobalTransform.basis.XformInv(LinearVelocity);


	vars.LocLinVel = localLinVel;
	vars.LinVel = LinearVelocity;


	yawAng = GlobalTransform.basis.z.SignedAngleTo(LinearVelocity, -GlobalTransform.basis.y);

	if (flying)
	{

	  AddTorque((torque_yaw + torque_roll + torque_pitch) * 100000f * delta * AngularDamp);

	  if (Input.IsActionPressed("thrust"))
	  {
		Vector3 newThrust = Mathf.Atan(Input.GetActionStrength("thrust")) * thrustBase * delta / OriginLength;
		thrustTween.InterpolateProperty(this, "thrust", thrust, newThrust, 10 * delta);

		// AddForce(Transform.basis.Xform(thrust), GlobalTransform.basis.Xform(tailPos));
		AddForce(Transform.basis.Xform(thrust), GlobalTransform.basis.Xform(tailPos));
		thrustTween.Start();
	  }

	  Clift = Mathf.Sin(2f * AoA);
	  Clift = delta * Clift * liftConst * LinearVelocity.Project(GlobalTransform.basis.z).LengthSquared();
	  lift = Lift(Clift);
	  AddCentralForce(lift);
	  vars.liftAng = lift.AngleTo(LinearVelocity);
	  vars.Lift = lift;
	  vars.Clift = Clift;

	  rayLift.CastTo = 10f * GlobalTransform.basis.XformInv(lift);
	}
	else
	{
	  if (Input.IsActionPressed("thrust")) { EngineForce = Input.GetActionStrength("thrust") * MAX_ENGINE_FORCE; }
	  else { EngineForce = 0f; }
	  if (Input.IsActionPressed("brake")) { Brake = Input.GetActionStrength("brake") * MAX_BRAKE; }
	  else { Brake = 0f; }
	  if (Input.IsActionPressed("turn_left") | Input.IsActionPressed("turn_right"))
	  { lInput = Input.GetAxis("turn_left", "turn_right"); }
	  else { lInput = 0f; }
	  steer.InterpolateProperty(this, "steer_val", steer_val, lInput * MAX_STEERING, 0.1f,
	Tween.TransitionType.Quad, Tween.EaseType.InOut);
	  Steering = steer_val;
	  steer.Start();
	  vars.debugFloat = steer_val;
	}

	rayTorque.CastTo = levelTorque / 10f;
	rayThrust.CastTo = thrust / 10f;
	rayVel.CastTo = localLinVel * 10000f;

	angVelLen = AngularVelocity.Length();

	linVelLen = LinearVelocity.Length();
	linVelLenSq = LinearVelocity.LengthSquared();
	LinearDamp = linVelLenSq / OriginLength;
	//LinearDamp = (LinearVelocity - wind).LengthSquared() / OriginLength;
	angVelLenSq = AngularVelocity.LengthSquared();
	AngularDamp = angVelLenSq / OriginLength + LinearDamp;
	angDampSq = Mathf.Pow(2f, AngularDamp);

  }
  Vector3 Lift(float liftCoefficient)
  {
	longPlane.Normal = GlobalTransform.basis.x;

	windFrame.basis.x = LinearVelocity;
	windFrame.basis.z = longPlane.Project(LinearVelocity).Rotated(GlobalTransform.basis.x, -pi / 2f);
	windFrame.basis.y = windFrame.basis.z.Cross(windFrame.basis.x);
	windFrame.Orthonormalized();
	AoA = pi / 2f - windFrame.basis.z.AngleTo(GlobalTransform.basis.z);

	return liftCoefficient * windFrame.basis.z.Normalized();
  }

  public override void _Input(InputEvent @event)
  {
	if (Input.IsActionJustPressed("wings"))
	{
	  flying = !flying;
	  if (flying)
	  {
		steer.Stop(this);
		Steering = 0f;
		steer_val = 0f;
		EngineForce = 0f;
		Brake = 0f;
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
