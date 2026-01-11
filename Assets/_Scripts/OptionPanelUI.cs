using UnityEngine;
using UnityEngine.UI;

public class OptionPanelUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject titlePanel;   // 타이틀 패널
    [SerializeField] private GameObject optionPanel;  // 옵션 패널(보통 자기 자신)

    [Header("Back")]
    [SerializeField] private Button backButton;
    [SerializeField] private KeyCode backKey = KeyCode.Escape; // PC 백키 대용

    private void Awake()
    {
        if (optionPanel == null) optionPanel = gameObject;
        if (backButton) backButton.onClick.AddListener(BackToTitle);
    }

    private void Update()
    {
        if (Input.GetKeyDown(backKey))
            BackToTitle();
    }

    public void BackToTitle()
    {
        // (선택) UI 뒤로가기 소리
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySfx(SoundManager.SfxId.UIBack);

        if (optionPanel) optionPanel.SetActive(false);
        else gameObject.SetActive(false);

        if (titlePanel) titlePanel.SetActive(true);
    }
}
