using System;
using System.IO;

namespace LSPDFRPluginReloader.Engine.Utility.Helpers;

internal static class Helper
{
    internal const string PluginName = "LSPDFRPluginReloader";
    internal static readonly string PluginPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "LSPDFR");
}