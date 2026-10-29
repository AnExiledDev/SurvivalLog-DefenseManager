using System;
using System.Linq;
using System.Reflection;

namespace DefenseManager.Adapters;

/// <summary>
/// Package Bank's packages, when that mod is installed (0.5.0 or newer). It is optional: without it the Install list
/// offers only the packages lying in the yard. Its API is found by name the first time it is needed, so this mod builds
/// and loads without it, and every call fails soft.
/// </summary>
internal static class OptionalBank
{
    public const string Guid = "com.dev.survivallog.packagebank";

    private const string ApiType = "PackageBank.Api.PackageBankApi";

    private static Func<int[]>? _banked;
    private static Func<int, long>? _takeOut;
    private static bool _isLooked;
    private static bool _hasFailed;

    /// <summary>The furniture id of every banked package; empty without Package Bank.</summary>
    public static int[] Banked(Action<string> log)
    {
        if (!Find(log) || _banked == null) return Array.Empty<int>();

        try
        {
            return _banked();
        }
        catch (Exception ex)
        {
            ReportOnce(log, "reading Package Bank failed: " + ex.Message);

            return Array.Empty<int>();
        }
    }

    /// <summary>Package Bank's TakeOut: the new yard package's id, -1 when no yard spot is free, 0 when it failed.</summary>
    public static long TakeOut(int configId, Action<string> log)
    {
        if (!Find(log) || _takeOut == null) return 0;

        try
        {
            return _takeOut(configId);
        }
        catch (Exception ex)
        {
            log("taking a package out of Package Bank failed: " + ex);

            return 0;
        }
    }

    private static bool Find(Action<string> log)
    {
        if (_isLooked) return _banked != null && _takeOut != null;

        _isLooked = true;

        try
        {
            var api = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => assembly.GetName().Name == "PackageBank")?.GetType(ApiType);

            if (api == null)
            {
                log("Package Bank 0.5.0 or newer is not installed, so the Install list shows only packages in the yard");

                return false;
            }

            _banked = (Func<int[]>)Delegate.CreateDelegate(typeof(Func<int[]>), api.GetMethod("BankedFurniture", BindingFlags.Public | BindingFlags.Static)!);
            _takeOut = (Func<int, long>)Delegate.CreateDelegate(typeof(Func<int, long>), api.GetMethod("TakeOut", BindingFlags.Public | BindingFlags.Static)!);
            log("found Package Bank: banked defense packages can be installed");

            return true;
        }
        catch (Exception ex)
        {
            _banked = null;
            _takeOut = null;
            log("Package Bank is installed but its packages could not be reached: " + ex.Message);

            return false;
        }
    }

    private static void ReportOnce(Action<string> log, string message)
    {
        if (_hasFailed) return;

        _hasFailed = true;
        log(message + " (reported once)");
    }
}
