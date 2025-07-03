using UnityEngine;
using UnityEngine.InputSystem;

public class TransformManipulatorNew : MonoBehaviour
{
    [Header("Manipulation Settings")]
    public Transform targetObject;
    public float moveSpeed = 5f;
    public float rotateSpeed = 50f;
    public float scaleSpeed = 1f;

    private void Start()
    {
        if (targetObject == null)
        {
            targetObject = transform;
        }
    }

    private void Update()
    {
        if (targetObject == null) return;

        HandleMovement();
        HandleRotation();
        HandleScale();
    }

    private void HandleMovement()
    {
        Vector3 movement = Vector3.zero;

        if (Keyboard.current.wKey.isPressed) movement += Vector3.forward;
        if (Keyboard.current.sKey.isPressed) movement += Vector3.back;
        if (Keyboard.current.aKey.isPressed) movement += Vector3.left;
        if (Keyboard.current.dKey.isPressed) movement += Vector3.right;
        if (Keyboard.current.qKey.isPressed) movement += Vector3.up;
        if (Keyboard.current.eKey.isPressed) movement += Vector3.down;

        targetObject.position += movement * moveSpeed * Time.deltaTime;
    }

    private void HandleRotation()
    {
        Vector3 rotation = Vector3.zero;

        if (Keyboard.current.iKey.isPressed) rotation += Vector3.right;
        if (Keyboard.current.kKey.isPressed) rotation += Vector3.left;
        if (Keyboard.current.jKey.isPressed) rotation += Vector3.up;
        if (Keyboard.current.lKey.isPressed) rotation += Vector3.down;
        if (Keyboard.current.uKey.isPressed) rotation += Vector3.forward;
        if (Keyboard.current.oKey.isPressed) rotation += Vector3.back;

        targetObject.Rotate(rotation * rotateSpeed * Time.deltaTime);
    }

    private void HandleScale()
    {
        if (Keyboard.current.equalsKey.isPressed || Keyboard.current.numpadPlusKey.isPressed)
        {
            targetObject.localScale += Vector3.one * scaleSpeed * Time.deltaTime;
        }
        if (Keyboard.current.minusKey.isPressed || Keyboard.current.numpadMinusKey.isPressed)
        {
            targetObject.localScale -= Vector3.one * scaleSpeed * Time.deltaTime;
            targetObject.localScale = Vector3.Max(targetObject.localScale, Vector3.one * 0.1f);
        }
    }
}