using Cinemachine;
using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.UI;

public class PlayerManager : MonoBehaviour
{
   
    //�̵�
    [Header("Move")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private Transform cameraTransform;


    //����
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
        // �Է� ���͸� �����ɴϴ�.
        Vector3 inputDir = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));

        // ī�޶��� Y�� ȸ���� ������ ���� ���͸� �����ɴϴ�.
        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;

        // ī�޶��� ���� ���Ϳ��� Y�� ������ �����մϴ�.
        cameraForward.y = 0;
        cameraRight.y = 0;

        // ����ȭ�մϴ�.
        cameraForward.Normalize();
        cameraRight.Normalize();

        // �Է� ������ ī�޶� �������� ��ȯ�մϴ�.
        Vector3 moveDir = cameraForward * inputDir.z + cameraRight * inputDir.x;

        // �̵��մϴ�.
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
