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
    public class ResetES : BattleBuff
    {
        public override string ID { get => "ResetES"; }
        public ResetES(Battle bt) : base(bt) { }

        public override void OnAttached()
        {
            Battle.BeforeStartNextRound1 += (int team) =>
            {
                Map.ForeachObjs<Warrior>((i, j, warrior) =>
                {
                    if (warrior.Team != team)
                        return;

                    warrior.ES = 0; // 重置所有护甲
                });
            };

            base.OnAttached();
        }

        public override void OnDetached()
        {
            throw new Exception("not implemented yet");
        }
    }
}
