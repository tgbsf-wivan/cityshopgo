using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.iOS;

public class PetControl : MonoBehaviour
{
	public GameObject petPrefab;
	public float maxRayDistance = 30.0f;
	public LayerMask collisionLayer = 1 << 10;  //ARKitPlane layer

    GameObject pet;
    Transform petTr;
    Animation anim;

    Vector3 petDest;

    bool buttonPressed;

    enum AnimState
    {
        Idle,
        Walk,
        Dance,
        Talk,
        Fail,
        Sleep,
        Roll
    }

    AnimState currentAnimation = AnimState.Idle;

    float MoveToThreshold = .5f; // meters
    float RotToThreshold = 1f; // degrees
    float MoveSpeed = 1f; // meters
    float RotSpeed = 180; // degrees

    void Start()
    {

    }

    public void OnTalkButton()
    {
        currentAnimation = AnimState.Talk;
        buttonPressed = true;
    }

    public void OnFailButton()
    {
        currentAnimation = AnimState.Fail;
        anim.CrossFade("Failure Giraffe");
        buttonPressed = true;
    }

    public void OnSleepButton()
    {
        currentAnimation = AnimState.Sleep;
        buttonPressed = true;
    }

    public void OnSpawnButton()
    {
        currentAnimation = AnimState.Roll;
        buttonPressed = true;
    }

    // Update is called once per frame
    void Update()
    {
        SpawnPet();
        ProcessInput();
        PetBehaviour();
        PetAnim();
    }

    void LateUpdate()
    {
        buttonPressed = false;
    }

    void PetAnim()
    {
        if (!anim) return;

        switch (currentAnimation)
        {
            case AnimState.Idle:
                anim.CrossFade("Idle Giraffe");
                break;
            case AnimState.Walk:
                anim.CrossFade("Walk Giraffe");
                break;
            case AnimState.Dance:
                anim.CrossFade("Success Giraffe");
                break;
            case AnimState.Talk:
                anim.CrossFade("Talk Giraffe");
                break;
            case AnimState.Fail:
                break;
            case AnimState.Sleep:
                anim.CrossFade("Sleep Giraffe");
                break;
            case AnimState.Roll:
                anim.CrossFade("Rolling Giraffe");
                break;
        }

        pet.GetComponent<Giraffe>().bubble.SetActive(currentAnimation == AnimState.Talk);
    }

    void PetBehaviour()
    {
        if (pet == null) return;

        if (currentAnimation == AnimState.Idle || currentAnimation == AnimState.Walk)
        {
            // If we are far from dest, we move
            if (Vector3.Distance(petTr.position, petDest) > float.Epsilon)
            {

                var petRotDest = Quaternion.LookRotation(petDest - petTr.position, Vector3.up);

                // But first, should we orient to dest ?
                if (Quaternion.Angle(petTr.rotation, petRotDest) > RotToThreshold)
                {
                    petTr.rotation = Quaternion.RotateTowards(petTr.rotation, petRotDest, RotSpeed * Time.deltaTime);
                }
                else // No ? Ok we move
                {
                    petTr.position = Vector3.MoveTowards(petTr.position, petDest, MoveSpeed * Time.deltaTime);
                }
                currentAnimation = AnimState.Walk;
            }
            else
            {
                currentAnimation = AnimState.Idle;
            }
        }
    }

    void SpawnPet()
    {
        if (pet == null)
        {
            var screenPosition = Camera.main.ScreenToViewportPoint(new Vector2(Screen.width / 2, Screen.height / 2));
            ARPoint point = new ARPoint
            {
                x = screenPosition.x,
                y = screenPosition.y
            };

            List<ARHitTestResult> hitResults =
                UnityARSessionNativeInterface.GetARSessionNativeInterface().HitTest(
                    point, ARHitTestResultType.ARHitTestResultTypeEstimatedHorizontalPlane);
            if (hitResults.Count > 0)
            {
                foreach (var hitResult in hitResults)
                {
                    Vector3 position = UnityARMatrixOps.GetPosition(hitResult.worldTransform);
                    pet = Instantiate(petPrefab);
                    petTr = pet.transform;
                    petTr.position = position;
                    anim = pet.GetComponent<Animation>();

                    Vector3 lookAt = Camera.main.transform.position;
                    lookAt.y = position.y;
                    petTr.LookAt(lookAt);
                    petDest = position;
                    break;
                }
            }
        }
    }

    void PetGo(Vector3 atPosition)
	{
        if (pet)
        {
            if (Vector3.Distance(petTr.position, atPosition) > MoveToThreshold)
            {
                atPosition.y = petDest.y; // We keep the same height even if they are new planes
                petDest = atPosition;
            }
            currentAnimation = AnimState.Idle;
        }
    }

    void ProcessInput()
    {
        if (Input.touchCount > 0 && buttonPressed == false)
        {
            var touch = Input.GetTouch(0);

            if (touch.tapCount == 2)
            {
                currentAnimation = AnimState.Dance;
                return;
            }

            if (touch.phase == TouchPhase.Began)
            {
                var screenPosition = Camera.main.ScreenToViewportPoint(touch.position);
                ARPoint point = new ARPoint
                {
                    x = screenPosition.x,
                    y = screenPosition.y
                };

                List<ARHitTestResult> hitResults =
                    UnityARSessionNativeInterface.GetARSessionNativeInterface().HitTest(
                        point, ARHitTestResultType.ARHitTestResultTypeEstimatedHorizontalPlane);
                if (hitResults.Count > 0)
                {
                    foreach (var hitResult in hitResults)
                    {
                        Vector3 position = UnityARMatrixOps.GetPosition(hitResult.worldTransform);
                        PetGo(position);
                        break;
                    }
                }
            }
        }

    }

}
