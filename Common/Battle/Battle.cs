﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using Swift;

namespace Avocat
{
    /// <summary>
    /// 战斗对象，包括一场战斗所需全部数据，在初始数据和操作过程完全一致的情况下，可完全复现一场战斗过程。
    /// 但战斗对象本身不包括战斗驱动过程，而是有 BattleRoom 驱动。
    /// </summary>
    public abstract partial class Battle
    {
        // 由初始随机种子确定的伪随机序列
        SRandom Srand { get; set; }

        // 战斗地图
        public BattleMap Map { get; private set; }

        public Battle(int mapWidth, int mapHeight, int randSeed)
        {
            Srand = new SRandom(randSeed);
            Map = new BattleMap(mapWidth, mapHeight);
        }

        // 移除指定角色
        public void RemoveWarrior(Warrior warrior)
        {
            warrior.GetPosInMap(out int x, out int y);
            Map.Warriors[x, y] = null;
            warrior.Map = null;
        }

        #region 战斗准备过程

        // 交换英雄位置
        event Action<int, int, int, int> BeforeExchangeWarroirsPosition = null;
        event Action<int, int, int, int> AfterExchangeWarroirsPosition = null;
        public event Action<int, int, int, int> OnWarriorPositionExchanged = null;
        public void ExchangeWarroirsPosition(int fx, int fy, int tx, int ty)
        {
            BeforeExchangeWarroirsPosition.SC(fx, fy, tx, ty);

            var item = Map.Warriors[fx, fy];
            Map.Warriors[fx, fy] = Map.Warriors[tx, ty];
            Map.Warriors[tx, ty] = item;

            AfterExchangeWarroirsPosition.SC(fx, fy, tx, ty);

            OnWarriorPositionExchanged.SC(fx, fy, tx, ty);
        }

        // 完成战斗准备
        event Action<int> BeforePlayerPrepared = null;
        event Action<int> AfterPlayerPrepared = null;
        public abstract void PlayerPreparedImpl(int player);
        public event Action<int> OnPlayerPrepared = null; // 有玩家完成战斗准备
        public void PlayerPrepared(int player)
        {
            BeforePlayerPrepared.SC(player);

            PlayerPreparedImpl(player);

            AfterPlayerPrepared.SC(player);

            OnPlayerPrepared.SC(player);
        }

        // 检查所有玩家是否都已经完成战斗准备
        public abstract bool AllPrepared { get; }

        // 检查战斗结束条件，0 表示尚未结束，否则返回值表示胜利玩家
        // 检查战斗结束条件
        public virtual int CheckEndCondition()
        {
            // 双方至少各存活一个角色
            var team1Survived = false;
            var team2Survived = false;
            FC.For2(Map.Width, Map.Height, (x, y) =>
            {
                var warrior = Map.Warriors[x, y];
                if (warrior != null && !warrior.IsDead)
                {
                    if (warrior.Owner == 1)
                        team1Survived = true;
                    else
                        team2Survived = true;
                }
            }, () => !team1Survived || !team2Survived);

            if (team1Survived && !team2Survived)
                return 1;
            else if (team2Survived && !team1Survived)
                return 2;
            else
                return 0;
        }

        #endregion

        #region 战斗过程

        // 迭代所有非空战斗角色
        public void ForeachWarriors(Action<int, int, Warrior> act, Func<bool> continueCondition = null)
        {
            FC.For2(Map.Width, Map.Height, (x, y) =>
            {
                var warrior = Map.Warriors[x, y];
                if (warrior != null)
                    act(x, y, warrior);

            }, continueCondition);
        }

        // 回合开始，重置所有角色行动标记
        event Action<int> BeforeStartNextRound = null;
        event Action<int> AfterStartNextRound = null;
        public event Action<int> OnNextRoundStarted = null;
        public void StartNextRound(int player)
        {
            BeforeStartNextRound.SC(player);

            ForeachWarriors((i, j, warrior) =>
            {
                warrior.Moved = false;
                warrior.ActionDone = false;
            });

            AfterStartNextRound.SC(player);

            OnNextRoundStarted.SC(player);
        }

        // 角色沿路径移动
        event Action<Warrior> BeforeMoveOnPath = null;
        event Action<Warrior> AfterMoveOnPath = null;
        public event Action<Warrior, List<int>> OnWarriorMovingOnPath = null; // 角色沿路径移动
        public void MoveOnPath(Warrior warrior)
        {
            BeforeMoveOnPath.SC(warrior);

            var lstPathXY = warrior.MovingPath;
            var fx = lstPathXY[0];
            var fy = lstPathXY[1];
            lstPathXY.RemoveRange(0, 2);

            Debug.Assert(!warrior.Moved, "attacker has already moved in this round");
            Debug.Assert(warrior == Map.Warriors[fx, fy], "the warrior has not been right on the start position: " + fx + ", " + fy);

            List<int> movedPath = new List<int>(); // 实际落实了的移动路径
            movedPath.Add(fx);
            movedPath.Add(fy);

            while (lstPathXY.Count >= 2)
            {
                var tx = lstPathXY[0];
                var ty = lstPathXY[1];

                fx = tx;
                fy = ty;
                lstPathXY.RemoveRange(0, 2);
                movedPath.Add(fx);
                movedPath.Add(fy);

                ExchangeWarroirsPosition(fx, fy, tx, ty);
            }

            warrior.Moved = true;

            AfterMoveOnPath.SC(warrior);
            OnWarriorMovingOnPath.SC(warrior, movedPath);
        }

        // 执行攻击
        event Action<Warrior, Warrior> BeforeAttack = null;
        event Action<Warrior, Warrior> AfterAttack = null;
        public event Action<Warrior, Warrior> OnWarriorAttack = null; // 角色进行攻击
        public event Action<Warrior> OnWarriorDying = null; // 角色死亡
        public void Attack(Warrior attacker, Warrior target)
        {
            Debug.Assert(!attacker.ActionDone, "attacker has already attacted in this round");

            BeforeAttack.SC(attacker, target);

            target.Shield -= attacker.Power;
            if (target.Shield < 0)
            {
                target.Hp += target.Shield;
                target.Shield = 0;
            }

            attacker.ActionDone = true;

            AfterAttack.SC(attacker, target);

            OnWarriorAttack.SC(attacker, target);

            if (target.IsDead)
            {
                OnWarriorDying.SC(target);
                RemoveWarrior(target);
            }
        }

        // 玩家本回合行动结束
        event Action<int> BeforeActionDone = null;
        event Action<int> AfterActionDone = null;
        public Action<int> OnActionDone = null;
        public void ActionDone(int player)
        {
            BeforeActionDone.SC(player);
            AfterActionDone.SC(player);
            OnActionDone.SC(player);
        }

        #endregion
    }
}