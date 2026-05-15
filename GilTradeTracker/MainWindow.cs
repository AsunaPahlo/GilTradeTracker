using System;
using System.IO;
using System.Linq;
using System.Text;
using Dalamud.Bindings.ImGui;

namespace GilTradeTracker;

public sealed class MainWindow : IDisposable
{
    private readonly Plugin plugin;

    private string playerName = string.Empty;
    private string gilAmount = string.Empty;
    private string statusMessage = string.Empty;

    public bool IsOpen { get; set; }

    public MainWindow(Plugin plugin)
    {
        this.plugin = plugin;
    }

    public void Draw()
    {
        if (!this.IsOpen)
            return;

        var isOpen = this.IsOpen;

        if (ImGui.Begin("Gil Trade Tracker", ref isOpen))
        {
            ImGui.Text("Track gil received by player name.");
            ImGui.Separator();

            if (string.IsNullOrWhiteSpace(this.playerName))
            {
                this.playerName = this.plugin.GetCurrentTargetName();
            }

            ImGui.InputText("Player Name", ref this.playerName, 100);
            ImGui.InputText("Gil Amount", ref this.gilAmount, 30);

            if (ImGui.Button("Use Current Target"))
            {
                this.playerName = this.plugin.GetCurrentTargetName();

                if (string.IsNullOrWhiteSpace(this.playerName))
                {
                    this.statusMessage = "No target selected.";
                }
                else
                {
                    this.statusMessage = "Target name added.";
                }
            }

            ImGui.Separator();

            if (ImGui.Button("Add Entry"))
            {
                this.AddEntry();
            }

            ImGui.SameLine();

            if (ImGui.Button("Export CSV"))
            {
                this.ExportCsv();
            }

            ImGui.SameLine();

            if (ImGui.Button("Delete All"))
            {
                this.plugin.Configuration.Entries.Clear();
                this.plugin.SaveConfig();
                this.statusMessage = "All entries deleted.";
            }

            if (!string.IsNullOrWhiteSpace(this.statusMessage))
            {
                ImGui.Text(this.statusMessage);
            }

            ImGui.Separator();

            var totalGil = this.plugin.Configuration.Entries.Sum(x => x.Amount);

            ImGui.Text($"Total Gil: {totalGil:N0}");
            ImGui.Separator();

            if (ImGui.BeginTable("gil_entries_table", 4))
            {
                ImGui.TableSetupColumn("Player");
                ImGui.TableSetupColumn("Gil");
                ImGui.TableSetupColumn("Date");
                ImGui.TableSetupColumn("Action");
                ImGui.TableHeadersRow();

                for (var i = this.plugin.Configuration.Entries.Count - 1; i >= 0; i--)
                {
                    var entry = this.plugin.Configuration.Entries[i];

                    ImGui.TableNextRow();

                    ImGui.TableNextColumn();
                    ImGui.Text(entry.PlayerName);

                    ImGui.TableNextColumn();
                    ImGui.Text(entry.Amount.ToString("N0"));

                    ImGui.TableNextColumn();
                    ImGui.Text(entry.Date.ToString("yyyy-MM-dd HH:mm"));

                    ImGui.TableNextColumn();

                    if (ImGui.Button($"Delete##{i}"))
                    {
                        this.plugin.Configuration.Entries.RemoveAt(i);
                        this.plugin.SaveConfig();
                        this.statusMessage = "Entry deleted.";
                    }
                }

                ImGui.EndTable();
            }
        }

        this.IsOpen = isOpen;
        ImGui.End();
    }

    private void AddEntry()
    {
        if (string.IsNullOrWhiteSpace(this.playerName))
        {
            this.statusMessage = "Please enter a player name or target a player.";
            return;
        }

        if (!long.TryParse(this.gilAmount.Replace(",", string.Empty), out var amount))
        {
            this.statusMessage = "Please enter a valid gil amount.";
            return;
        }

        if (amount <= 0)
        {
            this.statusMessage = "Gil amount must be higher than 0.";
            return;
        }

        this.plugin.Configuration.Entries.Add(new GilEntry
        {
            PlayerName = this.playerName.Trim(),
            Amount = amount,
            Date = DateTime.Now,
        });

        this.playerName = string.Empty;
        this.gilAmount = string.Empty;

        this.plugin.SaveConfig();
        this.statusMessage = "Entry added.";
    }

    private void ExportCsv()
    {
        var configDirectory = this.plugin.PluginInterface.ConfigDirectory.FullName;
        var exportPath = Path.Combine(configDirectory, "GilTradeTracker_Export.csv");

        var csv = new StringBuilder();
        csv.AppendLine("Player Name,Gil Amount,Date");

        foreach (var entry in this.plugin.Configuration.Entries)
        {
            csv.AppendLine($"{EscapeCsv(entry.PlayerName)},{entry.Amount},{entry.Date:yyyy-MM-dd HH:mm:ss}");
        }

        File.WriteAllText(exportPath, csv.ToString(), Encoding.UTF8);

        this.statusMessage = $"Exported to: {exportPath}";
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    public void Dispose()
    {
    }
}
