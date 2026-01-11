using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScrollbarToggleHandle : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler
{
    [Header("Target Scrollbar")]
    [SerializeField] private Scrollbar target;

    [Header("Slash Image (OFF 표시)")]
    [SerializeField] private GameObject slashImage; // OFF(0)일 때 켜질 슬래시

    public enum Channel { BGM, SFX }
    [SerializeField] private Channel channel = Channel.BGM;

    private const string KEY_BGM = "OPT_BGM"; // 1=ON, 0=OFF
    private const string KEY_SFX = "OPT_SFX";

    private void Reset()
    {
        target = GetComponentInParent<Scrollbar>();
    }

    private void OnEnable()
    {
        if (!target) target = GetComponentInParent<Scrollbar>();

        int saved = PlayerPrefs.GetInt(channel == Channel.BGM ? KEY_BGM : KEY_SFX, 1);
        Apply(saved, applySound: true);
    }

    // 핸들 클릭 순간 토글
    public void OnPointerDown(PointerEventData eventData)
    {
        int current = (target != null && target.value >= 0.5f) ? 1 : 0;
        int next = (current == 1) ? 0 : 1;
        Apply(next, applySound: true);
    }

    // 옵션 토글이라 드래그는 막기(원하면 삭제 가능)
    public void OnBeginDrag(PointerEventData eventData) { }
    public void OnDrag(PointerEventData eventData) { }

    private void Apply(int on, bool applySound)
    {
        if (target) target.value = (on == 1) ? 1f : 0f;
        if (slashImage) slashImage.SetActive(on == 0);

        if (channel == Channel.BGM) PlayerPrefs.SetInt(KEY_BGM, on);
        else PlayerPrefs.SetInt(KEY_SFX, on);

        if (!applySound || SoundManager.Instance == null) return;

        if (channel == Channel.BGM) SoundManager.Instance.SetBgmMute(on == 0);
        else SoundManager.Instance.SetSfxMute(on == 0);
    }
}
