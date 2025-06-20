using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using LSPDFRPluginReloader.Engine.Utility.Helpers;

namespace LSPDFRPluginReloader.Engine;

internal static class PluginManager
{
    #region Bindings

    // No update expected.
    private const string BOnDutyStateChanged = "OnOnDutyStateChanged";
    private const string BCalloutManagerPlugins = "plugins";
    // These (LSPDFR Build 0.4.9299.20427) need to be updated everytime LSPDFR updates.
    // In the following you should always copy what is inside <...> into the related constants.
    // In order to find all of these you need to head to LSPD_First_Response.Mod.API.Functions and look for the 'GetAllUserPlugins()' method.
    // 1. There you should find something like: 'return <BCalloutManagerHolder>.<BCalloutManagerInstance>.CM_ASSEMBLIES_PROPERTY'
    //
    // 2. CTRL+Click on 'CM_ASSEMBLIES_PROPERTY', this will take you to the <BCalloutManager> class,
    //    you should find something like: 'public Assembly[] CM_ASSEMBLIES_PROPERTY => <BCalloutManager>.<BCalloutManagerAssemblies>;'
    //
    // 3. Look in the <BCalloutManager> class for a field with this signature: 'private List<Type> <BCalloutManagerCallouts>;'.
    //
    // There you go! :)
    private const string BCalloutManager = "tUEATbDiDasmZFYisZGGeLJrXErA";
    private const string BCalloutManagerHolder = "dxIbJMIvSZmtYuVoMjTdWKeMGOweA";
    private const string BCalloutManagerInstance = "EGghWqmVJWUCxfprSdSbzrYBMrts";
    private const string BCalloutManagerAssemblies = "jSPcJgSPguCsjLItrrxuaXBnmnGx";
    private const string BCalloutManagerCallouts = "uTBErqqabbiTjfyfxWWsuYAqPbVL";
    
    #endregion
    
    private static Assembly _lspdfrAssembly; // LSPD_First_Response
    private static Type _lspdfrApi; // LSPD_First_Response.Mod.API.Functions
    private static EventInfo _onDutyEvent; // Event
    private static Type _lspdfrCalloutManager; // LSPD_First_Response.???
    private static PropertyInfo _lspdfrCalloutManagerInstance; // internal static <_lspdfrCalloutManager> { get...; private set...; }
    private static FieldInfo _assembliesField; // private static Assembly[]
    private static FieldInfo _pluginsField; // private static List<Plugin>
    private static FieldInfo _calloutsField; // private List<Type>
    
    private static List<Plugin> Plugins => _pluginsField.GetValue(null) as List<Plugin>;
    private static Assembly[] Assemblies => _assembliesField.GetValue(null) as Assembly[];
    private static object LspdfrCalloutManager => _lspdfrCalloutManagerInstance?.GetValue(null);
    private static List<Type> Callouts => _calloutsField.GetValue(LspdfrCalloutManager) as List<Type>;
    private static LSPDFRFunctions.OnDutyStateChangedEventHandler OnDutyEventHandler => GetOnDutyStateChangedHandler();
    
    internal static void Initialize()
    {
        const BindingFlags privateStaticBinds = BindingFlags.NonPublic | BindingFlags.Static;
        _lspdfrApi = typeof(LSPDFRFunctions);
        _lspdfrAssembly = Assembly.GetAssembly(_lspdfrApi);
        
        // Find on-duty event
        _onDutyEvent = _lspdfrApi.GetEvent(BOnDutyStateChanged, BindingFlags.Public | BindingFlags.Static);
        CheckBinding(_onDutyEvent, nameof(BOnDutyStateChanged));
        
        // Find callout manager
        _lspdfrCalloutManager = _lspdfrAssembly.GetType(BCalloutManager);
        AssertTypeBinding(_lspdfrCalloutManager, nameof(BCalloutManager));
        
        // Find class which stores an instance of the callout manager as private-static field (or internal property)
        Type lspdfrCalloutManagerHolderClass = _lspdfrAssembly.GetType(BCalloutManagerHolder);
        AssertTypeBinding(lspdfrCalloutManagerHolderClass, nameof(BCalloutManagerHolder));
        
        // Find property of the ^above
        _lspdfrCalloutManagerInstance = lspdfrCalloutManagerHolderClass.GetProperty(BCalloutManagerInstance, privateStaticBinds);
        AssertMemberBinding(_lspdfrCalloutManagerInstance, nameof(BCalloutManagerInstance));
        
        // Find field in callout manager which stores all loaded assemblies
        _assembliesField = _lspdfrCalloutManager.GetField(BCalloutManagerAssemblies, privateStaticBinds);
        AssertMemberBinding(_assembliesField, nameof(BCalloutManagerAssemblies));
        
        // Find field in callout manager which stores all loaded plugins
        _pluginsField = _lspdfrCalloutManager.GetField(BCalloutManagerPlugins, privateStaticBinds);
        AssertMemberBinding(_pluginsField, nameof(BCalloutManagerPlugins));
        
        // Find field in callout manager which stores all loaded callouts
        _calloutsField = _lspdfrCalloutManager.GetField(BCalloutManagerCallouts, BindingFlags.Instance | BindingFlags.NonPublic);
        AssertMemberBinding(_calloutsField, nameof(BCalloutManagerCallouts));
    }

    internal static void Unload(Assembly assembly, bool forceGC)
    {
        string asmName = assembly.ToName();
        LogDebug($"Unloading assembly '{asmName}'.");
        
        Type[] definedPluginTypes = AssemblyHelper.GetTypesOfAssemblyInheritingType<Plugin>(assembly);
        if (definedPluginTypes.Length == 0)
        {
            LogWarn($"Could not find any defined plugin types in assembly '{asmName}'.");
            return;
        }

        Plugin[] definedPlugins = Plugins.Where(p => definedPluginTypes.Contains(p.GetType())).ToArray();
        if (definedPlugins.Length == 0)
        {
            LogWarn($"Could not find any loaded plugins defined in assembly '{asmName}'.");
            return;
        }
        
        LogDebug($"{asmName}: Unloading {definedPlugins.Length} plugin(s).");
        foreach (Plugin plugin in definedPlugins)
        {
            Delegate[] delegates = GetOnDutyDelegates(assembly);
            foreach (Delegate del in delegates)
            {
                del.DynamicInvoke(false);
                _onDutyEvent?.RemoveEventHandler(null, del);
                LogDebug($"{asmName}: Unsubscribed from on-duty event.");
            }
            
            plugin.Finally();
            Plugins.Remove(plugin);
            
            LogDebug($"{asmName}: Unloaded plugin '{plugin.GetType().Name}'.");
        }

        Type[] definedCallouts = AssemblyHelper.GetTypesOfAssemblyInheritingType<Callout>(assembly);
        if (definedCallouts.Length != 0)
        {
            LogDebug($"{asmName}: Unloading {definedCallouts.Length} callout(s).");
            foreach (Type callout in definedCallouts)
            {
                Callouts.Remove(callout);
                LogDebug($"{asmName}: Unloaded callout '{callout.Name}'.");
            }
        }
        
        _assembliesField.SetValue(null, Assemblies.Where(a => !a.Equals(assembly)).ToArray());
        if (forceGC) 
        {
            LogDebug($"{asmName}: Triggering garbage collection.");
            GC.Collect();
        }
        
        LogDebug($"Successfully unloaded assembly '{asmName}'.");
    }

    internal static void Load(string assemblyName)
    {
        string dllPath = AssemblyHelper.GetAssemblyPath(assemblyName);
        byte[] dllFile = AssemblyHelper.LoadAssemblyFile(dllPath);
        if (dllFile == null)
        {
            LogWarn($"Could not find .dll file at '{dllPath}'.");
            return;
        }
        
        LogDebug($"Loading assembly '{assemblyName}'.");
        string pdbPath = Path.ChangeExtension(dllPath, ".pdb");
        byte[] pdbFile = AssemblyHelper.LoadAssemblyFile(pdbPath);
        Assembly loadedAssembly = pdbFile != null ? Assembly.Load(dllFile, pdbFile) : Assembly.Load(dllFile);
        LogDebug($"Successfully loaded assembly '{loadedAssembly.ToName()}' (has PDB: {pdbFile != null}) into memory.");
        UpdateLoadCount();
        
        List<Assembly> existingAssemblies = [loadedAssembly, ..Assemblies];
        _assembliesField.SetValue(null, existingAssemblies.ToArray());
        LogDebug($"Successfully loaded assembly '{loadedAssembly.ToName()}' into LSPDFR.");

        Type[] loadedPlugins = AssemblyHelper.GetTypesOfAssemblyInheritingType<Plugin>(loadedAssembly);
        if (loadedPlugins.Length == 0)
        {
            LogWarn("Loaded assembly does not have any defined plugin types.");
            return;
        }

        List<Plugin> createdPlugins = [];
        foreach (Type pluginType in loadedPlugins) // Create instances first
        {
            Plugin loadedPlugin = Activator.CreateInstance(pluginType) as Plugin;
            Plugins.Add(loadedPlugin);
            createdPlugins.Add(loadedPlugin);
            LogDebug($"Created plugin '{pluginType.Name}'.");
        }

        foreach (Plugin p in createdPlugins) // Initialize the instances
        {
            p.Initialize();
            LogDebug($"Successfully initialized plugin '{p.GetType().Name}'.");
        }

        if (EntryPoint.OnDutyState) // Eventually invoke the OnDuty event
        {
            Delegate[] invocationList = GetOnDutyDelegates(loadedAssembly);
            foreach (Delegate del in invocationList)
            {
                del.DynamicInvoke(true);
                LogDebug($"{assemblyName}: Successfully invoked on-duty event.");
            }
        }
        
        LogDebug($"Successfully loaded assembly '{assemblyName}'.");
    }
    
    internal static void Reload(Assembly assembly, bool forceGC)
    {
        string asmName = assembly.ToName();
        LogDebug($"Reloading assembly '{asmName}'.");
        Unload(assembly, forceGC);
        Load(asmName);
        LogDebug($"Successfully reloaded assembly '{asmName}'.");
    }

    private static LSPDFRFunctions.OnDutyStateChangedEventHandler GetOnDutyStateChangedHandler()
    {
        FieldInfo onDutyEvent = _lspdfrApi.GetField(BOnDutyStateChanged, BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Static);
        if (onDutyEvent != null)
        {
            return (LSPDFRFunctions.OnDutyStateChangedEventHandler)onDutyEvent.GetValue(null);
        }
        
        LogWarn("Could not get handler for on-duty event.");
        return null;
    }

    private static Delegate[] GetOnDutyDelegates(Assembly assembly)
    {
        return OnDutyEventHandler.GetInvocationList()
                                  .Where(d => d.Method.DeclaringType?.Assembly == assembly)
                                  .ToArray();
    }

    #region Debug

    private const int LoadCountThreshold = 20;
    private static int _loadCount;
    private static void UpdateLoadCount()
    {
        if (_loadCount++ < LoadCountThreshold) return;
        Game.DisplayNotification($"You have loaded plugins more than {LoadCountThreshold} times!~n~It is ~y~strongly recommended~s~ to reload LSPDFR from time to time to prevent ~r~memory leaks~s~ and ~r~performance issues~s~.");
    }
    
    private static void CheckBinding(object value, string name)
    {
        if (value != null) return;
        LogWarn($"Could not bind '{name}'.");
    }

    private static void AssertTypeBinding(object value, string name)
    {
        if (value != null) return;
        throw new TypeLoadException($"Could not bind type '{name}'.");
    }
    
    private static void AssertMemberBinding(object value, string name)
    {
        if (value != null) return;
        throw new MissingMemberException($"Could not bind field or property '{name}'.");
    }
    
    #endregion
}