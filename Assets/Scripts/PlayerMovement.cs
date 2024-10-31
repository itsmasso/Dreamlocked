
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Initialize")]
    [SerializeField] private Rigidbody playerRb;

    [Header("Movement")]
    [SerializeField] private float baseMoveSpeed;
    private float moveSpeed;
    [SerializeField] private float groundDrag; //adding drag to movement so player is not slippery on the ground
    private Vector2 inputDir; 
    private Vector3 moveDirection;

    [Header("Sprinting")]
    [SerializeField] private float addedSprintSpeed;
    private bool isSprinting;

    [Header("Crouching")]
    //[SerializeField] private 

    [Header("Jump")]
    [SerializeField] private float jumpForce;
    [SerializeField] private float fallForce;
    [SerializeField] private float jumpCooldown;
    [SerializeField] private float airMultiplier; //adds some air resistance while jumping
    private bool canJump;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheckTransform;
    [SerializeField] private LayerMask groundCheckLayer;
    private bool isGrounded;
    

    void Start()
    {
        moveSpeed = baseMoveSpeed;
        isSprinting = false;
        canJump = true;
        playerRb = gameObject.GetComponent<Rigidbody>();
    }

    public void OnMove(InputAction.CallbackContext ctx){
        inputDir = ctx.ReadValue<Vector2>(); //getting the player's input values. example: input A returns (-1, 0)
    }

    public void OnSprint(InputAction.CallbackContext ctx){
        //later probably add a limit to the spring and/or stamina bar
        if(ctx.performed){
            isSprinting = true;
        }
        if(ctx.canceled){
            isSprinting = false;
        }
    }

    public void OnJump(InputAction.CallbackContext ctx){
        if(isGrounded && ctx.performed && canJump){
            playerRb.linearVelocity = new Vector3(playerRb.linearVelocity.x, 0f, playerRb.linearVelocity.z);
            playerRb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
            StartCoroutine(ApplyJumpCooldown());
        }
    }

    private IEnumerator ApplyJumpCooldown(){
        canJump = false;
        yield return new WaitForSeconds(jumpCooldown);
        canJump = true;
    }

    void Update()
    {
        //checking to see if sphere collider is touching the ground to determine if player is grounded or not
        isGrounded = Physics.CheckSphere(groundCheckTransform.position, 0.25f, groundCheckLayer); 

        //apply movespeed increase if sprinting
        if(isSprinting){
            moveSpeed = baseMoveSpeed + addedSprintSpeed;
        }else{
            moveSpeed = baseMoveSpeed;
        }

        Vector3 moveDir = transform.right * inputDir.x + transform.forward * inputDir.y; //getting direction of movement. Multiplies player's X and Z values by input direction
        moveDirection = moveDir.normalized; //normalizing movement direction to prevent diagonal direction from moving faster
        LimitSpeed();
    }

    private void LimitSpeed(){
        //limiting the speed so that when the player moves diagonally, their velocity remains the same and does not speed up.
        Vector3 horizontalVelocity = new Vector3(playerRb.linearVelocity.x, 0f, playerRb.linearVelocity.z);
        if(horizontalVelocity.magnitude > moveSpeed){
            Vector3 maxVelocity = playerRb.linearVelocity.normalized * moveSpeed;
            playerRb.linearVelocity = new Vector3(maxVelocity.x, playerRb.linearVelocity.y, maxVelocity.z);
        }
    }
    
    private void FixedUpdate(){
        playerRb.linearDamping = isGrounded ? groundDrag : 0; //if player is on the ground, we apply the ground drag and if in the air we need 0 drag resistance
        if(isGrounded){
            playerRb.AddForce(moveDirection * moveSpeed * 10f, ForceMode.Force);
        }else{
            playerRb.AddForce(moveDirection * moveSpeed * 10f * airMultiplier, ForceMode.Force); //apply air resistance multiplier to slow down movement in the air
            if(playerRb.linearVelocity.y < 0){
                playerRb.AddForce(Vector3.down * fallForce * 10f, ForceMode.Acceleration); //apply some downward force when velocity is less than 0 meaning player is falling
            }
        }

    }
}
