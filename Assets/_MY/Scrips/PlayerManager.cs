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
    //애니메이션
    private Animator animator;
    //리지드바디
    private Rigidbody rigidbody;


    //이동
    [Header("Move")]
    [SerializeField] private float      walkSpeed = 2;      //걷는속도
    [SerializeField] private float      runSpeed = 6;       //뛰는속도
    [SerializeField] private Transform  cameraTransform;    //카메라
    [SerializeField] private float      rotationVelocity =10.0f; //회전 속력
    private float targetSpeed=0f; //목표 속도
    private float speed =0f; //현재 속도

    //점프
    [Header("Jump")]
    [SerializeField] private float  jumpPower = 250.0f;     //점프력
    [SerializeField] private int    jumpMaxCount = 2;       //점프최대횟수 
    [SerializeField] private float  groundDis = 0.1f;       //지면판정거리
    [SerializeField] private float  fallDis = 1f;           //자유낙하판정거리
    private bool    isGround = false;   //지면판정                                    
    private int     jumpCount = 0;          //접프횟수

    //구르기
    [Header("Roll")]
    [SerializeField] private float rollSpeedMultiplier = 1.0f; // 구르기 중 속도 배수
    [SerializeField] private float shortPressTime = 0.2f; // 짧은 누름 시간 임계값
    private float roll;
    private float rollDuration = 1.0f; // 구르기 애니메이션 지속 시간
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

        // 목표 속도 계산
        if (Input.GetKey(KeyCode.LeftShift)) //달리기
        {
            targetSpeed = runSpeed;
        }
        else if(inputDir != new Vector3(0,0,0)) //걷기
        {
            targetSpeed = walkSpeed;
        }
        else                                    //idle
        {
             targetSpeed = 0;
        }

        if(speed < 0.01f ) speed = 0f;

        // 이동
        speed = Mathf.Lerp(speed, targetSpeed, Time.deltaTime * 10);
        transform.Translate(moveDir * speed * rollSpeedMultiplier * Time.deltaTime, Space.World);
        // 회전
        if (moveDir != Vector3.zero)
        {          
            // 이동할 방향 기준으로 하는 Quaternion생성
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            // 목표 회전값으로 점진적으로 회전
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationVelocity * Time.deltaTime);  
        }
        //애니메이션 제어       
        animator.SetFloat("Speed", speed);

    }

    private void Jump()
    {
        //레이캐스팅을 이용해 바닥인지 판정
        RaycastHit hit;
        isGround = Physics.Raycast(transform.position, Vector3.down, groundDis);
        

        //지면접촉
        if (isGround)
        {
            jumpCount = 1;
            animator.SetBool("Grounded", true);
           
        }
        else
        {
            animator.SetBool("Grounded", false);
            
        }


        //자유낙하 
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
        // LeftShift 키를 누르기 시작할 때 시간 기록
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            shiftPressStartTime = Time.time;
        }
        // LeftShift 키를 뗄 때 누른 시간이 짧으면 구르기 동작 실행
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

        //구르기
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
