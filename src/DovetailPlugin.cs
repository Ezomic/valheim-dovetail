using BepInEx;
using BepInEx.Logging;
using Ezomic.Core;
using HarmonyLib;

namespace Dovetail
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency("ezomic.valheim.core", BepInDependency.DependencyFlags.HardDependency)]
    // No BepInProcess. Snapping itself is placement-time and so purely client-side, but this
    // adds child transforms to shared prefabs, and a server whose prefabs differ from its
    // clients' is a difference worth not having. Harmless there, and consistent.
    public class DovetailPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "ezomic.valheim.dovetail";
        public const string PluginName = "Dovetail";
        public const string PluginVersion = "1.0.0";
        public const string PluginAuthor = "Robbin Thijssen";

        internal static ManualLogSource Log;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            DovetailConfig.Bind(Config);
            // Everyone, not HostOnly. Both ends have to agree about this mod, and the
            // disagreement is silent when they do not: a client that cannot resolve a prefab
            // hash discards the ZDO rather than erroring - destroying what is already standing
            // in the world - and item data that differs desyncs inventories.
            Suite.Register(PluginGuid, PluginName, PluginVersion, Config);

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(ScenePatches));

            Log.LogInfo(PluginName + " " + PluginVersion + " by " + PluginAuthor + " - ready.");
        }

        private void OnDestroy()
        {
            if (_harmony != null) _harmony.UnpatchSelf();
        }

        private void Update()
        {
            if (ZNetScene.instance == null) return;
            SnapPoints.Apply();
        }
    }

    internal static class ScenePatches
    {
        /// <summary>
        /// Snap points are added to the prefabs, so this has to land before anything is
        /// built from them. Every chest in the world - including ones loaded back out of a
        /// save - is instantiated from these prefabs after Awake, so they all inherit the
        /// points rather than only newly placed ones.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ZNetScene), "Awake")]
        private static void AddSnapPointsOnScene()
        {
            SnapPoints.Apply();
        }
    }
}
