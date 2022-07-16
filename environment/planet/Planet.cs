using Godot;
using System;

public class Planet : StaticBody
{
  MeshDataTool mdt;
  MeshInstance mi;
  ArrayMesh mesh = new ArrayMesh();
  // MeshInstance surf;;
  SphereMesh surf = new SphereMesh();

  OpenSimplexNoise noise = new OpenSimplexNoise();
  NoiseTexture noiseTex = new NoiseTexture();
  CollisionShape collide = new CollisionShape();

  // Declare member variables here. Examples:
  // private int a = 2;
  // private string b = "text";

  // Called when the node enters the scene tree for the first time.
  public override void _Ready()
  {
    mi = new MeshInstance();
    mdt = new MeshDataTool();
    //surf = (MeshInstance)GetNode("Surface");
    surf.Radius = 26f;
    // mesh = (surf.Mesh as ArrayMesh);
    //mdt.CreateFromSurface(surf);
    Godot.Collections.Array surfArr = surf.GetMeshArrays();
    mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfArr);
    mdt.CreateFromSurface(mesh, 0);

    noise.Seed = (int)GD.Randi();
    noise.Octaves = 4;
    noise.Period = 20.0f;
    noise.Persistence = 0.8f;

    noiseTex.Noise = noise;
    noiseTex.Height = 1024;
    noiseTex.Width = 1024;

    // for (i in Range(mdt.GetVertexCount()))
    for (int i = 0; i < mdt.GetVertexCount(); ++i)
    {
      Vector3 vertex = mdt.GetVertex(i);
      // In this example we extend the mesh by one unit, which results in separated faces as it is flat shaded.
      vertex += mdt.GetVertexNormal(i) * noise.GetNoise3dv(vertex) * 2f;
      // Save your change.
      mdt.SetVertex(i, vertex);
    }
    mesh.SurfaceRemove(0);
    mdt.CommitToSurface(mesh);
    mi.Mesh = mesh;
    collide.Shape = mesh.CreateConvexShape(true, true);
    AddChild(collide);
    mi.Mesh = collide.Shape.GetDebugMesh();
    AddChild(mi);
    //surf.Mesh = collide.Shape.GetDebugMesh();

  }

  //  // Called every frame. 'delta' is the elapsed time since the previous frame.
  //  public override void _Process(float delta)
  //  {
  //      
  //  }
}
