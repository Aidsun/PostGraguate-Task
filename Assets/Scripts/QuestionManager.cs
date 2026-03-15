// 文件：QuestionManager.cs
// 模块：答题系统 / 核心管理器
// 说明：该脚本负责整个答题系统的逻辑，包括题库的加载、随机抽题、UI显示、用户交互、计分反馈和结果展示。
//      它采用单例模式，全局唯一，通过QuestionInteraction触发打开答题面板。
//      题库从Resources/Question_Ansower.txt加载，文件格式使用'#'分隔。
// 特性：单例模式，使用Resources.Load加载文本资源，协程实现延迟切换题目，
//      通过Time.timeScale暂停游戏，控制光标状态，与TutorPanel、AudioManager等交互。

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;      // 使用Button组件
using TMPro;               // 使用TextMeshPro文本

public class QuestionManager : MonoBehaviour
{
    // 全局单例实例
    public static QuestionManager Instance;

    [Header("组件绑定")]                     // 在Inspector中分组显示
    [Tooltip("面板根节点")]                   // 鼠标悬停时显示提示
    public GameObject QuestionPanel;        // 答题面板的根物体，控制整体显示/隐藏

    [Tooltip("问题文本")]
    public TMP_Text questionText;           // 显示题目内容的文本组件

    [Tooltip("四个选项")]
    public Button[] questionBtns;           // 四个选项按钮的数组，长度应为4

    [Header("设置")]                         // 答题系统设置参数
    [Tooltip("设定题目数量")]                 // 每轮答题抽取的题目数量
    public int questionCount = 7;

    [Tooltip("答完一题后延迟多少秒进入下一题")]
    public float delayTime = 1.5f;          // 回答后延迟显示下一题的时间（秒）

    [Header("选项颜色")]                      // 按钮颜色反馈配置
    public Color normalBtn = Color.white;    // 正常状态下的按钮颜色
    public Color correctBtn = Color.green;   // 回答正确时的按钮颜色
    public Color wrongBtn = Color.red;       // 回答错误时的按钮颜色

    // 分割符 - 用于解析题库文件，每行数据用'#'分隔
    private char Separator = '#';

    // 答题数据结构 - 私有内部类，存储一道题的信息
    private class QuestionData
    {
        public string question;      // 题目文本
        public string[] options;     // 四个选项的文本数组
        public int answerIndex;      // 正确答案的索引（0~3）
    }

    // 所有题目池 - 从文件加载的所有题目的列表
    private List<QuestionData> allQuestions = new List<QuestionData>();

    // 当前的一轮题目 - 本轮答题随机抽取的题目列表
    private List<QuestionData> currentRoundQuestions = new List<QuestionData>();

    private int currentIndex = 0;        // 当前正在显示的题目的索引（相对于currentRoundQuestions）
    private bool isAnswered = false;     // 当前题目是否已经被回答（防止重复点击）

    // 【新增】记录答对的题目数量 - 用于最后统计正确率
    private int correctCount = 0;

    private void Awake()
    {
        // 单例赋值
        Instance = this;
        // 初始时隐藏答题面板
        if (QuestionPanel) QuestionPanel.SetActive(false);
        // 加载题库
        LoadQuestions();
    }

    // 1. 加载并解析题库
    private void LoadQuestions()
    {
        // 从Resources文件夹加载名为"Question_Ansower"的文本文件（无需扩展名）
        TextAsset dataFile = Resources.Load<TextAsset>("Question_Ansower");
        if (dataFile == null)
        {
            Debug.LogError("❌ 未找到 Resources/Question_Ansower.txt！请检查文件名！");
            return;
        }

        // 按行分割文件内容
        string[] lines = dataFile.text.Split('\n');
        foreach (var line in lines)
        {
            // 跳过空行
            if (string.IsNullOrWhiteSpace(line)) continue;

            // 使用分隔符拆分每行，期望得到6部分：题目、选项A、B、C、D、答案
            string[] parts = line.Trim().Split(Separator);
            if (parts.Length < 6) continue;   // 格式不正确则跳过

            // 创建QuestionData对象并填充
            QuestionData Q = new QuestionData();
            Q.question = parts[0];
            Q.options = new string[] { parts[1], parts[2], parts[3], parts[4] };

            // 解析答案部分（可能是字母A~D或数字0~3）
            string answerLetter = parts[5].Trim().ToUpper();
            if (answerLetter.Length > 0)
            {
                char letter = answerLetter[0];
                switch (letter)
                {
                    case 'A': Q.answerIndex = 0; break;
                    case 'B': Q.answerIndex = 1; break;
                    case 'C': Q.answerIndex = 2; break;
                    case 'D': Q.answerIndex = 3; break;
                    default:
                        // 如果既不是A~D，尝试直接解析为整数
                        int.TryParse(parts[5], out Q.answerIndex);
                        break;
                }
            }
            // 将题目添加到题库池
            allQuestions.Add(Q);
        }
        Debug.Log($"✅ 成功加载题库，共 {allQuestions.Count} 道题");
    }

    // 2. 打开答题面板（由QuestionInteraction调用）
    public void openQuestionPanel()
    {
        // 如果题库为空，则不执行任何操作
        if (allQuestions.Count == 0) return;

        // 显示答题面板
        QuestionPanel.SetActive(true);

        // --- 随机抽取题目 ---
        currentRoundQuestions.Clear();
        // 创建一个临时池，从中随机抽取题目，避免重复
        List<QuestionData> tempPool = new List<QuestionData>(allQuestions);
        for (int i = 0; i < questionCount && tempPool.Count > 0; i++)
        {
            int r = Random.Range(0, tempPool.Count);
            currentRoundQuestions.Add(tempPool[r]);
            tempPool.RemoveAt(r);   // 移除已选中的题目，防止重复
        }

        // 【新增】重置答题状态
        currentIndex = 0;
        correctCount = 0;   // 归零

        // 显示第一题
        ShowQuestion(currentIndex);

        // 暂停游戏时间（实现答题时游戏暂停的效果）
        Time.timeScale = 0f;
        // 解锁并显示鼠标，允许玩家点击选项
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // 3. 关闭答题面板
    public void closeQuestionPanel()
    {
        // 隐藏面板
        QuestionPanel.SetActive(false);
        // 恢复游戏时间
        Time.timeScale = 1f;
        // 重新锁定鼠标（适用于第一人称视角）
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>
    /// 显示指定索引的题目
    /// </summary>
    /// <param name="index">在currentRoundQuestions中的索引</param>
    private void ShowQuestion(int index)
    {
        // === 结算逻辑：如果索引超出当前轮题目数量，表示所有题目已答完 ===
        if (index >= currentRoundQuestions.Count)
        {
            // 1. 先关闭答题面板
            closeQuestionPanel();

            // 2. 【新增】计算正确率并通过TutorPanel显示结果
            if (TutorPanel.Instance)
            {
                int total = currentRoundQuestions.Count;
                // 计算百分比
                int percent = (int)((float)correctCount / total * 100);

                // 构造显示的文字
                string resultMsg = $"考核结束！\n\n答对：{correctCount} / {total}\n正确率：{percent}%";

                // 根据分数加一句评语（可选）
                if (percent == 100) resultMsg += "\n\n太棒了！你是阅兵知识专家！";
                else if (percent >= 60) resultMsg += "\n\n成绩合格，继续加油！";
                else resultMsg += "\n\n还需要再接再厉哦！";

                // 使用TutorPanel显示结果（TutorPanel是一个全局提示面板）
                TutorPanel.Instance.ShowPanel(resultMsg);
            }
            return;
        }

        // 重置当前题目的回答状态
        isAnswered = false;
        // 获取当前题目数据
        QuestionData q = currentRoundQuestions[index];

        // 设置题目文本，显示当前题号和总题数
        questionText.text = $"（第{index + 1}题/共{currentRoundQuestions.Count}题）\n" + q.question;

        // 初始化四个选项按钮
        for (int i = 0; i < 4; i++)
        {
            // 确保按钮是激活的且可交互
            questionBtns[i].gameObject.SetActive(true);
            questionBtns[i].interactable = true;
            // 恢复按钮颜色为正常状态
            questionBtns[i].image.color = normalBtn;

            // 设置按钮上的文本为对应的选项文字
            TMP_Text btnText = questionBtns[i].GetComponentInChildren<TMP_Text>();
            if (btnText) btnText.text = q.options[i];

            // 为每个按钮添加点击监听，注意捕获索引值（避免闭包问题）
            int btnIndex = i;
            questionBtns[i].onClick.RemoveAllListeners();
            questionBtns[i].onClick.AddListener(() => OnOptionClick(btnIndex));
        }
    }

    /// <summary>
    /// 当玩家点击某个选项时调用
    /// </summary>
    /// <param name="selectIndex">玩家选择的选项索引（0~3）</param>
    private void OnOptionClick(int selectIndex)
    {
        // 如果已经回答过当前题目，则忽略后续点击
        if (isAnswered) return;

        // 标记为已回答
        isAnswered = true;
        // 获取当前题目数据
        QuestionData q = currentRoundQuestions[currentIndex];
        // 判断玩家选择的是否为正确答案
        bool isCorrect = (selectIndex == q.answerIndex);

        // 【新增】如果答对了，正确计数器加1
        if (isCorrect)
        {
            correctCount++;
        }

        // 变色反馈：选中的按钮根据正确/错误变色
        questionBtns[selectIndex].image.color = isCorrect ? correctBtn : wrongBtn;
        // 如果选错了，同时将正确答案的按钮显示为绿色，提示玩家
        if (!isCorrect)
        {
            questionBtns[q.answerIndex].image.color = correctBtn;
        }

        // 播放点击音效（通过AudioManager）
        if (AudioManager.Instance)
        {
            AudioManager.Instance.PlayClickSound();
        }

        // 启动延迟协程，等待delayTime后进入下一题
        StartCoroutine(NextQuestionDelay(delayTime));
    }

    /// <summary>
    /// 延迟后进入下一题的协程
    /// </summary>
    /// <param name="time">延迟秒数</param>
    IEnumerator NextQuestionDelay(float time)
    {
        // 注意：因为游戏时间可能被暂停（Time.timeScale = 0），这里使用WaitForSecondsRealtime
        // 以确保延迟不受游戏暂停影响
        yield return new WaitForSecondsRealtime(time);
        // 索引加1，显示下一题
        currentIndex++;
        ShowQuestion(currentIndex);
    }
}