using UnityEngine;
using Invector.vCharacterController;

public class vCrouchController : MonoBehaviour
{
    [Header("References")]
    public vThirdPersonController controller;
    public Animator animator;

    [Header("Crouch Settings")]
    public KeyCode crouchKey = KeyCode.C;
    public float crossFadeDuration = 0.08f;

    [HideInInspector] public bool isCrouching;

    private string currentCrouchState = "";

    protected virtual void Start()
    {
        if (!controller)
            controller = GetComponent<vThirdPersonController>();

        if (!animator)
            animator = GetComponent<Animator>();
    }

    protected virtual void Update()
    {
        HandleInput();
        UpdateCrouchAnimation();
        DebugCurrentClip();
    }

    protected virtual void HandleInput()
    {
        if (Input.GetKeyDown(crouchKey))
            ToggleCrouch();
    }

    public virtual void ToggleCrouch()
    {
        if (!controller.isGrounded) return;

        isCrouching = !isCrouching;

        if (isCrouching && controller.isSprinting)
            controller.isSprinting = false;

        if (animator)
            animator.SetBool("IsCrouching", isCrouching);
    }

    protected virtual void UpdateCrouchAnimation()
    {
        if (!animator) return;

        string target;

        if (isCrouching)
            target = (controller.input.sqrMagnitude > 0.1f) ? "Crouched Walking(2)" : "Crouch IDLE";
        else
            target = "Free Locomotion";

        if (target != currentCrouchState)
        {
            animator.CrossFade(target, crossFadeDuration);
            currentCrouchState = target;
        }
    }

    protected virtual void DebugCurrentClip()
    {
        if (!animator) return;

        var clipInfo = animator.GetCurrentAnimatorClipInfo(0);
        string clipNames = "";
        foreach (var clip in clipInfo)
            clipNames += clip.clip.name + " ";

        Debug.Log($"[Crouch Debug] isCrouching: {isCrouching} | Estado alvo: {currentCrouchState} | Clip(s) a tocar: {clipNames}");
    }
}