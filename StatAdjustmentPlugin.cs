using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using RoR2;

using System.Security;
using System.Security.Permissions;

[module: UnverifiableCode]
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete

namespace TPDespair.StatAdjustment
{
	[BepInPlugin(ModGuid, ModName, ModVer)]

	public class StatAdjustmentPlugin : BaseUnityPlugin
	{
		public const string ModVer = "1.1.2";
		public const string ModName = "StatAdjustment";
		public const string ModGuid = "com.TPDespair.StatAdjustment";



		public static ManualLogSource logSource;



		public static ConfigEntry<bool> AutoCompatEnable { get; set; }

		public static ConfigEntry<bool> DamageChangesEnable { get; set; }
		public static ConfigEntry<float> BaseMinCrit { get; set; }
		public static ConfigEntry<float> MonsterDamage { get; set; }

		public static ConfigEntry<bool> MobilityChangesEnable { get; set; }
		public static ConfigEntry<float> ExtraMovespeedPlayer { get; set; }
		public static ConfigEntry<float> ExtraMovespeedMonster { get; set; }
		public static ConfigEntry<int> ExtraPlayerJump { get; set; }

		public static ConfigEntry<bool> HealthChangesEnable { get; set; }
		public static ConfigEntry<bool> BaseHealthLimiter { get; set; }
		public static ConfigEntry<float> BaseHealthRatio { get; set; }
		public static ConfigEntry<float> BaseHealthTargetFactor { get; set; }
		public static ConfigEntry<float> MinBaseHealth { get; set; }
		public static ConfigEntry<float> CeilBaseHealth { get; set; }
		public static ConfigEntry<float> FactorLevelHealth { get; set; }

		public static ConfigEntry<bool> RegenChangesEnable { get; set; }
		public static ConfigEntry<float> MultBaseRegen { get; set; }
		public static ConfigEntry<float> MultLevelRegen { get; set; }
		public static ConfigEntry<float> ScaleRegenMult { get; set; }
		public static ConfigEntry<float> BurningRegenMult { get; set; }

		public static ConfigEntry<bool> BarrierChangesEnable { get; set; }
		public static ConfigEntry<bool> DynamicBarrier { get; set; }
		public static ConfigEntry<bool> BarrierSlow { get; set; }
		public static ConfigEntry<float> BarrierSlowStop { get; set; }
		public static ConfigEntry<float> AegisSlowMult { get; set; }



		public void Awake()
		{
			RoR2Application.isModded = true;
			NetworkModCompatibilityHelper.networkModList = NetworkModCompatibilityHelper.networkModList.Append(ModGuid + ":" + ModVer);

			logSource = Logger;
			SetupConfig(Config);

			RoR2Application.onLoad += LateSetup;
		}



		private static void LateSetup()
		{
			DamageChanges.LateSetup();
			MobilityChanges.LateSetup();
			HealthChanges.LateSetup();
			RegenChanges.LateSetup();
			BarrierChanges.LateSetup();
		}



		private static void SetupConfig(ConfigFile Config)
		{
			AutoCompatEnable = Config.Bind(
				"0-Stat - Compatibility", "enableAutoCompat", true,
				"Enable Automatic Compatibility. Changes settings based on other installed mods."
			);

			DamageChangesEnable = Config.Bind(
				"1-Stat - Damage", "damageChanges", true,
				"Enable or disable damage changes."
			);
			BaseMinCrit = Config.Bind(
				"1-Stat - Damage", "baseCritChance", 5f,
				"Set minimum base critical strike chance for all entities. Vanilla is 1"
			);
			MonsterDamage = Config.Bind(
				"1-Stat - Damage", "monsterDamageMult", 1f,
				"Multiply damage stat of non-players."
			);

			MobilityChangesEnable = Config.Bind(
				"2-Stat - Mobility", "mobilityChanges", false,
				"Enable or disable mobility changes."
			);
			ExtraMovespeedPlayer = Config.Bind(
				"2-Stat - Mobility", "baseExtraMovespeedPlayer", 0.1f,
				"Increase movement speed for players. 0.1 = +10%."
			);
			ExtraMovespeedMonster = Config.Bind(
				"2-Stat - Mobility", "baseExtraMovespeedMonster", 0.1f,
				"Increase movement speed for non-players. 0.1 = +10%."
			);
			ExtraPlayerJump = Config.Bind(
				"2-Stat - Mobility", "baseExtraJump", 1,
				"Extra jumps for players."
			);

			HealthChangesEnable = Config.Bind(
				"3-Stat - Health", "healthChanges", true,
				"Enable or disable health changes."
			);
			BaseHealthLimiter = Config.Bind(
				"3-Stat - Health", "baseHealthLimiter", true,
				"Prevent base health from being reduced by other settings."
			);
			BaseHealthRatio = Config.Bind(
				"3-Stat - Health", "baseHealthRatio", 0.35f,
				"Controls how much base health and level health influence new base health. 0 : baseHealth = baseHealth. 1 : baseHealth = levelHealth * baseHealthTargetFactor."
			);
			BaseHealthTargetFactor = Config.Bind(
				"3-Stat - Health", "baseHealthTargetFactor", 3.33333f,
				"BaseHealth target ratio factor. Used by baseHealthRatio. Target base health based on level health growth."
			);
			MinBaseHealth = Config.Bind(
				"3-Stat - Health", "baseHealthMinimum", 120f,
				"Minimum player base health."
			);
			CeilBaseHealth = Config.Bind(
				"3-Stat - Health", "baseHealthCeiling", 15f,
				"Round player base health up to nearest multiple."
			);
			FactorLevelHealth = Config.Bind(
				"3-Stat - Health", "levelHealthFactor", 0.333333f,
				"Player level health derived from base health. Vanilla is 0.3"
			);

			RegenChangesEnable = Config.Bind(
				"4-Stat - Regeneration", "regenChanges", true,
				"Enable or disable regen changes."
			);
			MultBaseRegen = Config.Bind(
				"4-Stat - Regeneration", "baseRegenMult", 2f,
				"Player base regen multiplier."
			);
			MultLevelRegen = Config.Bind(
				"4-Stat - Regeneration", "levelRegenMult", 2.5f,
				"Player level regen multiplier."
			);
			ScaleRegenMult = Config.Bind(
				"4-Stat - Regeneration", "scaleRegenFromLevel", 0.1f,
				"Set player regen increase from levels. 0.1 = +100% every 10 levels. Vanilla is 0.2 = +100% every 5 levels."
			);
			BurningRegenMult = Config.Bind(
				"4-Stat - Regeneration", "burningRegenMult", 0.5f,
				"Burning regen multiplier. Vanilla is 0"
			);

			BarrierChangesEnable = Config.Bind(
				"5-Stat - Barrier", "barrierChanges", true,
				"Enable or disable barrier changes."
			);
			DynamicBarrier = Config.Bind(
				"5-Stat - Barrier", "dynamicBarrierDecay", true,
				"Barrier decays based off current barrier. 2x when full. 0.5x at 25%."
			);
			BarrierSlow = Config.Bind(
				"5-Stat - Barrier", "barrierSlow", true,
				"Allows items and buffs to slow barrier decay."
			);
			BarrierSlowStop = Config.Bind(
				"5-Stat - Barrier", "barrierSlowedStop", 0.1f,
				"Barrier stops decaying at barrier fraction if decay is slowed."
			);
			AegisSlowMult = Config.Bind(
				"5-Stat - Barrier", "aegisBarrierSlow", 0.3f,
				"Slow barrier decay when entity has Aegis."
			);
		}



		internal static void LogWarn(object data)
		{
			logSource.LogWarning(data);
		}

		internal static bool PluginLoaded(string key)
		{
			return BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(key);
		}
	}
}
