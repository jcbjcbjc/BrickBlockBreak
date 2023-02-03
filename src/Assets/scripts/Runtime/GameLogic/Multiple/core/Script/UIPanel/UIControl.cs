using Assets.scripts.Utils;
using C2BNet;
using GameLogic;
using Managers;
using NetWork;
using Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum Action
{
    None,
    PathPoint = 1,
    Ban,
    Move,
    Brick,
    Break,
    ChangePlayer = 100,
    ChangeStage,
}
// Service
public class UIControl : Singleton<UIControl>
{
    List<Button> moves;
    List<Button> bricks;
    List<Button> breaks;
    List<TMPro.TextMeshProUGUI> textMeshPros;
    List<int> randomPool;
    Button BackButton;

    [SerializeField] GameObject pointPrefab;
    [SerializeField] GameObject playerPrefab;
    GameObject map;
    GameObject[] endPoints;
    GameObject[] pathPoints;
    GameObject[] players;
    bool[] isCondition;
    [SerializeField] int banNumberMax;
    [SerializeField] int pathNumberMax;
    [SerializeField] int mapSize;
    public int stage;
    public Action action;
    [SerializeField] int nowPlayer;
    [SerializeField] int[] actPoints;
    [SerializeField] int[] banNumber;
    int[] pathNumber;
    System.Random rnd;

    List<Character> characters;
    private EventSystem eventSystem;
    void Awake()
    {
        eventSystem = ServiceLocator.Get<EventSystem>();
        map = Instantiate(new GameObject(), transform.parent);
        map.name = "Map";
        map.transform.localScale = new Vector3(5.0f, 5.0f, 5.0f);
        moves = new List<Button>();
        bricks = new List<Button>();
        breaks = new List<Button>();
        randomPool = new List<int>();
        textMeshPros = new List<TMPro.TextMeshProUGUI>();
        rnd = new System.Random(RandomUtil.seed);

        Transform[] trans = transform.Find("Move").GetComponentsInChildren<Transform>();
        for (int i = 1; i < trans.Length; i++)
        {
            int idx = i - 1;
            moves.Add(trans[i].gameObject.GetComponent<Button>());
            moves.Last().onClick.AddListener(() =>
            {
                if (stage == 2 && nowPlayer == idx)
                {
                    action = Action.Move;
                }
            });
        }
        trans = transform.Find("Brick").GetComponentsInChildren<Transform>();
        for (int i = 1; i < trans.Length; i++)
        {
            int idx = i - 1;
            bricks.Add(trans[i].GetComponent<Button>());
            bricks.Last().onClick.AddListener(() =>
            {
                if (stage == 2 && nowPlayer == idx)
                {
                    action = Action.Brick;
                }
            });
        }
        trans = transform.Find("Break").GetComponentsInChildren<Transform>();
        for (int i = 1; i < trans.Length; i++)
        {
            int idx = i - 1;
            breaks.Add(trans[i].GetComponent<Button>());
            breaks.Last().onClick.AddListener(() =>
            {
                if (stage == 2 && nowPlayer == idx)
                {
                    action = Action.Break;
                }
            });
        }
        trans = transform.Find("ActPoint").GetComponentsInChildren<Transform>();
        for (int i = 1; i < trans.Length; i++)
        {
            if (trans[i].Find("Text") != null)
            {
                textMeshPros.Add(trans[i].Find("Text").GetComponent<TMPro.TextMeshProUGUI>());
            }
        }
        for (int i = 1; i <= 3; i++)
            for (int j = 1; j <= 3; j++) randomPool.Add(i);
        for (int i = 1; i < 7; i++)
            for (int j = 3; j >= 0; j--) randomPool.Add(i);
        eventSystem.AddListener(EEvent.OnGameLogicOver, OnGameOver);
    }
    private void Start()
    {
        EnterGame();
    }
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit raycast))
            {
                Point target = raycast.collider.gameObject.GetComponent<Point>();
                if (target != null)
                {
                    if (target.CompareTags("Point"))
                    {
                        if (stage == 0)
                        {
                            SendMessage(Action.PathPoint, EncodePoint(target));
                        }
                        else if (stage == 1)
                        {
                            SendMessage(Action.Ban, EncodePoint(target));
                        }
                        else if (stage == 2)
                        {
                            if (action == Action.Break) SendMessage(action, EncodePoint(target), UnityEngine.Random.Range(0, 2));
                            else SendMessage(action, EncodePoint(target));
                        }
                        SendMessage(Action.ChangePlayer);
                    }
                }
            }
        }
    }
    #region 玩家操作
    void PlayerPath(Point target) // 设置路径点
    {
        // 判断
        if (target.CompareTagsUnion(new string[] { "EndPoint", "PathPoint"})) return;
        // 结束判断

        // 执行
        target.SetStatus(Point.Status.PathPoint);
        target.AddTag("PathPoint");
        pathPoints[nowPlayer ^ 1] = target.gameObject;

        GameObject now = target.gameObject;

        if (now != null)
        {
            Transform tran = now.transform.Find("Text");
            tran.gameObject.SetActive(true);
            TextMesh text = tran.gameObject.GetComponent<TextMesh>();
            if (text == null) text = tran.gameObject.AddComponent<TextMesh>();
            text.text = ((nowPlayer ^ 1) + 1).ToString();
            tran.localScale = new Vector3(1f, 1 + -2f * (nowPlayer ^ 1), 1f);

            tran = now.transform.Find("Normal");
            SpriteRenderer spriteRenderer = tran.gameObject.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null) spriteRenderer = tran.gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.color = UnityEngine.Color.green;
        }

        pathNumber[nowPlayer]--;
        // 结束执行
    }
    void PlayerBan(Point target) // 设置 Ban 点
    {
        // 判断
        if (target.CompareTagsUnion(new string[] { "PathPoint", "Ban", "EndPoint"})) return;
        if (!isAvailable(target)) return;
        // 结束判断

        // 执行
        target.SetStatus(Point.Status.Ban);
        target.AddTag("Ban");

        GameObject now = target.gameObject;

        if (now != null)
        {
            Transform tran = now.transform.Find("Normal");
            SpriteRenderer spriteRenderer = tran.gameObject.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null) spriteRenderer = tran.gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.color = UnityEngine.Color.red;
        }

        banNumber[nowPlayer]--;
        // 结束执行

    }
    int Dist(Point point1, Point point2)
    {
        bool flag = (Mathf.Abs(point1.x - point2.x) + Mathf.Abs(point1.y - point2.y)) == 1;
        flag |= ((point1.x - point2.x) == (point1.y - point2.y)) && (Mathf.Abs(point1.x - point2.x) == 1);
        return flag ? 1 : 2;
    }
    void PlayerMove(Point target)
    {
        // 判断
        if (actPoints[nowPlayer] < 1) return;
        if (target.CompareTagsUnion(new string[]{ "Ban", "Brick" })) return;
        if (Dist(players[nowPlayer].GetComponent<Point>(), target) == 1)
        {
            players[nowPlayer].transform.localPosition = target.gameObject.transform.localPosition;

            Point now = players[nowPlayer].GetComponent<Point>();
            now.x = target.x;
            now.y = target.y;
        }
        else return;
        // 结束判断

        // 执行
        if (target.CompareTags("PathPoint") && pathPoints[nowPlayer] == target.gameObject)
        {
            isCondition[nowPlayer] = true;
            Debug.Log($"Player {nowPlayer} Get Condition");
        }

        if (target.CompareTags("EndPoint") && isCondition[nowPlayer] && EqualPoint(target, endPoints[nowPlayer].GetComponent<Point>()))
        {
            Debug.Log($"Player {nowPlayer} Win");
            ExitGame();
        }

        actPoints[nowPlayer]--;
        // 结束执行
        Debug.Log($"Player {nowPlayer} moved");
    }
    void PlayerBrick(Point target)
    {
        // 判断
        if (actPoints[nowPlayer] < 1) return;
        if (target.CompareTagsUnion(new string[] {"Ban", "Brick", "EndPoint", "PathPoint"})) return;
        if (!isAvailable(target)) return;
        // 结束判断

        // 执行
        target.SetStatus(Point.Status.Brick);
        target.AddTag("Brick");

        GameObject now = target.gameObject;

        if (now != null)
        {
            Transform tran = now.transform.Find("Normal");
            SpriteRenderer spriteRenderer = tran.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null) spriteRenderer = tran.gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.color = UnityEngine.Color.yellow;
        }

        actPoints[nowPlayer]--;
        // 结束执行
        Debug.Log($"Player {nowPlayer} bricked");
    }
    void PlayerBreak(Point target, int isSuccessful)
    {
        // 判断
        if (actPoints[nowPlayer] < 3) return;
        if (!target.CompareTags("Brick")) return;
        // 结束判断

        // 执行
        // 修改
        if (isSuccessful == 1)
        {
            target.SetStatus(Point.Status.Normal);
            target.DeleteTag("Brick");

            GameObject now = target.gameObject;

            if (now != null)
            {
                Transform tran = now.transform.Find("Normal");
                SpriteRenderer spriteRenderer = tran.GetComponent<SpriteRenderer>();
                if (spriteRenderer == null) spriteRenderer = tran.gameObject.AddComponent<SpriteRenderer>();
                spriteRenderer.color = new UnityEngine.Color(0f, 0.975f, 1f, 1f);
            }

            Debug.Log($"Player {nowPlayer} success to break");
        } else
        {
            Debug.Log($"Player {nowPlayer} fail to break");
        }
        actPoints[nowPlayer] -= 3;
        // 结束执行
    }
    #endregion
    void StageCheck()
    {
        // int x = ServiceLocator.Get<User>().NUser.Id; 自己的UserID
        if (stage == 0)
        {
            int flag = Mathf.Max(pathNumber);
            if (flag == 0)
            {
                stage = 1;
                Debug.Log("Enter Ban Stage");
            }
        } else if (stage == 1)
        {
            int flag = Mathf.Max(banNumber);
            if (flag == 0) 
            {
                stage = 2;
                Debug.Log("Enter Normal Stage");
            }
        }
    }
    void ChangeControl()
    {
        UpdateBoard();
        SendMessage(Action.ChangeStage);
        if (stage == 0)
        {
            if (pathNumber[nowPlayer] <= 0)
            {
                nowPlayer = (nowPlayer + 1) % characters.Count;
            }
        }
        else if (stage == 1)
        {
            if (banNumber[nowPlayer] <= 0)
            {
                nowPlayer = (nowPlayer + 1) % characters.Count;
            }
        }
        else if (stage == 2)
        {
            if (actPoints[nowPlayer] <= 0)
            {
                action = Action.None;
                nowPlayer = (nowPlayer + 1) % characters.Count;
                actPoints[nowPlayer] = randomPool[rnd.Next() % randomPool.Count];
            }
        }
        UpdateBoard();
    } 
    void UpdateBoard()
    {
        if (stage == 0)
        {
            textMeshPros[nowPlayer].text = $"<color=blue>{pathNumber[nowPlayer]}</color>";
        }
        else if (stage == 1)
        {
            textMeshPros[nowPlayer].text = $"<color=red>{banNumber[nowPlayer]}</color>";
        }
        else if (stage == 2)
        {
            textMeshPros[nowPlayer].text = $"<color=green>{actPoints[nowPlayer]}</color>";
        }
    }
    void OnBackClick()
    {
        ExitGame();
    }
    void ExitGame()
    {
        foreach (Character character in characters)
        {
            Destroy(character.gameObject);
        }
        characters.Clear();
        stage = -1;
        action = Action.None;
        foreach (Transform tran in map.transform)
        {
            Destroy(tran.gameObject);
        }
        Destroy(map.gameObject);
        eventSystem.RemoveListener(EEvent.OnGameLogicOver, OnGameOver);
        Destroy(gameObject);
    }
    Point GetPoint(int i, int j)
    {
        if (map.transform.Find($"Point_{i}_{j}") == null) return null;
        return map.transform.Find($"Point_{i}_{j}").GetComponent<Point>();
    }
    bool EqualPoint(Point point1, Point point2)
    {
        return point1.x == point2.x && point1.y == point2.y;
    }
    bool isAvailable(Point banned)
    {
        List<Point> points = new List<Point>();
        List<Point> visited = new List<Point>();
        int[] dx = { 0, 0, 1, -1, 1, -1 };
        int[] dy = { 1, -1, 0, 0, 1, -1 };
        bool[] flags = { false, false, false };
        points.Add(endPoints[nowPlayer].GetComponent<Point>());
        DebugPoint(endPoints[nowPlayer].GetComponent<Point>(), "Start");
        DebugPoint(endPoints[nowPlayer ^ 1].GetComponent<Point>(), "End");
        visited.Add(points.First());
        while (points.Count > 0)
        {
            // EndPoint 点注意两人与多人版本不同
            Point now = points.First(), tmp = null;
            if (EqualPoint(now, endPoints[(nowPlayer + 1) % characters.Count].GetComponent<Point>())) flags[0] = true;
            if (EqualPoint(now, pathPoints[(nowPlayer + 1) % characters.Count].GetComponent<Point>())) flags[1] = true;
            if (EqualPoint(now, pathPoints[nowPlayer].GetComponent<Point>())) flags[2] = true;
            if (flags[0] && flags[1] && flags[2])
            {
                Debug.Log($"{banned.x}, {banned.y} can");
                return true;
            }
            points.Remove(points.First());
            for (int i = 0; i < 6; i++)
            {
                tmp = GetPoint(now.x + dx[i], now.y + dy[i]);
                if (tmp != null) 
                {
                    if (!tmp.CompareTagsUnion(new string[] { "Brick", "Ban" }) && !EqualPoint(tmp, banned))
                    {
                        if (!visited.Contains(tmp))
                        {
                            visited.Add(tmp);
                            DebugPoint(tmp, $"is {visited.Count} visited");
                            points.Add(tmp);
                        }
                    }
                }
            }
        }
        Debug.Log($"{banned.x}, {banned.y} cannot");
        return false;
    }
    void DebugPoint(Point point, string now)
    {
        Debug.Log($"{point.x}, {point.y}" + now);
    }
    int EncodePoint(Point point)
    {
        return point.x * mapSize + point.y;
    }
    Point DecodePoint(int point)
    {
        Point ret = GetPoint(point / mapSize, point % mapSize);
        return ret;
    }
    #region 进入游戏
    public void EnterGame()
    {
        characters = Service.Get<CharacterManager>().GetCharacterList();
        isCondition = new bool[] { false, false };
        actPoints = new int[] { 0, 0 };
        pathNumber = new int[] { pathNumberMax, pathNumberMax };
        banNumber = new int[] { banNumberMax, banNumberMax };
        players = new GameObject[] { null, null };
        endPoints = new GameObject[] { null, null };
        pathPoints = new GameObject[] { null, null };
        action = Action.None;
        stage = 0;
        nowPlayer = RandomUtil.seed % 2;
        CreateMap();
    }
    bool InMap(int x, int y)
    {
        return 0 <= x && x <= mapSize && 0 <= y && y <= mapSize && Mathf.Abs(x - y) <= 3;
    }
    GameObject GeneratePoint(int x, int y)
    {
        GameObject now = Instantiate(pointPrefab, map.transform);
        now.name = $"Point_{x}_{y}";
        // 根据格子大小调整
        now.transform.localPosition += new Vector3(0, -(mapSize - 1) * 14.5f, 0);
        now.transform.localPosition += (x * new Vector3(25.5f, 14.5f, 0f) + y * new Vector3(-25.5f, 14.5f, 0));
        Point nowPoint = now.GetComponent<Point>();

        if (nowPoint == null) nowPoint = now.AddComponent<Point>();

        nowPoint.x = x;
        nowPoint.y = y;
        nowPoint.SetStatus(Point.Status.Normal);

        Transform tran = now.transform.Find("Normal");
        SpriteRenderer spriteRenderer = tran.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) spriteRenderer = tran.gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.color = new UnityEngine.Color(0f, 0.975f, 1f, 1f);

        return now;
    }
    void CreateMap()
    {
        for (int i = 0; i < mapSize; i++) // 生成地图
        {
            for (int j = 0; j < mapSize; j++)
            {
                if (InMap(i, j))
                {
                    GameObject now = GeneratePoint(i, j);
                    // 注意两人与多人版本不同
                    if (i == 0 && j == 0)
                    {
                        endPoints[1] = now;
                        now.GetComponent<Point>().AddTag("EndPoint");
                    }
                    else if (i == mapSize - 1 && j == mapSize - 1)
                    {
                        endPoints[0] = now;
                        now.GetComponent<Point>().AddTag("EndPoint");
                    }
                }
            }
        }

        for (int i = 0; i < characters.Count; i++) // 生成玩家
        {
            players[i] = Instantiate(playerPrefab, map.transform);
            players[i].name = $"Player{i}";
            players[i].transform.localPosition += new Vector3(0, -(mapSize - 1) * 14.5f + i * (mapSize - 1) * 29f, 0);

            Point nowPoint = players[i].GetComponent<Point>();

            if (nowPoint == null) nowPoint = players[i].AddComponent<Point>();

            // 注意两人与多人版本不同
            nowPoint.x = i * (mapSize - 1);
            nowPoint.y = i * (mapSize - 1);
        }
    }
    #endregion
    #region 服务器通信
    bool SendMessage(Action operation, int val1 = 0, int val2 = 0, int val3 = 0)
    {
        if (characters[nowPlayer].Userid == ServiceLocator.Get<User>().NUser.Id)
        {
            Debug.Log("SendMessage");
            FrameHandle frameHandle = new FrameHandle
            {
                OpretionId = Convert.ToInt32(operation),
                Opt = val1,
                OptValue1 = val2,
                OptValue2 = val3
            };
            eventSystem.Invoke<FrameHandle>(EEvent.OnAddOptClient, frameHandle);
            Debug.Log("Successful Send");
            return true;
        }
        else
        {
            return false;
        }
    }
    public bool HandleMessage(FrameHandle fh)
    {
        Debug.Log($"{fh.Opt} + {fh.OpretionId}");
        if (fh.OpretionId == 1) // PathPoint
        {
            PlayerPath(DecodePoint(fh.Opt));
        } else if (fh.OpretionId == 2) // Ban
        {
            PlayerBan(DecodePoint(fh.Opt));
        } else if (fh.OpretionId == 3) // Move
        {
            PlayerMove(DecodePoint(fh.Opt));
        } else if (fh.OpretionId == 4) // Brick
        {
            PlayerBrick(DecodePoint(fh.Opt));
        } else if (fh.OpretionId == 5) // Break;
        {
            PlayerBreak(DecodePoint(fh.Opt), fh.OptValue1);
        } else if (fh.OpretionId == 100) // ChangePlayer
        {
            ChangeControl();
        } else if (fh.OpretionId == 101) // ChangeStage
        {
            StageCheck(); 
        }
        return true;
    }
    #endregion
    public void OnClickReturn()
    {
        ServiceLocator.Get<GameLogicService>().SendGameOver();
        ServiceLocator.Get<RoomService>().SendGameOver2();
    }
    private void OnGameOver()
    {
        ExitGame();
        UIManager.GetInstance().CloseUIForms("Classic");
        UIManager.GetInstance().ShowUIForms("UIMain");
    }
}
