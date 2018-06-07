using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public delegate void Callback(params object[] data);

public class MessageEvent
{
    private static Dictionary<string, List<Callback>> eventDic = new Dictionary<string, List<Callback>>();
    private static List<List<Callback>> funcList = new List<List<Callback>>();

    public static void AddListener(string key, Callback callBack)
    {
        List<Callback> list = null;
        if (eventDic.TryGetValue(key, out list))
        {
            list.Add(callBack);
        }
        else
        {
            list = new List<Callback>();
            list.Add(callBack);

            funcList.Add(list);
            eventDic.Add(key, list);
        }
    }

    public static void RemoveListener(string key, Callback callBack)
    {
        List<Callback> list = null;
        if (eventDic.TryGetValue(key, out list))
        {
            list.Remove(callBack);
            if (list.Count == 0)
            {
                eventDic.Remove(key);
                funcList.Remove(list);
            }
        }
        else
        {
            Debug.Log("remove wrong no key");
        }
    }

    public static void BroadCast(string key, params object[] objs)
    {
        List<Callback> list = null;
        if (eventDic.TryGetValue(key, out list))
        {
            for (int i = 0; i < list.Count; i++)
            {
                list[i](objs);
            }
        }
        else
        {
            Debug.Log("broadcast wrong no key");
        }
    }
}