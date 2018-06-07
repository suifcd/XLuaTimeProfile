using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System;

[XLua.LuaCallCSharp]
public class LuaUtils : MonoBehaviour
{
    public static void TimeProfile(string name,string text)
    {
        MessageEvent.BroadCast("TimeProfile", name, text);
    }
}
