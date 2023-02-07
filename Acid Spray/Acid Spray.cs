using BepInEx;
using EntityStates;
using R2API;
using R2API.Utils;
using RoR2;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace AcidSprayMod
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
    public class ExamplePlugin : BaseUnityPlugin
    {
        //The Plugin GUID should be a unique ID for this plugin, which is human readable (as it is used in places like the config).
        //If we see this PluginGUID as it is on thunderstore, we will deprecate this mod. Change the PluginAuthor and the PluginName !
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "AuthorName";
        public const string PluginName = "Acid Spray";
        public const string PluginVersion = "0.0.1";

        //We need our item definition to persist through our functions, and therefore make it a class field.
        private static ItemDef myItemDef;

        //The Awake() method is run at the very start when the game is initialized.
        public void Awake()
        {
            //Init our logging class so that we can properly log for debugging
            Log.Init(Logger);

            //Now let's turn the tokens we made into actual strings for the game:
            AddTokens();

            // This line of log will appear in the bepinex console when the Awake method is done.
            Log.LogInfo(nameof(Awake) + " done.");
        }

        //This function adds the tokens from the item using LanguageAPI, the comments in here are a style guide, but is very opiniated. Make your own judgements!
        private void AddTokens()
        {
            //The Name should be self explanatory
            LanguageAPI.Add("CROCO_SPECIAL_ACIDSPRAY_NAME", "Acid Spray");

            //The Description is where you put the actual numbers and give an advanced description.
            LanguageAPI.Add("CROCO_SPECIAL_ACIDSPRAY_DESCRIPTION", "Description");
        }

        private void AddSkill()
        {
            GameObject crocoBodyPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Croco/CrocoBody.prefab").WaitForCompletion();
            SkillLocator skillLocator = crocoBodyPrefab.GetComponent<SkillLocator>();
            RoR2.Skills.SkillFamily specialSkillFamily = skillLocator.special.skillFamily;

            RoR2.Skills.SkillDef acidSpray = ScriptableObject.CreateInstance<RoR2.Skills.SkillDef>();

            acidSpray.activationState = new SerializableEntityStateType(typeof(FireAcidSpray));
            acidSpray.activationStateMachineName = "Weapon";
            acidSpray.baseMaxStock = 1;
            acidSpray.baseRechargeInterval = 6f;
            acidSpray.beginSkillCooldownOnSkillEnd = true;
            acidSpray.resetCooldownTimerOnUse = true;
            acidSpray.canceledFromSprinting = true;
            acidSpray.cancelSprintingOnActivation = true;
            acidSpray.fullRestockOnAssign = true;
            acidSpray.interruptPriority = InterruptPriority.Skill;
            acidSpray.isCombatSkill = true;
            acidSpray.mustKeyPress = true;
            acidSpray.rechargeStock = 1;
            acidSpray.requiredStock = 1;
            acidSpray.stockToConsume = 1;
            acidSpray.icon = null;
            acidSpray.skillName = "CROCO_SPECIAL_ACIDSPRAY_NAME";
            acidSpray.skillNameToken = "CROCO_SPECIAL_ACIDSPRAY_NAME";
            acidSpray.skillDescriptionToken = "CROCO_SPECIAL_ACIDSPRAY_DESCRIPTION";

            ContentAddition.AddSkillDef(acidSpray);

            Array.Resize(ref specialSkillFamily.variants, specialSkillFamily.variants.Length + 1);
            specialSkillFamily.variants[specialSkillFamily.variants.Length - 1] = new RoR2.Skills.SkillFamily.Variant
            {
                skillDef = acidSpray,
                unlockableDef = ScriptableObject.CreateInstance<UnlockableDef>(),
                viewableNode = new ViewablesCatalog.Node(acidSpray.skillNameToken, false, null)
            };
        }
    }

    public class FireAcidSpray : BaseSkillState
    {
        public override void OnEnter()
        {
            base.OnEnter();
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Skill;
        }
    }
}
