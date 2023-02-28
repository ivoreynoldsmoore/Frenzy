using BepInEx;
using EntityStates;
using EntityStates.Mage.Weapon;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.UI;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;
using static RoR2.OverlapAttack;

namespace FrenzyMod
{
    //This is an example plugin that can be put in BepInEx/plugins/ExamplePlugin/ExamplePlugin.dll to test out.
    //It's a small plugin that adds a relatively simple item to the game, and gives you that item whenever you press F2.

    //This attribute specifies that we have a dependency on R2API, as we're using it to add our item to the game.
    //You don't need this if you're not using R2API in your plugin, it's just to tell BepInEx to initialize R2API before this plugin so it's safe to use R2API.
    [BepInDependency(R2API.R2API.PluginGUID)]

    //This attribute is required, and lists metadata for your plugin.
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    //We will be using 2 modules from R2API: ItemAPI to add our item and LanguageAPI to add our language tokens.
    [R2APISubmoduleDependency(nameof(LanguageAPI))]

    //This is the main declaration of our plugin class. BepInEx searches for all classes inheriting from BaseUnityPlugin to initialize on startup.
    //BaseUnityPlugin itself inherits from MonoBehaviour, so you can use this as a reference for what you can declare and use in your plugin class: https://docs.unity3d.com/ScriptReference/MonoBehaviour.html
    public class Frenzy : BaseUnityPlugin
    {
        //The Plugin GUID should be a unique ID for this plugin, which is human readable (as it is used in places like the config).
        //If we see this PluginGUID as it is on thunderstore, we will deprecate this mod. Change the PluginAuthor and the PluginName !
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "AuthorName";
        public const string PluginName = "Frenzy";
        public const string PluginVersion = "0.0.1";
        public static PluginInfo pluginInfo;

        //The Awake() method is run at the very start when the game is initialized.
        public void Awake()
        {
            //Init our logging class so that we can properly log for debugging
            Log.Init(Logger);

            pluginInfo = this.Info;

            //Now let's turn the tokens we made into actual strings for the game:
            AddTokens();
            AddSkill();
            ContentAddition.AddEntityState<ChargeFrenzy>(out _);
            ContentAddition.AddEntityState<FrenzyAttack>(out _);

            // This line of log will appear in the bepinex console when the Awake method is done.
            Log.LogInfo(nameof(Awake) + " done.");
        }

        //This function adds the tokens from the item using LanguageAPI, the comments in here are a style guide, but is very opiniated. Make your own judgements!
        private void AddTokens()
        {
            //The Name should be self explanatory
            LanguageAPI.Add("CROCO_SPECIAL_FRENZY_NAME", "Frenzy");

            //The Description is where you put the actual numbers and give an advanced description.
            LanguageAPI.Add("CROCO_SPECIAL_FRENZY_DESCRIPTION", "<style=cIsHealing>Poisonous</style>. Rapidly slash at enemies in front of you for <style=cIsDamage>200%</style>.");
        }

        private void AddSkill()
        {
            GameObject crocoBodyPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Croco/CrocoBody.prefab").WaitForCompletion();
            SkillLocator skillLocator = crocoBodyPrefab.GetComponent<SkillLocator>();
            RoR2.Skills.SkillFamily specialSkillFamily = skillLocator.special.skillFamily;
            RoR2.Skills.SkillFamily primarySkillFamily = skillLocator.primary.skillFamily;

            RoR2.Skills.SkillDef frenzy = ScriptableObject.CreateInstance<RoR2.Skills.SkillDef>();

            frenzy.activationState = new SerializableEntityStateType(typeof(ChargeFrenzy));
            frenzy.activationStateMachineName = "Weapon";
            frenzy.baseMaxStock = 1;
            frenzy.baseRechargeInterval = 8f;
            frenzy.beginSkillCooldownOnSkillEnd = true;
            frenzy.resetCooldownTimerOnUse = true;
            frenzy.canceledFromSprinting = true;
            frenzy.cancelSprintingOnActivation = true;
            frenzy.fullRestockOnAssign = true;
            frenzy.interruptPriority = InterruptPriority.Skill;
            frenzy.isCombatSkill = true;
            frenzy.mustKeyPress = false;
            frenzy.rechargeStock = 1;
            frenzy.requiredStock = 1;
            frenzy.stockToConsume = 1;
            frenzy.icon = primarySkillFamily.variants[0].skillDef.icon;
            frenzy.skillName = "CROCO_SPECIAL_FRENZY_NAME";
            frenzy.skillNameToken = "CROCO_SPECIAL_FRENZY_NAME";
            frenzy.skillDescriptionToken = "CROCO_SPECIAL_FRENZY_DESCRIPTION";

            ContentAddition.AddSkillDef(frenzy);

            Array.Resize(ref specialSkillFamily.variants, specialSkillFamily.variants.Length + 1);
            specialSkillFamily.variants[specialSkillFamily.variants.Length - 1] = new RoR2.Skills.SkillFamily.Variant
            {
                skillDef = frenzy,
                unlockableDef = ScriptableObject.CreateInstance<UnlockableDef>(),
                viewableNode = new ViewablesCatalog.Node(frenzy.skillNameToken, false, null)
            };
        }
    }

    public class ChargeFrenzy : BaseSkillState
    {
        public static float baseDuration = 0.8f;

        public static float attackDuration = 1.2f;

        private float duration;

        public override void OnEnter()
        {
            base.OnEnter();
            duration = baseDuration / attackSpeedStat;
            PlayCrossfade("Gesture, Additive", "Slash3", "Slash.playbackRate", duration*5, 0.05f);
            PlayCrossfade("Gesture, Override", "Slash3", "Slash.playbackRate", duration*5, 0.05f);

        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (base.isAuthority && ((!IsKeyDownAuthority() && base.fixedAge >= duration) || base.fixedAge >= duration))
            {
                float charge =  Mathf.Clamp01(base.fixedAge / duration);
                FrenzyAttack nextState = new FrenzyAttack();
                nextState.attackCount = Math.Max(1,(int)Math.Floor(attackDuration / (FrenzyAttack.baseDurationParam / attackSpeedStat)));
                nextState.rightSwing = true;
                outer.SetNextState(nextState);
            }
        }

        public override void OnExit()
        {
            base.OnExit();
        }
    }

    public class FrenzyAttack : BasicMeleeAttack
    {
        public static float recoilAmplitude;

        public static float baseDurationParam = 0.4f;

        public static string slashSound;

        public int attackCount;

        public bool rightSwing;

        private CrocoDamageTypeController crocoDamageTypeController;

        private string animationStateName;

        private float bloom;

        public override void OnEnter()
        {
            recoilAmplitude = EntityStates.Croco.Slash.recoilAmplitude;
            slashSound = EntityStates.Croco.Slash.slash1Sound;

            EntityStates.Croco.Slash slash = new();
            hitBoxGroupName = slash.hitBoxGroupName;
            crocoDamageTypeController = GetComponent<CrocoDamageTypeController>();
            swingEffectPrefab = slash.swingEffectPrefab;
            bloom = slash.bloom;
            Debug.Log(EntityStates.Croco.Slash.baseDurationBeforeInterruptable);
            Debug.Log(EntityStates.Croco.Slash.comboFinisherBaseDurationBeforeInterruptable);
            hitPauseDuration = 0.05f;

            baseDuration = baseDurationParam;
            ignoreAttackSpeed = false;
            procCoefficient = 1f;
            damageCoefficient = 2f;

            base.OnEnter();
            base.characterDirection.forward = GetAimRay().direction;

        }

        public override void OnExit()
        {
            base.OnExit();
        }

        public override void AuthorityModifyOverlapAttack(OverlapAttack overlapAttack)
        {
            base.AuthorityModifyOverlapAttack(overlapAttack);
            overlapAttack.damageType = crocoDamageTypeController ? crocoDamageTypeController.GetDamageType() : DamageType.Generic;
        }

        public override void PlayAnimation()
        {
            animationStateName = "";
            if (rightSwing)
            {
                animationStateName = "Slash1";
            }
            else
            {
                animationStateName = "Slash2";
            }
            //Debug.Log("Animation Duration: " + duration);
            float num = Mathf.Max(duration, 0.2f);
            //Debug.Log("Animation Name: " + animationStateName);
            PlayCrossfade("Gesture, Additive", animationStateName, "Slash.playbackRate", num, 0.05f);
            PlayCrossfade("Gesture, Override", animationStateName, "Slash.playbackRate", num, 0.05f);
            Util.PlaySound(slashSound, gameObject);
        }

        public override void OnMeleeHitAuthority()
        {
            base.OnMeleeHitAuthority();
            base.characterBody.AddSpreadBloom(bloom);
            //Debug.Log("Hit!");
            //for (int i = 0; i < hitResults.Count; i++)
            //    Debug.Log(hitResults[i].healthComponent.body.name);
        }

        public override void BeginMeleeAttackEffect()
        {
            swingEffectMuzzleString = animationStateName;
            AddRecoil(-0.1f * recoilAmplitude, 0.1f * recoilAmplitude, -1f * recoilAmplitude, 1f * recoilAmplitude);
            base.BeginMeleeAttackEffect();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (isAuthority && fixedAge >= duration && attackCount > 1)
            {
                FrenzyAttack nextState = new FrenzyAttack();
                nextState.attackCount = attackCount - 1;
                nextState.rightSwing = !rightSwing;
                outer.SetNextState(nextState);
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return base.GetMinimumInterruptPriority();
        }
    }
}
