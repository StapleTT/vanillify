using System;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Settings;
using BeatSaberMarkupLanguage.Util;
using BS_Utils.Gameplay;
using IPA;
using IPA.Config.Stores;
using IPA.Logging;
using SongCore;

namespace Vanillify;

[Plugin(RuntimeOptions.DynamicInit)]
public sealed class Plugin
{
    private const string Capability = "Vivify";
    private const string SubmissionOwner = "Vanillify";
    private readonly Logger _logger;
    private readonly PluginConfig _config;
    private BSMLSettings? _settings;
    private bool _loaded;
    private bool _enabled;

    [Init]
    public Plugin(Logger logger, IPA.Config.Config config)
    {
        _logger = logger;
        _config = config.Generated<PluginConfig>();
    }

    [UIValue("enabled")]
    public bool Enabled
    {
        get => _config.Enabled;
        set
        {
            if (_loaded)
                SetEnabled(value);
            _config.Enabled = value;
        }
    }

    [OnEnable]
    public void OnEnable()
    {
        if (_loaded)
            return;

        SetEnabled(_config.Enabled);
        _loaded = true;
        MainMenuAwaiter.MainMenuInitializing += AddSettingsMenu;
        // Also support loading the plugin when the main menu is already open.
        if (MainMenuAwaiter.WaitForMainMenuAsync().IsCompleted)
            AddSettingsMenu();
    }

    private void AddSettingsMenu()
    {
        _settings = BSMLSettings.Instance;
        _settings.AddSettingsMenu("Vanillify", "Vanillify.Settings.bsml", this);
    }

    private void SetEnabled(bool enabled)
    {
        if (_enabled == enabled)
            return;

        if (!enabled)
        {
            Collections.DeregisterCapability(Capability);
            ScoreSubmission.RemoveProlongedDisable(SubmissionOwner);
            _enabled = false;
            _logger.Info("Vivify requirement bypass disabled.");
            return;
        }

        // SongCore capabilities have no ownership tracking; never take another provider's entry.
        if (Collections.capabilities.Contains(Capability))
            throw new InvalidOperationException("Remove Vivify or another Vivify capability provider before enabling Vanillify.");

        ScoreSubmission.ProlongedDisableSubmission(SubmissionOwner);
        Collections.RegisterCapability(Capability);
        _enabled = true;
        _logger.Info("Vivify requirement bypass enabled. Vivify effects are absent; score submission is disabled for all songs.");
    }

    [OnDisable]
    public void OnDisable()
    {
        if (!_loaded)
            return;

        MainMenuAwaiter.MainMenuInitializing -= AddSettingsMenu;
        _settings?.RemoveSettingsMenu(this);
        _settings = null;
        SetEnabled(false);
        _loaded = false;
    }
}
