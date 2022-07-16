using Godot;
using System;
// https://www.reddit.com/r/godot/comments/tzx19b/wip_godot_3x_perobject_motion_blur_solution/

public class PostProcess : Node
{
  CarCam cam;
  Camera velCam;
  internal ShaderMaterial velMat;
  ShaderMaterial atmoMat, atmoRectMat;
  bool check = false;
  Vector3 camBlurAngle;
  Globals vars;
  public override void _Ready()
  {
	vars = (Globals)GetTree().Root.FindNode("Globals", true, false);
	cam = (CarCam)GetTree().Root.FindNode("CarCam", true, false);
	velCam = (Camera)GetTree().Root.FindNode("VelocityCam", true, false);
	// Camera colCam = (Camera)GetTree().Root.FindNode("ColorCam", true, false);


	MeshInstance velMesh = (MeshInstance)GetTree().Root.FindNode("VelocityMesh", true, false);
	ColorRect staticBlur = (ColorRect)GetTree().Root.FindNode("StaticBlur", true, false);
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


	int blurTileSize = 20;
	velView.Size = vars.renderRes / 2;
	tiledView.Size = vars.renderRes / blurTileSize;
	neighborView.Size = vars.renderRes / blurTileSize;
	colView.Size = vars.renderRes / 2;

	ViewportTexture velBuff = velView.GetTexture();
	ViewportTexture neighborBuff = neighborView.GetTexture();
	ViewportTexture tiledBuff = tiledView.GetTexture();
	ViewportTexture colBuff = colView.GetTexture();
	ViewportTexture carBuff = carView.GetTexture();

	// Initiate velocity pass.
	velMat = velMesh.GetActiveMaterial(0) as ShaderMaterial;
	velMat.SetShaderParam("max_blur_angle", vars.MaxBlurAngleRad);

	velCam.Visible = true;
	atmoMesh.Visible = true;


	// Initiate blur tile pass.
	(tiledVel.Material as ShaderMaterial).SetShaderParam("velocity_buffer", velBuff);
	(tiledVel.Material as ShaderMaterial).SetShaderParam("reso", vars.renderRes);
	(tiledVel.Material as ShaderMaterial).SetShaderParam("tile_size", blurTileSize);
	Vector2 tileDimensions = new Vector2(64, 36);

	(tiledVel.Material as ShaderMaterial).SetShaderParam("dimensions", tileDimensions);


	// Initiate blur neighbor pass.
	// int numTiles = (int)tileDimensions.x * (int)tileDimensions.y;
	(neighborVel.Material as ShaderMaterial).SetShaderParam("tiled_velocity", tiledBuff);
	(neighborVel.Material as ShaderMaterial).SetShaderParam("dimensions", tileDimensions);

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
	Vector2 halfResoSq = vars.renderRes * vars.renderRes * 0.25f;

	float uvDepth = Mathf.Sqrt(halfResoSq.y / Mathf.Pow(Mathf.Sin(vars.FovHalfRad.y), 2f) - halfResoSq.y);

	(staticBlur.Material as ShaderMaterial).SetShaderParam("velocity_buffer", velBuff);
	(staticBlur.Material as ShaderMaterial).SetShaderParam("neighbor_buffer", neighborBuff);
	(staticBlur.Material as ShaderMaterial).SetShaderParam("tiled_buffer", tiledBuff);
	(staticBlur.Material as ShaderMaterial).SetShaderParam("color_buffer", colBuff);

	(staticBlur.Material as ShaderMaterial).SetShaderParam("fov", vars.FovRad);
	(staticBlur.Material as ShaderMaterial).SetShaderParam("uv_depth", uvDepth);
	// (staticBlur.Material as ShaderMaterial).SetShaderParam("reso", vars.renderRes);



	Image img = new Image();
	GD.Print(velBuff.GetData().GetFormat());

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
	atmoMat.SetShaderParam("f1", factor1);
	atmoMat.SetShaderParam("f2", factor2);
	atmoMat.SetShaderParam("f3", factor3);
	atmoMat.SetShaderParam("f4", factor4);

	atmoMat.SetShaderParam("fov", vars.FovHalfRad);
	atmoMat.SetShaderParam("reso", vars.renderRes);
	atmoMat.SetShaderParam("cam_xform", cam.GlobalTransform);
	processVelocity();
  }


  void processVelocity()
  {

	if (!check)
	{
	  // camXangle = cam.PrevGlobalTransform.basis.x.AngleTo(cam.GlobalTransform.basis.x);
	  // camYangle = cam.PrevGlobalTransform.basis.y.AngleTo(cam.GlobalTransform.basis.y);
	  // camZangle = cam.PrevGlobalTransform.basis.z.AngleTo(cam.GlobalTransform.basis.z);
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

	velMat.SetShaderParam("cam_prev_pos", cam.ToLocal(cam.PrevGlobalTransform.origin));
	velMat.SetShaderParam("cam_xform", cam.PrevGlobalTransform.basis.Inverse() * cam.GlobalTransform.basis);

  }
}
