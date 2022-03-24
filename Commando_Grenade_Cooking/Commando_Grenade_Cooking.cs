using EntityStates;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using EntityStates.Commando_Grenade_Cooking;
using Commando_Grenade_Cooking.Components;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using UnityEngine;
using System.Security.Permissions;

[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]


namespace Commando_Grenade_Cooking
{
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("com.Gatorism.Commando_Grenade_Cooking", "Commando Grenade Cooking", "1.0.0")]
    [BepInIncompatibility("com.RiskyLives.RiskyMod")]
    [R2API.Utils.R2APISubmoduleDependency(nameof(LanguageAPI), nameof(PrefabAPI), nameof(DamageAPI))]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    //Coded almost entirely by Moffein. Please credit him for this, I just wanted this mod by itself.
    //Started with fixing mod myself, then swapped to newer code in his RiskyMod mod. 
    //Compatibility code based off of ZetTweaks. Check them out!
    //Ian is Cute
    public class Commando_Grenade_Cooking : BaseUnityPlugin
    {
        public static GameObject CommandoObject = LegacyResourcesAPI.Load<GameObject>("Prefabs/CharacterBodies/CommandoBody");
        internal new static ManualLogSource Logger { get; set; }
        float selfDamage = 40f;
        float damageCoefficient = 1200f;
        bool enableFalloff = false;
        float grenadeCooldown = 10f;
        int baseMaxStock = 1;
        public void Awake()
        {
            selfDamage = base.Config.Bind<float>(new ConfigDefinition("General", "Self Damage Percent"), 40f, new ConfigDescription("Percent of current health to lose when overcooking.")).Value;
            damageCoefficient = base.Config.Bind<float>(new ConfigDefinition("General", "Damage"), 1200f, new ConfigDescription("Amount of damage done by grenade explosion.")).Value;
            enableFalloff = base.Config.Bind<bool>(new ConfigDefinition("General", "Enable Sweetspot Falloff"), false, new ConfigDescription("Enable grenade sweet spot falloff (Vanilla is true).")).Value;
            grenadeCooldown = base.Config.Bind<float>(new ConfigDefinition("General", "Cooldown"), 10f, new ConfigDescription("Cooldown, in seconds")).Value;
            baseMaxStock = base.Config.Bind<int>(new ConfigDefinition("General", "Base Stock"), 1, new ConfigDescription("Cooldown, in seconds")).Value;

            Logger = base.Logger;
            CookGrenade.selfHPDamagePercent = selfDamage / 50;
            ThrowGrenade._damageCoefficient = damageCoefficient / 100;
                Logger.LogMessage("Frag Grenade Cooking enabled");
                ModifyGrenade(CommandoObject.GetComponent<SkillLocator>());
                ContentCore.Init();

        }

        private void ModifyGrenade(SkillLocator sk)
        {

            LanguageAPI.Add("MFGC_COMMANDO_SPECIAL_ALT1_DESC", "Throw a grenade that explodes for <style=cIsDamage>" + (ThrowGrenade._damageCoefficient).ToString("P0").Replace(" ", "").Replace(",", "") + " damage</style> after 3 seconds. Can be <style=cIsDamage>cooked</style> to explode early. Deals <style=cIsDamage>" + (CookGrenade.selfHPDamagePercent / 2).ToString("P0").Replace(" ", "").Replace(",", "") + " damage</style> to current HP if not thrown.");

            ThrowGrenade._projectilePrefab = BuildGrenadeProjectile();
            CookGrenade.overcookExplosionEffectPrefab = BuildGrenadeOvercookExplosionEffect();

            Content.entityStates.Add(typeof(CookGrenade));
            Content.entityStates.Add(typeof(ThrowGrenade));

            SkillDef grenadeDef = SkillDef.CreateInstance<SkillDef>();
            grenadeDef.activationState = new SerializableEntityStateType(typeof(CookGrenade));
            grenadeDef.activationStateMachineName = "Weapon";
            grenadeDef.baseMaxStock = baseMaxStock;
            grenadeDef.baseRechargeInterval = grenadeCooldown;
            grenadeDef.beginSkillCooldownOnSkillEnd = false;
            grenadeDef.canceledFromSprinting = false;
            grenadeDef.dontAllowPastMaxStocks = true;
            grenadeDef.forceSprintDuringState = false;
            grenadeDef.fullRestockOnAssign = true;
            grenadeDef.icon = sk.special.skillFamily.variants[1].skillDef.icon;
            grenadeDef.interruptPriority = InterruptPriority.PrioritySkill;
            grenadeDef.isCombatSkill = true;
            grenadeDef.keywordTokens = new string[] { };
            grenadeDef.mustKeyPress = false;
            grenadeDef.cancelSprintingOnActivation = true;
            grenadeDef.rechargeStock = 1;
            grenadeDef.requiredStock = 1;
            grenadeDef.skillName = "Grenade";
            grenadeDef.skillNameToken = "COMMANDO_SPECIAL_ALT1_NAME";
            grenadeDef.skillDescriptionToken = "MFGC_COMMANDO_SPECIAL_ALT1_DESC";
            grenadeDef.stockToConsume = 1;
            Content.skillDefs.Add(grenadeDef);
            sk.special.skillFamily.variants[1].skillDef = grenadeDef;
            Skills.Grenade = grenadeDef;
        }
        private GameObject BuildGrenadeProjectile()
        {

            GameObject nandoNade = LegacyResourcesAPI.Load<GameObject>("prefabs/projectiles/CommandoGrenadeProjectile").InstantiateClone("RiskyRebalanceCommandoNade", true);
            ProjectileSimple ps = nandoNade.GetComponent<ProjectileSimple>();
            ps.lifetime = 10f;


            ProjectileImpactExplosion pie = nandoNade.GetComponent<ProjectileImpactExplosion>();
            pie.timerAfterImpact = false;
            pie.lifetime = CookGrenade.totalFuseTime;
            pie.blastRadius = CookGrenade.selfBlastRadius;
            pie.falloffModel = enableFalloff ? BlastAttack.FalloffModel.SweetSpot : BlastAttack.FalloffModel.None;

            ProjectileDamage pd = nandoNade.GetComponent<ProjectileDamage>();
            pd.damageType = DamageType.Generic;

            nandoNade.AddComponent<GrenadeTimer>();

            Content.projectilePrefabs.Add(nandoNade);
            return nandoNade;
        }

        private GameObject BuildGrenadeOvercookExplosionEffect()
        {
            GameObject effect = LegacyResourcesAPI.Load<GameObject>("prefabs/effects/omnieffect/OmniExplosionVFXCommandoGrenade").InstantiateClone("RiskyRebalanceCommandoNadeOvercookEffect", false);
            EffectComponent ec = effect.GetComponent<EffectComponent>();
            ec.soundName = "Play_commando_M2_grenade_explo";
            Content.effectDefs.Add(new EffectDef(effect));
            return effect;
        }

    }
    public class Skills
    {
        public static SkillDef Grenade;
    }



}

