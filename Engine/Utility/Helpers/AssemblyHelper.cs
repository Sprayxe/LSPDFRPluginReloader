using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace LSPDFRPluginReloader.Engine.Utility.Helpers;

internal static class AssemblyHelper
{
    #region Const
    
    private const BindingFlags MethodFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
    
    #endregion
    
    internal static Assembly[] Loaded => LSPDFRFunctions.GetAllUserPlugins();

    internal static string ToName(this Assembly assembly) => assembly.GetName().Name;

    internal static Assembly GetAssemblyByName(string name)
    {
        return Loaded.FirstOrDefault(a => a.ToName() == name);
    }

    internal static Type[] GetTypesOfAssemblyInheritingType<T>(Assembly assembly)
    {
        return assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(T))).ToArray();
    }

    internal static List<MethodInfo> GetMethodsOfAssemblyWithAttribute<T>(Assembly assembly) where T : Attribute
    {
        List<MethodInfo> methods = [];
        
        foreach (Type type in assembly.GetTypes())
        {
            foreach (MethodInfo method in type.GetMethods(MethodFlags))
            {
                if (method.GetCustomAttribute<T>() == null) continue;
                methods.Add(method);
            }
        }

        return methods;
    }

    internal static byte[] LoadAssemblyFile(string path)
    {
        if (!File.Exists(path)) return null;

        using FileStream fileStream = File.Open(path, FileMode.Open);
        using MemoryStream memoryStream = new();
        byte[] buffer = new byte[1024];

        int count;
        while ((count = fileStream.Read(buffer, 0, 1024)) > 0)
            memoryStream.Write(buffer, 0, count);
        return memoryStream.ToArray();
    }

    internal static string GetAssemblyPath(string assemblyName)
    {
        return PluginPath + $"\\{assemblyName}.dll";
    }
}