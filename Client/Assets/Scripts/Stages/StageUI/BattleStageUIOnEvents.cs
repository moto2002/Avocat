﻿using Avocat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

static class BattleStageUIOnEvents
{
    public static void SetupEventHandler(this BattleStageUI BattleStageUI, BattleRoomClient room)
    {
        var bt = BattleStageUI.BattleStage.Battle as BattlePVE;

        bt.OnPlayerPrepared.Add((int player) =>
        {
            if (room.Battle.AllPrepared)
            {
                BattleStageUI.gameObject.SetActive(true);
                BattleStageUI.RefreshEnergy(bt.Energy);
                BattleStageUI.RefreshItemUsage(bt.CardUsage);
            }
        });

        bt.OnBattleEnded.Add((winner) =>
        {
            BattleStageUI.gameObject.SetActive(false);
        });

        bt.OnActionDone.Add((player) =>
        {
            if (player != room.PlayerMe)
                return;

            BattleStageUI.RefreshCardsAvailable(bt.AvailableCards);
        });

        bt.OnAddBattleCard.Add((card) =>
        {
            BattleStageUI.RefreshCardsAvailable(bt.AvailableCards);
        });

        bt.OnNextRoundStarted.Add((player) =>
        {
            if (player != room.PlayerMe)
                return;

            var availableCards = new List<BattleCard>();
            availableCards.AddRange(bt.AvailableCards);
            BattleStageUI.RefreshCardsAvailable(availableCards);
        });

        bt.OnCardsConsumed.Add((warrior, cards) =>
        {
            if (warrior.Team != room.PlayerMe)
                return;

            var availableCards = new List<BattleCard>();
            availableCards.AddRange(bt.AvailableCards);
            BattleStageUI.RefreshCardsAvailable(availableCards);
        });

        bt.OnBattleCardsExchange.Add((int g1, int n1, int g2, int n2) =>
        {
            BattleStageUI.RefreshCardsAvailable(bt.AvailableCards);
            BattleStageUI.RefreshCardsStarshed(bt.StashedCards);
        });

        bt.OnAddEN.Add((den) => BattleStageUI.RefreshEnergy(bt.Energy));
        bt.OnAddCardDissambleValue.Add((dv) => BattleStageUI.RefreshItemUsage(bt.CardUsage));

        InBattleOps.OnCurrentWarriorChanged += () => BattleStageUI.RefreshEnergy(bt.Energy);
    }
}
