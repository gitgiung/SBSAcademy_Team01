using UnityEngine;

public class CameraPlayer : MonoBehaviour
{
    public GameObject PlayerTarget;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        Vector3 offset = new Vector3(0f, 0f, -10f); // Y로 2, Z로 -10 위치
        transform.position = PlayerTarget.transform.position + (PlayerTarget.transform.rotation * offset);

    }
}
