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

	if (vars.renderRes.x >= vars.renderRes.y)
	{
	  longDim = (uint)vars.renderRes.x;
	  shortDim = (uint)vars.renderRes.y;
	  dimCheck = new Vector2(1f, 0f);
	}
	else
	{
	  longDim = (uint)vars.renderRes.y;
	  shortDim = (uint)vars.renderRes.x;
	  dimCheck = new Vector2(0f, 1f);
	}

	MeshInstance velMesh = (MeshInstance)GetTree().Root.FindNode("VelocityMesh", true, false);
	staticBlur = (ColorRect)GetTree().Root.FindNode("StaticBlur", true, false);
	//staticBlur = (Sprite)GetTree().Root.FindNode("StaticBlur2", true, false);

	MeshInstance atmoMesh = (MeshInstance)GetTree().Root.FindNode("AtmoMesh", true, false);
	atmoMat = atmoMesh.GetActiveMaterial(0) as ShaderMaterial;

	ColorRect tiledVel = (ColorRect)GetTree().Root.FindNode("TiledVelocity", true, false);
	ColorRect neighborVel = (ColorRect)GetTree().Root.FindNode("NeighborVelocity", true, false);

	Viewport velView = (Viewport)GetTree().Root.FindNode("VelocityBuffer", true, false);
	Viewport tiledView = (Viewport)GetTree().Root.FindNode("TiledBuffer", true, false);
	Viewport neighborView = (Viewport)GetTree().Root.FindNode("NeighborBuffer", true, false);
	Viewport colView = (Viewport)GetTree().Root.FindNode("ColorBuffer", true, false);
	Viewport carView = (Viewport)GetTree().Root.FindNode("CarBuffer", true, false);
	ViewportContainer carContainer = (ViewportContainer)GetTree().Root.FindNode("CarContainer", true, false);

	//Image img = new Image();
	//img.Create((int)vars.renderRes.x, (int)vars.renderRes.y, false, Image.Format.Rgba8);
	//ImageTexture texture_n = new ImageTexture();
	//texture_n.CreateFromImage(img, 0);
	//staticBlur.Texture = texture_n;
	//staticBlur.Offset = Vector2.Zero;

	colView.Size = vars.renderRes;
	int blurTileSize = 40;
	velView.Size = vars.renderRes;
	tiledView.Size = velView.Size / blurTileSize;
	neighborView.Size = tiledView.Size;
	carView.Size = vars.renderRes;
	carContainer.RectSize = vars.renderRes;

	ViewportTexture carBuff = carView.GetTexture();
	ViewportTexture colBuff = colView.GetTexture();
	ViewportTexture velBuff = velView.GetTexture();
	ViewportTexture tiledBuff = tiledView.GetTexture();
	ViewportTexture variBuff = neighborView.GetTexture();
	ViewportTexture neighborBuff = neighborView.GetTexture();

	Vector2 halfResoSq = vars.renderRes * vars.renderRes * 0.25f;

	Vector2 halfUvDepthVec = new Vector2(Mathf.Tan(vars.FovHalfRad.x), Mathf.Tan(vars.FovHalfRad.y));

	Vector2 resDepthVec = 0.5f * velView.Size / halfUvDepthVec;
	Vector2 uvDepthVec = new Vector2(0.5f, 0.5f) / halfUvDepthVec;

	Vector2 tileUV = new Vector2(blurTileSize, blurTileSize) / velView.Size;

	// Initiate velocity pass.
	velMat = velMesh.GetActiveMaterial(0) as ShaderMaterial;
	velMat.SetShaderParam("res_depth_vec", resDepthVec);
	velMat.SetShaderParam("uv_depth_vec", uvDepthVec);
	velMat.SetShaderParam("tile_uv", tileUV);

	velMat.SetShaderParam("reso", velView.Size);

	velCam.Visible = true;
	atmoMesh.Visible = true;

	Vector2 invReso = Vector2.One / velView.Size;

	// Initiate blur tile pass.
	Vector2 tileDimensions = velView.Size / blurTileSize;
	(tiledVel.Material as ShaderMaterial).SetShaderParam("velocity_buffer", velBuff);
	(tiledVel.Material as ShaderMaterial).SetShaderParam("reso", velView.Size);
	(tiledVel.Material as ShaderMaterial).SetShaderParam("inv_reso", invReso);
	(tiledVel.Material as ShaderMaterial).SetShaderParam("tile_uv", tileUV);

	// Initiate blur neighbor pass.
	(neighborVel.Material as ShaderMaterial).SetShaderParam("tiled_velocity", tiledBuff);
	(neighborVel.Material as ShaderMaterial).SetShaderParam("dims", tileDimensions);

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
	(staticBlur.Material as ShaderMaterial).SetShaderParam("color_buffer", colBuff);

	(staticBlur.Material as ShaderMaterial).SetShaderParam("reso", vars.renderRes);

	(staticBlur.Material as ShaderMaterial).SetShaderParam("tile_size", blurTileSize);
	(staticBlur.Material as ShaderMaterial).SetShaderParam("long_dim", (int)longDim);
	(staticBlur.Material as ShaderMaterial).SetShaderParam("short_dim", (int)shortDim);
	(staticBlur.Material as ShaderMaterial).SetShaderParam("dim_check", dimCheck);
	(staticBlur.Material as ShaderMaterial).SetShaderParam("inv_reso", invReso);
	(staticBlur.Material as ShaderMaterial).SetShaderParam("tile_uv", tileUV);

  }

  public override void _Process(float delta)
  {
	float top = Mathf.Tan(vars.FovHalfRad.y) * velCam.Near;

	float factor1 = (top * vars.aspectRatio) / velCam.Near;
	float f_denom = 2 * velCam.Far * velCam.Near;
	float factor2 = top / velCam.Near;
	float factor3 = (velCam.Near - velCam.Far) / f_denom;
	float factor4 = (velCam.Near + velCam.Far) / f_denom;

	atmoMat.SetShaderParam("f1", factor1);
	atmoMat.SetShaderParam("f2", factor2);
	atmoMat.SetShaderParam("f3", factor3);
	atmoMat.SetShaderParam("f4", factor4);
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

	//if (!check)
	//{
	camBlurAngle.x = cam.PrevGlobalTransform.basis.x.AngleTo(cam.GlobalTransform.basis.x);
	camBlurAngle.y = cam.PrevGlobalTransform.basis.y.AngleTo(cam.GlobalTransform.basis.y);
	camBlurAngle.z = cam.PrevGlobalTransform.basis.z.AngleTo(cam.GlobalTransform.basis.z);
	//}
	//  else { check = !check; }


	if (camBlurAngle.x > 1.57f | camBlurAngle.y > 1.57f | camBlurAngle.z > 1.57f)// | camBlurAngle.Length() > 1.57f)
	{
	  velMat.SetShaderParam("snap", true);
	  //  check = !check;
	}
	else velMat.SetShaderParam("snap", false);

	velMat.SetShaderParam("cam_prev_pos", -cam.ToLocal(cam.PrevGlobalTransform.origin));
	velMat.SetShaderParam("cam_prev_xform", cam.PrevGlobalTransform.basis.Inverse() * cam.GlobalTransform.basis);
  }
}
