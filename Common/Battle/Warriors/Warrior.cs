﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Swift;

namespace Avocat
{
    /// <summary>
    /// 有变身能力
    /// </summary>
    public interface ITransformable
    {
        string State { set; get; }
    }

    /// <summary>
    /// 战斗角色
    /// </summary>
    public class Warrior : BattleMapObj
    {
        public Warrior(BattleMap map)
            : base(map)
        {
        }

        public WarriorAI AI { get; set; } // AI 类型
        public bool IsBoss { get; set; } = false; // 是否是 boss

        public int AvatarID { get; private set; } // 具体的角色形象 ID
        public int Team { get; set; } // 是属于哪一个玩家

        public int MaxHP { get; set; } // 最大血量
        public int HP { get; set; } // 血量

        public string AttackingType { get; set; } // 攻击类型：physic - 物理，magic - 法术，chaos - 混乱

        public int ATK { get; set; } // 物攻
        public int ATKInc { get; set; } // 物攻加成
        public int ATKMore { get; set; } // 物攻加成
        public int Crit { get; set; } // 暴击系数

        public int POW { get; set; } // 法攻
        public int POWInc { get; set; } // 法攻加成
        public int POWMore { get; set; } // 法攻加成
        public bool IsSkillReleased { get; set; }

        // 模式匹配技能
        public PatternSkill PatternSkill
        {
            get
            {
                foreach (var buff in buffs.Values)
                    if (buff is PatternSkill)
                        return buff as PatternSkill;

                return null;
            }
        }

        // 基本攻击力
        public int BasicAttackValue
        {
            get
            {
                if (AttackingType == "physic")
                    return ATK + POW / 2;
                else if (AttackingType == "magic")
                    return ATK / 2 + POW;
                else if (AttackingType == "chaos")
                    return ATK / 2 + POW / 2;

                Debug.Assert(false, "unknown attacking type: " + AttackingType);
                return 0;
            }
        }

        // 计算防御力评估值
        public int GetEstimatedDefence(string attackType = null)
        {
            if (attackType == "physic")
                return ARM;
            else if (attackType == "magic")
                return RES;
            else
                return (ARM + RES) / 2;
        }

        public int ARM { get; set; } // 物盾
        public int RES { get; set; } // 魔盾

        public int MaxES { get; set; } // 最大护盾
        public int ES { get; set; } // 护盾

        public TileType StandableTiles; // 可以站立的地块类型
        public int[] AttackRange { get; set; } // 最大攻击距离
        public int MoveRange { get; set; } // 最大移动距离

        public bool InAttackRange(int tx, int ty)
        {
            GetPosInMap(out int x, out int y);
            var dist = MU.ManhattanDist(x, y, tx, ty);
            return FC.IndexOf(AttackRange, dist) >= 0;
        }

        public bool Moved { get; set; } // 本回合是否已经移动过
        public bool ActionDone { get; set; } // 角色在本回合的行动已经结束

        public bool IsDead { get { return HP <= 0; } }

        // 角色要移动的路径信息放在角色身上
        public List<int> MovingPath { get; } = new List<int>();

        // 获取对象在地图中位置
        public override void GetPosInMap(out int x, out int y)
        {
            Debug.Assert(Map != null, "warrior is not in map now");
            Map.FindXY(this, out x, out y);
        }

        #region 技能相关

        // 所有主动技能
        string defaultSkillName = null;
        Dictionary<string, ActiveSkill> activeSkills = new Dictionary<string, ActiveSkill>();

        // 添加主动技能
        public void AddActiveSkill(ActiveSkill skill, bool asDefaultActiveSkill = true)
        {
            var name = skill.ID;
            Debug.Assert(!activeSkills.ContainsKey(name), "skill named: " + name + " has aleardy existed. Use ReplaceActiveSkill to replace it.");
            activeSkills[name] = skill;

            if (asDefaultActiveSkill)
                defaultSkillName = name;
        }

        // 替换同名主动技能
        public ActiveSkill ReplaceActiveSkill(ActiveSkill skill)
        {
            var s = activeSkills[skill.ID];
            activeSkills.Remove(skill.ID);
            AddActiveSkill(skill, defaultSkillName == skill.ID);
            return s;
        }

        // 获取主动技能
        public ActiveSkill GetActiveSkillByName(string name)
        {
            return activeSkills.ContainsKey(name) ? activeSkills[name] : null;
        }

        // 获取主动技能
        public T GetActiveSkill<T>() where T : ActiveSkill
        {
            foreach (var s in activeSkills.Values)
            {
                if (s is T)
                    return s as T;
            }

            return null;
        }

        // 获取默认主动技能
        public ActiveSkill GetDefaultActiveSkill()
        {
            return defaultSkillName == null ? null : GetActiveSkillByName(defaultSkillName);
        }

        // 移除主动技能
        public void RemoveActiveSkill(ActiveSkill skill)
        {
            Debug.Assert(skill.Owner == this, "skill named: " + skill.ID + " doest not exist.");
            RemoveActiveSkill(skill.ID);
        }

        // 移除主动技能
        public void RemoveActiveSkill(string name)
        {
            Debug.Assert(activeSkills.ContainsKey(name) && activeSkills[name].Owner == this, "skill named: " + name + " doest not exist.");
            activeSkills.Remove(name);
            if (defaultSkillName == name)
                defaultSkillName = null;
        }

        #endregion

        #region buff 相关

        Dictionary<string, Buff> buffs = new Dictionary<string, Buff>();
        public Buff[] Buffs { get => buffs.Values.ToArray(); }
        public Buff GetBuffByID(string id) => buffs.ContainsKey(id) ? buffs[id] : null;

        // 不应该直接调用，应该从 Battle.AddBuff 走
        public void AddOrOverBuffInternal(ref Buff buff)
        {
            if ((GetBuffByID(buff.ID) is Buff b))
            {
                if (b is CountDownBuff)
                    (b as CountDownBuff).ExpandRound((buff as CountDownBuff).Num);

                if (b is ISkillWithOverlays)
                    (b as ISkillWithOverlays).ExpandOverlay((buff as ISkillWithOverlays).Overlays);

                buff = b;
            }
            else
                buffs[buff.ID] = buff;
        }

        // 不应该直接调用，应该从 Battle.RemoveBuff 走
        public void RemoveBuffInternal(Buff ps)
        {
            Debug.Assert(buffs.ContainsKey(ps.ID) && buffs[ps.ID] == ps, "buff " + ps.ID + " has not been attached to target " + ID);
            buffs.Remove(ps.ID);
        }

        // 获取指定类型的 buff 或被动技能
        public T GetBuffSkill<T>() where T : Buff
        {
            foreach (var ps in buffs.Values)
                if (ps is T)
                    return ps as T;

            return null;
        }

        #endregion

        // 获取包括自己在内的同队队友
        public Warrior[] GetTeamMembers(bool includingSelf = false)
        {
            var members = new List<Warrior>();
            Battle.Map.ForeachObjs<Warrior>((x, y, w) =>
            {
                if (includingSelf || w != this)
                    members.Add(w);
            });

            return members.ToArray();
        }
    }
    public static class WarriorEx
    {
        public static string DisPlayName(this Warrior warrior)
        {
            return ClientConfiguration.GetAttribute<Warrior, string>(warrior, "DisplayName");
        }
    }
       
}