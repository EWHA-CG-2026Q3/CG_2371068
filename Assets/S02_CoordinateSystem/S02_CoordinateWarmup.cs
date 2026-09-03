using UnityEngine;

[ExecuteAlways]
public class S02_CoordinateWarmup : MonoBehaviour
{
    [SerializeField] private Vector3 targetPosition;
    [SerializeField] private Vector3 targetRotationEuler;
    [SerializeField] private Vector3 targetScale = Vector3.one; //[SerializeField] : inspector에 노출

    void Update()
    {
        transform.position = targetPosition;
        transform.rotation = Quaternion.Euler(targetRotationEuler);
        transform.localScale = targetScale; //transform : gameobject의 transform object. <-> 대문자 Transfrom
    }
}
