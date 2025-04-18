﻿using System;

namespace LSPDFRPluginReloader.Engine.Utility;

internal static class Logger
{
    private const string DefaultInfo = "[{0}] LSPDFRPluginReloader: {1}";

    // consider calling 'DisplayErrorNotification' along with this
    internal static void LogException(Exception ex, string location)
    {
        Game.LogTrivial(string.Format(DefaultInfo, $"ERROR - {location}", ex));
    }

    internal static void LogDebug(string msg)
    {
        Game.LogTrivial(string.Format(DefaultInfo, "DEBUG", msg));
    }

    internal static void LogWarn(string msg)
    {
        Game.LogTrivial(string.Format(DefaultInfo, "WARN", msg));
    }
}