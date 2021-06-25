using System;
using BepInEx;
using BepInEx.Configuration;
using UnboundLib;
using HarmonyLib;
using UnityEngine;
using Jotunn.Utils;
using System.Runtime.CompilerServices;

namespace CrosshairForAll 
{
    [BepInDependency("com.willis.rounds.unbound", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(ModId, ModName, "0.0.0.1")]
    [BepInProcess("Rounds.exe")]
    public class CrosshairForAll : BaseUnityPlugin
    {
        private const string ModId = "pykess-and-ascyst.rounds.plugins.crosshairforall";
        private const string ModName = "Crosshair For All";
        internal static AssetBundle Assets;

        public static ConfigEntry<bool> ModActive;
        public static ConfigEntry<bool> ShowEnemyCrosshairs;
        public static ConfigEntry<bool> MatchPlayerColor;

        private void Awake()
        {
            // bind configs with BepInEx
            ModActive = Config.Bind("CrosshairForall", "Enabled", true);
            ShowEnemyCrosshairs = Config.Bind("CrosshairForall", "ShowEnemyCrosshairs", false, "When enabled, draw crosshairs for enemies as well as for your player.");
            MatchPlayerColor = Config.Bind("CrosshairForall", "MatchPlayerColor", true, "When enabled, draw crosshairs as the same color as the player using them.");

            // apply patches
            new Harmony(ModId).PatchAll();
        }
        private void Start()
        {
            // load assets
            CrosshairForAll.Assets = AssetUtils.LoadAssetBundleFromResources("crosshairwhite", typeof(CrosshairForAll).Assembly);

            // add GUI to F1 menu
            Unbound.RegisterGUI("Crosshair For All", new Action(this.DrawGUI));
        }
        private void DrawGUI()
        {
            bool newModActive = GUILayout.Toggle(ModActive.Value, "Enabled", Array.Empty<GUILayoutOption>());
            bool newShowenemyCrosshairs = GUILayout.Toggle(ShowEnemyCrosshairs.Value, "Show Enemy Crosshairs", Array.Empty<GUILayoutOption>());
            bool newMatchPlayerColor = GUILayout.Toggle(MatchPlayerColor.Value, "Match Player Color", Array.Empty<GUILayoutOption>());

            ModActive.Value = newModActive;
            ShowEnemyCrosshairs.Value = newShowenemyCrosshairs;
            MatchPlayerColor.Value = newMatchPlayerColor;
        }



    }

    // make sure that guns ask for a crosshair after being instantiated
    [HarmonyPatch(typeof(Gun), "Start")]
    class GunPatchStart
    {
        private static void Postfix(Gun __instance)
        {
            // is the gun a child of something other than a player? if so, don't give it a crosshair
            if (__instance.gameObject.GetComponent<Holdable>() == null) { return; }

            GameObject crosshair = GameObject.Instantiate(CrosshairForAll.Assets.LoadAsset<GameObject>("CrosshairAsSprite"), __instance.gameObject.transform.position, __instance.gameObject.transform.rotation, __instance.gameObject.transform);
            crosshair.gameObject.transform.localScale = new Vector3(3f, 3f, 3f);
            crosshair.gameObject.transform.localPosition = new Vector3(0f, 5f, 0f);
            SpriteRenderer crosshairRenderer = crosshair.GetComponentInChildren<SpriteRenderer>();
            crosshairRenderer.sortingLayerName = "MapParticle";
            crosshairRenderer.sortingOrder = 1;

            ControllerCrosshair controller = crosshair.gameObject.GetOrAddComponent<ControllerCrosshair>();
            controller.SetGun(__instance);
        }
    }

    public class ControllerCrosshair : MonoBehaviour
    {
        private Player player = null;
        private GeneralInput input = null;
        private SpriteRenderer crosshairRenderer = null;
        private Gun gun = null;
        private GameObject crosshair;

        void Awake()
        {
        }

        void Start()
        {
            this.crosshair = this.gameObject;

            if (this.crosshair == null) { Destroy(this); }

            this.crosshairRenderer = this.crosshair.GetComponentInChildren<SpriteRenderer>();

            if (this.gun == null) { Destroy(this); }

        }

        void Update()
        {
            this.crosshairRenderer.enabled = CrosshairForAll.ModActive.Value;
            

            if (this.player == null)
            {
                this.player = this.gun.player;

                if (this.player == null) { return; }
            }
            if (this.input == null)
            {
                this.input = this.player.GetComponent<GeneralInput>();

                if (this.input == null) { return; }
            }

            if (this.input.controlledElseWhere)
            {
                this.crosshairRenderer.enabled = (CrosshairForAll.ModActive.Value && CrosshairForAll.ShowEnemyCrosshairs.Value);
            }
            else if (this.input.inputType == GeneralInput.InputType.Keyboard)
            {
                this.crosshairRenderer.enabled = false;
            }

            if (CrosshairForAll.MatchPlayerColor.Value)
            {
                this.crosshairRenderer.color = GetPlayerColor.GetColorMax(this.player);
            }
            else
            {
                this.crosshairRenderer.color = Color.white;
            }

            if (this.player.data.dead)
            {
                this.crosshairRenderer.enabled = false;
            }

        }
        public void OnDestroy()
        {
            if (this.crosshair != null) { Destroy(this.crosshair); }
        }

        public void Destroy()
        {
            UnityEngine.Object.Destroy(this);
        }

        public void SetGun(Gun gun)
        {
            this.gun = gun;
        }
    }
    class GetPlayerColor
    {
        public static Color GetColorMax(Player player)
        {
            // I "borrowed" this code from Willis
            Color colorMax = Color.clear;
            Color colorMin = Color.clear;


            PlayerSkinParticle[] componentsInChildren = player.gameObject.GetComponentsInChildren<PlayerSkinParticle>();
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                ParticleSystem particleSystem = (ParticleSystem)componentsInChildren[i].GetFieldValue("part");
                ParticleSystem.MinMaxGradient startColor = particleSystem.main.startColor;
                colorMax = startColor.colorMax;
                colorMin = startColor.colorMin;
            }

            return colorMax;
        }
        public static Color GetColorMin(Player player)
        {
            // I "borrowed" this code from Willis
            Color colorMax = Color.clear;
            Color colorMin = Color.clear;


            PlayerSkinParticle[] componentsInChildren = player.gameObject.GetComponentsInChildren<PlayerSkinParticle>();
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                ParticleSystem particleSystem = (ParticleSystem)componentsInChildren[i].GetFieldValue("part");
                ParticleSystem.MinMaxGradient startColor = particleSystem.main.startColor;
                colorMax = startColor.colorMax;
                colorMin = startColor.colorMin;
            }

            return colorMin;
        }
    }

}
