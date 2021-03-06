﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avocat;
using Swift;
using System.Diagnostics;
using System.Collections;

/// <summary>
/// 战斗消息回环，用于模拟网络消息从服务器到客户端的往返
/// </summary>
public class BattleMessageLooper : IBattlemessageProvider, IBattleMessageSender
{
    int SeqNo = 0;
    Queue<byte[]> msgs = new Queue<byte[]>();

    public void SendRaw(byte[] data)
    {
        msgs.Enqueue(data);
    }

    public void Send(string op, Action<IWriteableBuffer> w = null)
    {
        var buff = new WriteBuffer();
        buff.Write(op);
        buff.Write(1); // the test player
        w.SC(buff);
        msgs.Enqueue(buff.Data);
    }

    public Action<int, byte[]> OnMessageIn { get; set; }

    public void Clear()
    {
        msgs.Clear();
        hs.Clear();
        SeqNo = 0;
    }

    Dictionary<string, Action<int, IReadableBuffer>> hs = new Dictionary<string, Action<int, IReadableBuffer>>();
    public void HandleMsg(string msg, Action<int, IReadableBuffer> r)
    {
        hs[msg] = r;
    }

    public IEnumerator Loop()
    {
        while (true)
        {
            if (msgs.Count > 0)
            {
                var data = msgs.Dequeue();

                OnMessageIn.SC(++SeqNo, data);

                var buff = new RingBuffer();
                buff.Write(data);

                var op = buff.ReadString();
                var player = buff.ReadInt();
                Debug.Assert(hs.ContainsKey(op), "no handler for message: " + op);
                hs[op](player, buff);
            }
            else
                yield return null;
        }
    }
}
