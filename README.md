# LSPDFR Plugin Reloader

Exactly what the name says: This [LSPDFR](https://www.lcpdfr.com/) plugin allows you to dynamically unload, load and reload LSPDFR plugins without having to reload whole LSPDFR!\
It works by using Reflection to access certain internal fields of LSPDFR and editing its values, as well as invoking methods and events.

## Important
As you might have already guessed, this plugin is **very experimental**, so use it **at your own risk and with caution**.
- Be aware that I have unfortunately no way of unregistering console commands, so if your plugin has console commands, they will be registered as duplicates by RPH.
- It is very obvious that (re-)loading plugins on the fly way too often could lead to instability and performance issues or even crash your game.\
  Unfortunately, since .NET does not (directly) allow unloading assemblies from memory, those will stay in memory once loaded which leads to memory leaks.\
  To prevent such performance and memory leak issues, it is **strongly recommended** to reload LSPDFR from time to time (the plugin will warn you about that via a notification once you pass a certain loading threshold).
- **It is also your responsibility to properly clean up your plugin; that means stopping fibers and cleaning up other resources!!!** This plugin invokes the `OnOnDutyStateChanged(false)` event (only for your plugin) and calls your plugin's `Finally()` method upon unloading.

## Installation
This tool is intended for LSPDFR plugin developers, that's why you need to be able to **compile it yourself**.\
It doesn't use any extra dependencies besides [**RagePluginHook**](https://ragepluginhook.net/) and **LSPDFR**.
1. Clone the repository.
2. Make sure [these bindings](https://github.com/Sprayxe/LSPDFRPluginReloader/blob/master/Engine/PluginManager.cs#L19) are up-to-date with the LSPDFR build you are using.
They need to be updated everytime the LSPDFR binary is updated as they change due to obfuscation. An explanation on how to find and update these strings is there too.\
As of writing this, the bindings match build `0.4.9110.41894`.
3. Compile!
4. Copy the `.dll` and `.pdb` into `plugins/LSPDFR` like any other LSPDFR plugin.

## How to use it
All you gotta do when working on your plugin is to update your `.dll` (and `.pdb`) in `plugins/LSPDFR` and execute one of the following commands.\
There are three commands that can be executed using the in-game rage console:
- `UnloadLSPDFRPlugin(string pluginName)`: Unloads an active LSPDFR plugin.
- `LoadLSPDFRPlugin(string pluginName)`: Loads an inactive LSPDFR plugin.
- `ReloadLSPDFRPlugin(string pluginName)`: Unloads an active LSPDFR plugin and loads it right-away.
<br>

As already mentioned, I do not take any responsibility for game crashes and other issues you might experience when using this. I am not affiliated with Rockstar, RagePluginHook or LSPDFR.\
I do have the permission of LMS (developer of LSPDFR) to open-source and release this plugin though.\
Have fun!
