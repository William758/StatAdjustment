using System;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;

using static TPDespair.StatAdjustment.StatAdjustmentPlugin;

namespace TPDespair.StatAdjustment
{
	public static class MobilityChanges
	{
		internal static void LateSetup()
		{
			if (MobilityChangesEnable.Value)
			{
				JumpHook();
				MovespeedHook();
			}
		}



		private static void JumpHook()
		{
			IL.RoR2.CharacterBody.RecalculateStats += (il) =>
			{
				ILCursor c = new ILCursor(il);

				bool found = c.TryGotoNext(
					x => x.MatchLdarg(0),
					x => x.MatchLdarg(0),
					x => x.MatchLdfld<CharacterBody>("baseJumpCount"),
					x => x.MatchLdloc(8)
				);

				if (found)
				{
					c.Emit(OpCodes.Ldarg, 0);
					c.Emit(OpCodes.Ldloc, 8);
					c.EmitDelegate<Func<CharacterBody, int, int>>((self, value) =>
					{
						if (self.teamComponent.teamIndex == TeamIndex.Player) value += ExtraPlayerJump.Value;

						return value;
					});
					c.Emit(OpCodes.Stloc, 8);
				}
				else
				{
					LogWarn("JumpHook Failed!");
				}
			};
		}

		private static void MovespeedHook()
		{
			IL.RoR2.CharacterBody.RecalculateStats += (il) =>
			{
				ILCursor c = new ILCursor(il);

				const int baseValue = 74;
				const int multValue = 75;
				const int divValue = 76;

				bool found = c.TryGotoNext(
					x => x.MatchLdloc(baseValue),
					x => x.MatchLdloc(multValue),
					x => x.MatchLdloc(divValue),
					x => x.MatchDiv(),
					x => x.MatchMul(),
					x => x.MatchStloc(baseValue)
				);

				if (found)
				{
					c.Index += 1;

					c.Emit(OpCodes.Pop);

					// increase
					c.Emit(OpCodes.Ldarg, 0);
					c.Emit(OpCodes.Ldloc, multValue);
					c.EmitDelegate<Func<CharacterBody, float, float>>((self, value) =>
					{
						if (self.teamComponent.teamIndex == TeamIndex.Player) value += ExtraMovespeedPlayer.Value;
						else value += ExtraMovespeedMonster.Value;

						return value;
					});
					c.Emit(OpCodes.Stloc, multValue);

					c.Emit(OpCodes.Ldloc, baseValue);
				}
				else
				{
					LogWarn("MovespeedHook Failed!");
				}
			};
		}
	}
}
