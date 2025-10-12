using System.Text;
using UnityEngine;

namespace BackgroundThrust.Utils;

internal static class LogUtil
{
    static string BuildMessage(object[] args)
    {
        var builder = new StringBuilder("[BackgroundThrust] ");
        foreach (var arg in args)
        {
            builder.Append(arg.ToString());
        }
        return builder.ToString();
    }

    public static void Log(params object[] args)
    {
        Debug.Log(BuildMessage(args));
    }

    public static void Warn(params object[] args)
    {
        Debug.LogWarning(BuildMessage(args));
    }

    public static void Error(params object[] args)
    {
        Debug.LogError(BuildMessage(args));
    }
}
