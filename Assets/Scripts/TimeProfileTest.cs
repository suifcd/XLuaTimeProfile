using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XLua;

/// <summary>
/// 使用时先通过菜单XLua/TimeProfile开启编辑器窗口
/// </summary>
public class TimeProfileTest : MonoBehaviour
{
    private string ss =
        @"local profile = require ('perf.profiler')
        profile.start() 
        local a = 1 
        local go = CS.UnityEngine.GameObject();
        local text = profile.report() 
        CS.LuaUtils.TimeProfile('test',text)
        profile.stop() ";

	private void Start()
    {
        LuaEnv luaenv = new LuaEnv();
        luaenv.DoString(ss);
        luaenv.Dispose();
        
    }
}
