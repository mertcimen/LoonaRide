using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NextFeatureRotationEffect : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 90f; 

    void Update()
    {
        transform.localRotation *= Quaternion.Euler(0f, 0f, rotationSpeed * Time.deltaTime);
    }
}
