using System;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace GilTradeTracker;

public sealed class Plugin : IDalamudPlugin
{
    private const string CommandName = "/giltracker";

    private readonly ICommandManager commandManager;
    private readonly MainWindow mainWindow;

    public IDalamudPluginInterface PluginInterface { get; }

    public ITargetManager TargetManager { get; }

    public Configuration Configuration { get; }

    public Plugin(
        IDalamudPluginInterface pluginInterface,
        ICommandManager commandManager,
        ITargetManager targetManager)
    {
        this.PluginInterface = pluginInterface;
        this.commandManager = commandManager;
        this.TargetManager = targetManager;

        this.Configuration =
            this.PluginInterface.GetPluginConfig() as Configuration
            ?? new Configuration();

        this.Configuration.Initialize(this.PluginInterface);

        this.mainWindow = new MainWindow(this);

        this.commandManager.AddHandler(CommandName, new CommandInfo(this.OnCommand)
        {
            HelpMessage = "Open the Gil Trade Tracker window."
        });

        this.PluginInterface.UiBuilder.Draw += this.DrawUi;
    }

    private void OnCommand(string command, string args)
    {
        this.mainWindow.IsOpen = true;
    }

    private void DrawUi()
    {
        this.mainWindow.Draw();
    }

    public string GetCurrentTargetName()
    {
        var target = this.TargetManager.Target;

        if (target == null)
            return string.Empty;

        return target.Name.ToString();
    }

    public void SaveConfig()
    {
        this.PluginInterface.SavePluginConfig(this.Configuration);
    }

    public void Dispose()
    {
        this.PluginInterface.UiBuilder.Draw -= this.DrawUi;
        this.commandManager.RemoveHandler(CommandName);
        this.mainWindow.Dispose();
    }
}
