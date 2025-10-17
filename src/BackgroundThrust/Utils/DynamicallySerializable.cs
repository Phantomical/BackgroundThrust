using System;
using System.Collections.Generic;

namespace BackgroundThrust.Utils;

/// <summary>
/// A helper class for types which can be generically deserialized from a
/// <see cref="ConfigNode"/>. This is mostly an implementation detail, the
/// only way you should be interacting with it is by overriding
/// <see cref="OnLoad"/> and <see cref="OnSave"/> if you need to.
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class DynamicallySerializable<T>
    where T : DynamicallySerializable<T>
{
    private static Dictionary<string, Type> registry = [];

    private readonly BaseFieldList fields;

    protected DynamicallySerializable()
    {
        fields = new(this);
    }

    protected virtual void OnLoad(ConfigNode node) => fields.Load(node);

    protected virtual void OnSave(ConfigNode node) => fields.Save(node);

    public void Save(ConfigNode node)
    {
        node.AddValue("name", GetType().FullName);
        OnSave(node);
    }

    protected static T Load(ConfigNode node, Action<T> preload = null)
    {
        string name = null;
        if (!node.TryGetValue("name", ref name))
        {
            LogUtil.Error("ConfigNode has no `name` field");
            return null;
        }

        if (!registry.TryGetValue(name, out var type))
        {
            LogUtil.Error(
                $"Attempted to load a ConfigNode with name `{name}` but no type has been registered with that name"
            );
            return null;
        }

        var inst = (T)Activator.CreateInstance(type);

        preload?.Invoke(inst);
        inst.OnLoad(node);
        return inst;
    }

    protected static void RegisterAll()
    {
        Dictionary<string, Type> entries = [];

        foreach (var assembly in AssemblyLoader.loadedAssemblies)
        {
            foreach (var type in assembly.assembly.GetTypes())
            {
                if (!type.IsSubclassOf(typeof(T)))
                    continue;
                if (type.IsGenericTypeDefinition)
                    continue;
                if (type.IsAbstract)
                    continue;

                if (type.GetConstructor(Type.EmptyTypes) == null)
                {
                    LogUtil.Error($"Type {type.Name} does not have a default constructor");
                    continue;
                }

                if (entries.ContainsKey(type.FullName))
                {
                    LogUtil.Error($"Name conflict: multiple types with full name {type.FullName}");
                    continue;
                }

                entries.Add(type.FullName, type);
            }
        }

        registry = entries;
    }
}
