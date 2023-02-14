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

        //The Awake() method is run at the very start when the game is initialized.
        public void Awake()
        {
            //Init our logging class so that we can properly log for debugging
            Log.Init(Logger);

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
            LanguageAPI.Add("CROCO_SPECIAL_FRENZY_DESCRIPTION", "Description");
        }

        private void AddSkill()
        {
            GameObject crocoBodyPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Croco/CrocoBody.prefab").WaitForCompletion();
            SkillLocator skillLocator = crocoBodyPrefab.GetComponent<SkillLocator>();
            RoR2.Skills.SkillFamily specialSkillFamily = skillLocator.special.skillFamily;

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
            frenzy.icon = null;
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

        public static float minChargeDuration = 0.25f;

        public static float baseDuration = 1f;

        public static float maxAttackDuration = 1.2f;

        public static float minAttackDuration = 0.4f;

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
            if (base.isAuthority && ((!IsKeyDownAuthority() && base.fixedAge >= minChargeDuration) || base.fixedAge >= duration))
            {
                float charge =  Mathf.Clamp01(base.fixedAge / duration);
                FrenzyAttack nextState = new FrenzyAttack();
                float totalDuration = Util.Remap(charge, 0f, 1f, minAttackDuration, maxAttackDuration);
                nextState.attackCount = (int)Math.Floor(totalDuration/FrenzyAttack.baseDurationParam);
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
            EntityStates.Croco.Slash slash = new();
            hitBoxGroupName = slash.hitBoxGroupName;
            //mecanimHitboxActiveParameter = slash.mecanimHitboxActiveParameter;
            hitPauseDuration = 0f;
            ignoreAttackSpeed = false;
            baseDuration = baseDurationParam;
            base.OnEnter();

            bloom = slash.bloom;
            slashSound = EntityStates.Croco.Slash.slash1Sound;
            base.characterDirection.forward = GetAimRay().direction;
            crocoDamageTypeController = GetComponent<CrocoDamageTypeController>();

            procCoefficient = 1f;
            damageCoefficient = 1f;
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
            Debug.Log("Animation Duration: " + duration);
            Debug.Log("Animation Right Arm: " + rightSwing);
            float num = Mathf.Max(duration, 0.2f);
            Debug.Log("Animation Name: " + animationStateName);
            PlayCrossfade("Gesture, Additive", animationStateName, "Slash.playbackRate", num, 0.05f);
            PlayCrossfade("Gesture, Override", animationStateName, "Slash.playbackRate", num, 0.05f);
            Util.PlaySound(slashSound, gameObject);
        }

        public override void OnMeleeHitAuthority()
        {
            base.characterBody.AddSpreadBloom(bloom);
            base.OnMeleeHitAuthority();
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


    public class FireAcidSpray : BaseSkillState
    {
        public GameObject effectPrefab;

        public static GameObject impactEffectPrefab;

        private CrocoDamageTypeController crocoDamageTypeController;

        public static float maxDistance;

        public static float radius;

        public static float baseEntryDuration = 2f;

        public static float baseAcidSprayDuration = 1.5f;

        public static float baseExitDuration = 0.1f;

        public static float damageCoefficient = 1f;

        public static float procCoefficient;

        public static float tickFrequency;

        public static float force = 10f;

        public static float stopwatch;

        public static string startAttackSoundString;

        public static string endAttackSoundString;

        public static string muzzleName;

        public static float recoilForce;

        private bool isCrit;

        private bool hasBegunAcidSpray;

        private float entryDuration;

        private float acidSprayDuration;

        private float exitDuration;

        private float acidSprayStopwatch;

        private Animator modelAnimator;

        private string layerName;

        private string playbackRateParam;

        private float playbackRate;

        public override void OnEnter()
        {
            base.OnEnter();
            impactEffectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Croco/CrocoDiseaseImpactEffect.prefab").WaitForCompletion();
            modelAnimator = GetModelAnimator();
            AnimationClip[] animationClips = modelAnimator.runtimeAnimatorController.animationClips;
            AnimatorControllerParameter[] parameters = modelAnimator.parameters;
            for (int i = 0; i < animationClips.Length; i++)
            {
                Debug.Log("Animationclips: " + animationClips[0].name);
                Debug.Log("Animationclips: " + animationClips[0].hasGenericRootTransform);
                Debug.Log("Animationclips: " + animationClips[0].hasMotionCurves);
                Debug.Log("Animationclips: " + animationClips[0].hasMotionFloatCurves);
                Debug.Log("Animationclips: " + animationClips[0].hasRootCurves);
                Debug.Log("Animationclips: " + animationClips[0].hasRootMotion);
            }
            layerName = "Gesture, Mouth";
            playbackRateParam = "AcidSpray.playbackRate";

            crocoDamageTypeController = GetComponent<CrocoDamageTypeController>();
            tickFrequency = 4f;

            stopwatch = 0f;
            entryDuration = baseEntryDuration / attackSpeedStat;
            acidSprayDuration = baseAcidSprayDuration;
            exitDuration = baseExitDuration;

            if ((bool)base.characterBody)
            {
                base.characterBody.SetAimTimer(entryDuration + acidSprayDuration + 1f);
            }
            int num = Mathf.CeilToInt(acidSprayDuration * tickFrequency);
            if (base.isAuthority && (bool)base.characterBody)
            {
                isCrit = Util.CheckRoll(critStat, base.characterBody.master);
            }
            //PlayAnimation(layerName, "FireSpit", playbackRateParam, entryDuration + exitDuration);
            PlayAnimation("Gesture, Mouth", "FireSpit", "AcidSpray.playbackRate", entryDuration);
        }

        public override void OnExit()
        {
            base.OnExit();
            int layerIndex = modelAnimator.GetLayerIndex(layerName);
            float length = modelAnimator.GetCurrentAnimatorStateInfo(layerIndex).length;
            modelAnimator.SetFloat(playbackRateParam, length/(entryDuration + exitDuration));
            modelAnimator.Update(0f);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            stopwatch += Time.fixedDeltaTime;
            if (stopwatch >= entryDuration && !hasBegunAcidSpray)
            {
                modelAnimator.SetFloat(playbackRateParam, 0f);
                modelAnimator.Update(0f);
                hasBegunAcidSpray = true;
                Util.PlaySound(startAttackSoundString, base.gameObject);
                FireAcid();
            }
            if (hasBegunAcidSpray)
            {
                acidSprayStopwatch += Time.deltaTime;
                float num = 1f / tickFrequency / attackSpeedStat;
                if (acidSprayStopwatch > num)
                {
                    acidSprayStopwatch -= num;
                    FireAcid();
                }
            }
            if (stopwatch >= acidSprayDuration + entryDuration && base.isAuthority)
            {
                outer.SetNextStateToMain();
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Skill;
        }

        private void FireAcid()
        {
            Ray aimRay = GetAimRay();
            if (base.isAuthority)
            {
                BulletAttack bulletAttack = new BulletAttack();
                bulletAttack.owner = base.gameObject;
                bulletAttack.weapon = base.gameObject;
                bulletAttack.origin = aimRay.origin;
                bulletAttack.aimVector = aimRay.direction;
                bulletAttack.minSpread = 0f;
                bulletAttack.damage = damageCoefficient * damageStat;
                bulletAttack.force = force;
                bulletAttack.muzzleName = muzzleName;
                bulletAttack.hitEffectPrefab = impactEffectPrefab;
                bulletAttack.isCrit = isCrit;
                bulletAttack.radius = radius;
                bulletAttack.falloffModel = BulletAttack.FalloffModel.None;
                bulletAttack.stopperMask = LayerIndex.world.mask;
                bulletAttack.procCoefficient = procCoefficient;
                bulletAttack.maxDistance = maxDistance;
                bulletAttack.smartCollision = true;
                bulletAttack.damageType = (crocoDamageTypeController ? crocoDamageTypeController.GetDamageType() : DamageType.Generic);
                bulletAttack.Fire();
                if ((bool)base.characterMotor)
                {
                    base.characterMotor.ApplyForce(aimRay.direction * (0f - recoilForce));
                }
            }
        }
    }
}
