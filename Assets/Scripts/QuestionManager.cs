using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestionManager : MonoBehaviour
{
    public static QuestionManager Instance;

    [Header("组件绑定")]
    public GameObject QuestionPanel;
    public TMP_Text questionText;
    public Button[] questionBtns;

    [Header("设置")]
    public int questionCount = 7;
    public float delayTime = 1.5f;

    [Header("选项颜色")]
    public Color normalBtn = Color.white;
    public Color correctBtn = Color.green;
    public Color wrongBtn = Color.red;

    private char Separator = '#';

    [System.Serializable]
    public class QuestionData
    {
        public string question;
        public string[] options;
        public int answerIndex;
    }

    private List<QuestionData> allQuestions = new List<QuestionData>();
    private List<QuestionData> currentRoundQuestions = new List<QuestionData>();

    private int currentIndex = 0;
    private bool isAnswered = false;
    private int correctCount = 0;

    private System.Action<bool> singleQuestionCallback;
    private System.Action<int> roundCompleteCallback;

    private void Awake()
    {
        Instance = this;
        if (QuestionPanel) QuestionPanel.SetActive(false);
        LoadQuestions();
    }

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

    public void openQuestionPanel()
    {
        if (allQuestions.Count == 0) return;

        QuestionPanel.SetActive(true);

        currentRoundQuestions.Clear();
        List<QuestionData> tempPool = new List<QuestionData>(allQuestions);
        for (int i = 0; i < questionCount && tempPool.Count > 0; i++)
        {
            int r = Random.Range(0, tempPool.Count);
            currentRoundQuestions.Add(tempPool[r]);
            tempPool.RemoveAt(r);
        }

        currentIndex = 0;
        correctCount = 0;
        ShowQuestion(currentIndex);

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void closeQuestionPanel()
    {
        QuestionPanel.SetActive(false);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        // 清理回调
        singleQuestionCallback = null;
        roundCompleteCallback = null;
    }

    private void ShowQuestion(int index)
    {
        Debug.Log($"[QuestionManager] ShowQuestion 被调用，index={index}，总题数={currentRoundQuestions.Count}");
        if (index >= currentRoundQuestions.Count)
        {
            Debug.Log($"[QuestionManager] 所有题目答完，correctCount={correctCount}");
            // 【修复】先调用回调，再关闭面板，避免回调被清空
            if (roundCompleteCallback != null)
            {
                roundCompleteCallback(correctCount);
                roundCompleteCallback = null;
            }
            else if (singleQuestionCallback != null)
            {
                singleQuestionCallback(correctCount > 0);
                singleQuestionCallback = null;
            }
            closeQuestionPanel();
            return;
        }

        isAnswered = false;
        QuestionData q = currentRoundQuestions[index];

        questionText.text = $"（第{index + 1}题/共{currentRoundQuestions.Count}题）\n" + q.question;

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
        Debug.Log($"[QuestionManager] OnOptionClick 被调用，选择索引={selectIndex}");
        if (isAnswered) return;

        isAnswered = true;
        QuestionData q = currentRoundQuestions[currentIndex];
        bool isCorrect = (selectIndex == q.answerIndex);

        if (isCorrect) correctCount++;

        questionBtns[selectIndex].image.color = isCorrect ? correctBtn : wrongBtn;
        if (!isCorrect) questionBtns[q.answerIndex].image.color = correctBtn;

        if (AudioManager.Instance) AudioManager.Instance.PlayClickSound();

        if (currentRoundQuestions.Count == 1)
            StartCoroutine(DelayFinishSingle());
        else
            StartCoroutine(NextQuestionDelay(delayTime));
    }

    IEnumerator NextQuestionDelay(float time)
    {
        yield return new WaitForSecondsRealtime(time);
        currentIndex++;
        ShowQuestion(currentIndex);
    }

    IEnumerator DelayFinishSingle()
    {
        yield return new WaitForSecondsRealtime(delayTime);
        currentIndex = currentRoundQuestions.Count;
        ShowQuestion(currentIndex);
    }

    public void StartRandomRound(System.Action<int> callback)
    {
        Debug.Log("[QuestionManager] StartRandomRound 被调用");
        if (allQuestions.Count == 0)
        {
            Debug.LogWarning("题库为空，无法开始答题");
            return;
        }

        currentRoundQuestions.Clear();
        List<QuestionData> tempPool = new List<QuestionData>(allQuestions);
        int count = Mathf.Min(questionCount, tempPool.Count);
        for (int i = 0; i < count; i++)
        {
            int r = Random.Range(0, tempPool.Count);
            currentRoundQuestions.Add(tempPool[r]);
            tempPool.RemoveAt(r);
        }

        currentIndex = 0;
        correctCount = 0;
        roundCompleteCallback = callback;

        QuestionPanel.SetActive(true);
        ShowQuestion(currentIndex);

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}