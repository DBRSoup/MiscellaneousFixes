using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
namespace MiscFixes;

[MycoMod(null, ModFlags.IsClientSide)]
[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;

    public const string PLUGIN_GUID = "DBR.MiscFixes";
    public const string PLUGIN_NAME = "MiscFixes";
    public const string PLUGIN_VERSION = "1.5.2";

    private void Awake()
    {


        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {PLUGIN_GUID} is loaded");

        Harmony harmony = new(PLUGIN_GUID);
        try
        {
            harmony.PatchAll();
        }
        catch (Exception ex)
        {
            Logger.LogError((object)("Error applying patches: " + ex.Message));
        }
    }

    #region Voltaic
    [HarmonyPatch(typeof(VoltaicGrenade), "Update")]
    public static class VoltaicGrenadeUpdatePatch
    {
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);

            FieldInfo grenadeDataField = AccessTools.Field( typeof(VoltaicGrenade), "grenadeData" );
            FieldInfo igniteField = AccessTools.Field( grenadeDataField.FieldType, "igniteExplosionDamage" );

            //for (int i = 0; i < codes.Count; i++)
            //{
            //    Logger.LogInfo($"[Live Wire] {i:D3}: {codes[i]}");
            //}


            for (int i = 0; i < codes.Count; i++)
            {
                if ( !codes[i].LoadsField(igniteField) )
                    continue;

                codes[i - 3] = new CodeInstruction( OpCodes.Ldflda, grenadeDataField );

                codes.RemoveRange(i - 2, 2);

                Logger.LogInfo("[Live Wire] Successfully replaced igniteExplosionDamage access");

                break;
            }

            return codes;
        }
    }
    #endregion

    #region Modular Construction
    [HarmonyPatch(typeof(UpgradeProperty_ThrowableCooldown), nameof(UpgradeProperty_ThrowableCooldown.GetStatData))]
    public static class ShieldBlocksDisplayFix
    {
        private static bool Prefix(UpgradeProperty_ThrowableCooldown __instance, UpgradeInstance upgrade, ref IEnumerator<StatData> __result)
        {
            if (upgrade?.Upgrade?.name == "ShieldBlocks")
            {
                __result = GetStatData(__instance, UpgradeProperty.GetPatternSizeRatio(upgrade));
                return false;
            }
            return true;

        }
    }
    private static IEnumerator<StatData> GetStatData(UpgradeProperty_ThrowableCooldown instance, float ratio)
    {
        yield return StatData.Create(TextBlocks.GetString("recharge_s"), GetAdjustedValue(instance.rechargeDuration.data.max, ratio), OverrideType.Multiply).WithPatternRatio(ratio, false);

        yield return StatData.Create(TextBlocks.GetString("recharge_max"), instance.maxCharges.data.max, OverrideType.Add);
    }

    [HarmonyPatch(typeof(UpgradeProperty_ThrowableCooldown), nameof(UpgradeProperty_ThrowableCooldown.Apply))]
    public static class ShieldBlocksApplyFix
    {
        private static bool Prefix(UpgradeProperty_ThrowableCooldown __instance, UpgradeInstance upgrade, IGear gear)
        {
            if (upgrade?.Upgrade?.name == "ShieldBlocks")
            {
                if (gear is IActivatedAbility activatedAbility)
                {
                    ref CooldownData cooldownData = ref activatedAbility.CooldownData;

                    cooldownData.rechargeDuration *= 1f / GetAdjustedValue(__instance.rechargeDuration.data.max, UpgradeProperty.GetPatternSizeRatio(upgrade));
                    cooldownData.maxCharges += (int)__instance.maxCharges.data.max;
                }
                return false;
            }

            return true;

        }
    }

    private static float GetAdjustedValue(float value, float ratio)
    {
        return 1f + (1f / value - 1f) * ratio;
    }

    #endregion

    #region Cannonball
    [HarmonyPatch(typeof(NoseDiveUpgrade), nameof(NoseDiveUpgrade.TriggerNoseDive))]
    public static class CannonballPatch
    {
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);

            for (int i = 0; i < codes.Count - 3; i++)
            {
                // potency (argument 3)
                if (codes[i].opcode == OpCodes.Ldarg_3 &&
                    codes[i + 1].opcode == OpCodes.Ldarg_S &&
                    codes[i + 2].opcode == OpCodes.Mul &&
                    codes[i + 3].opcode == OpCodes.Mul)
                {
                    codes.RemoveAt(i + 2);
                    codes.RemoveAt(i);

                    Logger.LogInfo("[Cannonball] Successfully removed damage potency multiplier");

                    break;
                }
            }

            return codes;
        }
    }
    #endregion

    #region Accelerator
    [HarmonyPatch(typeof(AcceleratorGun), "OnBurstFinished")]
    public static class RocketDamagePatch
    {
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);

            for (int i = 0; i < codes.Count - 4; i++)
            {
                if (codes[i].opcode == OpCodes.Ldarg_0 &&
                    codes[i + 1].opcode == OpCodes.Ldflda &&
                    codes[i + 2].opcode == OpCodes.Ldfld &&
                    codes[i + 3].opcode == OpCodes.Conv_R4 &&
                    codes[i + 4].opcode == OpCodes.Div)
                {
                    codes.RemoveRange(i, 5);
                    Logger.LogInfo("[Oxythane's Promise] Successfully removed damage bullet division");
                    break;
                }
            }

            for (int i = 0; i < codes.Count - 4; i++)
            {
                if (codes[i].opcode == OpCodes.Ldarg_0 &&
                    codes[i + 1].opcode == OpCodes.Ldflda &&
                    codes[i + 2].opcode == OpCodes.Ldfld &&
                    codes[i + 3].opcode == OpCodes.Conv_R4 &&
                    codes[i + 4].opcode == OpCodes.Div)
                {
                    codes.RemoveRange(i, 5);
                    Logger.LogInfo("[Oxythane's Promise] Successfully removed range bullet division");
                    break;
                }
            }

            return codes;
        }
    }
    #endregion

}
