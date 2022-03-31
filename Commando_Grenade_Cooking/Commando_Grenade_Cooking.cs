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
using System;

[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]


namespace Commando_Grenade_Cooking
{
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("com.Gatorism.Commando_Grenade_Cooking", "Commando Grenade Cooking", "1.0.0")]
    [BepInIncompatibility("com.RiskyLives.RiskyMod")]
    [R2API.Utils.R2APISubmoduleDependency(nameof(LanguageAPI), nameof(PrefabAPI), nameof(DamageAPI))]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    //Ian is Cute
    public class Commando_Grenade_Cooking : BaseUnityPlugin
    {
        internal new static ManualLogSource Logger { get; set; }

        public static float selfDamage = 40f;
        public static float damageCoefficient = 1200f;
        public static bool enableFalloff = false;
        public static float grenadeCooldown = 10f;
        public static int baseMaxStock = 1;
        public static GameObject commandoBody = LegacyResourcesAPI.Load<GameObject>("Prefabs/CharacterBodies/CommandoBody");
        public void Awake()
        {
            selfDamage = base.Config.Bind<float>(new ConfigDefinition("General", "Self Damage Percent"), 40f, new ConfigDescription("Percent of current health to lose when overcooking.")).Value;
            damageCoefficient = base.Config.Bind<float>(new ConfigDefinition("General", "Damage"), 1200f, new ConfigDescription("Amount of damage done by grenade explosion.")).Value;
            enableFalloff = base.Config.Bind<bool>(new ConfigDefinition("General", "Enable Sweetspot Falloff"), false, new ConfigDescription("Enable grenade sweet spot falloff (Vanilla is true).")).Value;
            grenadeCooldown = base.Config.Bind<float>(new ConfigDefinition("General", "Cooldown"), 10f, new ConfigDescription("Cooldown, in seconds")).Value;
            baseMaxStock = base.Config.Bind<int>(new ConfigDefinition("General", "Base Stock"), 1, new ConfigDescription("Cooldown, in seconds")).Value;
            Commando_Grenade_Cooking.instance = this;
            Utils.InitConfig(base.Config);

            Logger = base.Logger;
            CookGrenade.selfHPDamagePercent = selfDamage / 50;
            ThrowGrenade._damageCoefficient = damageCoefficient / 100;
            this.CreateGrenade();
            this.AddNewGrenadeSkill();
            Logger.LogMessage("Frag Grenade Cooking enabled");
            ContentCore.Init();

        }

        private void CreateGrenade()
        {
            GameObject GrenadeObject = PrefabAPI.InstantiateClone(LegacyResourcesAPI.Load<GameObject>("prefabs/projectiles/CommandoGrenadeProjectile"), "CookableGrenade", true);
            ThrowGrenade._projectilePrefab = BuildGrenadeProjectile();
            CookGrenade.overcookExplosionEffectPrefab = BuildGrenadeOvercookExplosionEffect();
            ContentAddition.AddProjectile(GrenadeObject);
        }

        public void AddNewGrenadeSkill()
        {
            SkillLocator component = commandoBody.GetComponent<SkillLocator>();
            SkillFamily skillFamily = component.special.skillFamily;
            SteppedSkillDef steppedSkillDef = spoofGrenadeSkillDef(skillFamily.variants[0].skillDef as SteppedSkillDef);
            
            ContentAddition.AddSkillDef(steppedSkillDef);
            Content.entityStates.Add(typeof(CookGrenade));
            Content.entityStates.Add(typeof(ThrowGrenade));
            Array.Resize<SkillFamily.Variant>(ref skillFamily.variants, skillFamily.variants.Length + 1);
            SkillFamily.Variant[] variants = skillFamily.variants;
            int num = skillFamily.variants.Length - 1;
            SkillFamily.Variant variant = default(SkillFamily.Variant);
            variant.skillDef = steppedSkillDef;
            variant.unlockableDef = null;
            variant.viewableNode = new ViewablesCatalog.Node(steppedSkillDef.skillNameToken, false, null);
            variants[num] = variant;

        }

        private static SteppedSkillDef spoofGrenadeSkillDef(SteppedSkillDef grenadeDef)
        {
           
            LanguageAPI.Add("GC_COOKABLEGRENADE_NAME", "Cookable Grenade");
            LanguageAPI.Add("GC_COMMANDO_SPECIAL_ALT1_DESC", "Throw a grenade that explodes for <style=cIsDamage>" + (ThrowGrenade._damageCoefficient).ToString("P0").Replace(" ", "").Replace(",", "") + " damage</style> after 3 seconds. Can be <style=cIsDamage>cooked</style> to explode sooner after being thrown. Deals <style=cIsDamage>" + (CookGrenade.selfHPDamagePercent / 2).ToString("P0").Replace(" ", "").Replace(",", "") + " damage</style> to current HP if not thrown.");
            SkillLocator iconcomponent = commandoBody.GetComponent<SkillLocator>();
            SteppedSkillDef steppedSkillDef = ScriptableObject.CreateInstance<SteppedSkillDef>();
            steppedSkillDef.activationState = new SerializableEntityStateType(typeof(CookGrenade));
            steppedSkillDef.activationStateMachineName = "Weapon";
            steppedSkillDef.baseMaxStock = baseMaxStock;
            steppedSkillDef.baseRechargeInterval = grenadeCooldown;
            steppedSkillDef.beginSkillCooldownOnSkillEnd = false;
            steppedSkillDef.canceledFromSprinting = false;
            steppedSkillDef.dontAllowPastMaxStocks = true;
            steppedSkillDef.forceSprintDuringState = false;
            steppedSkillDef.icon = iconcomponent.special.skillFamily.variants[1].skillDef.icon;
            steppedSkillDef.interruptPriority = InterruptPriority.PrioritySkill;
            steppedSkillDef.isCombatSkill = true;
            steppedSkillDef.keywordTokens = new string[] { };
            steppedSkillDef.mustKeyPress = false;
            steppedSkillDef.cancelSprintingOnActivation = true;
            steppedSkillDef.rechargeStock = 1;
            steppedSkillDef.requiredStock = 1;
            steppedSkillDef.skillName = "GC_COOKABLEGRENADE_NAME";
            steppedSkillDef.skillNameToken = "GC_COOKABLEGRENADE_NAME";
            steppedSkillDef.skillDescriptionToken = "GC_COMMANDO_SPECIAL_ALT1_DESC";
            steppedSkillDef.stockToConsume = 1;
            return steppedSkillDef;
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
        public static Commando_Grenade_Cooking instance;
    }
    
    public class Skills
    {
        public static SkillDef Grenade;
    }



}

