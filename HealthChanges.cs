using System;
using UnityEngine;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;

using static TPDespair.StatAdjustment.StatAdjustmentPlugin;

namespace TPDespair.StatAdjustment
{
	public static class HealthChanges
	{
		internal static void LateSetup()
		{
			if (HealthChangesEnable.Value)
			{
				HealthHook();
			}
		}



		private static void HealthHook()
		{
			IL.RoR2.CharacterBody.RecalculateStats += (il) =>
			{
				ILCursor c = new ILCursor(il);

				const int baseValue = 62;
				const int multValue = 63;

				bool found = c.TryGotoNext(
					x => x.MatchLdloc(baseValue),
					x => x.MatchLdloc(multValue),
					x => x.MatchMul(),
					x => x.MatchStloc(baseValue)
				);

				if (found)
				{
					// add
					c.Emit(OpCodes.Ldarg, 0);
					c.Emit(OpCodes.Ldloc, baseValue);
					c.EmitDelegate<Func<CharacterBody, float, float>>((self, value) =>
					{
						if (!self.isPlayerControlled && self.teamComponent.teamIndex != TeamIndex.Player) return value;

						float addedHealth = 0f;
						float targetBaseHealth;
						float targetLevelHealth;

						if (BaseHealthRatio.Value <= 0f)
						{
							targetBaseHealth = self.baseMaxHealth;
						}
						else if (BaseHealthRatio.Value >= 1f)
						{
							targetBaseHealth = self.levelMaxHealth * BaseHealthTargetFactor.Value;
						}
						else
						{
							targetBaseHealth = Mathf.Lerp(self.baseMaxHealth, self.levelMaxHealth * BaseHealthTargetFactor.Value, BaseHealthRatio.Value);
						}

						if (BaseHealthLimiter.Value) targetBaseHealth = Mathf.Max(self.baseMaxHealth, targetBaseHealth);
						targetBaseHealth = Mathf.Max(MinBaseHealth.Value, targetBaseHealth);
						targetBaseHealth = StepCeil(targetBaseHealth, CeilBaseHealth.Value);
						addedHealth += targetBaseHealth - self.baseMaxHealth;

						targetLevelHealth = targetBaseHealth * FactorLevelHealth.Value;
						addedHealth += (targetLevelHealth - self.levelMaxHealth) * (self.level - 1f);

						return value + Mathf.Round(addedHealth);
					});
					c.Emit(OpCodes.Stloc, baseValue);
				}
				else
				{
					LogWarn("HealthHook Failed!");
				}
			};
		}

		private static float StepCeil(float value, float step)
		{
			return Mathf.Ceil((value - 0.1f) / step) * step;
		}
	}
}
