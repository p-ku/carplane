using Godot;
using System;

public class Contrail : Particles
{

  Car car;
  SpatialMaterial mat;
  Color color;
  Mesh meh;

  ShaderMaterial procMat;
  Vector3 totalVel;
  float magVel;

  float displacement;
  GradientTexture cRamp = new GradientTexture();
  Gradient grad = new Gradient();
  Color[] colorArr = new Color[2];
  float[] offsetArr = new float[2];
  float lati;
  DirectionalLight sun;
  Vector3 sunRot;
  //ShaderMaterial contrailMat = new ShaderMaterial();
  //ShaderMaterial particleMat = new ShaderMaterial();
  //ShaderMaterial refMat = new ShaderMaterial();

  //Shader contrailShader;
  //Shader particleShader;
  //Shader refShader;

  float partAng;
  // int count = 0;

  public override void _Ready()
  {
    //car = (Car)GetParent();
    car = (Car)GetTree().Root.FindNode("Car", true, false);

    // sun = (DirectionalLight)GetNode("../../WorldEnvironment/Sun");
    sun = (DirectionalLight)GetTree().Root.FindNode("Sun", true, false);

    //sunRot = sun.Rotation.x;
    //mat = (SpatialMaterial)DrawPass1.SurfaceGetMaterial(0);
    meh = (Mesh)DrawPass1;
    //Amount = 1000;
    Amount = 500;
    Lifetime = 2f;
    // color = new Color(1, 1, 1, 1);
    // procMat = (ParticlesMaterial)ProcessMaterial;

    // colorArr[0] = new Color(1, 1, 1, 1);
    // colorArr[1] = new Color(1, 1, 1, 0);
    // offsetArr[0] = 0;
    // offsetArr[1] = 1;
    //cRamp = Gradient;
    displacement = Mathf.Sqrt(Mathf.Pow(2f, Transform.origin.x) + Mathf.Pow(2f, Transform.origin.z));
    //contrailShader = GD.Load<Shader>("res://vehicles/player/contrail.shader");
    //particleShader = GD.Load<Shader>("res://vehicles/player/particle.shader");
    //refShader = GD.Load<Shader>("res://vehicles/player/refShader.shader");

    //particleShader = GD.Load<Shader>("res://vehicles/player/workingParticle.shader");

    //contrailMat.Shader = contrailShader;
    //particleMat.Shader = particleShader;
    //refMat.Shader = refShader;
    //contrailMat.SetShaderParam("amount", Amount);
    //particleMat.SetShaderParam("amount", Amount);
    //Lifetime = 2f;
  }



  public override void _PhysicsProcess(float delta)
  {
    sunRot = -sun.GlobalTransform.basis.z;

    totalVel = car.LinearVelocity + car.AngularVelocity.Cross(GlobalTransform.origin - car.GlobalTransform.origin);
    magVel = totalVel.Length();
    Lifetime = Amount * 0.08f / magVel;

    if (car.GlobalTransform.origin.y > 0f)
    {
      lati = car.GlobalTransform.origin.AngleTo(Vector3.Down);
    }
    else
    {
      lati = car.GlobalTransform.origin.AngleTo(Vector3.Up);
    }
        //    count += count;
        //  if (count == Amount) { count = 0; }

        //contrailMat.SetShaderParam("angDamp", car.AngularDamp);
        // (ProcessMaterial as ShaderMaterial).SetShaderParam("angDamp", car.AngularDamp);
        (ProcessMaterial as ShaderMaterial).SetShaderParam("lift", car.lift.Length());

    (ProcessMaterial as ShaderMaterial).SetShaderParam("lati", lati);
    (ProcessMaterial as ShaderMaterial).SetShaderParam("sun", sunRot);
    (DrawPass1.SurfaceGetMaterial(0) as ShaderMaterial).SetShaderParam("sun", sunRot);
    (DrawPass1.SurfaceGetMaterial(0) as ShaderMaterial).SetShaderParam("lati", lati);

    //particleMat.SetShaderParam("windInit", car.wind);
    //refMat.SetShaderParam("angDamp", car.AngularDamp);
    //refMat.SetShaderParam("windInit", car.wind);
    //procMat = particleMat;
    //meh.SurfaceSetMaterial(0, contrailMat);
    //(ProcessMaterial as ShaderMaterial) = particleMat;
  }
}
