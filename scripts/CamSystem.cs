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
  float lerpVal = 0.05f;


  Vector3 rotPos;
  float input;
  float prevInput;
  float angularSpeed = 2f * Mathf.Pi;
  float angDiff;
  float tanLen;
  float debugFloat;
  float horizonAng;
  Vector3 horizon;

  Transform rotTransform;
  float amount = 0.5f;
  Vector3 offset;

  OpenSimplexNoise noise = new OpenSimplexNoise();
  int noise_y = 0;
  RigidBody car;
  Camera cam;
  Tween camTween;
  [Export]
  float camAngle = 0f;
  float stickX;
  float stickY;
  float camAngleRad;
  float camMove;
  float prevCamAngle;


  public override void _Ready()
  {
    vars = (Vars)GetNode("/root/Vars");
    car = (RigidBody)GetNode("../../Car");
    //carAnchor = (StaticBody)GetNode("../CarAnchor");

    camAnchor = (RigidBody)GetNode("../CamAnchor");
    cam = (Camera)GetNode("../CamAnchor/Camera");

    rotTransform = cam.GlobalTransform;

    systemTransform = GlobalTransform;
    camTween = new Tween();
    AddChild(camTween);
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

    systemTransform.origin = vars.car_pos * 1.05f;

    systemTransform.basis = GlobalTransform.basis;

    GlobalTransform = systemTransform;
    //camTween.FollowProperty(this, "GlobalTransform:origin", GlobalTransform.origin, car, "GlobalTransform:origin", 0.2f, Tween.TransitionType.Quad, Tween.EaseType.Out);

    LookAt(Vector3.Zero, GlobalTransform.basis.x);


    horizonAng = Mathf.Asin(vars.planet_radius / GlobalTransform.origin.Length());
    horizon = camAnchor.GlobalTransform.origin.DirectionTo(GlobalTransform.origin);

    horizon = horizon.Rotated(GlobalTransform.origin.Normalized(), Mathf.Pi / 2f);

    horizon = GlobalTransform.origin.Normalized().Rotated(horizon, horizonAng);
    horizon = 1.4f * vars.planet_radius * horizon;

    if (Input.IsActionPressed("CamReverse"))
    {
      //lerpVal = 1f;
      horizon = horizon.Rotated(GlobalTransform.origin.Normalized(), Mathf.Pi);
      //lerpVal = 0.1f;
      //prevAng = 0f;

    }
    else
    {
      if (Input.IsActionPressed("CamUp") | Input.IsActionPressed("CamDown") | Input.IsActionPressed("CamLeft") | Input.IsActionPressed("CamRight"))
      {
        input = Mathf.Rad2Deg(Input.GetVector("CamDown", "CamUp", "CamRight", "CamLeft").Angle());
        if (Mathf.Abs(camMove) > 3f | Mathf.Abs(prevInput - input) > 3f)
        {
          stickAng = Mathf.Round(input / 3f) * 3f;
          angDiff = Mathf.Wrap(camAngle - stickAng, -180, 180);
          camMove = 0f;

          camTween.InterpolateProperty(this, "camAngle", camAngle, camAngle - angDiff, lerpVal,
           Tween.TransitionType.Quad, Tween.EaseType.InOut);
          prevAng = stickAng;
          prevInput = input;
          //angDiff -= camAngle - prevCamAngle;
        }
        else
        {
          angDiff = Mathf.Wrap(camAngle - stickAng, -180, 180);

          camTween.InterpolateProperty(this, "camAngle", camAngle, camAngle - angDiff, lerpVal,
          Tween.TransitionType.Quad, Tween.EaseType.InOut);
          camMove += input - prevInput;
          //camTween.InterpolateProperty(this, "camAngle", camAngle, camAngle, 0.1f, Tween.TransitionType.Quad, Tween.EaseType.Out);

        }
        GD.Print(camAngle);

        //angDiff = Mathf.Wrap(camAngle - prevAng, -Mathf.Pi, Mathf.Pi);

        //stickAng = prevAng + angDiff * lerpVal;
        //camAngle = prevAng + angDiff * lerpVal;
        camAngleRad = Mathf.Deg2Rad(camAngle);
        // horizon = horizon.Rotated(vars.car_norm, stickAng);
        horizon = horizon.Rotated(GlobalTransform.origin.Normalized(), camAngleRad);
        // prevAng = stickAng;
        prevCamAngle = camAngle;

        vars.debugFloat = angDiff;

      }
      else
      {
        camAngle = 0f;
        prevAng = 0f;
        input = 0f;

      }
    }

    rotTransform.origin = GlobalTransform.origin + horizon.Rotated(GlobalTransform.origin.Normalized(), Mathf.Pi) / 10f;


    cam.GlobalTransform = rotTransform;

    cam.LookAt(horizon, GlobalTransform.origin);

    vars.cam_pos = cam.GlobalTransform.origin;
    vars.cam_alt = cam.GlobalTransform.origin.Length() - vars.planet_radius;


    noise_y += 1;
    offset.x = amount * noise.GetNoise2d(noise.Seed, noise_y);
    offset.y = amount * noise.GetNoise2d(noise.Seed * 2, noise_y);
    offset.z = amount * noise.GetNoise2d(noise.Seed * 3, noise_y);

    Rotation = Rotation + offset;
    camTween.Start();

  }

}
