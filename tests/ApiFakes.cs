using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

// These fakes check lifecycle behavior, not compatibility with the actual game APIs.
namespace IPA
{
    public enum RuntimeOptions { DynamicInit }
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class PluginAttribute(RuntimeOptions options) : Attribute
    {
        public RuntimeOptions RuntimeOptions { get; } = options;
    }
    [AttributeUsage(AttributeTargets.Constructor)]
    public sealed class InitAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class OnEnableAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class OnDisableAttribute : Attribute { }
}

namespace IPA.Logging
{
    public sealed class Logger
    {
        public void Info(string message) { }
    }
}

namespace IPA.Config
{
    public sealed class Config
    {
        public Vanillify.PluginConfig Store { get; } = new();
    }
}

namespace IPA.Config.Stores
{
    public static class GeneratedStore
    {
        public static T Generated<T>(this IPA.Config.Config config) where T : class => (T)(object)config.Store;
    }
}

namespace BeatSaberMarkupLanguage.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class UIValueAttribute(string name) : Attribute
    {
        public string Name { get; } = name;
    }
}

namespace BeatSaberMarkupLanguage.Settings
{
    public sealed class BSMLSettings
    {
        public static BSMLSettings Instance { get; set; } = new();
        public readonly HashSet<object> Hosts = [];
        public void AddSettingsMenu(string name, string resource, object host) => Hosts.Add(host);
        public void RemoveSettingsMenu(object host) => Hosts.Remove(host);
    }
}

namespace BeatSaberMarkupLanguage.Util
{
    public static class MainMenuAwaiter
    {
        private static TaskCompletionSource Completion = new();
        public static event Action? MainMenuInitializing;
        public static Task WaitForMainMenuAsync() => Completion.Task;
        public static void EnterMenu()
        {
            Settings.BSMLSettings.Instance = new();
            Completion.TrySetResult();
            MainMenuInitializing?.Invoke();
        }
        public static void LeaveMenu() => Completion = new();
    }
}

namespace SongCore
{
    public static class Collections
    {
        private static readonly List<string> Entries = [];
        public static ReadOnlyCollection<string> capabilities => Entries.AsReadOnly();
        public static void RegisterCapability(string capability)
        {
            if (!Entries.Contains(capability)) Entries.Add(capability);
        }
        public static void DeregisterCapability(string capability) => Entries.Remove(capability);
    }
}

namespace BS_Utils.Gameplay
{
    public static class ScoreSubmission
    {
        public static readonly HashSet<string> Owners = [];
        public static void ProlongedDisableSubmission(string mod) => Owners.Add(mod);
        public static void RemoveProlongedDisable(string mod) => Owners.Remove(mod);
    }
}
