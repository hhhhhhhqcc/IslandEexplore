using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
     [Header("跟随目标")]
    public Transform target;

    [Header("平滑度")]
    public float smoothSpeed = 0.125f;

    private Vector3 offset;

    // Start is called before the first frame update
    void Start()
    {
        // 如果没手动拖目标，自动找 Player
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }

        // 记录相机和目标的初始偏移距离
        offset = transform.position - target.position;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(
            transform.position,
            desiredPosition,
            smoothSpeed
        );
        transform.position = smoothedPosition;
    }
}
