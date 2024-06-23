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
    //�ִϸ��̼�
    private Animator animator;
    //������ٵ�
    private Rigidbody rigidbody;

    //ī�޶�
    [Header("Camera")]
    [SerializeField] private Camera camera ;    //ī�޶�
    [SerializeField] private float aimFOV =30.0f;     //���� �þ߰�
    [SerializeField] private float aimSpeed = 10.0f;   //���� �ӵ�
    private bool isAim = false;

    //�̵�
    [Header("Move")]
    [SerializeField] private float      defaultSpeed = 50;      //�⺻ �ӵ�
    [SerializeField] private float      walkSpeed = 2;      //�ȴ¼ӵ�
    [SerializeField] private float      runSpeed = 6;       //�ٴ¼ӵ�   
    [SerializeField] private float      rotationVelocity =10.0f; //ȸ�� �ӷ�
    private Vector3 moveDir = Vector3.zero;
    private float targetSpeed=0f; //��ǥ �ӵ�
    private float speed =0f; //���� �ӵ�

    //����
    [Header("Jump")]
    [SerializeField] private float  jumpPower = 250.0f;     //������
    [SerializeField] private int    jumpMaxCount = 2;       //�����ִ�Ƚ�� 
    [SerializeField] private float  jumpCooldown = 1.0f;       //�����ִ�Ƚ�� 
    [SerializeField] private float  groundDis = 0.1f;       //���������Ÿ�
    [SerializeField] private float  fallDis = 1f;           //�������������Ÿ�
    private float lastJumpTime =0.0f;
    private bool    isGround = false;   //��������                                    
    private int     jumpCount = 0;          //����Ƚ��


    private float SpeedMultiplier; // ����
    private float saveSpeed;// ������,�����̵� �ൿ�� �ӵ� ����
    private Vector3 saveDir; // ��������

    //������
    [Header("Roll")]
    [SerializeField] private float rollMultiplier = 3.0f; // ������ �� �ӵ� ���
    [SerializeField] private float shortPressTime = 0.2f; // ª�� ���� �ð� �Ӱ谪   
    private float rollDuration = 1.1f; // ������ �ִϸ��̼� ���� �ð�
    private float rollTimer = 0.0f;
    private float shiftPressStartTime;  
    private bool  isRolling = false;

    //�����̵�
    [Header("Slide")]
    [SerializeField] private float slideMultiplier = 2.0f;   
    private float slideDuration = 1.1f; // �����̵� �ִϸ��̼� ���� �ð�
    private float slideTimer = 0.0f;
    private bool isSlide = false;

    //�׷��ø� ��
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
            camera.fieldOfView = Mathf.Lerp(camera.fieldOfView, aimFOV, Time.deltaTime * aimSpeed);// ���ؽ� �þ߰� Ȯ��

            //���ع��� �ٶ󺸱�
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
            camera.fieldOfView = Mathf.Lerp(camera.fieldOfView, 60.0f, Time.deltaTime * aimSpeed);// �����ؽ� �þ߰� ����
        }

        
        
    }

    private void Move()
    {
        // �Է� ���͸� �����ɴϴ�.
        Vector3 inputDir = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));

        // ī�޶��� Y�� ȸ���� ������ ���� ���͸� �����ɴϴ�.
        Vector3 cameraForward = camera.transform.forward;
        Vector3 cameraRight = camera.transform.right;
        
        // ī�޶��� ���� ���Ϳ��� Y�� ������ �����մϴ�.
        cameraForward.y = 0;
        cameraRight.y = 0;

        // ����ȭ�մϴ�.
        cameraForward.Normalize();
        cameraRight.Normalize();

        // �Է� ������ ī�޶� �������� ��ȯ�մϴ�.
        moveDir = cameraForward * inputDir.z + cameraRight * inputDir.x;
        

        // ��ǥ �ӵ� ���
        if (Input.GetKey(KeyCode.LeftShift)&&  !isAim) //�޸���
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

        //������ �����̵�� �ӵ� ��ġ ����
        if (isRolling || isSlide)
        {
            speed = saveSpeed;
            moveDir = saveDir;
        }

        if(speed < 0.01f ) speed = 0f;

        // �̵�
        speed = Mathf.Lerp(speed, targetSpeed, Time.deltaTime * 10);
        transform.Translate(moveDir * speed * SpeedMultiplier * Time.deltaTime, Space.World);       
        
        
        
        // ȸ��
        if (moveDir != Vector3.zero && !isAim)
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
            
        //����
        if (Input.GetKeyDown(KeyCode.Space) && jumpCount < jumpMaxCount)
        {
            if (Time.time >= lastJumpTime + jumpCooldown) // ��Ÿ�� Ȯ��
            {
                if (!isGround) jumpCount++;
                animator.SetTrigger("Jump");               
               rigidbody.AddForce(new Vector3(0, jumpPower, 0));               
                lastJumpTime = Time.time; // ������ ���� �ð� ����
            }
            
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
        if (Input.GetKeyUp(KeyCode.LeftShift) && speed >= 1.0f)
        {
            float pressDuration = Time.time - shiftPressStartTime;
            if (pressDuration <= shortPressTime && animator.GetFloat("Speed") >= 1 && !isRolling)
            {
                isRolling = true;
                rollTimer = 0.0f;
                animator.SetTrigger("Roll");
                saveSpeed = speed;  //�ӵ� ����
                saveDir = moveDir;  //���� ����
            }
        }

        //������
        if(isRolling)
        {
            rollTimer += Time.deltaTime;
            SpeedMultiplier = rollMultiplier; //�ӵ��� ����� ����
            if (rollTimer >= rollDuration)
                isRolling = false;
        }
        else
        {
            SpeedMultiplier = 1.0f; //�����Ⱑ ������ 1����� ����
        }

        
    }

    private void slide()
    {
       
        if (Input.GetKeyDown(KeyCode.C) && speed >= 4.0f)
        {
            isSlide = true;
            slideTimer = 0.0f;
            animator.SetTrigger("Slide");
            saveSpeed = speed; //�ӵ�����
            saveDir = moveDir;  //���� ����
        }

        if (isSlide)
        {
            slideTimer += Time.deltaTime;
            SpeedMultiplier = slideMultiplier; //�ӵ��� ����� ����
            if (slideTimer >= slideDuration)
                isSlide = false;
        }
        else
        {
            SpeedMultiplier = 1.0f; //�����Ⱑ ������ 1����� ����
        }



    }

    private void GrapplingHook()
    {
        hookPos = animator.GetBoneTransform(HumanBodyBones.LeftIndexProximal).position;
        
        //�ڵ� ������ ���
        Vector3 realHitPoint = Vector3.zero;

        RaycastHit sphereCastHit;
        Physics.SphereCast(camera.transform.position, 1.0f, camera.transform.forward,
            out sphereCastHit, grapplingDis, grapplingOBJ);

        RaycastHit rayCastHit;
        Physics.Raycast(camera.transform.position, camera.transform.forward,
            out rayCastHit, grapplingDis, grapplingOBJ);

        if(rayCastHit.point != Vector3.zero) //��Ȯ�� ����
            realHitPoint = rayCastHit.point;
        else if(sphereCastHit.point != Vector3.zero) // ��ó ����
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

        // E Ű�� ���� ���� �۵��ϵ��� ���� �и�
        if (Input.GetKeyDown(KeyCode.E))
        {
           

            if (!isGrappling)
            {
                
                
                    Vector3 grapplingDir = transform.position - prediction.point;
                    float angle = Vector3.Angle(transform.forward, grapplingDir);
                    if (angle > grapplingMaxAngle)
                    {
                        //�׷��� ����
                        isGrappling = true;
                        grapplingStartTime = Time.time;
                        lineRenderer.positionCount = 2;
                        lineRenderer.SetPosition(0, hookPos);
                        lineRenderer.SetPosition(1, prediction.point);

                        Vector3 savePredictionPos = prediction.point;
                        aimOBJ.transform.position = savePredictionPos;

                        //���� �ۿ��ϴ� �� �߰�
                        rigidbody.AddForce(new Vector3(0, grapplingJumpPower, 0));

                        //������ ����Ʈ ����
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
            else // E Ű ���Է½� �׷��� ����
            {
                isGrappling = false;
                lineRenderer.positionCount = 0;
                Destroy(springJoint);
            }
        }

        //�׷��ø�
        if (isGrappling)
        {
            // ���� ������ ������Ʈ
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
