using DG.Tweening;
using UnityEngine;

public class RestartTweenOnEnable : MonoBehaviour
{
    private DOTweenAnimation anim;

    void Awake()
    {
        anim = GetComponent<DOTweenAnimation>();
        if (anim != null && anim.tween == null)
            anim.CreateTween(); 
    }

    void OnEnable()
    {
        if (anim == null) return;
        anim.DORewind();
        anim.DOPlay();
    }
}