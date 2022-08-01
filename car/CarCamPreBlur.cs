// using Godot;
// using System;
// // using System.Collections.Generic;
// public class CarCam : Camera
// {
//   internal float OriginLength;
//   float overCarLength, lerpVal, horizonAng = 0f;
//   Globals vars;
//   float camAngle = 0f, MaxCamTurn;
//   internal Transform PrevGlobalTransform = Transform.Identity;
//   internal Transform PrevTransform;
//   internal float Altitude;
//   Vector3 camBlurAngle;
// 
//   ShaderMaterial velMat;
//   PostProcess postProc;
//   OpenSimplexNoise noise = new OpenSimplexNoise();
//   int noise_y = 0, curDir = 0;
//   Car car;
//   Tween camTween, stickTween;
//   Vector3 desiredCamPos, Normal = Vector3.Forward, originLerp;
//   internal Plane camPlane;
//   Vector3 blurAngle;
//   Camera colorCam, velCam, debugCam;
//   Vector3 overCar = Vector3.Forward;
//   Transform targetTransform;
//   Quat slerpQuat = Quat.Identity;
//   float camThreshold = 0.035f, camXangle = 0f, camYangle = 0f, camZangle = 0f;
//   const float ninetyRad = Mathf.Pi / 2f;
//   public override void _Ready()
//   {
//     vars = (Globals)GetTree().Root.FindNode("Globals", true, false);
//     camTween = (Tween)GetTree().Root.FindNode("CamTween", true, false);
//     stickTween = (Tween)GetTree().Root.FindNode("StickTween", true, false);
//     car = (Car)GetTree().Root.FindNode("Car", true, false);
//     colorCam = (Camera)GetTree().Root.FindNode("ColorCam", true, false);
//     velCam = (Camera)GetTree().Root.FindNode("VelocityCam", true, false);
//     debugCam = (Camera)GetTree().Root.FindNode("DebugCam", true, false);
//     postProc = (PostProcess)GetTree().Root.FindNode("PostProcess", true, false);
//     MeshInstance velMesh = (MeshInstance)GetTree().Root.FindNode("VelocityMesh", true, false);
//     MaxCamTurn = ninetyRad;
//     lerpVal = MaxCamTurn / 400f;
// 
//     noise.Seed = (int)GD.Randi();
//     noise.Period = 4f;
//     noise.Octaves = 2;
// 
//     Fov = vars.FovDeg.y;
//     colorCam.Fov = Fov;
//     desiredCamPos = GlobalTransform.origin;
//     originLerp = GlobalTransform.origin;
//     overCar = car.GlobalTransform.origin;
//     targetTransform = GlobalTransform;
// 
//     velMat = velMesh.GetActiveMaterial(0) as ShaderMaterial;
// 
//   }
//   public override void _PhysicsProcess(float delta)
//   {
//     PrevGlobalTransform = GlobalTransform;
//     PrevTransform = Transform;
// 
//     // Transform prevTargetTransform = targetTransform;
//     Vector3 prevOverCar = overCar;
//     overCarLength = car.OriginLength + 4f;
//     overCar = car.Normal * overCarLength;
// 
//     Vector3 posDelta = overCar - prevOverCar;
// 
//     camPlane = new Plane(car.Normal, overCarLength);
//     //desiredCamPos = camPlane.Project(desiredCamPos - posDelta / (overCarLength * overCarLength));
//     float upsideDown = car.Normal.Dot(car.GlobalTransform.basis.y) * 0.5f + 0.5f;
//     // desiredCamPos = camPlane.Project(desiredCamPos - posDelta * upsideDown / (overCarLength * overCarLength));
//     desiredCamPos = camPlane.Project(desiredCamPos);
//     Vector3 toAnchor = (desiredCamPos - overCar).Normalized();
//     desiredCamPos = overCar + toAnchor * 5f;
// 
//     horizonAng = Mathf.Acos(vars.PlanetRadius / overCarLength);
//     Normal = overCar.Normalized();
// 
//     Vector3 lookTarget = toAnchor.Rotated(Normal, -ninetyRad);
//     lookTarget = Normal.Rotated(lookTarget, horizonAng);
//     lookTarget = vars.PlanetRadius * lookTarget;
// 
//     targetTransform = new Transform(targetTransform.basis, desiredCamPos);
//     targetTransform = targetTransform.LookingAt(lookTarget, desiredCamPos);
// 
// 
//     // Quat a = slerpQuat;
//     Quat b = targetTransform.basis.Quat();
//     slerpQuat = slerpQuat.Slerp(b, 0.25f).Normalized(); // find halfway point between a and b
//     camTween.InterpolateProperty(this, "originLerp",
//     originLerp, targetTransform.origin, 4f * delta, Tween.TransitionType.Quad, Tween.EaseType.Out);
// 
//     camTween.Start();
// 
//     Vector3 rotNormal = car.Normal.Rotated(targetTransform.origin.Cross(originLerp).Normalized(), originLerp.AngleTo(targetTransform.origin));
// 
// 
// 
// 
// 
//     if (Input.IsActionPressed("CamReverse"))
//     {
//       camAngle = Mathf.Pi;
//     }
//     else if (Input.IsActionPressed("CamUp") | Input.IsActionPressed("CamDown") |
//     Input.IsActionPressed("CamLeft") | Input.IsActionPressed("CamRight"))
//     {
//       float inputAngle = Input.GetVector("CamDown", "CamUp", "CamRight", "CamLeft").Angle();
//       float angDiff = Mathf.Wrap(camAngle - inputAngle, -Mathf.Pi, Mathf.Pi);
// 
//       int inputDir = Mathf.Sign(angDiff);
// 
//       if (Mathf.Abs(angDiff) > camThreshold)
//       {
//         if (Mathf.Abs(angDiff) > MaxCamTurn)
//         {
//           camAngle = inputAngle;
//           stickTween.Stop(this);
//         }
//         else
//           curDir = inputDir;
//       }
//       if (curDir == inputDir & !Input.IsActionJustReleased("CamReverse"))
//       {
//         stickTween.InterpolateProperty(this, "camAngle", camAngle, camAngle - angDiff, 0.1f, Tween.TransitionType.Quad, Tween.EaseType.Out);
//         stickTween.Start();
//       }
//     }
//     else
//     {
//       if (Mathf.Abs(Mathf.Wrap(camAngle, -Mathf.Pi, Mathf.Pi)) > MaxCamTurn)
//       {
//         camAngle = 0;
//         stickTween.Stop(this);
//       }
//       else
//       {
//         curDir = 0;
//         stickTween.InterpolateProperty(this, "camAngle", camAngle, 0f, 0.1f, Tween.TransitionType.Quad, Tween.EaseType.Out);
//         stickTween.Start();
//       }
//     }
//     if (blurAngle.x > 1.57f | blurAngle.y > 1.57f | blurAngle.z > 1.57f)// | camBlurAngle.Length() > 1.57f)
//     {
//       velMat.SetShaderParam("snap", true);
//       //  check = !check;
//     }
//     else velMat.SetShaderParam("snap", false);
// 
//     velMat.SetShaderParam("cam_prev_pos", -ToLocal(PrevGlobalTransform.origin));
//     velMat.SetShaderParam("cam_prev_xform", PrevGlobalTransform.basis.Inverse() * GlobalTransform.basis);
// 
//     // Useful for troubleshooting motion blur.
//     GlobalTransform = new Transform(targetTransform.basis.Rotated(car.Normal, camAngle), targetTransform.origin.Rotated(car.Normal, camAngle));
//     //  if ((float)Time.GetTicksMsec() % 1000 < 500)
//     //    GlobalTransform = GlobalTransform.Rotated(car.Normal, (float)Time.GetTicksMsec() / 110);
//     //  else
//     //    GlobalTransform = GlobalTransform.Rotated(car.Normal, (float)Time.GetTicksMsec() / -110);
// 
//     // GlobalTransform = GlobalTransform.Rotated(car.Normal, Mathf.Sin((float)Time.GetTicksMsec() / -50));
//     Altitude = GlobalTransform.origin.Length() - vars.PlanetRadius;
//     //   postProc.processVelocity();
//     blurAngle.x = PrevGlobalTransform.basis.x.AngleTo(GlobalTransform.basis.x);
//     blurAngle.y = PrevGlobalTransform.basis.y.AngleTo(GlobalTransform.basis.y);
//     blurAngle.z = PrevGlobalTransform.basis.z.AngleTo(GlobalTransform.basis.z);
//     //}
//     //  else { check = !check; }
// 
//     followers();
// 
//     //RotationDegrees = RotationDegrees * 10;
//     //GlobalTransform = new Transform(targetTransform.basis, targetTransform.origin).Translated(Vector3.Right * camAngle);
// 
//     // Intended for use.
//     //GlobalTransform = new Transform(new Basis(slerpQuat).Rotated(rotNormal, camAngle), originLerp.Rotated(rotNormal, camAngle));
// 
// 
//     //   if (check == false)
//     //   {
//     //     camXangle = PrevGlobalTransform.basis.x.AngleTo(GlobalTransform.basis.x);
//     //     camYangle = PrevGlobalTransform.basis.y.AngleTo(GlobalTransform.basis.y);
//     //     camZangle = PrevGlobalTransform.basis.z.AngleTo(GlobalTransform.basis.z);
//     //   }
//     //   //  else { check = !check; }
//     //   // GD.Print(camXangle);
//     //   // GD.Print(camYangle);
//     //   // GD.Print(camZangle);
//     //
//     //   if (camXangle > ninetyRad | camYangle > ninetyRad | camZangle > ninetyRad)
//     //   {
//     //     velm.mat.SetShaderParam("snap", true);
//     //     check = !check;
//     //   }
//     //   else velm.mat.SetShaderParam("snap", false);
//   }
//   public override void _Process(float delta)
//   {
//     //   postProc.processVelocity();
//   }
//   void cameraShake(float rotFactor, float transFactor)
//   {
//     noise_y += 1;
//     float rotationOffsetX = rotFactor * noise.GetNoise2d(noise.Seed, noise_y);
//     float rotationOffsetY = rotFactor * noise.GetNoise2d(noise.Seed * 2, noise_y);
//     float rotationOffsetZ = rotFactor * noise.GetNoise2d(noise.Seed * 3, noise_y);
//     float translationOffsetX = transFactor * noise.GetNoise2d(noise.Seed * 4, noise_y);
//     float translationOffsetY = transFactor * noise.GetNoise2d(noise.Seed * 5, noise_y);
//     float translationOffsetZ = transFactor * noise.GetNoise2d(noise.Seed * 6, noise_y);
//     Rotation = Rotation + new Vector3(rotationOffsetX, rotationOffsetY, rotationOffsetZ);
//     Translation = Translation + new Vector3(translationOffsetX, translationOffsetY, translationOffsetZ);
//   }
// 
//   void followers()
//   {
//     velCam.GlobalTransform = GlobalTransform;
//     velCam.Far = GlobalTransform.origin.Length() + vars.PlanetRadius * 0.5f;
//     velCam.Near = Mathf.Tan(vars.FovHalfRad.y) * (GlobalTransform.origin.Length() - vars.PlanetRadius - 4f);
// 
//     colorCam.GlobalTransform = GlobalTransform;
//     colorCam.Far = velCam.Far;
//     colorCam.Near = velCam.Near;
// 
//     //  debugCam.Near = 2;
//     colorCam.Near = 2f;
//     velCam.Near = 2f;
// 
//     camBlurAngle.x = PrevGlobalTransform.basis.x.AngleTo(GlobalTransform.basis.x);
//     camBlurAngle.y = PrevGlobalTransform.basis.y.AngleTo(GlobalTransform.basis.y);
//     camBlurAngle.z = PrevGlobalTransform.basis.z.AngleTo(GlobalTransform.basis.z);
//     //}
//     //  else { check = !check; }
// 
// 
//     if (camBlurAngle.x > 1.57f | camBlurAngle.y > 1.57f | camBlurAngle.z > 1.57f)// | camBlurAngle.Length() > 1.57f)
//     {
//       velMat.SetShaderParam("snap", true);
//       //  check = !check;
//     }
//     else velMat.SetShaderParam("snap", false);
// 
//     velMat.SetShaderParam("cam_prev_pos", -ToLocal(PrevGlobalTransform.origin));
//     velMat.SetShaderParam("cam_prev_xform", PrevGlobalTransform.basis.Inverse() * GlobalTransform.basis);
//   }
// 
//   float movingAverage(System.Collections.Generic.Queue<float> buffer, float current, float sum)
//   {
//     buffer.Enqueue(current);
//     sum += current;
//     float ma = sum / buffer.Count;
//     sum -= buffer.Dequeue();
//     return ma;
//   }
// }
// 