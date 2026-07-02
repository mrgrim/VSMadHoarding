using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using PurposefulStorage;
using Vintagestory.API.Common;

namespace MadHoarding.Patches;

[HarmonyPatch]
public static class ResourceBinPatch
{
    internal static IEnumerable<MethodBase> TargetMethods ()
    {
        var slotDelegateLambda = typeof(BEResourceBin)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(m =>
                Attribute.IsDefined(m, typeof(CompilerGeneratedAttribute), true)
                && m.Name.Contains("ctor")
                && m.GetParameters().Select(p => p.ParameterType).SequenceEqual([typeof(int), typeof(InventoryGeneric)])
                && m.ReturnType == typeof(ItemSlot))
            .SingleOrDefault((MethodInfo?)null)
            ?? throw new InvalidOperationException("[Mad Hoarding] Could not find resource bins new slot delegate lambda.");
        
        yield return slotDelegateLambda;
        yield return AccessTools.Method(typeof(BEResourceBin), nameof(BEResourceBin.OnTesselation));
        yield return AccessTools.Method(typeof(BEResourceBin), "genTransformationMatrices");
    }
    
    internal static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions);

        matcher.MatchStartForward(new CodeMatch(OpCodes.Ldc_I4_8));
        matcher.SetInstruction(new CodeInstruction(OpCodes.Ldc_I4_S, 24));

        return matcher.Instructions();
    }
}