using System.Collections;
using System.Collections.Generic;
using Proton;
using Proton.Structures;
using UnityEngine;

public class ProtonTransformView : MonoBehaviour
{
    private ProtonView protonView;

    private Vector3 oldPosition;
    private Quaternion oldRotation;

    private bool ForceSync = false;

    [HideInInspector] public Vector3 targetPosition;
    private Quaternion targetRotation;

    private float currentTime;
    private float previousTime;
    private float interpolateCoefficient;

    private void Start()
    {
        protonView = gameObject.GetComponent<ProtonView>();
        if (protonView == null)
        {
            return;
        }

        oldPosition = transform.position;
        oldRotation = transform.rotation;
        
        if (protonView.IsMine == false)
        {
            targetPosition = transform.position;
            targetRotation = transform.rotation;
            return;
        }

        StartCoroutine(SendPosition());
        StartCoroutine(ForceSyncSend());
        StartCoroutine(CheckMoving());
    }
    public void Update()
    {
        if (protonView == null)
        {
            return;
        }

        if (protonView.IsMine == false)
        {
            interpolateCoefficient = (Time.time - previousTime) / (currentTime - previousTime);

            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * interpolateCoefficient);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 3);
        }
    }
    public void NetworkSync(TransformPacket transformData, float time)
    {
        if (protonView.IsMine == true)
        {
            return;
        }

        targetPosition = transformData.Position;
        targetRotation = transformData.Rotation;

        previousTime = currentTime;
        currentTime = time;
    }
    private IEnumerator SendPosition()
    {
        while (true)
        {
            yield return new WaitForSeconds(1.0f / ProtonEngine.CurrentRoom.SendRate / protonView.Priority);
            if (ForceSync == true | oldPosition != transform.position || oldRotation != transform.rotation)
            {
                ProtonEngine.SendTransformSync(protonView.ID, transform.position, transform.rotation);
                oldPosition = transform.position;
                oldRotation = transform.rotation;

                ForceSync = false;
            }
        }
    }
    private IEnumerator ForceSyncSend()
    {
        while (true)
        {
            yield return new WaitForSeconds(3);
            if (ProtonEngine.CurrentRoom.MineGameobjectPool.Count >= 50)
            {
                yield return new WaitForSeconds(4);
            }
            ForceSync = true;
        }
    }
    private IEnumerator CheckMoving()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.1f);

            if (ProtonEngine.IsConnected())
            {
                if (Vector3.Distance(oldPosition, transform.position) > 0.2f || oldRotation != transform.rotation)
                {
                    if (!ProtonEngine.CurrentRoom.MovingObjects.ContainsKey(protonView.ID))
                    {
                        ProtonEngine.CurrentRoom.MovingObjects[protonView.ID] = gameObject;
                    }
                }
                else
                {
                    if (ProtonEngine.CurrentRoom.MovingObjects.ContainsKey(protonView.ID))
                    {
                        ProtonEngine.CurrentRoom.MovingObjects.Remove(protonView.ID);
                    }
                }
            }
        }
    }
}
