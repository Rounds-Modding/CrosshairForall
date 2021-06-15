using System;
using BepInEx;
using UnboundLib;
using HarmonyLib;
using UnityEngine;

namespace CrosshairForAll 
{
    [BepInDependency("com.willis.rounds.unbound", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin("pykess-and-ascyst.rounds.plugins.crosshairforall", "Crosshair For All", "0.0.0.0")]
    [BepInProcess("Rounds.exe")]
    public class CrosshairForAll : BaseUnityPlugin
    {
    }
}
