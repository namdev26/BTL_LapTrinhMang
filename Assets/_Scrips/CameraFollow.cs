using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Range(1, 10)] public float followSpeed = 2;
    [Range(1, 10)] public float lookSpeed = 5;

    private Transform carTransform;
    private Vector3 offset;

    public void SetTarget(Transform target)
    {
        carTransform = target;
        offset = transform.position - target.position;
        Debug.Log("✅ CameraFollow target set to " + target.name);
    }

    void LateUpdate()
    {
        if (carTransform == null) return;

        Vector3 lookDirection = carTransform.position - transform.position;
        Quaternion rot = Quaternion.LookRotation(lookDirection, Vector3.up);
        transform.rotation = Quaternion.Lerp(transform.rotation, rot, lookSpeed * Time.deltaTime);

        Vector3 targetPos = carTransform.position + offset;
        transform.position = Vector3.Lerp(transform.position, targetPos, followSpeed * Time.deltaTime);
    }
}
