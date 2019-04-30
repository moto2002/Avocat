﻿using Swift;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avocat
{
    public interface ISkillWithAXY // 一种伤害计算类型，具体详见数值文档
    {
        int A { get; }
        int X { get; }
        int Y { get; }
    }

    /// <summary>
    /// 所有主被动技能
    /// </summary>
    public abstract class Skill
    {
        public virtual string Name { get; } // 每个技能必须唯一
        public virtual Warrior Warrior { get; set; } // 技能在哪个角色身上
    }

    /// <summary>
    /// 所有 buff 效果基础结构
    /// </summary>
    public abstract class Buff : Skill
    {
        public virtual Battle Battle { get; set; } // 所属战斗对象
        public virtual BattleMap Map { get { return Battle?.Map; } }
        public virtual IEnumerator OnAttached() { yield return null; }
        public virtual IEnumerator OnDetached() { yield return null; }
    }

    /// <summary>
    /// 回合计数的 buff
    /// </summary>
    public abstract class BuffCountDown : Buff
    {
        public int MaxNum { get; set; } = 0;
        public int Num { get; set; } = 0;

        public BuffCountDown(int num)
        {
            Num = num;
        }

        // 叠加回合数
        public void Expand(int addtionalNum)
        {
            Num = (Num + addtionalNum).Clamp(0, MaxNum);
        }

        IEnumerator CountDown(int player)
        {
            if (player != Warrior.Team)
                yield break;

            Num--;
            if (Num <= 0)
                yield return Battle.RemoveBuff(this);
        }

        public override IEnumerator OnAttached()
        {
            Battle.BeforeActionDone.Add(CountDown);
            yield return base.OnAttached();
        }

        public override IEnumerator OnDetached()
        {
            Battle.BeforeActionDone.Del(CountDown);
            yield return base.OnDetached();
        }
    }
}