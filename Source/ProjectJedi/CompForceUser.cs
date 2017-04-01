﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Verse;
using AbilityUser;
using UnityEngine;

namespace ProjectJedi
{
    /*
     *  Force User Class
     *  
     *  This class initializes a Jedi / Sith with force powers.
     *  Force users use the Force Pool tracker in the needs menu.
     *  When force users use force powers, the pool deteriorates.
     * 
     */
    public class CompForceUser : CompAbilityUser
    {

        private int forceUserLevel = 0;
        public int ForceUserLevel
        {
            get
            {
                return forceUserLevel;
            }
            set
            {
                if (value > forceUserLevel) abilityPoints++;
                forceUserLevel = value;
            }
        }

        private int forceUserXP = 1;
        public int ForceUserXP
        {
            get
            {
                return forceUserXP;
            }
            set
            {
                forceUserXP = value;
            }
        }

        public float XPLastLevel
        {
            get
            {
                float result = 0f;
                if (forceUserLevel > 0) result = forceUserLevel * 600;
                return result;
            }
        }

        public float XPTillNextLevelPercent
        {
            get
            {
                return ((float)(forceUserXP - XPLastLevel) / (float)(ForceUserXPTillNextLevel - XPLastLevel));
            }
        }

        public int ForceUserXPTillNextLevel
        {
            get
            {
                return (forceUserLevel + 1) * 600;
            }
        }

        public List<ForceSkill> forceSkills;
        public List<ForceSkill> ForceSkills
        {
            get
            {
                if (forceSkills == null)
                {
                    forceSkills = new List<ForceSkill>
                    {
                        new ForceSkill("PJ_LightsaberOffense", 0),
                        new ForceSkill("PJ_LightsaberDefense", 0),
                        new ForceSkill("PJ_LightsaberAccuracy", 0),
                        new ForceSkill("PJ_LightsaberReflection", 0),
                        new ForceSkill("PJ_ForcePool", 0)
                    };
                }
                return forceSkills;
            }
        }

        public bool forcePowersInitialized = false;
        public List<ForcePower> forcePowersDark;
        public List<ForcePower> ForcePowersDark
        {
            get
            {
                if (forcePowersDark == null)
                {
                    forcePowersDark = new List<ForcePower>
                    {
                        new ForcePower("forceRage", 0, ProjectJediDefOf.PJ_ForceRage),
                        new ForcePower("forceChoke", 0, ProjectJediDefOf.PJ_ForceChoke),
                        new ForcePower("forceDrain", 0, ProjectJediDefOf.PJ_ForceDrain),
                        new ForcePower("forceLightning", 0, ProjectJediDefOf.PJ_ForceLightning),
                        new ForcePower("forceStorm", 0, ProjectJediDefOf.PJ_ForceStorm)
                    };
                }
                return forcePowersDark;
            }
        }

        public List<ForcePower> forcePowersGray;
        public List<ForcePower> ForcePowersGray
        {
            get
            {
                if (forcePowersGray == null)
                {
                    forcePowersGray = new List<ForcePower>
                    {
                        new ForcePower("forcePush", 0, ProjectJediDefOf.PJ_ForcePush),
                        new ForcePower("forcePull", 0, ProjectJediDefOf.PJ_ForcePull),
                        new ForcePower("forceSpeed", 0, ProjectJediDefOf.PJ_ForceSpeed)
                    };
                }
                return forcePowersGray;
            }
        }
        public List<ForcePower> forcePowersLight;
        public List<ForcePower> ForcePowersLight
        {
            get
            {
                if (forcePowersLight == null)
                {
                    forcePowersLight = new List<ForcePower>
                    {
                        new ForcePower("forceHeal", 0, ProjectJediDefOf.PJ_ForceHealingSelf),
                        new ForcePower("forceHealOther", 0, ProjectJediDefOf.PJ_ForceHealingOther),
                        new ForcePower("forceDefense", 0, ProjectJediDefOf.PJ_ForceDefense),
                        new ForcePower("mindTrick", 0, ProjectJediDefOf.PJ_MindTrick),
                        new ForcePower("forceGhost", 0, ProjectJediDefOf.PJ_ForceGhost)
                    };
                }
                return forcePowersLight;
            }
        }


        public int abilityPoints = 0;

        //public int levelLightsaberOff = 4;
        //public int levelLightsaberDef = 3;
        //public int levelLightsaberAcc = 2;
        //public int levelLightsaberRef = 1;
        //public int levelForcePool = 0;

        /// <summary>
        /// Keep track of an internal alignment.
        /// As a float value, this allows greater roleplaying possibilities.
        /// </summary>
        private float alignmentValue;
        public float AlignmentValue
        {
            get
            {
                return alignmentValue;
            }
            set
            {
                alignmentValue = value;
            }
        }

        public ForceAlignmentType ForceAlignmentType
        {
            set
            {
                switch (value)
                {
                    case ForceAlignmentType.Dark:
                        alignmentValue = 0.0f;
                        break;
                    case ForceAlignmentType.Gray:
                    case ForceAlignmentType.None:
                        alignmentValue = 0.5f;
                        break;
                    case ForceAlignmentType.Light:
                        alignmentValue = 1.0f;
                        break;
                }
            }
            get
            {
                if (alignmentValue < 0.4)
                    return ForceAlignmentType.Dark;
                if (alignmentValue < 0.6)
                    return ForceAlignmentType.Gray;
                return ForceAlignmentType.Light;
            }
        }

        /// <summary>
        /// The force pool is where all fatigue and
        /// casting limits are decided.
        /// </summary>
        private Need_ForcePool forcePool;
        public Need_ForcePool ForcePool
        {
            get
            {
                if (forcePool == null) forcePool = abilityUser.needs.TryGetNeed<Need_ForcePool>();
                return forcePool;
            }
        }

        public bool firstTick = false;

        public bool IsForceUser
        {
            get
            {
                if (this.abilityUser != null)
                {
                    if (this.abilityUser.story != null)
                    {
                        if (this.abilityUser.story.traits.HasTrait(ProjectJediDefOf.PJ_JediTrait) ||
                            this.abilityUser.story.traits.HasTrait(ProjectJediDefOf.PJ_SithTrait) ||
                            this.abilityUser.story.traits.HasTrait(ProjectJediDefOf.PJ_GrayTrait) ||
                            this.abilityUser.story.traits.HasTrait(ProjectJediDefOf.PJ_ForceSensitive))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        public override void CompTick()
        {
            if (Find.TickManager.TicksGame > 200)
            {
                if (Find.TickManager.TicksGame % 30 == 0)
                {
                    if (IsForceUser)
                    {
                        if (!firstTick) PostInitializeTick();
                        base.CompTick();
                        if (forceUserXP > ForceUserXPTillNextLevel) ForceUserLevel += 1;
                        forceUserXP++;
                    }
                }
            }
        }

        /// <summary>
        /// Creates a force user by adding a hidden Hediff that adds their Force Pool needs.
        /// </summary>
        public void PostInitializeTick()
        {
            if (this.abilityUser != null)
            {
                if (this.abilityUser.Spawned)
                {
                    if (this.abilityUser.story != null)
                    {
                        firstTick = true;
                        this.Initialize();
                        ResolveForceTab();
                        ResolveForcePowers();
                        ResolveForcePool();
                    }
                }
            }
        }

        public void ResolveForceTab()
        {
            //PostExposeData();
            //Make the ITab
            IEnumerable<InspectTabBase> tabs = this.abilityUser.GetInspectTabs();
            if (tabs != null && tabs.Count<InspectTabBase>() > 0)
            {
                if (tabs.FirstOrDefault((InspectTabBase x) => x is ITab_Pawn_Force) == null)
                {
                    try
                    {
                        this.abilityUser.def.inspectorTabsResolved.Add(InspectTabManager.GetSharedInstance(typeof(ITab_Pawn_Force)));
                    }
                    catch (Exception ex)
                    {
                        Log.Error(string.Concat(new object[]
                        {
                    "Could not instantiate inspector tab of type ",
                    typeof(ITab_Pawn_Force),
                    ": ",
                    ex
                        }));
                    }
                }
            }
        }

        public void ResolveForcePowers()
        {

            //Set the force alignment
            if (this.abilityPowerManager == null)
            {
                Log.Message("Null handled");
                this.abilityPowerManager = new AbilityPowerManager(this);
            }

            if (forcePowersInitialized) return;
            forcePowersInitialized = true;

            Trait jediTrait = this.abilityUser.story.traits.GetTrait(ProjectJediDefOf.PJ_JediTrait);
            Trait sithTrait = this.abilityUser.story.traits.GetTrait(ProjectJediDefOf.PJ_SithTrait);
            Trait grayTrait = this.abilityUser.story.traits.GetTrait(ProjectJediDefOf.PJ_GrayTrait);
            Trait sensitiveTrait = this.abilityUser.story.traits.GetTrait(ProjectJediDefOf.PJ_ForceSensitive);

            if (jediTrait != null)
            {
                switch (jediTrait.Degree)
                {
                    case 0:
                        this.alignmentValue = 0.7f;
                        return;
                    case 1:
                        this.alignmentValue = 0.8f;
                        ForcePower forceHeal = this.ForcePowersLight.FirstOrDefault((ForcePower x) => x.label == "forceHeal");
                        LevelUpPower(forceHeal);
                        return;
                    case 2:
                        this.alignmentValue = 0.85f;
                        forceHeal = this.ForcePowersLight.FirstOrDefault((ForcePower x) => x.label == "forceHeal");
                        LevelUpPower(forceHeal);
                        ForcePower forceHealOther = this.ForcePowersLight.FirstOrDefault((ForcePower x) => x.label == "forceHealOther");
                        LevelUpPower(forceHealOther);
                        return;
                    case 3:
                        this.alignmentValue = 1.0f;
                        forceHeal = this.ForcePowersLight.FirstOrDefault((ForcePower x) => x.label == "forceHeal");
                        LevelUpPower(forceHeal);
                        forceHealOther = this.ForcePowersLight.FirstOrDefault((ForcePower x) => x.label == "forceHealOther");
                        LevelUpPower(forceHealOther);
                        return;
                }

                // !! DEBUG -- TO BE REMOVED LATER !!
            }
            if (grayTrait != null)
            {
                this.ForceAlignmentType = ForceAlignmentType.Gray; // Default to Gray
            }

            if (sithTrait != null)
            {
                switch (sithTrait.Degree)
                {
                    case 0:
                        this.alignmentValue = 0.3f;
                        return;
                    case 1:
                        this.alignmentValue = 0.2f;
                        ForcePower forceDrain = this.ForcePowersDark.FirstOrDefault((ForcePower x) => x.label == "forceDrain");
                        LevelUpPower(forceDrain);
                        return;
                    case 2:
                        this.alignmentValue = 0.15f;
                        forceDrain = this.ForcePowersDark.FirstOrDefault((ForcePower x) => x.label == "forceDrain");
                        LevelUpPower(forceDrain);
                        ForcePower forceLightning = this.ForcePowersDark.FirstOrDefault((ForcePower x) => x.label == "forceLightning");
                        LevelUpPower(forceLightning);
                        return;
                    case 3:
                        this.alignmentValue = 0.0f;
                        forceDrain = this.ForcePowersDark.FirstOrDefault((ForcePower x) => x.label == "forceDrain");
                        LevelUpPower(forceDrain);
                        forceLightning = this.ForcePowersDark.FirstOrDefault((ForcePower x) => x.label == "forceLightning");
                        LevelUpPower(forceLightning);
                        ForcePower forceStorm = this.ForcePowersDark.FirstOrDefault((ForcePower x) => x.label == "forceStorm");
                        LevelUpPower(forceStorm);
                        return;
                }
            }
        }

        public void LevelUpPower(ForcePower power)
        {
            power.level++;
            this.abilityPowerManager.AddPawnAbility(power.abilityDef);
        }

        public void ResolveForcePool()
        {
            //Add the hediff if no pool exists.
            if (ForcePool == null)
            {
                Hediff forceWielderHediff = abilityUser.health.hediffSet.GetFirstHediffOfDef(ProjectJediDefOf.PJ_ForceWielderHD);
                if (forceWielderHediff != null)
                {
                    forceWielderHediff.Severity = 1.0f;
                }
                else
                {
                    Hediff newForceWielderHediff = HediffMaker.MakeHediff(ProjectJediDefOf.PJ_ForceWielderHD, abilityUser, null);
                    newForceWielderHediff.Severity = 1.0f;
                    abilityUser.health.AddHediff(newForceWielderHediff, null, null);
                }
            }
        }

        /// <summary>
        /// Shows the required alignment (optional), 
        /// alignment change (optional),
        /// and the force pool usage
        /// </summary>
        /// <param name="verb"></param>
        /// <returns></returns>
        public override string PostAbilityVerbDesc(Verb_UseAbility verb)
        {
            string result = "";
            StringBuilder postDesc = new StringBuilder();
            ForceAbilityDef forceDef = (ForceAbilityDef)verb.useAbilityProps.abilityDef;
            if (forceDef != null)
            {
                string alignDesc = "";
                string changeDesc = "";
                string pointsDesc = "";
                if (forceDef.requiresAlignment)
                {
                    alignDesc = "ForceAbilityDescAlign".Translate(new object[]
                    {
                    forceDef.requiredAlignmentType.ToString(),
                    });
                }
                if (forceDef.changesAlignment)
                {
                    changeDesc = "ForceAbilityDescChange".Translate(new object[]
                    {
                    forceDef.changedAlignmentType.ToString(),
                    forceDef.changedAlignmentRate.ToString("p1")
                    });
                }
                pointsDesc = "ForceAbilityDescPoints".Translate(new object[]
                {
                    forceDef.forcePoolCost.ToString("p1")
                });
                if (alignDesc != "") postDesc.AppendLine(alignDesc);
                if (changeDesc != "") postDesc.AppendLine(changeDesc);
                if (pointsDesc != "") postDesc.AppendLine(pointsDesc);
                result = postDesc.ToString();
            }
            return result;
        }

        /// <summary>
        /// This section checks if the force pool allows for the casting of the spell.
        /// </summary>
        /// <param name="verbAbility"></param>
        /// <param name="reason">Why did we fail?</param>
        /// <returns></returns>
        public override bool CanCastPowerCheck(Verb_UseAbility verbAbility, out string reason)
        {
            reason = "";
            ForceAbilityDef forceDef = (ForceAbilityDef)verbAbility.useAbilityProps.abilityDef;
            if (forceDef != null)
            {
                if (forceDef.requiresAlignment)
                {
                    if (forceDef.requiredAlignmentType != ForceAlignmentType.Gray &&
                    forceDef.requiredAlignmentType != this.ForceAlignmentType)
                    {
                        reason = "WrongAlignment";
                        return false;
                    }
                }
                if (ForcePool != null)
                {
                    if (forceDef.forcePoolCost > 0 &&
                        forceDef.forcePoolCost > ForcePool.CurLevel)
                    {
                        reason = "DrainedForcePool";
                        return false;
                    }
                }

            }
            return true;
        }

        public override List<HediffDef> ignoredHediffs()
        {
            List<HediffDef> newDefs = new List<HediffDef>();
            newDefs.Add(ProjectJediDefOf.PJ_ForceWielderHD);
            return newDefs;
        }



        /// <summary>
        /// This section checks what force abilities were used, and thus their effect on the Jedi's force powers.
        /// </summary>
        public override void PostCastAbilityEffects(Verb_UseAbility verbAbility)
        {
            ForceAbilityDef forceDef = (ForceAbilityDef)verbAbility.useAbilityProps.abilityDef;
            if (forceDef != null)
            {
                if (ForcePool != null)
                {
                    float value = ForcePool.CurLevel - forceDef.forcePoolCost;
                    ForcePool.CurLevel = Mathf.Clamp(value, 0.01f, 0.99f);
                }
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.LookValue<float>(ref this.alignmentValue, "alignmentValue", 0.5f);
            Scribe_Values.LookValue<int>(ref this.forceUserLevel, "forceUserLevel", 0);
            Scribe_Values.LookValue<int>(ref this.forceUserXP, "forceUserXP");
            Scribe_Values.LookValue<bool>(ref this.forcePowersInitialized, "forcePowersInitialized", false);
            //Scribe_Values.LookValue<int>(ref this.levelLightsaberOff, "levelLightsaberOff", 0);
            //Scribe_Values.LookValue<int>(ref this.levelLightsaberDef, "levelLightsaberDef", 0);
            //Scribe_Values.LookValue<int>(ref this.levelLightsaberAcc, "levelLightsaberAcc", 0);
            //Scribe_Values.LookValue<int>(ref this.levelLightsaberRef, "levelLightsaberRef", 0);
            //Scribe_Values.LookValue<int>(ref this.levelForcePool, "levelForcePool", 0);
            Scribe_Values.LookValue<int>(ref this.abilityPoints, "abilityPoints", 0);
            Scribe_Collections.LookList<ForcePower>(ref this.forcePowersDark, "forcePowersDark", LookMode.Deep, null);
            Scribe_Collections.LookList<ForcePower>(ref this.forcePowersGray, "forcePowersGray", LookMode.Deep, null);
            Scribe_Collections.LookList<ForcePower>(ref this.forcePowersLight, "forcePowersLight", LookMode.Deep, null);
            Scribe_Collections.LookList<ForceSkill>(ref this.forceSkills, "forceSkills", LookMode.Deep, null);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {

                if (this.abilityPowerManager == null) this.abilityPowerManager = new AbilityPowerManager(this);

                if (ForcePowersDark != null && ForcePowersDark.Count > 0)
                {
                    foreach (ForcePower power in ForcePowersDark)
                    {
                        if (power.abilityDef != null)
                        {
                            if (power.level > 0)
                            {
                                this.abilityPowerManager.AddPawnAbility(power.abilityDef);
                            }
                        }
                    }
                }

                if (ForcePowersGray != null && ForcePowersGray.Count > 0)
                {
                    foreach (ForcePower power in ForcePowersGray)
                    {
                        if (power.abilityDef != null)
                        {
                            if (power.level > 0)
                            {
                                this.abilityPowerManager.AddPawnAbility(power.abilityDef);
                            }
                        }
                    }
                }

                if (ForcePowersLight != null && ForcePowersLight.Count > 0)
                {
                    foreach (ForcePower power in ForcePowersLight)
                    {
                        if (power.abilityDef != null)
                        {
                            if (power.level > 0)
                            {
                                this.abilityPowerManager.AddPawnAbility(power.abilityDef);
                            }
                        }
                    }
                }

            }

            //Log.Message("PostExposeData Called: ForceUser");
        }
    }
}
