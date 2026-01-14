using System.Collections;
using UnityEngine;

public class CameraRoomSlider : MonoBehaviour
{
    public static CameraRoomSlider Instance;

    [SerializeField] private Transform cameraRig; // 비우면 Camera.main.transform
    [SerializeField] private float slideDuration = 0.55f;
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public bool IsSliding { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (!cameraRig) cameraRig = Camera.main.transform;
    }

    public void SnapTo(Transform anchor)
    {
        if (!anchor) return;
        Vector3 p = cameraRig.position;
        cameraRig.position = new Vector3(anchor.position.x, anchor.position.y, p.z);
    }

    public void SlideTo(Transform anchor, System.Action onDone = null)
    {
        if (!anchor || IsSliding) return;
        StartCoroutine(SlideRoutine(anchor, onDone));
    }

    IEnumerator SlideRoutine(Transform anchor, System.Action onDone)
    {
        IsSliding = true;

        Vector3 start = cameraRig.position;
        Vector3 end = new Vector3(anchor.position.x, anchor.position.y, start.z);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, slideDuration);
            float k = ease.Evaluate(Mathf.Clamp01(t));
            cameraRig.position = Vector3.Lerp(start, end, k);
            yield return null;
        }

        cameraRig.position = end;
        IsSliding = false;
        onDone?.Invoke();
    }
}
