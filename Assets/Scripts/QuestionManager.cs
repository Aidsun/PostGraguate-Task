using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestionManager : MonoBehaviour
{
    // 全局单例
    public static QuestionManager Instance;

    [Header("组件绑定")]
    [Tooltip("面板根节点")]
    public GameObject QuestionPanel;
    [Tooltip("问题文本")]
    public TMP_Text questionText;
    [Tooltip("四个选项")]
    public Button[] questionBtns;

    [Header("设置")]
    [Tooltip("设定题目数量")]
    public int questionCount = 7;
    [Tooltip("答完一题后延迟多少秒进入下一题")]
    public float delayTime = 1.5f;

    [Header("选项颜色")]
    public Color normalBtn = Color.white;
    public Color correctBtn = Color.green;
    public Color wrongBtn = Color.red;

    // 分割符
    private char Separator = '#';

    // 答题数据结构
    private class QuestionData
    {
        public string question;
        public string[] options;
        public int answerIndex;
    }

    // 所有题目池
    private List<QuestionData> allQuestions = new List<QuestionData>();
    // 当前的一轮题目
    private List<QuestionData> currentRoundQuestions = new List<QuestionData>();

    private int currentIndex = 0;
    private bool isAnswered = false;

    // 【新增】记录答对的题目数量
    private int correctCount = 0;

    private void Awake()
    {
        Instance = this;
        if (QuestionPanel) QuestionPanel.SetActive(false);
        LoadQuestions();
    }

    // 1. 加载并解析题库
    private void LoadQuestions()
    {
        TextAsset dataFile = Resources.Load<TextAsset>("Question_Ansower");
        if (dataFile == null)
        {
            Debug.LogError("❌ 未找到 Resources/Question_Ansower.txt！请检查文件名！");
            return;
        }

        string[] lines = dataFile.text.Split('\n');
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] parts = line.Trim().Split(Separator);
            if (parts.Length < 6) continue;

            QuestionData Q = new QuestionData();
            Q.question = parts[0];
            Q.options = new string[] { parts[1], parts[2], parts[3], parts[4] };

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
                    default: int.TryParse(parts[5], out Q.answerIndex); break;
                }
            }
            allQuestions.Add(Q);
        }
        Debug.Log($"✅ 成功加载题库，共 {allQuestions.Count} 道题");
    }

    // 2. 打开面板
    public void openQuestionPanel()
    {
        if (allQuestions.Count == 0) return;

        QuestionPanel.SetActive(true);

        // --- 随机抽取题目 ---
        currentRoundQuestions.Clear();
        List<QuestionData> tempPool = new List<QuestionData>(allQuestions);
        for (int i = 0; i < questionCount && tempPool.Count > 0; i++)
        {
            int r = Random.Range(0, tempPool.Count);
            currentRoundQuestions.Add(tempPool[r]);
            tempPool.RemoveAt(r);
        }

        // 【新增】重置数据
        currentIndex = 0;
        correctCount = 0; // 归零

        ShowQuestion(currentIndex);

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // 3. 关闭面板
    public void closeQuestionPanel()
    {
        QuestionPanel.SetActive(false);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void ShowQuestion(int index)
    {
        // === 结算逻辑 ===
        if (index >= currentRoundQuestions.Count)
        {
            // 1. 先关闭答题面板
            closeQuestionPanel();

            // 2. 【新增】计算正确率并显示
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

                TutorPanel.Instance.ShowPanel(resultMsg);
            }
            return;
        }

        isAnswered = false;
        QuestionData q = currentRoundQuestions[index];

        questionText.text = $"（第{index + 1}题/共{currentRoundQuestions.Count}题）\n"+q.question;

        for (int i = 0; i < 4; i++)
        {
            questionBtns[i].gameObject.SetActive(true);
            questionBtns[i].interactable = true;
            questionBtns[i].image.color = normalBtn;

            TMP_Text btnText = questionBtns[i].GetComponentInChildren<TMP_Text>();
            if (btnText) btnText.text = q.options[i];

            int btnIndex = i;
            questionBtns[i].onClick.RemoveAllListeners();
            questionBtns[i].onClick.AddListener(() => OnOptionClick(btnIndex));
        }
    }

    private void OnOptionClick(int selectIndex)
    {
        if (isAnswered) return;

        isAnswered = true;
        QuestionData q = currentRoundQuestions[currentIndex];
        bool isCorrect = (selectIndex == q.answerIndex);

        // 【新增】如果答对了，计数器+1
        if (isCorrect)
        {
            correctCount++;
        }

        // 变色反馈
        questionBtns[selectIndex].image.color = isCorrect ? correctBtn : wrongBtn;
        if (!isCorrect)
        {
            questionBtns[q.answerIndex].image.color = correctBtn;
        }

        // 音效
        if (AudioManager.Instance)
        {
            AudioManager.Instance.PlayClickSound();
        }

        StartCoroutine(NextQuestionDelay(delayTime));
    }

    IEnumerator NextQuestionDelay(float time)
    {
        yield return new WaitForSecondsRealtime(time);
        currentIndex++;
        ShowQuestion(currentIndex);
    }
}