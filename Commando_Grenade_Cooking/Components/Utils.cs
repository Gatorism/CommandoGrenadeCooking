using BepInEx;
using UnityEngine;
using BepInEx.Configuration;
using RoR2;

namespace Commando_Grenade_Cooking
{
    public static class Utils
    {
        

        public static void InitConfig(ConfigFile Config)
        {
            Utils.selfDamage = Config.Bind<float>(new ConfigDefinition("General", "Self Damage Percent"), 40f, new ConfigDescription("Percent of current health to lose when overcooking.")).Value;
            Utils.damageCoefficient = Config.Bind<float>(new ConfigDefinition("General", "Damage"), 1200f, new ConfigDescription("Amount of damage done by grenade explosion.")).Value;
            Utils.enableFalloff = Config.Bind<bool>(new ConfigDefinition("General", "Enable Sweetspot Falloff"), false, new ConfigDescription("Enable grenade sweet spot falloff (Vanilla is true).")).Value;
            Utils.grenadeCooldown = Config.Bind<float>(new ConfigDefinition("General", "Cooldown"), 10f, new ConfigDescription("Cooldown, in seconds")).Value;
            Utils.baseMaxStock = Config.Bind<int>(new ConfigDefinition("General", "Base Stock"), 1, new ConfigDescription("Cooldown, in seconds")).Value;
        }
        public static float selfDamage = 40f;
        public static float damageCoefficient = 1200f;
        public static bool enableFalloff = false;
        public static float grenadeCooldown = 10f;
        public static int baseMaxStock = 1;
    }
}
