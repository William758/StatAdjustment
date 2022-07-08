using System;
using UnityEngine;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;

using static TPDespair.StatAdjustment.StatAdjustmentPlugin;

namespace TPDespair.StatAdjustment
{
	public static class RegenChanges
	{
		public static void LateSetup()
		{
			if (RegenChangesEnable.Value)
			{
				BaseRegenHook();
				RegenScalingHook();
				BurningRegenHook();
			}
		}



		private static void BaseRegenHook()
		{
			IL.RoR2.CharacterBody.RecalculateStats += (il) =>
			{
				ILCursor c = new ILCursor(il);

				const int knurlValue = 67;
				const int multValue = 72;

				bool found = c.TryGotoNext(
					x => x.MatchLdcR4(1f),
					x => x.MatchStloc(multValue)
				);

				if (found)
				{
					c.Emit(OpCodes.Ldarg, 0);
					c.Emit(OpCodes.Ldloc, knurlValue);
					c.EmitDelegate<Func<CharacterBody, float, float>>((self, value) =>
					{
						if (!self.isPlayerControlled && self.teamComponent.teamIndex != TeamIndex.Player) return value;

						float addedRegen = 0f;

						if (self.baseRegen > 0f) addedRegen += self.baseRegen * (MultBaseRegen.Value - 1f);
						if (self.levelRegen > 0f) addedRegen += self.levelRegen * (MultLevelRegen.Value - 1f) * (self.level - 1f);

						return value + addedRegen;
					});
					c.Emit(OpCodes.Stloc, knurlValue);
				}
				else
				{
					Debug.LogWarning("BaseRegenHook Failed!");
				}
			};
		}

		private static void RegenScalingHook()
		{
			IL.RoR2.CharacterBody.RecalculateStats += (il) =>
			{
				ILCursor c = new ILCursor(il);

				bool found = c.TryGotoNext(
					x => x.MatchLdcR4(1f),
					x => x.MatchLdloc(53),
					x => x.MatchLdcR4(0.2f),
					x => x.MatchMul(),
					x => x.MatchAdd(),
					x => x.MatchStloc(66)
				);

				if (found)
				{
					c.Index += 3;

					c.Emit(OpCodes.Pop);
					c.Emit(OpCodes.Ldarg, 0);
					c.EmitDelegate<Func<CharacterBody, float>>((self) =>
					{
						if (!self.isPlayerControlled && self.teamComponent.teamIndex != TeamIndex.Player) return 0.2f;
						return ScaleRegenMult.Value;
					});
				}
				else
				{
					Debug.LogWarning("RegenScalingHook Failed!");
				}
			};
		}

		private static void BurningRegenHook()
		{
			IL.RoR2.CharacterBody.RecalculateStats += (il) =>
			{
				ILCursor c = new ILCursor(il);

				const int combinedFlatRegenValue = 73;

				bool found = c.TryGotoNext(
					x => x.MatchLdcR4(0f),
					x => x.MatchLdloc(combinedFlatRegenValue),
					x => x.MatchCall<Mathf>("Min"),
					x => x.MatchStloc(combinedFlatRegenValue)
				);

				if (found)
				{
					int indexOfMatch = c.Index + 1;

					found = c.TryGotoNext(
						x => x.MatchCallOrCallvirt<CharacterBody>("get_cursePenalty")
					);

					if (found)
					{
						int indexOfCurse = c.Index;

						if (indexOfCurse - 12 > indexOfMatch)
						{
							c.Index = indexOfMatch;

							c.Emit(OpCodes.Pop);
							c.Emit(OpCodes.Ldloc, combinedFlatRegenValue);
							c.EmitDelegate<Func<float>>(() =>
							{
								return Mathf.Max(0f, BurningRegenMult.Value);
							});
							c.Emit(OpCodes.Mul);
						}
						else
						{
							LogWarn("BurningRegenHook Failed! : get_cursePenalty Too Close!");
						}
					}
					else
					{
						LogWarn("BurningRegenHook Failed! : get_cursePenalty Not Found!");
					}
				}
				else
				{
					LogWarn("BurningRegenHook Failed!");
				}
			};
		}
	}
}
