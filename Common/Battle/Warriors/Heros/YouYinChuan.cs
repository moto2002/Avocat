﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Swift;

namespace Avocat
{
    /// <summary>
    /// 游川隐
    /// 剑道，一闪
    /// </summary>
    public class YouYinChuan : Hero
    {
        public YouYinChuan(Battle bt)
            : base(bt)
        {
            Name = "游川隐";
            SetupBuffAndSkills(null, new Kendo(), new FlashAttack());
        }
    }
}
