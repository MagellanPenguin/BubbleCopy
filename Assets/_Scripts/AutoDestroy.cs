using UnityEngine;

public class AutoDestroy : MonoBehaviour
{
    [SerializeField] float lifeTime = 0.4f;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }
}
