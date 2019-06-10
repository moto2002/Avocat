﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Swift;
using Swift.AStar;
using Swift.Math;

namespace Avocat
{
    /// <summary>
    /// 地块类型
    /// </summary>
    public enum TileType : int
    {
        None = 0,

        Water = 0x01,
        Lava = 0x02,
        Grass = 0x04,
        Rock = 0x08,
        Soli = 0x10,
        Hill = 0x20,

        All = 0xFFFF,
    }

    /// <summary>
    /// 战斗地图
    /// </summary>
    public class BattleMap
    {
        public Battle Battle { get; set; }

        // 用来给加入地图的 item 进行 id 分配
        public int ItemIDInMap
        {
            get
            {
                return ++itemInMapIndex;
            }
        } int itemInMapIndex = 0;

        public BattleMap(int w, int h, Func<int, int, TileType> tilesFun = null)
        {
            Width = w;
            Height = h;
            Tiles = new TileType[w, h];
            if (tilesFun != null)
                ResetTiles(tilesFun);
            objs = new BattleMapObj[w, h];
            BuildPathFinder();
        }

        // 地图尺寸
        public int Width { get; private set; }
        public int Height { get; private set; }

        // 所有地块底图
        public TileType[,] Tiles { get; private set; }
        public void ResetTiles(Func<int, int, TileType> fun)
        {
            FC.For2(Width, Height, (x, y) => Tiles[x, y] = fun(x, y));
        }

        // 所有地图上的占位对象
        public readonly BattleMapObj[,] objs;

        // 获取指定位置的对象
        public T GetAt<T>(int x, int y) where T : BattleMapObj
        {
            return (x < 0 || y < 0 || x >= Width || y >= Height) ? null : objs[x, y] as T;
        }

        // 迭代所有非空道具
        public void ForeachObjs(Action<int, int, BattleMapObj> act, Func<bool> continueCondition = null, Func<BattleMapObj, bool> filter = null)
        {
            FC.For2(Width, Height, (x, y) =>
            {
                var obj = objs[x, y];
                if (obj != null && (filter == null || filter(obj)))
                    act(x, y, obj);
            }, continueCondition);
        }

        // 迭代所有非空道具
        public void ForeachObjs<T>(Action<int, int, T> act, Func<bool> continueCondition = null) where T : BattleMapObj
        {
            ForeachObjs((x, y, obj) => act(x, y, obj as T), continueCondition, (obj) => obj is T);
        }

        // 查找指定角色的地图坐标
        public void FindXY(BattleMapObj target, out int px, out int py)
        {
            Debug.Assert(target != null, "the target should not be null");
            Debug.Assert(target.Map == this, "the obj is not in the map : " + target.DisplayName);

            var found = false;
            var tx = 0;
            var ty = 0;
            ForeachObjs((x, y, obj) =>
            {
                if (obj == target)
                {
                    tx = x;
                    ty = y;
                    found = true;
                }
            }, () => !found);

            Debug.Assert(found, "can not find the obj in map : " + target.DisplayName);

            px = tx;
            py = ty;
        }

        // 移除地图对象
        public void RemoveObj(BattleMapObj obj)
        {
            Debug.Assert(obj == null || obj.Map == this, "the warrior is not in the map");
            FindXY(obj, out int x, out int y);
            objs[x, y] = null;
        }

        // 添加对象到地图
        public void SetObjAt(int x, int y, BattleMapObj obj)
        {
            Debug.Assert(obj == null || obj.Map == this, "no warrior on the map specified");

            if (objs[x, y] == obj)
                return;

            if (obj != null && obj.Map == this)
                objs[x, y] = null;

            if (objs[x, y] != null)
                RemoveObj(objs[x, y]);

            objs[x, y] = obj;
        }

        // 根据 id 寻找指定对象
        public T GetByID<T>(int idInMap) where T : BattleMapObj
        {
            T target = null;
            ForeachObjs((x, y, obj) =>
            {
                if (obj.IDInMap == idInMap)
                    target = obj as T;
            }, () => target == null);

            return target;
        }

        // 判断指定位置是否已经被占据
        public bool BlockedAt(int x, int y, TileType tileMask)
        {
            return (x < 0 || x >= Width || y < 0 || y >= Height) ? true : objs[x, y] != null || (Tiles[x, y] & tileMask) == 0;
        }

        // 寻找最近目标
        public Warrior FindNearestTarget(Warrior warrior)
        {
            Warrior nearestTarget = null;
            int tx = 0;
            int ty = 0;

            warrior.GetPosInMap(out int fx, out int fy);
            warrior.Map.ForeachObjs<Warrior>((x, y, target) =>
            {
                // 过滤队友和已经死亡的敌人
                if (target.IsDead || target.Team == warrior.Team)
                    return;

                if (nearestTarget == null || MU.ManhattanDist(fx, fy, x, y) < MU.ManhattanDist(fx, fy, tx, ty))
                {
                    tx = x;
                    ty = y;
                    nearestTarget = target;
                }
            });

            return nearestTarget;
        }

        #region 寻路相关

        // 检查指定区域是否被占用
        public bool CheckSpareSpace(int cx, int cy, int r, TileType tileMask)
        {
            var free = true;
            FC.RectFor(cx, cx, cy, cy, (x, y) =>
            {
                free = x >= 0 && x < Width
                        && y >= 0 && y < Height
                        && !BlockedAt(x, y, tileMask);
            }, () => free);

            return free;
        }

        class PathNode : IPathNode<KeyValuePair<int, int>>
        {
            public Vec2 Pos { get; private set; }
            Func<int, int, int, TileType, bool> CheckSpareSpace = null;

            public PathNode(int x, int y, Func<int, int, int, TileType, bool> checker)
            {
                Pos = new Vec2(x, y);
                CheckSpareSpace = checker;
            }

            public Boolean IsWalkable(KeyValuePair<int, int> ps)
            {
                return CheckSpareSpace((int)Pos.x, (int)Pos.y, ps.Key, (TileType)ps.Value);
            }
        }

        PathNode[,] pathNodes = null;
        SpatialAStar<PathNode, KeyValuePair<int, int>> pathFinder = null;

        // 构建寻路器
        void BuildPathFinder()
        {
            pathNodes = new PathNode[Width, Height];
            FC.For2(Width, Height, (x, y) => pathNodes[x, y] = new PathNode(x, y, CheckSpareSpace));
            pathFinder = new SpatialAStar<PathNode, KeyValuePair<int, int>>(pathNodes) { Only4Adjacent = true };
        }

        // 寻路
        public List<int> FindPath(int fx, int fy, int tx, int ty, int maxSteps, TileType tileMask)
        {
            var path = new List<int>();

            var nodes = pathFinder.Search(fx, fy, tx, ty, new KeyValuePair<int, int>(maxSteps, (int)tileMask), true);
            if (nodes != null)
            {
                nodes.RemoveFirst(); // remove the src node
                FC.ForEach(nodes, (i, n) =>
                {
                    path.Add((int)n.Pos.x);
                    path.Add((int)n.Pos.y);
                }, () => path.Count < maxSteps * 2);
            }

            return path;
        }

        #endregion
    }
}
