using Godot;
using System;

public class Vars : Node
{
  public Vector3 debug;

  public bool flying;
  public Vector3 sun_pos = new Vector3(1F, 0F, 0F);
  public float planet_radius = 26F;
  public float atmo_radius = 100F;
  public Vector3 cam_pos;
  public float cam_alt;
  public float cam_dist;
  public Basis cam_basis;
  public float car_alt = 1f;
  public Vector3 car_pos;
  public Vector3 car_norm;
  public Basis car_basis;
  public Transform car_transform;
  public float roll_val;
  public float pitch_val;
  public float sun_ang;

  public Vector3 rotAng;
  public float RVert;
  public float RHori;
  public float LVert;
  public float LHori;
  public float DragMag;
  public float AngDamp;
  public float LiftMag;
  public Vector3 LinVel;
  public Vector3 LocLinVel;

  public float AoA;
  //public float orbitAng;
  public float eF;
  public float bF;
  public Vector3 Lift;
  public float Ap;
  public Vector3 Drag;
  public Basis carBasis;

  public float Clift;
  public float stickAng;
  public float liftAng;
  public Basis windFrame;

  public Vector2 stick2;
  public override void _Input(InputEvent @event)
  {
    if ((Input.IsPhysicalKeyPressed(65)) & (Input.IsPhysicalKeyPressed(68)))
    {
      LHori = 0;
    }
    else if (Input.IsPhysicalKeyPressed(65))
    {
      LHori = -1;
    }
    else if (Input.IsPhysicalKeyPressed(68))
    {
      LHori = 1;
    }
    else
    {
      LHori = Input.GetAxis("turn_left", "turn_right");
    }
    if ((Input.IsPhysicalKeyPressed(83)) & (Input.IsPhysicalKeyPressed(87)))
    {
      LVert = 0;
    }
    else if (Input.IsPhysicalKeyPressed(83))
    {
      LVert = -1;
    }
    else if (Input.IsPhysicalKeyPressed(87))
    {
      LVert = 1;
    }
    else
    {
      LVert = Input.GetAxis("pitch_up", "pitch_down");

    }
    /*     RHori = Input.GetActionStrength("CamLeft") - Input.GetActionStrength("CamRight");
       RVert = Input.GetActionStrength("CamDown") - Input.GetActionStrength("CamUp");

          RVert = Input.GetAxis("CamUp", "CamDown");
           RVert = Input.GetAxis("CamUp", "CamDown");
        */

  }
}



