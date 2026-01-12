using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class InGameUIManager : MonoBehaviour
{
    public static InGameUIManager Instance { get; private set; }

    [Header("Insert / GameOver UI")]
    public GameObject p1InsertCoin;
    public GameObject p2InsertCoin;
    public GameObject p1GameOver;
    public GameObject p2GameOver;

    [Header("Text UI")]
    public TMP_Text p1LifeText;   // "Life : X"
    public TMP_Text p2LifeText;
    public TMP_Text coinText;     // "Coin : X"

    [Header("Score UI")]
    public TMP_Text p1ScoreText;  // 숫자만 (Score : 는 따로 있음)
    public TMP_Text p2ScoreText;

    [Header("Input Keys")]
    public KeyCode coinKey = KeyCode.Alpha5;
    public KeyCode start1PKey = KeyCode.Alpha1;
    public KeyCode start2PKey = KeyCode.Alpha2;

    [Header("Blink")]
    public float blinkInterval = 0.35f;

    // ===== 상태 =====
    public int credits { get; private set; } = 0;

    private int p1Score = 0;
    private int p2Score = 0;

    private const int MAX_LIFE = 3;
    private int p1Lives = MAX_LIFE;
    private int p2Lives = MAX_LIFE;

    private bool p1Playing = false;
    private bool p2Playing = false;

    private float blinkTimer = 0f;
    private bool blinkOn = true;

    public event Action<int> OnStartPlayer;
    public event Action<int> OnContinuePlayer;

    // ===== 점수 규칙 =====
    public const int SCORE_MONSTER_KILL = 1000;
    public const int SCORE_BUBBLE_POP = 10;
    public const int SCORE_BOSS_KILL = 10000;
    public const int SCORE_ITEM_PICKUP = 2000;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += (_, __) => RefreshUI();
    }

    private void Update()
    {
        // 코인 입력 (타이틀에서도 누적)
        if (Input.GetKeyDown(coinKey))
        {
            credits++;
            RefreshUI();
        }

        if (Input.GetKeyDown(start1PKey))
            TryStartOrContinue(1);

        if (Input.GetKeyDown(start2PKey))
            TryStartOrContinue(2);

        UpdateBlink();
    }

    // ================= Blink =================
    private void UpdateBlink()
    {
        blinkTimer += Time.deltaTime;
        if (blinkTimer < blinkInterval) return;

        blinkTimer = 0f;
        blinkOn = !blinkOn;

        if (p1InsertCoin) p1InsertCoin.SetActive(!p1Playing && blinkOn);
        if (p2InsertCoin) p2InsertCoin.SetActive(!p2Playing && blinkOn);
    }

    // ================= Start / Continue =================
    private void TryStartOrContinue(int player)
    {
        if (credits <= 0) return;

        if (player == 1)
        {
            if (p1Playing) return;
            credits--;

            if (p1Lives <= 0)
            {
                p1Lives = MAX_LIFE;
                p1Playing = true;
                OnContinuePlayer?.Invoke(1);
            }
            else
            {
                p1Playing = true;
                OnStartPlayer?.Invoke(1);
            }
        }
        else
        {
            if (p2Playing) return;
            credits--;

            if (p2Lives <= 0)
            {
                p2Lives = MAX_LIFE;
                p2Playing = true;
                OnContinuePlayer?.Invoke(2);
            }
            else
            {
                p2Playing = true;
                OnStartPlayer?.Invoke(2);
            }
        }

        RefreshUI();
    }

    // ================= 외부 호출 =================
    public void NotifyPlayerDied(int player)
    {
        if (player == 1 && p1Playing)
        {
            p1Lives = Mathf.Max(0, p1Lives - 1);
            if (p1Lives <= 0) p1Playing = false;
        }
        else if (player == 2 && p2Playing)
        {
            p2Lives = Mathf.Max(0, p2Lives - 1);
            if (p2Lives <= 0) p2Playing = false;
        }

        RefreshUI();
    }

    // ================= Score =================
    public void AddScore(int player, int amount)
    {
        if (amount <= 0) return;

        if (player == 1)
            p1Score += amount;
        else if (player == 2)
            p2Score += amount;

        RefreshUI();
    }

    // 편의 함수
    public void AddMonsterKillScore(int player) => AddScore(player, SCORE_MONSTER_KILL);
    public void AddBubblePopScore(int player) => AddScore(player, SCORE_BUBBLE_POP);
    public void AddBossKillScore(int player) => AddScore(player, SCORE_BOSS_KILL);
    public void AddItemPickupScore(int player) => AddScore(player, SCORE_ITEM_PICKUP);

    // ================= UI =================
    private void RefreshUI()
    {
        if (coinText)
            coinText.text = $"Coin : {credits}";

        if (p1LifeText)
            p1LifeText.text = $"Life : {p1Lives}";

        if (p2LifeText)
            p2LifeText.text = $"Life : {p2Lives}";

        // 숫자만 변경 (Score : 는 UI에 따로 있음)
        if (p1ScoreText)
            p1ScoreText.text = p1Score.ToString("0000000");

        if (p2ScoreText)
            p2ScoreText.text = p2Score.ToString("0000000");

        if (p1GameOver)
            p1GameOver.SetActive(p1Lives <= 0);

        if (p2GameOver)
            p2GameOver.SetActive(p2Lives <= 0);

        if (p1InsertCoin)
            p1InsertCoin.SetActive(!p1Playing && blinkOn);

        if (p2InsertCoin)
            p2InsertCoin.SetActive(!p2Playing && blinkOn);
    }
}
