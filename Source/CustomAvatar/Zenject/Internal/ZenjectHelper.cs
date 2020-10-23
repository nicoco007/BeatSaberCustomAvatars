using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using Zenject;

namespace CustomAvatar.Zenject.Internal
{
    internal class ZenjectHelper
    {
        private static readonly List<InstallerRegistration> _installerRegistrations = new List<InstallerRegistration>();

        internal static void Init(Harmony harmony)
        {
            PatchInstallInstallers(harmony);
        }

        public static InstallerRegistration Register<TInstaller>() where TInstaller : Installer
        {
            var registration = new InstallerRegistration(typeof(TInstaller));

            _installerRegistrations.Add(registration);

            return registration;
        }

        private static void PatchInstallInstallers(Harmony harmony)
        {
            MethodInfo methodToPatch = typeof(Context).GetMethod("InstallInstallers", BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[0], null);
            MethodInfo patch = typeof(ZenjectHelper).GetMethod(nameof(InstallInstallers), BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(methodToPatch, null, new HarmonyMethod(patch));
        }

        private static void InstallInstallers(Context __instance)
        {
            foreach (InstallerRegistration installerRegistration in _installerRegistrations)
            {
                installerRegistration.InstallInto(__instance);
            }
        }
    }
}
