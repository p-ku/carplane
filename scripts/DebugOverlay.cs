using Godot;
using System;

public class DebugOverlay : Label
{
  // Declare member variables here. Examples:
  // private int a = 2;
  // private string b = "text";

  Label LableText;
  string label_text;
  float fps;
  ulong mem;
  int order;
  Vars vars;
  string[] sizes = { "B", "KB", "MB", "GB", "TB" };

  string[] arr = new string[1];
  string[] z;
  //private void add_stat(string stat_name, Godot.Object obj, string stat_ref, bool is_method)

  // Called when the node enters the scene tree for the first time.

  string[] stats = { "" };
  /*   void add_stat(string stat_name, string stat_val)
	{


	arr[0] = stat_name + ":" + stat_val;
	z = new string[stats.Length + 1];

	stats.CopyTo(z, 0);
	arr.CopyTo(z, stats.Length);
	stats = z;
	} */

  public override void _Ready()
  {
	vars = (Vars)GetNode("/root/Vars");
	/* 	add_stat("boo", GD.Str(vars.car_pos));
	  add_stat("boo1", "3F"); */

  }


  public override void _Process(float delta)
  {
	label_text = "";
	//fps = Engine.GetFramesPerSecond();
	mem = OS.GetStaticMemoryUsage();
	order = 0;
	while (mem >= 1024 && order < sizes.Length - 1)
	{
	  order++;
	  mem = mem / 1024;
	}
	label_text += GD.Str("FPS: ", Engine.GetFramesPerSecond()) + "\n";
	label_text += String.Format("{0:0.##} {1}", mem, sizes[order]) + "\n";
	label_text += GD.Str(vars.car_pos) + "\n";


	//Text = label_text;
  }
}
