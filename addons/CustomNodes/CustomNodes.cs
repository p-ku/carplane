#if TOOLS
using Godot;
using System;

[Tool]
public class CustomNodes : EditorPlugin
{
  // Each row of this array is a new node type: name, parent type, and icon.
  private string[,] types = {
      { "MeshBlur","MeshInstance","MeshInstance3D"},
      {"TranslateBlur","MeshInstance","MeshInstance3D"},
      {"WIPBlur","MeshInstance","MeshInstance3D"},
      {"ViewportBuffer", "Viewport", "Viewport"},
      {"BufferSprite", "Sprite", "Sprite2D"}
    };
  public override void _EnterTree()
  {
    for (int i = 0; i < types.GetLength(0); i++)
    {
      Script script = GD.Load<Script>("res://addons/CustomNodes/" + types[i, 0] + ".cs");
      Texture icon = GD.Load<Texture>("res://addons/CustomNodes/icons/" + types[i, 2] + ".svg");
      AddCustomType(types[i, 0], types[i, 1], script, icon);
    }
  }

  public override void _ExitTree()
  {
    for (int i = 0; i < types.GetLength(0); i++)
    {
      RemoveCustomType(types[i, 0]);
    }
  }
}
#endif
