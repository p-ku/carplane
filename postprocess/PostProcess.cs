using Godot;
using System;
// https://www.reddit.com/r/godot/comments/tzx19b/wip_godot_3x_perobject_motion_blur_solution/

public class PostProcess : Node
{
  CarCam cam;
  Camera velCam, leadCam, colorCam, debugCam;
  ColorRect staticBlur;
  internal ShaderMaterial velMat;
  ShaderMaterial atmoMat, atmoRectMat;
  bool check = false;
  Vector3 camBlurAngle;
  Globals vars;
  [Export(PropertyHint.Range, "0,1")]
  float shutterAngle = 0.5f;
  uint blurSteps = 16;
  // uint haltonMod, haltonMax = 2147483647; // Top range of 32-bit integer (minus one).
  uint pixelCount;// = (uint)vars.renderRes.x * (uint)vars.renderRes.y;
  uint shortDim;
  uint longDim;
  Vector2 dimCheck;
  public override void _Ready()
  {

	vars = (Globals)GetTree().Root.FindNode("Globals", true, false);
	cam = (CarCam)GetTree().Root.FindNode("CarCam", true, false);
	velCam = (Camera)GetTree().Root.FindNode("VelocityCam", true, false);
	leadCam = (CarCam)GetTree().Root.FindNode("CarCam", true, false);

	colorCam = (Camera)GetTree().Root.FindNode("ColorCam", true, false);
	debugCam = (Camera)GetTree().Root.FindNode("DebugCam", true, false);

	pixelCount = (uint)vars.renderRes.x * (uint)vars.renderRes.y;
	// haltonMod = haltonMax - pixelCount;
	// haltonMod = (uint)vars.renderRes.x + (uint)vars.renderRes.y;

	if (vars.renderRes.x >= vars.renderRes.y)
	{
	  longDim = (uint)vars.renderRes.x;
	  shortDim = (uint)vars.renderRes.y;
	  dimCheck = new Vector2(1, 0);
	}
	else
	{
	  longDim = (uint)vars.renderRes.y;
	  shortDim = (uint)vars.renderRes.x;
	  dimCheck = new Vector2(0, 1);
	}

	// haltonMod = pixelCount * 100;
	MeshInstance velMesh = (MeshInstance)GetTree().Root.FindNode("VelocityMesh", true, false);
	staticBlur = (ColorRect)GetTree().Root.FindNode("StaticBlur", true, false);

	MeshInstance atmoMesh = (MeshInstance)GetTree().Root.FindNode("AtmoMesh", true, false);
	// ColorRect atmoRect = (ColorRect)GetTree().Root.FindNode("AtmoRect", true, false);
	atmoMat = atmoMesh.GetActiveMaterial(0) as ShaderMaterial;

	// atmoMat = atmoRect.Material as ShaderMaterial;

	ColorRect tiledVel = (ColorRect)GetTree().Root.FindNode("TiledVelocity", true, false);

	ColorRect neighborVel = (ColorRect)GetTree().Root.FindNode("NeighborVelocity", true, false);

	Viewport velView = (Viewport)GetTree().Root.FindNode("VelocityBuffer", true, false);
	Viewport tiledView = (Viewport)GetTree().Root.FindNode("TiledBuffer", true, false);
	Viewport neighborView = (Viewport)GetTree().Root.FindNode("NeighborBuffer", true, false);
	Viewport colView = (Viewport)GetTree().Root.FindNode("ColorBuffer", true, false);
	Viewport carView = (Viewport)GetTree().Root.FindNode("CarBuffer", true, false);

	colView.Size = vars.renderRes;
	int blurTileSize = 40;
	velView.Size = vars.renderRes;
	tiledView.Size = vars.renderRes / blurTileSize;
	neighborView.Size = tiledView.Size;

	ViewportTexture carBuff = carView.GetTexture();
	ViewportTexture colBuff = colView.GetTexture();
	ViewportTexture velBuff = velView.GetTexture();
	ViewportTexture tiledBuff = tiledView.GetTexture();
	ViewportTexture variBuff = neighborView.GetTexture();
	ViewportTexture neighborBuff = neighborView.GetTexture();


	Vector2 halfResoSq = vars.renderRes * vars.renderRes * 0.25f;

	float uvDepth = Mathf.Sqrt(halfResoSq.y / Mathf.Pow(Mathf.Sin(vars.FovHalfRad.y), 2f) - halfResoSq.y);
	Vector2 halfUvDepthVec;
	halfUvDepthVec.x = Mathf.Tan(vars.FovHalfRad.x);
	halfUvDepthVec.y = Mathf.Tan(vars.FovHalfRad.y);
	Vector2 uvDepthVec = 2 * halfUvDepthVec;
	uvDepthVec = 0.5f * vars.renderRes / halfUvDepthVec;

	// Initiate velocity pass.
	velMat = velMesh.GetActiveMaterial(0) as ShaderMaterial;
	velMat.SetShaderParam("max_blur_angle", vars.MaxBlurAngleRad);
	velMat.SetShaderParam("fov", vars.FovRad);
	velMat.SetShaderParam("uv_depth_vec", uvDepthVec);
	velMat.SetShaderParam("reso", vars.renderRes);

	velCam.Visible = true;
	atmoMesh.Visible = true;

	// Initiate blur tile pass.
	// Vector2 tileDimensions = new Vector2(64, 36);
	Vector2 tileDimensions = vars.renderRes / blurTileSize;
	(tiledVel.Material as ShaderMaterial).SetShaderParam("dims", tileDimensions);
	(tiledVel.Material as ShaderMaterial).SetShaderParam("velocity_buffer", velBuff);
	(tiledVel.Material as ShaderMaterial).SetShaderParam("reso", vars.renderRes);
	(tiledVel.Material as ShaderMaterial).SetShaderParam("tile_size", blurTileSize);

	// Initiate blur neighbor pass.
	// int numTiles = (int)tileDimensions.x * (int)tileDimensions.y;
	(neighborVel.Material as ShaderMaterial).SetShaderParam("velocity_buffer", velBuff);
	(neighborVel.Material as ShaderMaterial).SetShaderParam("tiled_velocity", tiledBuff);
	(neighborVel.Material as ShaderMaterial).SetShaderParam("dims", tileDimensions);
	(neighborVel.Material as ShaderMaterial).SetShaderParam("shutter_angle", shutterAngle);
	(neighborVel.Material as ShaderMaterial).SetShaderParam("fov", vars.FovRad);
	(neighborVel.Material as ShaderMaterial).SetShaderParam("uv_depth", uvDepth);
	(neighborVel.Material as ShaderMaterial).SetShaderParam("reso", vars.renderRes);
	(neighborVel.Material as ShaderMaterial).SetShaderParam("uv_depth_vec", uvDepthVec);

	// Initiate atmosphere.
	atmoMat.SetShaderParam("velocity_buffer", velBuff);
	atmoMat.SetShaderParam("color_buffer", colBuff);
	atmoMat.SetShaderParam("plan_rad", vars.PlanetRadius);
	atmoMat.SetShaderParam("atmo_height", vars.atmoHeight);
	atmoMat.SetShaderParam("atmo_rad", vars.AtmoRadius);
	atmoMat.SetShaderParam("atmo_rad_sq", vars.AtmoRadius * vars.AtmoRadius);
	float tanDist = vars.PlanetRadius * Mathf.Tan(Mathf.Acos(vars.PlanetRadius / vars.AtmoRadius));
	atmoMat.SetShaderParam("tangent_dist", tanDist);
	float cloudTanDist = vars.CloudRadius * Mathf.Tan(Mathf.Acos(vars.CloudRadius / vars.AtmoRadius));
	atmoMat.SetShaderParam("cloud_tangent_dist", vars.AtmoRadius * vars.AtmoRadius);

	// Create final image.
	(staticBlur.Material as ShaderMaterial).SetShaderParam("velocity_buffer", velBuff);
	(staticBlur.Material as ShaderMaterial).SetShaderParam("neighbor_buffer", neighborBuff);
	(staticBlur.Material as ShaderMaterial).SetShaderParam("tiled_buffer", tiledBuff);
	(staticBlur.Material as ShaderMaterial).SetShaderParam("color_buffer", colBuff);
	(staticBlur.Material as ShaderMaterial).SetShaderParam("vari_buffer", variBuff);

	(staticBlur.Material as ShaderMaterial).SetShaderParam("fov", vars.FovRad);
	(staticBlur.Material as ShaderMaterial).SetShaderParam("half_fov", vars.FovHalfRad);
	(staticBlur.Material as ShaderMaterial).SetShaderParam("uv_depth", uvDepth);
	(staticBlur.Material as ShaderMaterial).SetShaderParam("uv_depth_vec", uvDepthVec);
	(staticBlur.Material as ShaderMaterial).SetShaderParam("half_uv_depth_vec", halfUvDepthVec);
	(staticBlur.Material as ShaderMaterial).SetShaderParam("reso", vars.renderRes);
	(staticBlur.Material as ShaderMaterial).SetShaderParam("buffer_correction", shutterAngle * 0.25f);
	(staticBlur.Material as ShaderMaterial).SetShaderParam("shutter_angle", shutterAngle);
	(staticBlur.Material as ShaderMaterial).SetShaderParam("tile_size", blurTileSize);
	(staticBlur.Material as ShaderMaterial).SetShaderParam("long_dim", (int)longDim);
	(staticBlur.Material as ShaderMaterial).SetShaderParam("short_dim", (int)shortDim);
	(staticBlur.Material as ShaderMaterial).SetShaderParam("dim_check", dimCheck);

	int haltonShift = 28; // 12,20,28,36
	int haltonMod = (int)longDim / haltonShift;
	(staticBlur.Material as ShaderMaterial).SetShaderParam("halton_shift", haltonShift);
	(staticBlur.Material as ShaderMaterial).SetShaderParam("halton_mod", haltonMod);
	(staticBlur.Material as ShaderMaterial).SetShaderParam("pixel_count", (float)pixelCount);

	// (staticBlur.Material as ShaderMaterial).SetShaderParam("steps", (float)blurSteps);



	// img.Create((int)vars.displayRes.x, (int)vars.displayRes.y, false, Image.Format.Rgbah);
	// ImageTexture windowTex = new ImageTexture();
	// windowTex.CreateFromImage(img, 0);
	// staticBlur.Texture = windowTex;

	//  Transform projmat = new Transform(
	//    new Vector3(S, 0, 0),
	//    new Vector3(0, S, 0),
	//    new Vector3(0, 0, -velCam.Far / (velCam.Far - velCam.Near)),
	//    new Vector3(0, 0, -velCam.Far * velCam.Near / (velCam.Far - velCam.Near)));




  }

  public override void _Process(float delta)
  {
	//  float bottom = -top;
	//  float right = top * vars.aspectRatio;
	//  float left = -right;

	// float S = 1 / Mathf.Tan(vars.FovHalfRad.y);

	//  float factor2 = 1 / velCam.Near;
	//
	//  float factor1 = (1 / velCam.Far) - factor2;

	//  Transform projmat = new Transform(
	//    new Vector3(2 * velCam.Near / (right * left), 0, 0),
	//    new Vector3(0, 2 * velCam.Near / (top - bottom), 0),
	//    Vector3.Zero,
	//    new Vector3(0, 0, factor1));

	//  Transform invprojmat = new Transform(
	//    new Vector3((top * vars.aspectRatio) / velCam.Near, 0, 0),
	//      Vector3.Zero,
	//      Vector3.Zero,
	//    new Vector3(0, 0, -1));
	float top = Mathf.Tan(vars.FovHalfRad.y) * velCam.Near;


	float factor1 = (top * vars.aspectRatio) / velCam.Near;
	float f_denom = 2 * velCam.Far * velCam.Near;
	float factor2 = top / velCam.Near;
	float factor3 = (velCam.Near - velCam.Far) / f_denom;
	float factor4 = (velCam.Near + velCam.Far) / f_denom;

	// velMat.SetShaderParam("inv_mat", invprojmat);
	(staticBlur.Material as ShaderMaterial).SetShaderParam("f1", factor1);
	(staticBlur.Material as ShaderMaterial).SetShaderParam("f2", factor2);
	(staticBlur.Material as ShaderMaterial).SetShaderParam("f3", factor3);
	(staticBlur.Material as ShaderMaterial).SetShaderParam("f4", factor4);

	(staticBlur.Material as ShaderMaterial).SetShaderParam("cam_xform", velCam.GlobalTransform);

	// Random integer between haltonMin and haltonMax.
	// uint haltonNum = GD.Randi() % haltonMod;
	// uint haltonNum = GD.Randi() % (3600 + longDim);
	uint haltonNum = GD.Randi() % 3600 + 21;

	(staticBlur.Material as ShaderMaterial).SetShaderParam("halton_num", (int)haltonNum);

  }

  public override void _PhysicsProcess(float delta)
  {

	debugCam.Far = debugCam.GlobalTransform.origin.Length() + vars.planet_radius * 0.5f;
	debugCam.Near = Mathf.Tan(vars.FovHalfRad.y) * (debugCam.GlobalTransform.origin.Length() - vars.planet_radius - 4);

	velCam.GlobalTransform = leadCam.GlobalTransform;
	velCam.Far = leadCam.GlobalTransform.origin.Length() + vars.planet_radius * 0.5f;
	velCam.Near = Mathf.Tan(vars.FovHalfRad.y) * (leadCam.GlobalTransform.origin.Length() - vars.planet_radius - 4);

	colorCam.GlobalTransform = leadCam.GlobalTransform;
	colorCam.Far = velCam.Far;
	colorCam.Near = velCam.Near;
	processVelocity();

	// debugCam.GlobalTransform = GlobalTransform;
  }

  void processVelocity()
  {

	if (!check)
	{
	  camBlurAngle.x = cam.PrevGlobalTransform.basis.x.AngleTo(cam.GlobalTransform.basis.x);
	  camBlurAngle.y = cam.PrevGlobalTransform.basis.y.AngleTo(cam.GlobalTransform.basis.y);
	  camBlurAngle.z = cam.PrevGlobalTransform.basis.z.AngleTo(cam.GlobalTransform.basis.z);
	}
	//  else { check = !check; }


	if (camBlurAngle.x > 1.57f | camBlurAngle.y > 1.57f | camBlurAngle.z > 1.57f)// | camBlurAngle.Length() > 1.57f)
	{
	  velMat.SetShaderParam("snap", true);
	  check = !check;
	}
	else velMat.SetShaderParam("snap", false);

	velMat.SetShaderParam("cam_prev_pos", -cam.ToLocal(cam.PrevGlobalTransform.origin));
	velMat.SetShaderParam("cam_prev_xform", cam.PrevGlobalTransform.basis.Inverse() * cam.GlobalTransform.basis);
	// velMat.SetShaderParam("cam_prev_xform", cam.PrevTransform.basis.Inverse() * cam.Transform.basis);

  }
}
