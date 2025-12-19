using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using LSPDFRPluginReloader.Engine.Utility.Helpers;
using Rage.ConsoleCommands;
using Rage.ConsoleCommands.AutoCompleters;

namespace LSPDFRPluginReloader.AutoCompleters;

internal sealed class AutoCompleterLSPDFRAssembly(Type type) : ConsoleCommandParameterAutoCompleter(type)
{
    public override void UpdateOptions()
    {
        Options.Clear();
        string[] availableDlls = Directory.GetFiles(PluginPath, "*.dll");
        HashSet<string> loadedDlls = GetLoadedAssemblies();

        // Add options.
        foreach (string dllPath in availableDlls)
        {
            string name = Path.GetFileNameWithoutExtension(dllPath);
            if (loadedDlls.Contains(name)) continue;
            Options.Add(new AutoCompleteOption(name, name, $"{name} Plugin"));
        }
    }

    private static HashSet<string> GetLoadedAssemblies()
    {
        Assembly[] loadedAssemblies = AssemblyHelper.Loaded;
        HashSet<string> assemblySet = new(loadedAssemblies.Length);

        foreach (Assembly assembly in loadedAssemblies)
        {
            assemblySet.Add(assembly.ToName());
        }

        return assemblySet;
    }
}