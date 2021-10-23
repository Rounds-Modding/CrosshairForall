using System;
using BepInEx;
using BepInEx.Configuration;
using UnboundLib;
using HarmonyLib;
using UnityEngine;
using Jotunn.Utils;
using System.Runtime.CompilerServices;
using InControl;
using System.Reflection;
using UnboundLib.Utils.UI;
using TMPro;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;

namespace CrosshairForAll 
{
    [BepInDependency("com.willis.rounds.unbound", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(ModId, ModName, "1.0.1")]
    [BepInProcess("Rounds.exe")]
    public class CrosshairForAll : BaseUnityPlugin
    {
        private const string ModId = "pykess-and-ascyst.rounds.plugins.crosshairforall";
        private const string ModName = "Crosshair For All";
        private const string CompatibilityModName = "CrosshairForAll";
        internal static AssetBundle Assets;

        public static ConfigEntry<bool> ModActive;
        public static ConfigEntry<bool> ShowEnemyCrosshairs;
        public static ConfigEntry<bool> MatchPlayerColor;
        public static ConfigEntry<int> CrosshairControlType;
        public static ConfigEntry<bool> ScaleSelfWithBulletSpeed;
        public static ConfigEntry<bool> ScaleEnemiesWithBulletSpeed;
        public static ConfigEntry<float> DistanceOffset;

        private static List<Toggle> ControlTypeNone = new List<Toggle>() { };
        private static List<Toggle> ControlTypeStepped = new List<Toggle>() { };
        private static List<Toggle> ControlTypeSmooth = new List<Toggle>() { };


        private void Awake()
        {
            // bind configs with BepInEx
            ModActive = Config.Bind(CompatibilityModName, "Enabled", true);
            ShowEnemyCrosshairs = Config.Bind(CompatibilityModName, "ShowEnemyCrosshairs", true, "When enabled, draw crosshairs for enemies as well as for your player.");
            MatchPlayerColor = Config.Bind(CompatibilityModName, "MatchPlayerColor", true, "When enabled, draw crosshairs as the same color as the player using them.");
            CrosshairControlType = Config.Bind(CompatibilityModName, "CrosshairControlType", 0, "0: No crosshair distance control\n1: Right stick button (R3) steps crosshair through three different ranges\n3: Right stick button (R3) smoothly move crosshair away, left stick button (L3) smoothly brings it back");
            ScaleSelfWithBulletSpeed = Config.Bind(CompatibilityModName, "ScaleSelfWithBulletSpeed", true, "When enabled, your crosshair's position will depend on your bullet speed stat.");
            ScaleEnemiesWithBulletSpeed = Config.Bind(CompatibilityModName, "ScaleEnemiesWithBulletSpeed", true, "When enabled, your enemies crosshairs' positions will depend on their bullet speed stats.");
            DistanceOffset = Config.Bind(CompatibilityModName, "DistanceOffset", 0f, "Offset of the player's default crosshair distance, can be negative, positive, or zero.");

            // apply patches
            new Harmony(ModId).PatchAll();
        }
        private void Start()
        {
            // load assets
            CrosshairForAll.Assets = AssetUtils.LoadAssetBundleFromResources("crosshairwhite", typeof(CrosshairForAll).Assembly);

            // OLD GUI
            //Unbound.RegisterGUI("Crosshair For All", new Action(this.DrawGUI));

            // add credits
            Unbound.RegisterCredits(ModName, new string[] { "Pykess (Code)", "Ascyst (Assets)" }, new string[] { "github", "Buy Pykess a coffee", "Buy Ascyst a coffee" }, new string[] { "https://github.com/Rounds-Modding/CrosshairForall", "https://www.buymeacoffee.com/Pykess", "https://www.buymeacoffee.com/Ascyst" });

            // add GUI to modoptions menu
            Unbound.RegisterMenu(ModName, () => { }, this.NewGUI, null, true);

            // register as client-side
            Unbound.RegisterClientSideMod(ModId);
        }
        private void NewGUI(GameObject menu)
        {
            MenuHandler.CreateText(ModName + " Options", menu, out TextMeshProUGUI _, 60);
            void ModActiveChanged(bool val)
            {
                ModActive.Value = val;
            }
            MenuHandler.CreateToggle(ModActive.Value, "Enabled", menu, ModActiveChanged, 30);
            void ShowEnemyCrosshairsChanged(bool val)
            {
                ShowEnemyCrosshairs.Value = val;
            }
            MenuHandler.CreateToggle(ShowEnemyCrosshairs.Value, "Show Other Players' Crosshairs", menu, ShowEnemyCrosshairsChanged, 30);
            void MatchPlayerColorChanged(bool val)
            {
                MatchPlayerColor.Value = val;
            }
            MenuHandler.CreateToggle(MatchPlayerColor.Value, "Match Crosshair Color to Player Color", menu, MatchPlayerColorChanged, 30);
            void ScaleSelfChanged(bool val)
            {
                ScaleSelfWithBulletSpeed.Value = val;
            }
            MenuHandler.CreateToggle(ScaleSelfWithBulletSpeed.Value, "Scale local players' crosshair distance with projectile speed", menu, ScaleSelfChanged, 30);
            void ScaleEnemiesChanged(bool val)
            {
                ScaleEnemiesWithBulletSpeed.Value = val;
            }
            MenuHandler.CreateToggle(ScaleEnemiesWithBulletSpeed.Value, "Scale other players' crosshair distance with projectile speed", menu, ScaleEnemiesChanged, 30);
            void ControlTypeChanged(int newControlType)
            {
                if (newControlType == 0)
                {
                    CrosshairControlType.Value = 0;
                }
                else if (newControlType == 1)
                {
                    CrosshairControlType.Value = 1;
                }
                else if (newControlType == 2)
                {
                    CrosshairControlType.Value = 2;
                }
                UpdateControlTypeGUI();
            }
            MenuHandler.CreateText("Control Type", menu, out TextMeshProUGUI _, 30);
            ControlTypeNone.Add(MenuHandler.CreateToggle(CrosshairControlType.Value == 0, "None", menu, b => ControlTypeChanged(b ? 0 : -1), 30).GetComponent<Toggle>());
            ControlTypeStepped.Add(MenuHandler.CreateToggle(CrosshairControlType.Value == 1, "Stepped", menu, b => ControlTypeChanged(b ? 1 : -2), 30).GetComponent<Toggle>());
            ControlTypeSmooth.Add(MenuHandler.CreateToggle(CrosshairControlType.Value == 2, "Smooth", menu, b => ControlTypeChanged(b ? 2 : -3), 30).GetComponent<Toggle>());
            void OffsetChanged(float val)
            {
                DistanceOffset.Value = val/10f;
            }
            MenuHandler.CreateSlider("Offset", menu, 30, -50f, 200f, DistanceOffset.Value, OffsetChanged, out Slider _, true);

        }
        private static void UpdateControlTypeGUI()
        {
            foreach (Toggle toggle in ControlTypeNone.Where(t => t != null))
            {
                toggle.isOn = CrosshairControlType.Value == 0;
            }
            foreach (Toggle toggle in ControlTypeStepped.Where(t => t != null))
            {
                toggle.isOn = CrosshairControlType.Value == 1;
            }
            foreach (Toggle toggle in ControlTypeSmooth.Where(t => t != null))
            {
                toggle.isOn = CrosshairControlType.Value == 2;
            }
        }
        private void DrawGUI()
        {
            bool newModActive = GUILayout.Toggle(ModActive.Value, "Enabled", Array.Empty<GUILayoutOption>());
            bool newShowenemyCrosshairs = GUILayout.Toggle(ShowEnemyCrosshairs.Value, "Show Enemy Crosshairs", Array.Empty<GUILayoutOption>());
            bool newMatchPlayerColor = GUILayout.Toggle(MatchPlayerColor.Value, "Match Player Color", Array.Empty<GUILayoutOption>());
            bool newScaleSelf = GUILayout.Toggle(ScaleSelfWithBulletSpeed.Value, "Scale Player Crosshair Distance\nWith Bullet Speed", Array.Empty<GUILayoutOption>());
            bool newScaleEnemy = GUILayout.Toggle(ScaleEnemiesWithBulletSpeed.Value, "Scale Enemy Crosshair Distance\nWith Bullet Speed", Array.Empty<GUILayoutOption>());
            GUILayout.Label("Control Type", Array.Empty<GUILayoutOption>());
            int newControlType = GUILayout.SelectionGrid(CrosshairControlType.Value, new string[] { "None", "Stepped", "Smooth" }, 3, Array.Empty<GUILayoutOption>());
            GUILayout.Label("Offset");
            float newOffset = GUILayout.HorizontalSlider(DistanceOffset.Value, -5f, 20f);
            GUILayout.Label(newOffset.ToString("N2"));

            ModActive.Value = newModActive;
            ShowEnemyCrosshairs.Value = newShowenemyCrosshairs;
            MatchPlayerColor.Value = newMatchPlayerColor;
            CrosshairControlType.Value = newControlType;
            ScaleSelfWithBulletSpeed.Value = newScaleSelf;
            ScaleEnemiesWithBulletSpeed.Value = newScaleEnemy;
            DistanceOffset.Value = newOffset;
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

    [Serializable]
    public class PlayerActionsAdditionalData
    {
        public PlayerAction AdjustCrosshairOut;
        public PlayerAction AdjustCrosshairIn;


        public PlayerActionsAdditionalData()
        {
            AdjustCrosshairOut = null;
            AdjustCrosshairIn = null;
        }
    }
    public static class GunExtension
    {
        public static readonly ConditionalWeakTable<PlayerActions, PlayerActionsAdditionalData> data =
            new ConditionalWeakTable<PlayerActions, PlayerActionsAdditionalData>();

        public static PlayerActionsAdditionalData GetAdditionalData(this PlayerActions playerActions)
        {
            return data.GetOrCreateValue(playerActions);
        }

        public static void AddData(this PlayerActions playerActions, PlayerActionsAdditionalData value)
        {
            try
            {
                data.Add(playerActions, value);
            }
            catch (Exception) { }
        }
    }
    // postfix PlayerActions constructor to add controls for the crosshair distance
    [HarmonyPatch(typeof(PlayerActions))]
    [HarmonyPatch(MethodType.Constructor)]
    [HarmonyPatch(new Type[] { })]
    class PlayerActionsPatchPlayerActions
    {
        private static void Postfix(PlayerActions __instance)
        {
            __instance.GetAdditionalData().AdjustCrosshairOut = (PlayerAction)typeof(PlayerActions).InvokeMember("CreatePlayerAction",
                                    BindingFlags.Instance | BindingFlags.InvokeMethod |
                                    BindingFlags.NonPublic, null, __instance, new object[] { "Adjust Crosshair Out" });
            __instance.GetAdditionalData().AdjustCrosshairIn = (PlayerAction)typeof(PlayerActions).InvokeMember("CreatePlayerAction",
                        BindingFlags.Instance | BindingFlags.InvokeMethod |
                        BindingFlags.NonPublic, null, __instance, new object[] { "Adjust Crosshair In" });
        }
    }
    // postfix PlayerActions to add controls for the crosshair distance
    [HarmonyPatch(typeof(PlayerActions), "CreateWithControllerBindings")]
    class PlayerActionsPatchCreateWithControllerBindings
    {
        private static void Postfix(ref PlayerActions __result)
        {
            __result.GetAdditionalData().AdjustCrosshairOut.AddDefaultBinding(InputControlType.RightStickButton);
            __result.GetAdditionalData().AdjustCrosshairIn.AddDefaultBinding(InputControlType.LeftStickButton);
        }
    }


    public class ControllerCrosshair : MonoBehaviour
    {
        private Player player = null;
        private GeneralInput input = null;
        private SpriteRenderer crosshairRenderer = null;
        private Gun gun = null;
        private GameObject crosshair;

        private float distance;
        private float smoothdistance;
        private readonly float defaultDistance = 5f;
        private readonly float[] ranges = new float[] { 3f, 5f, 10f, 15f };

        private int region = 1;

        private readonly float maxProjectileSpeed = 10f;
        private readonly float minProjectileSpeed = 1f;

        private readonly float changePerFrame = 0.1f;

        void Awake()
        {
        }

        void Start()
        {
            this.crosshair = this.gameObject;

            if (this.crosshair == null) { Destroy(this); }

            this.crosshairRenderer = this.crosshair.GetComponentInChildren<SpriteRenderer>();

            if (this.gun == null) { Destroy(this); }


            this.distance = this.defaultDistance;
            this.smoothdistance = this.defaultDistance;

            this.crosshair.transform.localPosition = new Vector3(0f, this.distance, 0f);

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

            // figure out what distance to render the crosshair at
            this.distance = 0f;


            // get the control type first
            if (this.input.controlledElseWhere)
            {
                // enemy player

                this.distance += this.defaultDistance;

                float multiplier = 0f;

                if (CrosshairForAll.ScaleEnemiesWithBulletSpeed.Value)
                {
                    multiplier = UnityEngine.Mathf.Clamp((this.gun.projectileSpeed - this.minProjectileSpeed) / (this.maxProjectileSpeed - this.minProjectileSpeed), 0f, 1f);
                }

                this.distance += (this.ranges[this.ranges.Length - 1]-this.defaultDistance)*multiplier;

            }
            else if (CrosshairForAll.CrosshairControlType.Value == 0)
            {
                // no control, fixed crosshair
                this.distance += this.defaultDistance;

                float multiplier = 0f;
                if (CrosshairForAll.ScaleSelfWithBulletSpeed.Value)
                {
                    multiplier = UnityEngine.Mathf.Clamp((this.gun.projectileSpeed - this.minProjectileSpeed) / (this.maxProjectileSpeed - this.minProjectileSpeed), 0f, 1f);
                }

                this.distance += (this.ranges[this.ranges.Length - 1] - this.defaultDistance) * multiplier;

                this.distance += CrosshairForAll.DistanceOffset.Value;

            }
            else if (CrosshairForAll.CrosshairControlType.Value == 1)
            {
                // three discrete regions
                if (this.player.data.playerActions.GetAdditionalData().AdjustCrosshairOut.WasPressed)
                {
                    this.region = (this.region + 1) % (this.ranges.Length - 1);
                }

                float min = this.ranges[this.region];
                float max = this.ranges[this.region+1];

                float multiplier = 0f;

                if (CrosshairForAll.ScaleSelfWithBulletSpeed.Value)
                {
                    multiplier = UnityEngine.Mathf.Clamp((this.gun.projectileSpeed-this.minProjectileSpeed)/(this.maxProjectileSpeed-this.minProjectileSpeed), 0f, 1f);
                }

                this.distance += min + (max - min) * multiplier;

                this.distance += CrosshairForAll.DistanceOffset.Value;

            }
            else if (CrosshairForAll.CrosshairControlType.Value == 2)
            {
                // continuous adjustment
                if (this.player.data.playerActions.GetAdditionalData().AdjustCrosshairOut.IsPressed)
                {
                    this.smoothdistance += this.changePerFrame;
                }
                if (this.player.data.playerActions.GetAdditionalData().AdjustCrosshairIn.IsPressed)
                {
                    this.smoothdistance -= this.changePerFrame;
                }
                float multiplier = 0f;
                if (CrosshairForAll.ScaleSelfWithBulletSpeed.Value)
                {
                    multiplier = UnityEngine.Mathf.Clamp01((this.gun.projectileSpeed - this.minProjectileSpeed) / (this.maxProjectileSpeed - this.minProjectileSpeed));
                }

                this.smoothdistance = UnityEngine.Mathf.Clamp(this.smoothdistance, this.ranges[0], this.ranges[this.ranges.Length - 1]);

                float extra = (this.ranges[this.ranges.Length - 1] - this.smoothdistance - CrosshairForAll.DistanceOffset.Value);

                this.distance = this.smoothdistance + CrosshairForAll.DistanceOffset.Value + extra * multiplier;
            }

            this.distance = UnityEngine.Mathf.Clamp(this.distance, this.ranges[0], this.ranges[this.ranges.Length - 1]);

            this.crosshair.transform.localPosition = new Vector3(0f, this.distance, 0f);

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
    public class GetPlayerColor
    {
        public static Color GetColorMax(Player player)
        {
            if (player.gameObject.GetComponentInChildren<PlayerSkinHandler>().simpleSkin)
            {
                return GetSimpleColor(player);
            }

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
            if (player.gameObject.GetComponentInChildren<PlayerSkinHandler>().simpleSkin)
            {
                return GetSimpleColor(player);
            }

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

        public static Color GetSimpleColor(Player player)
        {
            return player.gameObject.GetComponentInChildren<SetPlayerSpriteLayer>().transform.root.GetComponentInChildren<SpriteMask>().GetComponent<SpriteRenderer>().color;
        }
    }

}
