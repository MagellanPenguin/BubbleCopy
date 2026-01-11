using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class TitleMenuUI : MonoBehaviour
{
    public enum Menu { Start, Option, Ranking, Exit }

    [Header("Menu Text (TMP)")]
    public TextMeshProUGUI startText;
    public TextMeshProUGUI optionText;
    public TextMeshProUGUI rankingText;
    public TextMeshProUGUI exitText;

    [Header("Cursor")]
    public RectTransform cursorRT;              // 커서 Image의 RectTransform
    public Vector2 cursorOffset = new Vector2(-24f, 0f);
    public Canvas canvas;                       // 이 UI가 속한 Canvas

    [Header("Panels")]
    public GameObject titlePanel;
    public GameObject optionPanel;
    public GameObject rankPanel;
    public GameObject gameUIPanel;

    [Header("Input Actions (UI)")]
    public InputActionReference navigate;       // UI/Navigate (Value Vector2)
    public InputActionReference submit;         // UI/Submit (Button)

    [Header("Behavior")]
    public bool wrap = true;
    public float repeatDelay = 0.18f;

    [Header("Colors")]
    public Color selectedColor = Color.white;
    public Color normalColor = new Color(1f, 1f, 1f, 0.55f);

    int index = 0;
    float nextMoveTime = 0f;

    void OnEnable()
    {
        if (navigate != null) navigate.action.Enable();
        if (submit != null)
        {
            submit.action.Enable();
            submit.action.performed += OnSubmit;
        }

        SetPanels(title: true, option: false, rank: false, gameUI: false);
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
        if (Time.time < nextMoveTime) return;

        int move = 0;
        if (v.y > 0.5f) move = -1;        // 위
        else if (v.y < -0.5f) move = +1;  // 아래

        if (move != 0)
        {
            Move(move);
            nextMoveTime = Time.time + repeatDelay;
        }
    }

    void Move(int delta)
    {
        int count = 4;
        int newIndex = index + delta;

        if (wrap)
            newIndex = (newIndex % count + count) % count;
        else
            newIndex = Mathf.Clamp(newIndex, 0, count - 1);

        if (newIndex == index) return;

        index = newIndex;
        UpdateVisual();
    }

    void OnSubmit(InputAction.CallbackContext ctx)
    {
        switch ((Menu)index)
        {
            case Menu.Start:
                SetPanels(title: false, option: false, rank: false, gameUI: true);
                break;

            case Menu.Option:
                SetPanels(title: false, option: true, rank: false, gameUI: false);
                break;

            case Menu.Ranking:
                SetPanels(title: false, option: false, rank: true, gameUI: false);
                break;

            case Menu.Exit:
                Application.Quit();
                break;
        }
    }
    
    void UpdateVisual()
    {
        // 기본 색 (RGB 50)
        Color baseColor = new Color32(50, 50, 50, 255);
        // 선택 색 (RGB 255)
        Color selectedColor = new Color32(255, 255, 255, 255);

        // 전부 기본색으로 초기화
        SetColor(startText, baseColor);
        SetColor(optionText, baseColor);
        SetColor(rankingText, baseColor);
        SetColor(exitText, baseColor);

        // 선택된 항목만 흰색으로
        switch ((Menu)index)
        {
            case Menu.Start:
                SetColor(startText, selectedColor);
                break;
            case Menu.Option:
                SetColor(optionText, selectedColor);
                break;
            case Menu.Ranking:
                SetColor(rankingText, selectedColor);
                break;
            case Menu.Exit:
                SetColor(exitText, selectedColor);
                break;
        }

        // 커서 위치 이동
        RectTransform targetRT = GetTargetRT(index);
        SnapCursorToLeftCenter(cursorRT, targetRT, cursorOffset, canvas);
    }

    void SetColor(TextMeshProUGUI text, Color color)
    {
        if (text == null) return;
        text.color = color;
    }


    RectTransform GetTargetRT(int idx)
    {
        return idx switch
        {
            0 => startText.rectTransform,
            1 => optionText.rectTransform,
            2 => rankingText.rectTransform,
            3 => exitText.rectTransform,
            _ => startText.rectTransform
        };
    }

    void SetPanels(bool title, bool option, bool rank, bool gameUI)
    {
        if (titlePanel != null) titlePanel.SetActive(title);
        if (optionPanel != null) optionPanel.SetActive(option);
        if (rankPanel != null) rankPanel.SetActive(rank);
        if (gameUIPanel != null) gameUIPanel.SetActive(gameUI);
    }

    // ===== 커서 위치 계산 유틸 =====
    static void SnapCursorToLeftCenter(
        RectTransform cursorRT,
        RectTransform targetRT,
        Vector2 pixelOffset,
        Canvas canvas)
    {
        if (cursorRT == null || targetRT == null || canvas == null) return;

        // 타겟의 월드 코너
        Vector3[] corners = new Vector3[4];
        targetRT.GetWorldCorners(corners);
        // 0=LB, 1=LT

        Vector3 leftCenterWorld = (corners[0] + corners[1]) * 0.5f;

        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera;

        Vector2 screenPoint =
            RectTransformUtility.WorldToScreenPoint(cam, leftCenterWorld);

        RectTransform parentRT = cursorRT.parent as RectTransform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRT, screenPoint, cam, out Vector2 localPoint);

        cursorRT.anchoredPosition = localPoint + pixelOffset;
    }
}
