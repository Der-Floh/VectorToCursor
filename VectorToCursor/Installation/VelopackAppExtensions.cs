using Velopack;

namespace VectorToCursor.Installation;

internal static class VelopackAppExtensions
{
    /// <summary>
    /// Adds <paramref name="folder"/> to the current user's PATH once Setup has installed the app, and removes it before the
    /// app is uninstalled. Updates replace the folder's contents but keep its path, so they need no hook.
    /// </summary>
    public static VelopackApp AddFolderToUserPath(this VelopackApp app, string folder)
    {
        ArgumentNullException.ThrowIfNull(app);
        if (!OperatingSystem.IsWindows())
            return app;

        UserPathRegistration registration = new(new RegistryUserPathStore(), folder);
        return app
            .OnAfterInstallFastCallback(_ => registration.Register())
            .OnBeforeUninstallFastCallback(_ => registration.Unregister());
    }
}
