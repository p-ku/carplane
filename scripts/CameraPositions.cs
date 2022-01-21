using Godot;
using System;

//private int _numCameras;

public class CameraPositions : Spatial
{
  [Signal]
  delegate void ChangeCamera();
  // Declare member variables here. Examples:
  // private int a = 2;
  // private string b = "text";
  int CurrentCamera;
  int NumCameras;
  Transform up;
  float sines;
  float cosines;
  float myAngle;
  public Position3D Pos1;
  Quat a;
  Quat b;
  Quat c;
  public Vars vars;
  // Called when the node enters the scene tree for the first time.
  Quat get_quaternion(Basis m)
  {

	/* Allow getting a quaternion from an unnormalized transform */
	float trace = m[0][0] + m[1][1] + m[2][2];
	float[] temp = new float[4];

	if (trace > 0.0F)
	{
	  float s = Mathf.Sqrt(trace + 1.0F);
	  temp[3] = (s * 0.5F);
	  s = 0.5F / s;

	  temp[0] = ((m[2][1] - m[1][2]) * s);
	  temp[1] = ((m[0][2] - m[2][0]) * s);
	  temp[2] = ((m[1][0] - m[0][1]) * s);
	}
	else
	{
	  int i = m[0][0] < m[1][1] ? (m[1][1] < m[2][2] ? 2 : 1) : (m[0][0] < m[2][2] ? 2 : 0);
	  int j = (i + 1) % 3;
	  int k = (i + 2) % 3;

	  float s = Mathf.Sqrt(m[i][i] - m[j][j] - m[k][k] + 1.0F);
	  temp[i] = s * 0.5F;
	  s = 0.5F / s;

	  temp[3] = (m[k][j] - m[j][k]) * s;
	  temp[j] = (m[j][i] + m[i][j]) * s;
	  temp[k] = (m[k][i] + m[i][k]) * s;
	}

	return new Quat(temp[0], temp[1], temp[2], temp[3]);
  }
  Quat get_rotation_quaternion(Basis m)
  {


	// Assumes that the matrix can be decomposed into a proper rotation and scaling matrix as M = R.S,
	// and returns the Euler angles corresponding to the rotation part, complementing get_scale().
	// See the comment in get_scale() for further information.
	m = m.Orthonormalized();
	float det = m.Determinant();
	if (det < 0)
	{
	  // Ensure that the determinant is 1, such that result is a proper rotation matrix which can be represented by Euler angles.
	  //m.Scale(Vector3(-1, -1, -1));
	  //m.Scale = new Vector3(-1, -1, -1);
	  m = m.Scaled(new Vector3(-1, -1, -1));
	}

	return get_quaternion(m);
  }
  public override void _Ready()
  {
	vars = (Vars)GetNode("/root/Vars");
	NumCameras = GetChildCount();
	Pos1 = (Position3D)GetNode("Pos1");
	EmitSignal("ChangeCamera", GetChild(CurrentCamera));

	//var DebugOverlay = GetNode();
	//DebugOverlay.stats.add_property(this, "sines", "");
	//DebugOverlay.stats.add_property(self, "cosines", "");
	//DebugOverlay.stats.add_property(self, "myAngle", "");
	//DebugOverlay.stats.add_property(self, "value", "");
	//GlobalTransform.origin = Vector3();
  }
  public override void _Input(InputEvent inputEvent)
  {
	if (inputEvent.IsActionPressed("change_camera"))
	{
	  if (CurrentCamera == NumCameras - 1)
	  {
		CurrentCamera = 0;
	  }
	  else
	  {
		CurrentCamera += 1;
	  }
	  EmitSignal("ChangeCamera", GetChild(CurrentCamera));
	}

  }
  public override void _PhysicsProcess(float delta)
  {
	//up = new Transform(vars.car_norm.Rotated(GlobalTransform.basis.z, Mathf.Pi), vars.car_norm, new Vector3(), new Vector3());
	//up = vars.car_pos.AngleTo(Transform.basis.y);


	// LookAt(vars.car_pos, Transform.basis.y);
	//LookAt(vars.car_pos, vars.car_pos);


	sines = Mathf.Cos(vars.car_basis.z.AngleTo(vars.car_pos));
	cosines = Mathf.Cos(vars.car_basis.y.AngleTo(vars.car_pos));
	//a = new Quat(Pos1.Transform.basis);
	a = new Quat(Pos1.Transform.basis).Normalized();
	//a = get_rotation_quaternion(Pos1.Transform.basis);

	/* Pos1.GlobalTransform = new Transform(Pos1.GlobalTransform[0],
	  Pos1.GlobalTransform[1],
	  Pos1.GlobalTransform[2],
	  vars.car_pos * 1.05F - 10F * (vars.car_basis.z * cosines - vars.car_basis.y * sines)); */
	//$Pos1.global_transform.origin = carPo-carBasis.z*(cosines+8)+carBasis.y*(sines+altitude/4+4)\    Pos1.LookAtFromPosition(vars.car_pos * 1.05F - 10F * (vars.car_basis.z * cosines - vars.car_basis.y * sines), vars.car_pos, -GlobalTransform.basis.z);
	//Pos1.LookAt(vars.car_pos, -GlobalTransform.basis.z);

	//Pos1.LookAtFromPosition(vars.car_pos * 1.05F - 10F * (vars.car_basis.z * cosines - vars.car_basis.y * sines), vars.car_pos, -GlobalTransform.basis.z);
	Pos1.LookAtFromPosition(vars.car_pos * 1.05F - 10F * (vars.car_basis.z * cosines - vars.car_basis.y * sines), vars.car_pos, vars.car_pos);

	//var b = Quat(Basis($Position3D.transform.origin+$Position3D.global_transform.origin.direction_to($"../../Car/CarBody".global_transform.origin)))
	//b = new Quat(Pos1.Transform.basis);
	//float m = Pos1.Transform.basis.Determinant();
	b = new Quat(Pos1.Transform.basis).Normalized();
	//b = get_rotation_quaternion(Pos1.Transform.basis);
	c = a.Slerp(b, 0.5F); //find halfway point between a and b
						  //Transform = Transform.Orthonormalized();


	Pos1.Transform = new Transform(new Basis(c), Pos1.Transform.origin);

  }
  //  // Called every frame. 'delta' is the elapsed time since the previous frame.
  //  public override void _Process(float delta)
  //  {
  //      
  //  }
}
