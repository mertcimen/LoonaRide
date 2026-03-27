using UnityEngine;

public class RandomChildActivator : MonoBehaviour
{
    void OnEnable()
    {
        ActivateRandomChild();
    }

    void ActivateRandomChild()
    {
        int childCount = transform.childCount;

        if (childCount == 0) return;

        for (int i = 0; i < childCount; i++)
            transform.GetChild(i).gameObject.SetActive(false);

        int r = Random.Range(0, childCount);
        transform.GetChild(r).gameObject.SetActive(true);
    }
}