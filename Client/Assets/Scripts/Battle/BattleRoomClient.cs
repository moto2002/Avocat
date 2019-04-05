﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Swift;
using Avocat;
using System.Diagnostics;

/// <summary>
/// 客户端战斗对象
/// </summary>
public class BattleRoomClient : BattleRoom
{
    public int PlayerMe { get; private set; }

    public BattleRoomClient(Battle bt)
        :base(bt)
    {
    }

    // 战斗准备完毕
    public void Prepared()
    {
        PlayerPrepared(PlayerMe);
    }

    // 执行攻击操作
    public override void DoAttack(Warrior attacker, Warrior target)
    {
        Debug.Assert(!attacker.IsOpponent, "attacker should be in my team");
        base.DoAttack(attacker, target);
    }
}
