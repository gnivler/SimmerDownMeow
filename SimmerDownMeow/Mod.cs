using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using Module = TaleWorlds.MountAndBlade.Module;

// ReSharper disable InconsistentNaming

namespace SimmerDownMeow
{
    public class Mod : MBSubModuleBase
    {
        private readonly Harmony harmony = new Harmony("ca.gnivler.bannerlord.SimmerDownMeow");

        private static void Log(object input)
        {
            //FileLog.Log($"[SimmerDownMeow] {input ?? "null"}");
        }

        protected override void OnSubModuleLoad()
        {
            //Harmony.DEBUG = true;
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            //Harmony.DEBUG = false;
        }

        [HarmonyPatch(typeof(AgentVictoryLogic), "CheckAnimationAndVoice")]
        public static class AgentVictoryLogicCheckAnimationAndVoicePatch
        {
            private const int limit = 1;
            private static readonly Dictionary<Agent, int> yells = new Dictionary<Agent, int>();

            private static bool YelledEnough(Agent agent)
            {
                if (!yells.ContainsKey(agent))
                {
                    yells.Add(agent, 0);
                }

                Log($"{agent.Name}-{agent.Index} ({yells[agent]})");
                if (yells[agent] <= limit)
                {
                    Log($"Returning {yells[agent]}++ >= {limit}");
                }

                return yells[agent]++ >= limit;
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGenerator)
            {
                var codes = instructions.ToList();
                var target = codes.FindIndex(c => c.opcode == OpCodes.Callvirt &&
                                                  (MethodInfo) c.operand == AccessTools.Method(typeof(VictoryComponent), "CheckTimer"));

                target += 2;
                var helper = AccessTools.Method(typeof(AgentVictoryLogicCheckAnimationAndVoicePatch), nameof(YelledEnough), new[] {typeof(Agent)});
                Log($"TARGET CODE: {codes[target]}");
                var label = ilGenerator.DefineLabel();
                // label the final return
                codes[codes.Count - 1].labels.Add(label);

                var stack = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldloc_1), // agent
                    new CodeInstruction(OpCodes.Call, helper), // bool YelledEnough(agent)
                    new CodeInstruction(OpCodes.Brtrue, label) // stop yelling after threshold met
                };

                codes.InsertRange(target, stack);
                return codes.AsEnumerable();
            }
        }
    }
}
