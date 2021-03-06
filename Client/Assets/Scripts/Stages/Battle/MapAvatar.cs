﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Swift;
using Avocat;
using Spine.Unity;

public class MapAvatar : MonoBehaviour
{
    public BattleStage BattleStage { get; set; }
    public Vector2 CenterOffset = new Vector2(0.5f, 0.5f);

    public TextMesh NameText;
    public TextMesh HpText;
    public TextMesh PowerText;
    public TextMesh ShieldText;
    public GameObject Life;
    public GameObject Shield;
    public GameObject FriendSkillDec;
    public GameObject EnemySkillDec;
    public GameObject AttackHint;


    // 对应的角色
    public Warrior Warrior { get; set; }

    // 延迟刷新属性显示，值按照当前属性值算
    public Action DelayRefreshAttrs()
    {
        var hp = Warrior.HP;
        var atk = Warrior.BasicAttackValue;
        var es = Warrior.ES;
        var actionDone = Warrior.ActionDone;
        return () =>
        {
            if (this != null) // may has been destroyed.
                RefreshAttrs2(hp, atk, es, actionDone);
        };


    }

    // 刷新属性值到指定值
    public void RefreshAttrs2(int hp, int basicAttackValue, int es, bool actionDone)
    {
      //HpText.text = hp.ToString();
       //owerText.text = basicAttackValue.ToString();
       //hieldText.text = es.ToString();
      //ShieldText.transform.parent.gameObject.SetActive(es > 0);
        Color = actionDone ? ColorActionDone : ColorDefault;
        ShowActionDoneMask(Warrior);
        Life.transform.localScale = new Vector3(hp / (float)Warrior.MaxHP,1,1);
        if (Warrior.MaxES != 0)
            Shield.transform.localScale = new Vector3(es / (float)Warrior.MaxES, 1, 1);
        else
            Shield.transform.gameObject.SetActive(false);
    }

    // 刷新属性值
    public void RefreshAttrs()
    {
        NameText.text = Warrior.ID;
        RefreshAttrs2(Warrior.HP, Warrior.BasicAttackValue, Warrior.ES, Warrior.ActionDone);
    }

    public int X
    {
        get
        {
            return x;
        }
        set
        {
            x = value;
            transform.localPosition = new Vector3(x + CenterOffset.x, y + CenterOffset.y);
        }
    } int x;

    public int Y
    {
        get
        {
            return y;
        }
        set
        {
            y = value;
            transform.localPosition = new Vector3(x + CenterOffset.x, y + CenterOffset.y);
        }
    } int y;

    public static readonly Color ColorDefault = Color.black;
    public static readonly Color ColorSelected = Color.blue;
    public static readonly Color ColorActionDone = Color.gray;

    public Color Color
    {
        get
        {
            return transform.Find("Name").Find("Name").GetComponent<TextMesh>().color;
        }
        set
        {
            transform.Find("Name").Find("Name").GetComponent<TextMesh>().color = value;
        }
    }
    public bool IsShowClickFrame
    {
        get
        {
            return transform.Find("ClickFrame").gameObject.activeSelf ? true :false;
        }
        set {
            transform.Find("ClickFrame").gameObject.SetActive(value);
        }
    }

    void SetIdleAni(Warrior warrior, string path)
    {
        var skeletonName = path + warrior.ID + "_SkeletonData";
        var runtimeSkeletonDataAsset = Resources.Load<SkeletonDataAsset>(skeletonName);
        var skeletonAnimation = gameObject.transform.Find("_MapWarrior").GetComponent<SkeletonAnimation>();
        skeletonAnimation.skeletonDataAsset = runtimeSkeletonDataAsset;
        skeletonAnimation.Initialize(true);
        if (warrior.Team != 1) {
            skeletonAnimation.skeleton.scaleX = -1;
        }
        skeletonAnimation.AnimationState.SetAnimation(0, "idle", true);
        gameObject.transform.Find("_MapWarrior").GetComponent<MeshRenderer>().sortingOrder = BattleStage.Map.Height + 2 ;
    }

    public void SetIdleAnimation(Warrior warrior) {
        string path;
        if (warrior is ITransformable)
        {
            path = "Animation/" + warrior.ID + "/" + (warrior as ITransformable).State + "/";
        }
        else {
            path = "Animation/" + warrior.ID + "/";
        }
        SetIdleAni(warrior, path);
    }

    public void ShowActionDoneMask(Warrior warrior)
    {
        if (warrior is Hero)
        {
            var mask = transform.Find("Mask").gameObject;
            if (warrior.ActionDone)
                mask.SetActive(true);
            else
                mask.SetActive(false);
        }
    }
}
