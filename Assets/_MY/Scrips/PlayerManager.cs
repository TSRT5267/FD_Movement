using Cinemachine;
using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.UI;

public class PlayerManager : MonoBehaviour
{
   
    //이동
    [Header("Move")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private Transform cameraTransform;


    //점프
    [Header("Jump")]
    [SerializeField] private float jumpPower;
    





   

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       
    }

    // Update is called once per frame
    void Update()
    {
        Move();
        Jump();
        
    }

    private void Move()
    {
        // 입력 벡터를 가져옵니다.
        Vector3 inputDir = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));

        // 카메라의 Y축 회전을 무시한 방향 벡터를 가져옵니다.
        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;

        // 카메라의 방향 벡터에서 Y축 성분을 제거합니다.
        cameraForward.y = 0;
        cameraRight.y = 0;

        // 정규화합니다.
        cameraForward.Normalize();
        cameraRight.Normalize();

        // 입력 방향을 카메라 기준으로 변환합니다.
        Vector3 moveDir = cameraForward * inputDir.z + cameraRight * inputDir.x;

        // 이동합니다.
        transform.Translate(moveDir * moveSpeed * Time.deltaTime, Space.World);
    }

    private void Jump()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            GetComponent<Rigidbody>().AddForce(new Vector3(0, jumpPower, 0));
        }
    }

   
}
