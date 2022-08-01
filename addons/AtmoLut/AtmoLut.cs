#if TOOLS
using Godot;
using System;

[Tool]
public class AtmoLut : EditorPlugin
{
  Control mainPanel;

  public override void _EnterTree()
  {
	mainPanel = (Control)GD.Load<PackedScene>("res://addons/AtmoLut/atmolut_dock.tscn").Instance();
	GetEditorInterface().GetEditorViewport().AddChild(mainPanel);
	MakeVisible(false);
  }

  public override void _ExitTree()
  {
	mainPanel.QueueFree();
  }
  public override bool HasMainScreen()
  {
	return true;
  }
  public override void MakeVisible(bool visible)
  {
	mainPanel.Visible = true;
  }
  public override string GetPluginName()
  {
	return "AtmoLUT";
  }
}
#endif
