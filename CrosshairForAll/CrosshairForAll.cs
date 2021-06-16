using System;
using BepInEx;
using UnboundLib;
using HarmonyLib;
using UnityEngine;
using Jotunn.Utils;

namespace CrosshairForAll 
{
    [BepInDependency("com.willis.rounds.unbound", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin("pykess-and-ascyst.rounds.plugins.crosshairforall", "Crosshair For All", "0.0.0.0")]
    [BepInProcess("Rounds.exe")]
    public class CrosshairForAll : BaseUnityPlugin
    {
        private void Awake()
        {
            new Harmony("pykess-and-ascyst.rounds.plugins.crosshairforall").PatchAll();
        }
        private void Start()
        {
            // load asset
            //CrosshairForAll.Assets = AssetUtils.LoadAssetBundleFromResources("crosshairwhite", typeof(CrosshairForAll).Assembly);
            //CrosshairForAll.Assets = AssetUtils.LoadAssetBundleFromResources("pceassetbundle", typeof(CrosshairForAll).Assembly); // test asset bundle while waiting for Ascyst

        }

        private const string ModId = "pykess-and-ascyst.rounds.plugins.crosshairforall";
        private const string ModName = "Crosshair For All";
        internal static AssetBundle Assets;
    }

    [Serializable]
    [HarmonyPatch(typeof(Gun), "Start")]
    class GunPatchStart
    {
        private static void Postfix(Gun __instance)
        {
            // all of these give null reference exceptions
            //Player player = __instance.holdable.holder.player;
            //Player player = __instance.GetComponentInParent<Player>();
            //Player player = __instance.player;
            //GeneralInput input = player.GetComponent<GeneralInput>();

            // don't modify keyboard players
            //if (input.inputType == GeneralInput.InputType.Keyboard) { return; }

            // crosshair doesn't move with the player, the gun, or really at all
            GameObject crosshair = GameObject.Instantiate(CrosshairForAll.Assets.LoadAsset<GameObject>("crosshair"), __instance.transform.position, __instance.transform.rotation, __instance.transform);
            ControllerCrosshair controller = crosshair.gameObject.GetOrAddComponent<ControllerCrosshair>();
            crosshair.transform.localPosition = Vector3.zero;
            crosshair.transform.SetAsFirstSibling();
            crosshair.transform.localScale = Vector3.one;
            //crosshair.transform.SetParent(__instance.transform);

        }
    }

    public class ControllerCrosshair : MonoBehaviour
    {
        void Awake()
        {

        }

        void Start()
        {

        }

        void Update()
        {
        }
        public void OnDestroy()
        {
        }

        public void Destroy()
        {
            UnityEngine.Object.Destroy(this);
        }
    }

}
