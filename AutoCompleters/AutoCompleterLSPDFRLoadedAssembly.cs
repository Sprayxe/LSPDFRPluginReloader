using System;
using System.Linq;
using LSPDFRPluginReloader.Engine.Utility.Helpers;
using Rage.ConsoleCommands;
using Rage.ConsoleCommands.AutoCompleters;

namespace LSPDFRPluginReloader.AutoCompleters;

internal class AutoCompleterLSPDFRLoadedAssembly(Type type) : ConsoleCommandParameterAutoCompleter(type)
{
    public override void UpdateOptions()
    {
        Options.Clear();
        string[] plugins = AssemblyHelper.Loaded.Select(a => a.ToName()).ToArray();
        
        // Add options.
        foreach (string plugin in plugins)
        {
            if (plugin == PluginName) continue;
            Options.Add(new AutoCompleteOption(plugin, plugin, $"'{plugin}' Plugin"));
        }
    }
}