using System;
using UnityEngine;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;

using static TPDespair.StatAdjustment.StatAdjustmentPlugin;

namespace TPDespair.StatAdjustment
{
	public static class BarrierChanges
	{
		public static bool SlowedBarrierEnabled = false;
		public static bool DynamicBarrierEnabled = false;

		public static bool DefNucSlow = false;

		public static BuffIndex ClayCatalystBuff = BuffIndex.None;
		public static BuffIndex BoneVisorBuff = BuffIndex.None;

		public static ItemIndex MechSnailItem;



		internal static void LateSetup()
		{
			if (BarrierChangesEnable.Value)
			{
				bool disableBarrierChanges = false;
				bool disableDynamicBarrier = false;

				if (AutoCompatEnable.Value)
				{
					if (PluginLoaded("com.Borbo.BORBO")) disableBarrierChanges = true;
					if (PluginLoaded("com.zombieseatflesh7.dynamicbarrierdecay")) disableDynamicBarrier = true;
				}

				if (!disableBarrierChanges)
				{
					if (DynamicBarrier.Value && !disableDynamicBarrier) DynamicBarrierEnabled = true;

					if (BarrierSlow.Value)
					{
						SlowedBarrierEnabled = true;

						GatherIndexes();
					}

					if (DynamicBarrierEnabled || SlowedBarrierEnabled)
					{
						BarrierDecayHook();
					}
				}
			}
		}



		private static void GatherIndexes()
		{
			BuffIndex buffIndex = BuffCatalog.FindBuffIndex("BuffDefClayCatalyst");
			if (buffIndex != BuffIndex.None) ClayCatalystBuff = buffIndex;
			buffIndex = BuffCatalog.FindBuffIndex("BuffDefBoneVisor");
			if (buffIndex != BuffIndex.None) BoneVisorBuff = buffIndex;

			ItemIndex itemIndex = ItemCatalog.FindItemIndex("ItemDefDecreaseBarrierDecay");
			if (itemIndex != ItemIndex.None) MechSnailItem = itemIndex;
		}



		private static void BarrierDecayHook()
		{
			IL.RoR2.HealthComponent.ServerFixedUpdate += (il) =>
			{
				ILCursor c = new ILCursor(il);

				bool found = c.TryGotoNext(
					x => x.MatchCallvirt<CharacterBody>("get_barrierDecayRate")
				);

				if (found)
				{
					c.Index += 1;

					c.Emit(OpCodes.Ldarg, 0);
					c.EmitDelegate<Func<float, HealthComponent, float>>((rate, self) =>
					{
						if (self.body)
						{
							if (DynamicBarrierEnabled)
							{
								rate *= GetDynamicBarrierDecayMult(self.body);
							}

							if (SlowedBarrierEnabled)
							{
								rate *= GetSlowedBarrierDecayMult(self.body);
							}
						}

						return rate;
					});
				}
				else
				{
					LogWarn("BarrierDecayHook:DecayMult Failed!");
				}

				found = c.TryGotoNext(
					x => x.MatchCall<HealthComponent>("set_Networkbarrier")
				);

				if (found)
				{
					c.Emit(OpCodes.Ldarg, 0);
					c.EmitDelegate<Func<float, HealthComponent, float>>((decayed, self) =>
					{
						if (SlowedBarrierEnabled)
						{
							if (self.body && (GetSlowedBarrierDecayMult(self.body) <= 0.99f || HasExternalSlow(self.body)))
							{
								float limit = self.fullBarrier * BarrierSlowStop.Value;

								if (self.barrier > limit)
								{
									return Mathf.Max(limit, decayed);
								}
								else
								{
									return self.barrier;
								}
							}
						}

						return decayed;
					});
				}
				else
				{
					LogWarn("BarrierDecayHook:DecayStop Failed!");
				}
			};
		}

		private static float GetDynamicBarrierDecayMult(CharacterBody body)
		{
			HealthComponent healthComponent = body.healthComponent;
			float barrierFraction = healthComponent.barrier / healthComponent.fullBarrier;

			return Mathf.Clamp(barrierFraction * 2f, 0.5f, 2f);
		}

		private static float GetSlowedBarrierDecayMult(CharacterBody body)
		{
			float mult = 1f;

			Inventory inventory = body.inventory;
			if (inventory)
			{
				if (inventory.GetItemCount(RoR2Content.Items.BarrierOnOverHeal) > 0) mult *= 1f - AegisSlowMult.Value;
			}

			return mult;
		}

		private static bool HasExternalSlow(CharacterBody body)
		{
			if (ClayCatalystBuff != BuffIndex.None && body.HasBuff(ClayCatalystBuff)) return true;
			if (BoneVisorBuff != BuffIndex.None && body.HasBuff(BoneVisorBuff)) return true;

			Inventory inventory = body.inventory;
			if (inventory)
			{
				if (DefNucSlow && inventory.GetItemCount(DLC1Content.Items.MinorConstructOnKill) > 0) return true;

				if (MechSnailItem != ItemIndex.None && inventory.GetItemCount(MechSnailItem) > 0) return true;
			}

			return false;
		}
	}
}
