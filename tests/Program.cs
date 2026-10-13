using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Xml.Linq;
using BeatSaberMarkupLanguage.Settings;
using BeatSaberMarkupLanguage.Util;
using BS_Utils.Gameplay;
using IPA.Logging;
using SongCore;
using Vanillify;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

static bool CanPlay(params string[] requirements) => requirements.All(Collections.capabilities.Contains);

var config = new IPA.Config.Config();
var plugin = new Plugin(new Logger(), config);
Collections.RegisterCapability("Mapping Extensions");
ScoreSubmission.ProlongedDisableSubmission("OtherMod");
plugin.OnDisable();
Check(!CanPlay("Vivify"), "Disable before enable must not grant capability.");
plugin.OnEnable();
plugin.OnEnable();
MainMenuAwaiter.EnterMenu();
Check(BSMLSettings.Instance.Hosts.Contains(plugin), "Settings must register when the main menu initializes.");
Check(CanPlay("Vivify"), "Vivify-only chart should be playable.");
Check(CanPlay(), "Ordinary chart should remain playable.");
Check(CanPlay("Vivify", "Mapping Extensions"), "Installed mechanics must remain available.");
Check(!CanPlay("Vivify", "Noodle Extensions"), "Missing mechanics must still block Play.");
Check(Collections.capabilities.Count(c => c == "Vivify") == 1, "Enable must be idempotent.");
Check(ScoreSubmission.Owners.Contains("Vanillify"), "Bypassed gameplay must disable submission.");
plugin.OnDisable();
plugin.OnDisable();
Check(!BSMLSettings.Instance.Hosts.Contains(plugin), "Unloading must remove the settings menu.");
Check(config.Store.Enabled, "Unloading must preserve the saved preference.");
Check(!CanPlay("Vivify"), "Disable must restore requirement enforcement.");
Check(CanPlay("Mapping Extensions"), "Disable must preserve other capabilities.");
Check(ScoreSubmission.Owners.SetEquals(["OtherMod"]), "Disable must preserve other submission blockers.");
plugin.OnEnable();
Check(BSMLSettings.Instance.Hosts.Contains(plugin), "Loading in an existing menu must register settings.");
plugin.OnDisable();
Check(!CanPlay("Vivify"), "Re-enable/disable must clean up.");

Collections.RegisterCapability("Vivify");
bool refused = false;
try { plugin.OnEnable(); }
catch (InvalidOperationException) { refused = true; }
Check(refused, "An existing capability provider must be refused.");
plugin.OnDisable();
Check(CanPlay("Vivify"), "A refused enable must not remove another provider's capability.");
Check(!ScoreSubmission.Owners.Contains("Vanillify"), "A refused enable must not claim a submission blocker.");
Collections.DeregisterCapability("Vivify");

plugin.OnEnable();
plugin.Enabled = false;
plugin.Enabled = false;
Check(!CanPlay("Vivify"), "The menu toggle must immediately restore requirement enforcement.");
Check(!config.Store.Enabled, "The menu toggle must save the disabled preference.");
Check(BSMLSettings.Instance.Hosts.Contains(plugin), "Turning mode off must keep its menu available.");
Check(ScoreSubmission.Owners.SetEquals(["OtherMod"]), "Turning mode off must only remove Vanillify's submission blocker.");
plugin.Enabled = true;
plugin.Enabled = true;
Check(CanPlay("Vivify") && config.Store.Enabled, "Turning mode on must apply and save the preference.");
Check(ScoreSubmission.Owners.Contains("Vanillify"), "Turning mode on must block submission.");
Check(!CanPlay("Vivify", "Noodle Extensions"), "The menu toggle must preserve other missing requirements.");
MainMenuAwaiter.LeaveMenu();
MainMenuAwaiter.EnterMenu();
Check(BSMLSettings.Instance.Hosts.Contains(plugin), "Settings must return when the main menu is recreated.");
plugin.Enabled = false;
plugin.OnDisable();
var nextLaunch = new Plugin(new Logger(), config);
nextLaunch.OnEnable();
Check(!CanPlay("Vivify") && !nextLaunch.Enabled, "A saved off preference must be honored on startup.");
Check(BSMLSettings.Instance.Hosts.Contains(nextLaunch), "Starting off must still provide the toggle menu.");
Collections.RegisterCapability("Vivify");
refused = false;
try { nextLaunch.Enabled = true; }
catch (InvalidOperationException) { refused = true; }
Check(refused && !config.Store.Enabled, "A failed toggle must not save an enabled preference.");
nextLaunch.OnDisable();
Check(CanPlay("Vivify"), "A failed toggle must leave another provider's capability alone.");
Collections.DeregisterCapability("Vivify");
MainMenuAwaiter.LeaveMenu();
MainMenuAwaiter.EnterMenu();
Check(BSMLSettings.Instance.Hosts.Count == 0, "Unloaded plugins must unsubscribe from menu initialization.");

var settings = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "Settings.bsml"));
var toggle = settings.Root!.Element("bool-setting")!;
Check(toggle.Attribute("value")?.Value == "enabled" && toggle.Attribute("apply-on-change")?.Value == "true", "The menu must bind an immediate toggle.");
Check(typeof(Plugin).GetProperty(nameof(Plugin.Enabled))!.GetCustomAttribute<BeatSaberMarkupLanguage.Attributes.UIValueAttribute>()?.Name == "enabled", "The toggle binding must target the plugin preference.");

using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "manifest.json")));
Check(manifest.RootElement.GetProperty("gameVersion").GetString() == "1.40.8", "Manifest must target 1.40.8.");
Check(manifest.RootElement.GetProperty("conflictsWith").GetProperty("Vivify").GetString() == "*", "Real Vivify must conflict.");
Check(manifest.RootElement.GetProperty("dependsOn").GetProperty("BS Utils").GetString() == "^1.14.2", "Submission API dependency must be declared.");
Check(manifest.RootElement.GetProperty("dependsOn").GetProperty("BeatSaberMarkupLanguage").GetString() == "^1.12.5", "The menu dependency must be declared.");
if (args.Length == 1)
{
    var assembly = Assembly.LoadFile(Path.GetFullPath(args[0]));
    using var resource = assembly.GetManifestResourceStream("Vanillify.manifest.json");
    Check(resource != null, "Built DLL must embed the BSIPA manifest.");
    using var embedded = JsonDocument.Parse(resource!);
    Check(JsonElement.DeepEquals(manifest.RootElement, embedded.RootElement), "Built DLL manifest must match source.");
    Check(assembly.GetName().Version == new Version(0, 2, 0, 0), "Assembly and manifest versions must match.");
    using var menuResource = assembly.GetManifestResourceStream("Vanillify.Settings.bsml");
    Check(menuResource != null, "Built DLL must embed the settings menu.");
    Check(XNode.DeepEquals(settings, XDocument.Load(menuResource!)), "Built menu must match source.");
}
Console.WriteLine("Passed: requirement gating, lifecycle, menu registration, immediate toggling, saved preferences, ownership, and embedded resources.");
