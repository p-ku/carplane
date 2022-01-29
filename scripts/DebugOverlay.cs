using Godot;
using System;

public class DebugOverlay : Label
{
  string label_text;
  ulong mem;
  int order;
  Vars vars;
  string[] sizes = { "B", "KB", "MB", "GB", "TB" };

  public override void _Process(float delta)
  {
	label_text = "";
	mem = OS.GetStaticMemoryUsage();
	order = 0;
	while (mem >= 1024 && order < sizes.Length - 1)
	{
	  order++;
	  mem = mem / 1024;
	}
	label_text += GD.Str("FPS: ", Engine.GetFramesPerSecond()) + "\n";
	label_text += String.Format("{0:0.##} {1}", mem, sizes[order]) + "\n";
	label_text += GD.Str("Drag: ", vars.DragMag) + "N" + "\n";
	label_text += GD.Str("AoA: ", vars.AoA) + "N" + "\n";
	label_text += GD.Str("Lift: ", vars.liftAng) + "N" + "\n";
	label_text += GD.Str("AngDamp", vars.AngDamp) + "\n";
	label_text += GD.Str("Debug", vars.debug) + "\n";




	Text = label_text;
  }
  public override void _Ready()
  {
	vars = (Vars)GetNode("/root/Vars");
  }
}
