﻿// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using System;
using Aura.Channel.Network.Sending;
using Aura.Channel.Skills.Base;
using Aura.Channel.World.Entities;
using Aura.Data;
using Aura.Data.Database;
using Aura.Mabi;
using Aura.Mabi.Const;
using Aura.Shared.Util;

namespace Aura.Channel.Skills.Life
{
	/// <summary>
	/// Handles the Rest skill. Also called when using a chair.
	/// </summary>
	/// <remarks>
	/// Var1: Life regen multiplicator
	/// Var2: Stamina regen multiplicator
	/// Var3: Injury regen
	/// </remarks>
	[Skill(SkillId.Rest)]
	public class Rest : StartStopSkillHandler
	{
		/// <summary>
		/// Starts rest skill.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skill"></param>
		/// <param name="dict"></param>
		/// <returns></returns>
		public override StartStopResult Start(Creature creature, Skill skill, MabiDictionary dict)
		{
			creature.StopMove();

			var chairItemEntityId = dict.GetLong("ITEMID");

			if (chairItemEntityId != 0)
				this.SetUpChair(creature, chairItemEntityId);

			creature.Activate(CreatureStates.SitDown);
			if (skill.Info.Rank >= SkillRank.R9)
				creature.Activate(CreatureStatesEx.RestR9);

			Send.SitDown(creature);

			var bonusLife = ((skill.RankData.Var1 - 100) / 100);
			var bonusStamina = ((skill.RankData.Var2 - 100) / 100);
			var bonusInjury = skill.RankData.Var3;

			// Add bonus from campfire
			// TODO: Check for disappearing of campfire? (OnDisappears+Recheck)
			var campfires = creature.Region.GetProps(a => a.Info.Id == 203 && a.GetPosition().InRange(creature.GetPosition(), 500));
			if (campfires.Count > 0)
			{
				// Campfire rank? Mine? The fire's?
				bonusLife *= 1.5f;
				bonusStamina *= 1.5f;
				bonusInjury *= 1.5f;

				Send.Notice(creature, Localization.Get("The fire feels very warm"));
			}

			creature.Regens.Add("Rest", Stat.Life, (0.12f * bonusLife), creature.LifeMax);
			creature.Regens.Add("Rest", Stat.Stamina, (0.4f * bonusStamina), creature.StaminaMax);
			creature.Regens.Add("Rest", Stat.LifeInjured, bonusInjury, creature.LifeMax); // TODO: Test if LifeInjured = Injuries

			if (skill.Info.Rank == SkillRank.Novice) skill.Train(1); // Use Rest.

			return StartStopResult.Okay;
		}

		/// <summary>
		/// Stops rest skill, called when moving or stopping it.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skill"></param>
		/// <param name="dict"></param>
		/// <returns></returns>
		public override StartStopResult Stop(Creature creature, Skill skill, MabiDictionary dict)
		{
			creature.Deactivate(CreatureStates.SitDown);
			if (skill.Info.Rank >= SkillRank.R9)
				creature.Deactivate(CreatureStatesEx.RestR9);

			Send.StandUp(creature);

			creature.Regens.Remove("Rest");

			if (creature.Temp.SittingProp != null)
				this.RemoveChair(creature);

			return StartStopResult.Okay;
		}

		/// <summary>
		/// Creates sitting prop, fails silently if item or chair
		/// data doesn't exist.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="chairItemEntityId"></param>
		private void SetUpChair(Creature creature, long chairItemEntityId)
		{
			if (chairItemEntityId == 0)
				return;

			// Check item
			var item = creature.Inventory.GetItem(chairItemEntityId);
			if (item == null || item.Data.Type != ItemType.Misc)
				return;

			// Get chair data
			var chairData = AuraData.ChairDb.Find(item.Info.Id);
			if (chairData == null)
				return;

			var pos = creature.GetPosition();

			// Effect
			if (chairData.Effect != 0)
				Send.Effect(creature, chairData.Effect, true);

			// Chair prop
			var sittingProp = new Prop((!creature.IsGiant ? chairData.PropId : chairData.GiantPropId), creature.RegionId, pos.X, pos.Y, MabiMath.ByteToRadian(creature.Direction));
			sittingProp.Info.Color1 = item.Info.Color1;
			sittingProp.Info.Color2 = item.Info.Color2;
			sittingProp.Info.Color3 = item.Info.Color3;
			sittingProp.State = "stand";
			creature.Region.AddProp(sittingProp);

			// Move char
			Send.AssignSittingProp(creature, sittingProp.EntityId, 1);

			// Update chair
			sittingProp.Xml.SetAttributeValue("OWNER", creature.EntityId);
			sittingProp.Xml.SetAttributeValue("SITCHAR", creature.EntityId);

			Send.PropUpdate(sittingProp);

			creature.Temp.CurrentChairData = chairData;
			creature.Temp.SittingProp = sittingProp;
		}

		/// <summary>
		/// Removes current chair prop.
		/// </summary>
		/// <param name="creature"></param>
		private void RemoveChair(Creature creature)
		{
			if (creature.Temp.SittingProp == null || creature.Temp.CurrentChairData == null)
				return;

			// Effect
			if (creature.Temp.CurrentChairData.Effect != 0)
				Send.Effect(creature, creature.Temp.CurrentChairData.Effect, false);

			// Update chair
			creature.Temp.SittingProp.Xml.SetAttributeValue("OWNER", 0);
			creature.Temp.SittingProp.Xml.SetAttributeValue("SITCHAR", 0);

			Send.PropUpdate(creature.Temp.SittingProp);

			Send.AssignSittingProp(creature, 0, 0);

			// Remove chair in 1s
			creature.Temp.SittingProp.DisappearTime = DateTime.Now.AddSeconds(1);

			creature.Temp.SittingProp = null;
		}
	}
}
