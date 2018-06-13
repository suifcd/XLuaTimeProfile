using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;

[InitializeOnLoad]
public class LuaTimeProfileWin : EditorWindow
{
    private static LuaTimeProfileWin profileWin;

    [MenuItem("XLua/TimeProfile")]
    public static EditorWindow ShowWindow()
    {
        if(profileWin == null)
        {
            profileWin = GetWindow<LuaTimeProfileWin>(false, "LuaTimeProfile", true);
        }
        return profileWin;
    }

    public LuaTimeProfileWin()
    {
        //注册事件监听
        //Notify.Event.register("LuaTimeProfile", this, SnapShot);
        MessageEvent.AddListener("TimeProfile", SnapShot);
    }

    ~LuaTimeProfileWin()
    {
        //反注册事件监听
        //Notify.Event.deregister(this);
        MessageEvent.RemoveListener("TimeProfile", SnapShot);
    }

    [System.Serializable]
    private class LuaTimeProfile
    {
        public string Id;
        public string Name;
        public string Time;
        public LuaTimeProfileItem SelectItem;
        public List<LuaTimeProfileItem> ItemList;

        public LuaTimeProfile(int count)
        {
            Id = DateTime.UtcNow.Ticks.ToString();
            Name = string.Format("Snap{0}", count);
            Time = string.Format("{0:MM/dd-HH:mm}", DateTime.Now);
            ItemList = new List<LuaTimeProfileItem>();
        }

        public LuaTimeProfile(string profileName)
        {
            Id = DateTime.UtcNow.Ticks.ToString() + UnityEngine.Random.Range(1,100000);
            Name = profileName;
            Time = string.Format("{0:MM/dd-HH:mm}", DateTime.Now);
            ItemList = new List<LuaTimeProfileItem>();
        }
    }

    [System.Serializable]
    private class LuaTimeProfileItem
    {
        public string RawText;

        public string Function;
        public string Source;
        public float Total;
        public float Average;
        public string Relative;
        public int Called;
    }

    private enum ItemSortType
    {
        Function = 1,
        Source = 2,
        Total = 3,
        Average = 4,
        Relative = 5,
        Called = 6,
    }

    private int m_snapCount;
    private List<LuaTimeProfile> m_snaps = new List<LuaTimeProfile>();
    private LuaTimeProfile m_selectSnap;
    private Vector2 m_snapScroll;
    private Vector2 m_itemScroll;

    private ItemSortType m_sortType = ItemSortType.Function;
    private bool m_sortDir = true;//排序方向
    private float itemWidth = 120;

    private bool m_ignoreTotalZero;
    private bool m_ignoreC;
    private bool m_ignoreCSharp;

    private GUIStyle m_titleStyle = new GUIStyle();

    private void OnEnable()
    {
        m_titleStyle.fontSize = 24;
        m_titleStyle.fontStyle = FontStyle.Bold;

        m_snapCount = 0;
    }

    private void OnGUI()
    {
        float height = Screen.height - 120;
        UpView();
        LeftView(height);
        RightView(height);
    }


    private void UpView()
    {
        if (m_selectSnap != null)
        {
            GUI.Label(new Rect((Screen.width - 200) / 2 + 100, 20, 200, 20), m_selectSnap.Name, m_titleStyle);
        }
        GUILayout.BeginArea(new Rect(30, 30, Screen.width, 40));
        UpBtnView();
        IgnoreView();
        GUILayout.EndArea();
    }

    private void UpBtnView()
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Load", GUILayout.Height(20), GUILayout.Width(80)))
        {
            string filePath = EditorUtility.OpenFilePanel("加载Lua时间性能分析文件", "", "txt");
            string json = File.ReadAllText(filePath);
            if (!string.IsNullOrEmpty(json))
            {
                LuaTimeProfile lm = JsonUtility.FromJson<LuaTimeProfile>(json);
                if (lm != null)
                {
                    for (int i = 0; i < m_snaps.Count; i++)
                    {
                        if (m_snaps[i].Id == lm.Id)
                        {
                            m_selectSnap = m_snaps[i];

                            Debug.LogError("已经存在该文件");
                            return;
                        }
                    }
                    m_selectSnap = lm;
                    m_snaps.Add(lm);
                    SetSortType(m_sortType);
                }
            }
        }

        if (GUILayout.Button("Save", GUILayout.Height(20), GUILayout.Width(80)))
        {
            if (m_selectSnap != null)
            {
                string filePath = EditorUtility.SaveFilePanel("保存Lua内存分析文件", "", m_selectSnap.Name, "txt");
                string json = JsonUtility.ToJson(m_selectSnap);
                File.WriteAllText(filePath, json);
            }
        }

        if (GUILayout.Button("Clear", GUILayout.Height(20), GUILayout.Width(80)))
        {
            m_snaps.Clear();
            m_snapCount = 0;
            m_selectSnap = null;
        }
        GUILayout.EndHorizontal();
    }

    private void IgnoreView()
    {
        GUILayout.BeginHorizontal();

        m_ignoreTotalZero = GUILayout.Toggle(m_ignoreTotalZero, "IgnoreTotalZero", GUILayout.Width(120));
        m_ignoreC = GUILayout.Toggle(m_ignoreC, "IgnoreC", GUILayout.Width(80));
        m_ignoreCSharp = GUILayout.Toggle(m_ignoreCSharp, "IgnoreC#", GUILayout.Width(80));

        GUILayout.EndHorizontal();
    }

    private void LeftView(float height)
    {
        GUI.Box(new Rect(30, 74, 140, height), "");
        GUILayout.BeginArea(new Rect(30, 75, 140, height));
        m_snapScroll = GUILayout.BeginScrollView(m_snapScroll);
        for (int i = 0; i < m_snaps.Count; i++)
        {
            GUILayout.BeginHorizontal();
            bool isSelectItem = false;
            if (m_snaps[i].Id == m_selectSnap.Id)
            {
                GUI.contentColor = Color.green;
                isSelectItem = true;
            }
            if (GUILayout.Button(m_snaps[i].Name))
            {
                m_selectSnap = m_snaps[i];
            }

            if (GUILayout.Button("x", GUILayout.Width(20)))
            {
                m_snaps.Remove(m_snaps[i]);
                if (isSelectItem && m_snaps.Count > 0)
                {
                    m_selectSnap = m_snaps[0];
                }
                return;
            }
            GUILayout.EndHorizontal();
            GUI.contentColor = Color.white;
        }
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void RightView(float height)
    {
        float width = Screen.width - 200;
        GUI.Box(new Rect(180, 74, width, height), "");
        GUILayout.BeginArea(new Rect(182, 70, width, height));

        float expandWidth = width - 4 * itemWidth;
        float bgWidth = width - 4;
        DrawTitle(expandWidth);
        EditorTools.DrawSeparator(bgWidth);
        DrawItemRow(expandWidth, bgWidth);

        GUILayout.EndArea();
    }

    private void DrawTitle(float width)
    {
        GUILayout.Space(10);
        GUILayout.BeginHorizontal(GUILayout.Height(10));
        ItemSortType type = (ItemSortType)m_sortType;

        DrawTitle(type, ItemSortType.Function, width / 2);
        DrawTitle(type, ItemSortType.Source, width / 2);
        DrawTitle(type, ItemSortType.Total, itemWidth);
        DrawTitle(type, ItemSortType.Average, itemWidth);
        DrawTitle(type, ItemSortType.Relative, itemWidth);
        DrawTitle(type, ItemSortType.Called, itemWidth);

        GUILayout.EndHorizontal();
    }

    private void DrawTitle(ItemSortType type, ItemSortType title, float width)
    {
        if (type == title)
        {
            GUI.color = Color.green;
        }

        string titleStr = title.ToString();
        if (title == ItemSortType.Total || title == ItemSortType.Average)
            titleStr += "(ms)";

        if (GUILayout.Button(titleStr, EditorStyles.label, GUILayout.Width(width)))
        {
            SetSortType(title);
        }
        GUI.color = Color.white;
    }

    private void DrawItemRow(float width, float bgWidth)
    {
        m_itemScroll = GUILayout.BeginScrollView(m_itemScroll, GUILayout.Width(bgWidth));

        if (m_selectSnap != null)
        {
            List<LuaTimeProfileItem> itemList = m_selectSnap.ItemList.FindAll((p) =>
            {
                if (m_ignoreTotalZero && p.Total == 0)
                {
                    return false;
                }
                if (m_ignoreC && p.Source.Equals("[C]"))
                {
                    return false;
                }
                if (m_ignoreCSharp && p.Source.Equals("[C#]"))
                {
                    return false;
                }

                return true;
            });

            //GUI.backgroundColor
            for (int i = 0; i < itemList.Count; i++)
            {
                LuaTimeProfileItem item = itemList[i];
                if (m_selectSnap.SelectItem == item)
                {
                    GUI.backgroundColor = new Color(120 / 255f, 146 / 255f, 190 / 255f);
                }
                else if (i % 2 == 0)
                {
                    GUI.backgroundColor = new Color(198 / 255f, 198 / 255f, 198 / 255f);
                }
                else
                {
                    GUI.backgroundColor = new Color(174 / 255f, 174 / 255f, 174 / 255f);
                }

                Rect rect = EditorGUILayout.BeginHorizontal(GUILayout.Height(20f));
                if (GUI.Button(rect, "", EditorStyles.textArea))
                {
                    m_selectSnap.SelectItem = item;
                }
                GUI.backgroundColor = Color.white;
                //GUILayout.BeginHorizontal(); 

                GUILayout.Label(item.Function, GUILayout.Width(width / 2));
                GUILayout.Label(item.Source.ToString(), GUILayout.Width(width / 2));
                GUILayout.Label(item.Total.ToString(), GUILayout.Width(itemWidth));
                GUILayout.Label(item.Average.ToString(), GUILayout.Width(itemWidth));
                GUILayout.Label(item.Relative, GUILayout.Width(itemWidth));
                GUILayout.Label(item.Called.ToString(), GUILayout.Width(itemWidth - 40));
                GUILayout.EndHorizontal();
            }
        }
        GUILayout.EndScrollView();
        GUI.contentColor = Color.white;
    }

    private void SetSortType(ItemSortType type, List<LuaTimeProfileItem> itemList = null)
    {
        if (type == m_sortType)
        {
            m_sortDir = !m_sortDir;
        }

        m_sortType = type;

        if (itemList == null)
        {
            if (m_selectSnap == null)
                return;

            itemList = m_selectSnap.ItemList;
        }

        int sortDir = m_sortDir ? 1 : -1;
        switch (type)
        {
            case ItemSortType.Function:
                itemList.Sort((p, q) =>
                {
                    return sortDir * string.Compare(p.Function, q.Function);
                });
                break;
            case ItemSortType.Source:
                itemList.Sort((p, q) =>
                {
                    return sortDir * string.Compare(p.Source, q.Source);
                });
                break;
            case ItemSortType.Total:
                itemList.Sort((p, q) =>
                {
                    if (p.Total > q.Total)
                    {
                        return sortDir * 1;
                    }
                    else if (p.Total < q.Total)
                    {
                        return sortDir * -1;
                    }
                    else
                    {
                        return 0;
                    }
                });
                break;
            case ItemSortType.Average:
                itemList.Sort((p, q) =>
                {
                    if (p.Average > q.Average)
                    {
                        return sortDir * 1;
                    }
                    else if (p.Average < q.Average)
                    {
                        return sortDir * -1;
                    }
                    else
                    {
                        return 0;
                    }
                });
                break;
            case ItemSortType.Relative:
                itemList.Sort((p, q) =>
                {
                    return sortDir * string.Compare(p.Relative, q.Relative);
                });
                break;
            case ItemSortType.Called:
                itemList.Sort((p, q) =>
                {
                    if (p.Called > q.Called)
                    {
                        return sortDir * 1;
                    }
                    else if (p.Called < q.Called)
                    {
                        return sortDir * -1;
                    }
                    else
                    {
                        return 0;
                    }
                });
                break;
            default:
                Debug.Log("未定义的类型");
                break;
        }
    }

    public void SnapShot(params object[] objs)
    {
        string profileName = (string)objs[0];
        string text = (string)objs[1];
        LuaTimeProfile profiler = null;
        if (string.IsNullOrEmpty(profileName))
        {
            profiler = new LuaTimeProfile(++m_snapCount);
        }
        else
        {
            profiler = new LuaTimeProfile(profileName);
        }
        string[] lines = text.Split(new string[] { "\n" }, StringSplitOptions.None);
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrEmpty(line))
                continue;

            string[] fields = line.Split(new string[] { " : " }, StringSplitOptions.None);
            LuaTimeProfileItem item = new LuaTimeProfileItem();
            item.RawText = line;
            item.Function = fields[0].Replace("|", "").Trim();
            item.Source = fields[1].Trim();
            item.Total = float.Parse(fields[2].Trim());
            item.Average = float.Parse(fields[3].Replace("%","").Trim());
            item.Relative = fields[4].Trim();
            item.Called = int.Parse(fields[5].Replace("|", "").Trim());

            if (item.Source.Contains("perf/profiler"))
                continue;

            //Debug.Log(line);
            //Debug.Log(item.Function + " 11 " + item.Source + " 22 " + item.Total + " 33 " + item.Average + " 44 " + item.Relative + " 55 " + item.Called);

            profiler.ItemList.Add(item);
        }

        m_selectSnap = profiler;
        m_snaps.Add(profiler);
        SetSortType(m_sortType);
    }

}
