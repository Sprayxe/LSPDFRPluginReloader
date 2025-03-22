using System.Reflection;
using LSPD_First_Response.Mod.API;
using LSPDFRPluginReloader.AutoCompleters;
using LSPDFRPluginReloader.Engine;
using LSPDFRPluginReloader.Engine.Utility.Helpers;
using Rage.Attributes;

namespace LSPDFRPluginReloader;

public class EntryPoint : Plugin
{
    internal static bool OnDutyState;
    
    public override void Initialize()
    {
        LSPDFRFunctions.OnOnDutyStateChanged += OnOnDutyStateChanged;
        Game.AddConsoleCommands();
        PluginManager.Initialize();
    }

    public override void Finally()
    {
        LSPDFRFunctions.OnOnDutyStateChanged -= OnOnDutyStateChanged;
    }
    
    [ConsoleCommand("Unloads a LSPDFR plugin. Made by MarcelWRLD.")]
    internal static void UnloadLSPDFRPlugin([ConsoleCommandParameter(AutoCompleterType = typeof(AutoCompleterLSPDFRLoadedAssembly))] string pluginName)
    {
        Assembly plugin = AssemblyHelper.GetAssemblyByName(pluginName);
        if (plugin == null)
        {
            LogWarn($"Could not find assembly '{pluginName}'.");
            return;
        }

        PluginManager.Unload(plugin);
    }

    [ConsoleCommand("Loads a LSPDFR plugin from disk. Made by MarcelWRLD.")]
    internal static void LoadLSPDFRPlugin([ConsoleCommandParameter(AutoCompleterType = typeof(AutoCompleterLSPDFRAssembly))] string pluginName)
    {
        Assembly plugin = AssemblyHelper.GetAssemblyByName(pluginName);
        if (plugin != null)
        {
            LogWarn($"Assembly '{pluginName}' is already loaded.");
            return;
        }
        
        PluginManager.Load(pluginName);
    }

    [ConsoleCommand("Unloads and then loads a LSPDFR plugin from disk. Made by MarcelWRLD.")]
    internal static void ReloadLSPDFRPlugin([ConsoleCommandParameter(AutoCompleterType = typeof(AutoCompleterLSPDFRLoadedAssembly))] string pluginName)
    {
        Assembly plugin = AssemblyHelper.GetAssemblyByName(pluginName);
        if (plugin == null)
        {
            LogWarn($"Could not find assembly '{pluginName}'.");
            return;
        }
        
        PluginManager.Reload(plugin);
    }

    private static void OnOnDutyStateChanged(bool onDuty)
    {
        OnDutyState = onDuty;
    }
}