﻿// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aura.Channel.Scripting.Scripts
{
	/// <summary>
	/// Simple base class for region scripts.
	/// </summary>
	public class RegionScript : BaseScript
	{
		public virtual void LoadWarps()
		{
		}

		public virtual void LoadSpawns()
		{
		}
	}
}
