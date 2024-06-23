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
using JetBrains.Annotations;
using TMPro;


public class PlayerManager : MonoBehaviour
{
    //애니메이션
    private Animator animator;
    //리지드바디
    private Rigidbody rigidbody;

    //카메라
    [Header("Camera")]
    [SerializeField] private Camera camera ;    //카메라
    [SerializeField] private float aimFOV =30.0f;     //조준 시야각
    [SerializeField] private float aimSpeed = 10.0f;   //조준 속도
    private bool isAim = false;

    //이동
    [Header("Move")]
    [SerializeField] private float      defaultSpeed = 50;      //기본 속도
    [SerializeField] private float      walkSpeed = 2;      //걷는속도
    [SerializeField] private float      runSpeed = 6;       //뛰는속도   
    [SerializeField] private float      rotationVelocity =10.0f; //회전 속력
    private Vector3 moveDir = Vector3.zero;
    private float targetSpeed=0f; //목표 속도
    private float speed =0f; //현재 속도

    //점프
    [Header("Jump")]
    [SerializeField] private float  jumpPower = 250.0f;     //점프력
    [SerializeField] private int    jumpMaxCount = 2;       //점프최대횟수 
    [SerializeField] private float  jumpCooldown = 1.0f;       //점프최대횟수 
    [SerializeField] private float  groundDis = 0.1f;       //지면판정거리
    [SerializeField] private float  fallDis = 1f;           //자유낙하판정거리
    private float lastJumpTime =0.0f;
    private bool    isGround = false;   //지면판정                                    
    private int     jumpCount = 0;          //접프횟수


    private float SpeedMultiplier; // 가속
    private float saveSpeed;// 구르기,슬라이드 행동시 속도 저장
    private Vector3 saveDir; // 방향저장

    //구르기
    [Header("Roll")]
    [SerializeField] private float rollMultiplier = 3.0f; // 구르기 중 속도 배수
    [SerializeField] private float shortPressTime = 0.2f; // 짧은 누름 시간 임계값   
    private float rollDuration = 1.1f; // 구르기 애니메이션 지속 시간
    private float rollTimer = 0.0f;
    private float shiftPressStartTime;  
    private bool  isRolling = false;

    //슬라이드
    [Header("Slide")]
    [SerializeField] private float slideMultiplier = 2.0f;   
    private float slideDuration = 1.1f; // 슬라이드 애니메이션 지속 시간
    private float slideTimer = 0.0f;
    private bool isSlide = false;

    //그래플링 훅
    [Header("Grappling Hook")]
    [SerializeField] private LayerMask grapplingOBJ;
    [SerializeField] private GameObject aimOBJ;   
    [SerializeField] private float  grapplingDis = 100.0f;
    [SerializeField] private float  grapplingDur = 5.0f;
    [SerializeField] private float  grapplingMaxAngle = 90.0f;
    [SerializeField] private float  grapplingJumpPower = 300.0f;
    [SerializeField] private float  spring = 5.0f;
    [SerializeField] private float  damper = 5.0f;
    [SerializeField] private float  massScale = 5.0f;
    private Vector3 hookPos = Vector3.zero;
    private Vector3 Spot = Vector3.zero;
    private float grapplingStartTime = 0.0f;
    private bool isGrappling = false;
    private LineRenderer lineRenderer;
    private SpringJoint springJoint;




    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       animator = GetComponent<Animator>();
       rigidbody = GetComponent<Rigidbody>();
       lineRenderer = GetComponent<LineRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        Aim();
        Move();
        Jump();
        Roll();
        slide();
        GrapplingHook();
    }

    private void Aim()
    {
        if (Input.GetMouseButton(1)) 
        {
            isAim = true;
            camera.fieldOfView = Mathf.Lerp(camera.fieldOfView, aimFOV, Time.deltaTime * aimSpeed);// 조준시 시야각 확대

            //조준방향 바라보기
            RaycastHit hit;
            Vector3 targetPosition = Vector3.zero;
            if(Physics.Raycast(camera.transform.position,camera.transform.forward,out hit,Mathf.Infinity))
            {
                targetPosition = hit.point;
            }
            else
            {
                targetPosition = camera.transform.position + camera.transform.forward * 10.0f;
            }
            targetPosition.y = transform.position.y;
            Vector3 aimDir = (targetPosition - transform.position).normalized;
            transform.forward = Vector3.Lerp(transform.forward,aimDir, Time.deltaTime * rotationVelocity);
        }
        else 
        {
            isAim=false;
            camera.fieldOfView = Mathf.Lerp(camera.fieldOfView, 60.0f, Time.deltaTime * aimSpeed);// 비조준시 시야각 복구
        }

        
        
    }

    private void Move()
    {
        // 입력 벡터를 가져옵니다.
        Vector3 inputDir = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));

        // 카메라의 Y축 회전을 무시한 방향 벡터를 가져옵니다.
        Vector3 cameraForward = camera.transform.forward;
        Vector3 cameraRight = camera.transform.right;
        
        // 카메라의 방향 벡터에서 Y축 성분을 제거합니다.
        cameraForward.y = 0;
        cameraRight.y = 0;

        // 정규화합니다.
        cameraForward.Normalize();
        cameraRight.Normalize();

        // 입력 방향을 카메라 기준으로 변환합니다.
        moveDir = cameraForward * inputDir.z + cameraRight * inputDir.x;
        

        // 목표 속도 계산
        if (Input.GetKey(KeyCode.LeftShift)&&  !isAim) //달리기
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

        //구르기 슬라이드시 속도 위치 고정
        if (isRolling || isSlide)
        {
            speed = saveSpeed;
            moveDir = saveDir;
        }

        if(speed < 0.01f ) speed = 0f;

        // 이동
        speed = Mathf.Lerp(speed, targetSpeed, Time.deltaTime * 10);
        transform.Translate(moveDir * speed * SpeedMultiplier * Time.deltaTime, Space.World);       
        
        
        
        // 회전
        if (moveDir != Vector3.zero && !isAim)
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
            
        //점프
        if (Input.GetKeyDown(KeyCode.Space) && jumpCount < jumpMaxCount)
        {
            if (Time.time >= lastJumpTime + jumpCooldown) // 쿨타임 확인
            {
                if (!isGround) jumpCount++;
                animator.SetTrigger("Jump");               
               rigidbody.AddForce(new Vector3(0, jumpPower, 0));               
                lastJumpTime = Time.time; // 마지막 점프 시간 갱신
            }
            
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
        if (Input.GetKeyUp(KeyCode.LeftShift) && speed >= 1.0f)
        {
            float pressDuration = Time.time - shiftPressStartTime;
            if (pressDuration <= shortPressTime && animator.GetFloat("Speed") >= 1 && !isRolling)
            {
                isRolling = true;
                rollTimer = 0.0f;
                animator.SetTrigger("Roll");
                saveSpeed = speed;  //속도 저장
                saveDir = moveDir;  //방향 저장
            }
        }

        //구르기
        if(isRolling)
        {
            rollTimer += Time.deltaTime;
            SpeedMultiplier = rollMultiplier; //속도에 배속을 적용
            if (rollTimer >= rollDuration)
                isRolling = false;
        }
        else
        {
            SpeedMultiplier = 1.0f; //구르기가 끝난후 1배수로 복원
        }

        
    }

    private void slide()
    {
       
        if (Input.GetKeyDown(KeyCode.C) && speed >= 4.0f)
        {
            isSlide = true;
            slideTimer = 0.0f;
            animator.SetTrigger("Slide");
            saveSpeed = speed; //속도저장
            saveDir = moveDir;  //방향 저장
        }

        if (isSlide)
        {
            slideTimer += Time.deltaTime;
            SpeedMultiplier = slideMultiplier; //속도에 배속을 적용
            if (slideTimer >= slideDuration)
                isSlide = false;
        }
        else
        {
            SpeedMultiplier = 1.0f; //구르기가 끝난후 1배수로 복원
        }



    }

    private void GrapplingHook()
    {
        hookPos = animator.GetBoneTransform(HumanBodyBones.LeftIndexProximal).position;
        
        //자동 조준점 계산
        Vector3 realHitPoint = Vector3.zero;

        RaycastHit sphereCastHit;
        Physics.SphereCast(camera.transform.position, 1.0f, camera.transform.forward,
            out sphereCastHit, grapplingDis, grapplingOBJ);

        RaycastHit rayCastHit;
        Physics.Raycast(camera.transform.position, camera.transform.forward,
            out rayCastHit, grapplingDis, grapplingOBJ);

        if(rayCastHit.point != Vector3.zero) //정확한 조준
            realHitPoint = rayCastHit.point;
        else if(sphereCastHit.point != Vector3.zero) // 근처 조준
            realHitPoint = sphereCastHit.point;  
        else realHitPoint = Vector3.zero; // MISS

        if(realHitPoint != Vector3.zero)
        {           
            aimOBJ.gameObject.SetActive(true);
            if(!isGrappling) aimOBJ.transform.position = realHitPoint;
        }
        else
        {
            aimOBJ.gameObject.SetActive(false);
        }

        RaycastHit prediction = rayCastHit.point == Vector3.zero ? sphereCastHit : rayCastHit;

        // E 키를 누를 때만 작동하도록 조건 분리
        if (Input.GetKeyDown(KeyCode.E))
        {
           

            if (!isGrappling)
            {
                
                
                    Vector3 grapplingDir = transform.position - prediction.point;
                    float angle = Vector3.Angle(transform.forward, grapplingDir);
                    if (angle > grapplingMaxAngle)
                    {
                        //그래플 생성
                        isGrappling = true;
                        grapplingStartTime = Time.time;
                        lineRenderer.positionCount = 2;
                        lineRenderer.SetPosition(0, hookPos);
                        lineRenderer.SetPosition(1, prediction.point);

                        Vector3 savePredictionPos = prediction.point;
                        aimOBJ.transform.position = savePredictionPos;

                        //위로 작용하는 힘 추가
                        rigidbody.AddForce(new Vector3(0, grapplingJumpPower, 0));

                        //스프링 조인트 생성
                        Spot = prediction.point;
                        springJoint = this.gameObject.AddComponent<SpringJoint>();
                        springJoint.autoConfigureConnectedAnchor = false;
                        springJoint.connectedAnchor = Spot;

                        float dis = Vector3.Distance(transform.position, Spot);
                        springJoint.spring = spring;
                        springJoint.damper = damper;
                        springJoint.massScale = massScale;
                    
                    }
                    

                    
                
                
            }
            else // E 키 재입력시 그래플 해재
            {
                isGrappling = false;
                lineRenderer.positionCount = 0;
                Destroy(springJoint);
            }
        }

        //그래플링
        if (isGrappling)
        {
            // 라인 렌더러 업데이트
            lineRenderer.SetPosition(0, hookPos);
            Vector3 grapplingDir = lineRenderer.GetPosition(1) - lineRenderer.GetPosition(0);
            float angle = Vector3.Angle(transform.forward, grapplingDir);

            if ((Time.time - grapplingStartTime > grapplingDur || angle > grapplingMaxAngle))
            {
                isGrappling = false;
                lineRenderer.positionCount = 0;                
                Destroy(springJoint);
            }

        }
        else
        {          
            
        }

        
    }


    
}
