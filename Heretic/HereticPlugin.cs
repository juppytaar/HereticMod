﻿using BepInEx;
using BepInEx.Configuration;
using HereticMod.Components;
using R2API;
using R2API.Utils;
using RiskOfOptions;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Zio.FileSystems;

namespace HereticMod
{
    [BepInDependency("com.rune580.riskofoptions")]
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("com.Moffein.Heretic", "Heretic", "1.0.0")]
    [R2API.Utils.R2APISubmoduleDependency(nameof(RecalculateStatsAPI), nameof(ContentAddition), nameof(ItemAPI), nameof(PrefabAPI), nameof(LoadoutAPI))]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    public class HereticPlugin : BaseUnityPlugin
    {
        public static bool visionsAttackSpeed = true;
        public static bool giveHereticItem = true;
        public static ConfigEntry<KeyboardShortcut> squawkButton;

        public static PluginInfo pluginInfo;

        public static BodyIndex HereticBodyIndex;
        public static GameObject HereticBodyObject;
        public static SurvivorDef HereticSurvivorDef;

        public void Awake()
        {
            ReadConfig();

            pluginInfo = Info;
            Assets.Init();
            Tokens.Init();
            HereticBodyObject = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Heretic/HereticBody.prefab").WaitForCompletion();

            Skins.InitSkins(HereticBodyObject);
            ModifyStats(HereticBodyObject.GetComponent<CharacterBody>());

            ModifySurvivorDef();
            ModifyLunarSkillDefs.Init();
            SkillSetup.Init();
            Squawk.Init();

            HereticItem.Init();

            On.RoR2.CameraRigController.OnEnable += DisableLobbyFade;
            On.RoR2.SurvivorCatalog.Init += (orig) =>
            {
                orig();
                HereticBodyIndex = BodyCatalog.FindBodyIndex("HereticBody");
            };
        }

        public static void DisableLobbyFade(On.RoR2.CameraRigController.orig_OnEnable orig, CameraRigController self)
        {
            SceneDef sd = RoR2.SceneCatalog.GetSceneDefForCurrentScene();
            if (sd && sd.baseSceneName.Equals("lobby"))
            {
                self.enableFading = false;
            }
            orig(self);
        }

        private void ModifyStats(CharacterBody cb)
        {
            cb.baseMaxHealth = 110f;
            cb.levelMaxHealth = 33f;

            cb.baseDamage = 12f;
            cb.levelDamage = 2.4f;

            cb.baseRegen = 1f;
            cb.levelRegen = 0.2f;
        }

        private void ModifySurvivorDef()
        {
            HereticSurvivorDef = Addressables.LoadAssetAsync<SurvivorDef>("RoR2/Base/Heretic/Heretic.asset").WaitForCompletion();
            HereticSurvivorDef.hidden = false;

            GameObject HereticDisplayPrefab = HereticBodyObject.GetComponent<ModelLocator>().modelTransform.gameObject.InstantiateClone("MoffeinHereticDisplay", false);
            HereticDisplayPrefab.transform.localScale *= 0.6f;
            HereticSurvivorDef.displayPrefab = HereticDisplayPrefab;
            HereticDisplayPrefab.AddComponent<MenuAnimComponent>();

            HereticSurvivorDef.desiredSortPosition = 15f;

            //Hopefully hooking this will make it work in MP
            On.RoR2.SurvivorMannequins.SurvivorMannequinSlotController.RebuildMannequinInstance += (orig, self) =>
            {
                orig(self);
                if (self.currentSurvivorDef == HereticSurvivorDef)
                {
                    MenuAnimComponent mac = self.mannequinInstanceTransform.gameObject.GetComponent<MenuAnimComponent>();
                    if (mac) mac.Play();
                }
            };
        }

        private void ReadConfig()
        {
            HereticPlugin.squawkButton = Config.Bind("General", "Squawk Button", KeyboardShortcut.Empty, "Press this button to squawk.");
            ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(HereticPlugin.squawkButton));
        }
    }
}
