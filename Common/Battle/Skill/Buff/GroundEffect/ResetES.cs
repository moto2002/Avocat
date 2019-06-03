﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avocat
{
    /// <summary>
    /// 回合开始重置护盾
    /// </summary>
    public class ResetES : Buff
    {
        public override void OnAttached()
        {
            Battle.BeforeStartNextRound += (int player) =>
            {
                Map.ForeachWarrior((i, j, warrior) =>
                {
                    if (warrior.Team != player)
                        return;

                    warrior.ES = 0; // 重置所有护甲
                });
            };

            base.OnAttached();
        }
    }
}
