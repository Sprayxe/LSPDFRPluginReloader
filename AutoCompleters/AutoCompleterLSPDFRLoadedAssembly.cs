using System;
using System.Reflection;
using LSPDFRPluginReloader.Engine.Utility.Helpers;
using Rage.ConsoleCommands;
using Rage.ConsoleCommands.AutoCompleters;

namespace LSPDFRPluginReloader.AutoCompleters;

internal sealed class AutoCompleterLSPDFRLoadedAssembly(Type type) : ConsoleCommandParameterAutoCompleter(type)
{
    public override void UpdateOptions()
    {
        Options.Clear();

        // Add options.
        foreach (Assembly assembly in AssemblyHelper.Loaded)
        {
            string plugin = assembly.ToName();
            if (plugin == PluginName) continue;
            Options.Add(new AutoCompleteOption(plugin, plugin, $"'{plugin}' Plugin"));
        }
    }
}