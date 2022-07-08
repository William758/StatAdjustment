using System;
using UnityEngine;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;

using static TPDespair.StatAdjustment.StatAdjustmentPlugin;

namespace TPDespair.StatAdjustment
{
	public static class DamageChanges
	{
		internal static void LateSetup()
		{
			if (DamageChangesEnable.Value)
			{
				CritHook();
				DamageHook();
			}
		}



		private static void CritHook()
		{
			On.RoR2.CharacterBody.RecalculateStats += (orig, self) =>
			{
				self.baseCrit = Mathf.Max(self.baseCrit, BaseMinCrit.Value);

				orig(self);
			};
		}

		private static void DamageHook()
		{
			IL.RoR2.CharacterBody.RecalculateStats += (il) =>
			{
				ILCursor c = new ILCursor(il);

				const int baseValue = 78;
				const int multValue = 79;

				bool found = c.TryGotoNext(
					x => x.MatchLdloc(baseValue),
					x => x.MatchLdloc(multValue),
					x => x.MatchMul(),
					x => x.MatchStloc(baseValue)
				);

				if (found)
				{
					c.Index += 4;

					// multiplier
					c.Emit(OpCodes.Ldarg, 0);
					c.Emit(OpCodes.Ldloc, baseValue);
					c.EmitDelegate<Func<CharacterBody, float, float>>((self, value) =>
					{
						if (self.teamComponent.teamIndex != TeamIndex.Player)
						{
							value *= Mathf.Max(0.1f, Mathf.Abs(MonsterDamage.Value));
						}

						return value;
					});
					c.Emit(OpCodes.Stloc, baseValue);
				}
				else
				{
					LogWarn("DamageHook Failed!");
				}
			};
		}
	}
}
