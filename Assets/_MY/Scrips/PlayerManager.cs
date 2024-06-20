using Cinemachine;
using StarterAssets;
using System.Numerics;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.UI;
using UnityEngine.Windows;

using Vector3 = UnityEngine.Vector3;
using Input = UnityEngine.Input;
using Quaternion = UnityEngine.Quaternion;


public class PlayerManager : MonoBehaviour
{
    //�ִϸ��̼�
    private Animator animator;
    //������ٵ�
    private Rigidbody rigidbody;


    //�̵�
    [Header("Move")]
    [SerializeField] private float      walkSpeed = 2;      //�ȴ¼ӵ�
    [SerializeField] private float      runSpeed = 6;       //�ٴ¼ӵ�
    [SerializeField] private Transform  cameraTransform;    //ī�޶�
    [SerializeField] private float      rotationVelocity =10.0f; //ȸ�� �ӷ�
    private float targetSpeed=0f; //��ǥ �ӵ�
    private float speed =0f; //���� �ӵ�

    //����
    [Header("Jump")]
    [SerializeField] private float  jumpPower = 250.0f;     //������
    [SerializeField] private int    jumpMaxCount = 2;       //�����ִ�Ƚ�� 
    [SerializeField] private float  groundDis = 0.1f;       //���������Ÿ�
    [SerializeField] private float  fallDis = 1f;           //�������������Ÿ�
    private bool    isGround = false;   //��������                                    
    private int     jumpCount = 0;          //����Ƚ��

    //������
    [Header("Roll")]
    [SerializeField] private float rollSpeedMultiplier = 1.0f; // ������ �� �ӵ� ���
    [SerializeField] private float shortPressTime = 0.2f; // ª�� ���� �ð� �Ӱ谪
    private float roll;
    private float rollDuration = 1.0f; // ������ �ִϸ��̼� ���� �ð�
    private float rollTimer = 0.0f;
    private float shiftPressStartTime;
    private bool  isRolling = false;





    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       animator = GetComponent<Animator>();
       rigidbody = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        Move();
        Jump();
        Roll();
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

        // ��ǥ �ӵ� ���
        if (Input.GetKey(KeyCode.LeftShift)) //�޸���
        {
            targetSpeed = runSpeed;
        }
        else if(inputDir != new Vector3(0,0,0)) //�ȱ�
        {
            targetSpeed = walkSpeed;
        }
        else                                    //idle
        {
             targetSpeed = 0;
        }

        if(speed < 0.01f ) speed = 0f;

        // �̵�
        speed = Mathf.Lerp(speed, targetSpeed, Time.deltaTime * 10);
        transform.Translate(moveDir * speed * rollSpeedMultiplier * Time.deltaTime, Space.World);
        // ȸ��
        if (moveDir != Vector3.zero)
        {          
            // �̵��� ���� �������� �ϴ� Quaternion����
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            // ��ǥ ȸ�������� ���������� ȸ��
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationVelocity * Time.deltaTime);  
        }
        //�ִϸ��̼� ����       
        animator.SetFloat("Speed", speed);

    }

    private void Jump()
    {
        //����ĳ������ �̿��� �ٴ����� ����
        RaycastHit hit;
        isGround = Physics.Raycast(transform.position, Vector3.down, groundDis);
        

        //��������
        if (isGround)
        {
            jumpCount = 1;
            animator.SetBool("Grounded", true);
           
        }
        else
        {
            animator.SetBool("Grounded", false);
            
        }


        //�������� 
        if(Physics.Raycast(transform.position+new Vector3(0,1,0), Vector3.down, out hit))
        {          
            if (hit.distance >= fallDis)
            {
                animator.SetBool("FreeFall", true);               
            }                
            else
            {
                animator.SetBool("FreeFall", false);
            }
                
        }
            


        if (Input.GetKeyDown(KeyCode.Space) && jumpCount < jumpMaxCount)
        {
            if(!isGround) jumpCount++;          
            animator.SetTrigger("Jump");
            rigidbody.linearVelocity = new Vector3(rigidbody.linearVelocity.x, 0f, rigidbody.linearVelocity.z);
            rigidbody.AddForce(new Vector3(0, jumpPower, 0));
        }


        
    }

    private void Roll()
    {
        // LeftShift Ű�� ������ ������ �� �ð� ���
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            shiftPressStartTime = Time.time;
        }
        // LeftShift Ű�� �� �� ���� �ð��� ª���� ������ ���� ����
        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            float pressDuration = Time.time - shiftPressStartTime;
            if (pressDuration <= shortPressTime && animator.GetFloat("Speed") >= 1 && !isRolling)
            {
                isRolling = true;
                rollTimer = 0.0f;
                animator.SetTrigger("Roll");
                
            }
        }

        //������
        if(isRolling)
        {
            rollTimer += Time.deltaTime;                
            rollSpeedMultiplier = 1.5f;
            if (rollTimer >= rollDuration)
                isRolling = false;
        }
        else
        {
            rollSpeedMultiplier = 1.0f;
        }

        
    }
}
