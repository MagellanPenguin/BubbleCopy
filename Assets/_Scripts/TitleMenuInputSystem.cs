using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

public class TitleMenuInputSystem : MonoBehaviour
{
    public enum MenuItem { GameStart = 0, Option = 1, ExitGame = 2 }

    [Header("UI References (TMP)")]
    public TextMeshProUGUI startText;
    public TextMeshProUGUI optionText;
    public TextMeshProUGUI exitText;

    [Header("Scene")]
    public string gameSceneName = "Game";

    [Header("Input Actions")]
    public InputActionReference navigate; // UI/Navigate
    public InputActionReference submit;   // UI/Submit

    [Header("Behavior")]
    public float repeatDelay = 0.18f;     // 키 꾹 누를 때 이동 간격
    public bool wrap = true;             // 위에서 위 누르면 아래로 감

    int index = 0;
    float nextRepeatTime = 0f;

    void OnEnable()
    {
        if (navigate != null) navigate.action.Enable();
        if (submit != null) submit.action.Enable();

        if (submit != null)
            submit.action.performed += OnSubmit;

        UpdateVisual();
    }

    void OnDisable()
    {
        if (submit != null)
            submit.action.performed -= OnSubmit;

        if (navigate != null) navigate.action.Disable();
        if (submit != null) submit.action.Disable();
    }

    void Update()
    {
        if (navigate == null) return;

        Vector2 v = navigate.action.ReadValue<Vector2>();

        // 위/아래만 사용: y가 일정 이상일 때만 반응
        int move = 0;

        if (Time.time >= nextRepeatTime)
        {
            if (v.y > 0.5f) move = -1;       // 위로
            else if (v.y < -0.5f) move = +1; // 아래로

            if (move != 0)
            {
                Move(move);
                nextRepeatTime = Time.time + repeatDelay;
            }
        }
    }

    void Move(int delta)
    {
        int max = 3;
        int newIndex = index + delta;

        if (wrap)
        {
            newIndex = (newIndex % max + max) % max; // 안전한 모듈러
        }
        else
        {
            newIndex = Mathf.Clamp(newIndex, 0, max - 1);
        }

        if (newIndex != index)
        {
            index = newIndex;
            UpdateVisual();
            // 여기서 "삑" 사운드 넣으면 좋음
        }
    }

    void UpdateVisual()
    {
        // 선택된 항목만 강조(색/접두사 둘 중 취향)
        SetLine(startText, index == 0, "GAME START");
        SetLine(optionText, index == 1, "OPTION");
        SetLine(exitText, index == 2, "EXIT GAME");
    }

    void SetLine(TextMeshProUGUI t, bool selected, string label)
    {
        if (t == null) return;
        t.text = selected ? $"> {label}" : $"  {label}";
        // 색으로 강조하고 싶으면:
        // t.color = selected ? Color.white : new Color(1f,1f,1f,0.6f);
    }

    void OnSubmit(InputAction.CallbackContext ctx)
    {
        switch ((MenuItem)index)
        {
            case MenuItem.GameStart:
                SceneManager.LoadScene(gameSceneName);
                break;

            case MenuItem.Option:
                // 옵션 패널 열기 (나중에 구현)
                Debug.Log("Option selected");
                break;

            case MenuItem.ExitGame:
                Application.Quit();
                break;
        }
    }
}
