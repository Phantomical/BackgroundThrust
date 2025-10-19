using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using BackgroundThrust.Utils;
using HarmonyLib;
using UnityEngine;

namespace BackgroundThrust;

[KSPAddon(KSPAddon.Startup.Instantly, true)]
internal class Loader : MonoBehaviour
{
    void Awake()
    {
        var harmony = new Harmony("BackgroundThrust");
        harmony.PatchAll(typeof(Loader).Assembly);
    }

    void Start()
    {
        TargetHeadingProvider.RegisterAll();

        StartCoroutine(DelayLoadKerbalism());
    }

    IEnumerator DelayLoadKerbalism()
    {
        // Kerbalism loads its binaries in Start, so we need to wait for that
        // to run before we try to do anything.
        yield return new WaitForEndOfFrame();

        LoadKerbalismIntegration();
    }

    void LoadKerbalismIntegration()
    {
        if (!DetectKerbalism())
        {
            LogUtil.Log("Kerbalism not detected. Not loading kerbalism integration.");
            return;
        }
        if (DetectKerbalismIntegration())
        {
            LogUtil.Log("Kerbalism integration already loaded.");
            return;
        }

        var plugin = Path.Combine(
            GetPluginDirectory(),
            "BackgroundThrust.Kerbalism.dll"
        );

        AssemblyLoader.LoadedAssembly loaded;
        try
        {
            var assembly = Assembly.LoadFile(plugin);
            loaded = new AssemblyLoader.LoadedAssembly(
                assembly,
                assembly.Location,
                assembly.Location,
                null
            );

            UpdateAssemblyLoaderTypeCache(loaded);

            LogUtil.Log($"Loaded integration {assembly.FullName}");
            AssemblyLoader.loadedAssemblies.Add(loaded);
        }
        catch (ReflectionTypeLoadException e)
        {
            LogUtil.Warn($"Failed to load integration {name}: {e}");
            var message = "Additional information:";
            foreach (Exception inner in e.LoaderExceptions)
                message += $"\n{inner}";
            LogUtil.Warn(message);
            return;
        }
        catch (Exception e)
        {
            LogUtil.Warn($"Failed to load integration {name}: {e}");
            return;
        }

        StartAssemblyAddons(loaded);
    }

    /// <summary>
    /// KSP maintains a type cache for loaded assemblies that is used to
    /// answer things like <c>GetTypeByName</c>. We need to manually
    /// populate that here for the assemblies we are loading.
    /// </summary>
    private static void UpdateAssemblyLoaderTypeCache(AssemblyLoader.LoadedAssembly loaded)
    {
        var assembly = loaded.assembly;
        var loadedTypes = new AssemblyLoader.LoadedTypes();

        foreach (var type in assembly.GetTypes())
        {
            foreach (Type loadedType in AssemblyLoader.loadedTypes)
            {
                if (type.IsSubclassOf(loadedType) || type == loadedType)
                    loadedTypes.Add(loadedType, type);
            }
        }

        foreach (var (key, items) in loadedTypes)
        {
            foreach (Type item in items)
            {
                loaded.types.Add(key, item);
                loaded.typesDictionary.Add(key, item);
            }
        }
    }

    /// <summary>
    /// KSP is already loading addons and adding new assemblies to the list
    /// now won't cause it to pick them up. This means we need to do it ourselves.
    /// </summary>
    private static void StartAssemblyAddons(AssemblyLoader.LoadedAssembly loaded)
    {
        var assembly = loaded.assembly;
        foreach (var type in assembly.GetTypes())
        {
            if (!type.IsSubclassOf(typeof(MonoBehaviour)))
                continue;

            KSPAddon attribute = type.GetCustomAttributes<KSPAddon>(inherit: true).FirstOrDefault();
            if (attribute == null)
                continue;

            if (attribute.startup != KSPAddon.Startup.Instantly)
                continue;

            StartAddon(loaded, type, attribute);
        }
    }

    private static string GetPluginDirectory()
    {
        return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    }

    private bool DetectKerbalism()
    {
        foreach (var a in AssemblyLoader.loadedAssemblies)
        {
            // Kerbalism comes with more than one assembly. There is Kerbalism for debug builds, KerbalismBootLoader,
            // then there are Kerbalism18 or Kerbalism16_17 depending on the KSP version, and there might be ohter
            // assemblies like KerbalismContracts etc.
            // So look at the assembly name object instead of the assembly name (which is the file name and could be renamed).

            AssemblyName nameObject = new(a.assembly.FullName);
            string realName = nameObject.Name; // Will always return "Kerbalism" as defined in the AssemblyName property of the csproj

            if (realName.Equals("Kerbalism"))
                return true;
        }

        return false;
    }

    private bool DetectKerbalismIntegration()
    {
        foreach (var a in AssemblyLoader.loadedAssemblies)
        {
            // Kerbalism comes with more than one assembly. There is Kerbalism for debug builds, KerbalismBootLoader,
            // then there are Kerbalism18 or Kerbalism16_17 depending on the KSP version, and there might be ohter
            // assemblies like KerbalismContracts etc.
            // So look at the assembly name object instead of the assembly name (which is the file name and could be renamed).

            AssemblyName nameObject = new(a.assembly.FullName);
            string realName = nameObject.Name; // Will always return "Kerbalism" as defined in the AssemblyName property of the csproj

            if (realName.Equals("BackgroundThrust.Kerbalism"))
                return true;
        }

        return false;
    }

    private static readonly MethodInfo StartAddonMethod = typeof(AddonLoader).GetMethod(
        "StartAddon",
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
    );

    private static void StartAddon(AssemblyLoader.LoadedAssembly asm, Type type, KSPAddon addon)
    {
        StartAddonMethod.Invoke(AddonLoader.Instance, [asm, type, addon, addon.startup]);
    }
}
