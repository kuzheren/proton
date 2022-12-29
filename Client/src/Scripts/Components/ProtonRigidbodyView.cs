using System.Collections;
using System.Collections.Generic;
using Proton;
using Proton.Structures;
using UnityEngine;

public class ProtonRigidbodyView : MonoBehaviour
{
    private ProtonView protonView;
    private Rigidbody TargetRigidbody;

    private Vector3 oldPosition;
    private Quaternion oldRotation;
    
    private bool ForceSync = false;

    public Vector3 targetPosition;
    private Quaternion targetRotation;

    private bool trueKinematic = false;
    private bool desynced = false;
    private Vector3 pausedSpeed;
    private Vector3 pausedAngularSpeed;

    private void Start()
    {
        protonView = gameObject.GetComponent<ProtonView>();
        if (protonView == null)
        {
            return;
        }
        TargetRigidbody = gameObject.GetComponent<Rigidbody>();
        if (TargetRigidbody == null)
        {
            Debug.LogError("Объект с ProtonRigidbodyView обязан иметь на себе Rigidbody!");
            return;
        }

        if (protonView.IsMine == false)
        {
            TargetRigidbody.useGravity = false;
            return;
        }

        oldPosition = transform.position;
        oldRotation = transform.rotation;

        StartCoroutine(SendPosition());
        StartCoroutine(ForceSyncSend());
        StartCoroutine(CheckMoving());
    }
    private void Update()
    {
        if (protonView == null)
        {
            return;
        }

        if (protonView.IsMine == false)
        {
            TargetRigidbody.position = Vector3.Lerp(TargetRigidbody.position, targetPosition, Time.deltaTime * 2 );
            TargetRigidbody.rotation = Quaternion.Slerp(TargetRigidbody.rotation, targetRotation, Time.deltaTime * 3).normalized;
        }
    }
    public void NetworkSync(RigidbodyPacket rigidbodyData)
    {
        if (protonView.IsMine == true)
        {
            return;
        }

        TargetRigidbody.mass = rigidbodyData.Mass;
        TargetRigidbody.drag = rigidbodyData.Drag;
        TargetRigidbody.angularDrag = rigidbodyData.AngularDrag;
        TargetRigidbody.useGravity = false;
        TargetRigidbody.isKinematic = rigidbodyData.Kinematic;
        targetPosition = rigidbodyData.Position;
        TargetRigidbody.velocity = rigidbodyData.Velocity;
        TargetRigidbody.angularVelocity = rigidbodyData.AngularSpeed;
        targetRotation = rigidbodyData.Rotation;
    }
    private IEnumerator SendPosition()
    {
        while (true)
        {
            yield return new WaitForSeconds(1.0f / ProtonEngine.CurrentRoom.SendRate / protonView.Priority);

            if (Vector3.Distance(TargetRigidbody.position, ProtonEngine.LocalCameraPosition) > ProtonEngine.StreamZone)
            {
                if (desynced == false)
                {
                    FreezeThisObject();
                }
            }
            else
            {
                if (desynced == true)
                {
                    UnfreezeThisObject();
                }

                if (ForceSync == true | oldPosition != transform.position || oldRotation != transform.rotation)
                {
                    ForceSync = false;

                    oldPosition = transform.position;
                    oldRotation = transform.rotation;

                    RigidbodyPacket rigidbodyData = new RigidbodyPacket();
                    rigidbodyData.ID = protonView.ID;
                    rigidbodyData.Mass = (ushort) TargetRigidbody.mass;
                    rigidbodyData.Drag = TargetRigidbody.drag;
                    rigidbodyData.AngularDrag = TargetRigidbody.angularDrag;
                    rigidbodyData.Gravity = TargetRigidbody.useGravity;
                    rigidbodyData.Kinematic = TargetRigidbody.isKinematic;
                    rigidbodyData.Position = TargetRigidbody.position;
                    rigidbodyData.Velocity = TargetRigidbody.velocity;
                    rigidbodyData.AngularSpeed = TargetRigidbody.angularVelocity;
                    rigidbodyData.Rotation = TargetRigidbody.rotation;

                    ProtonEngine.SendRigidbodySync(rigidbodyData);
                }
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
    private void FreezeThisObject()
    {
        desynced = true;
        trueKinematic = TargetRigidbody.isKinematic;
        pausedSpeed = TargetRigidbody.velocity;
        pausedAngularSpeed = TargetRigidbody.angularVelocity;

        TargetRigidbody.isKinematic = true;
    }
    private void UnfreezeThisObject()
    {
        desynced = false;
        TargetRigidbody.isKinematic = trueKinematic;

        TargetRigidbody.velocity = pausedSpeed;
        TargetRigidbody.angularVelocity = pausedAngularSpeed;
    }
    public void NetworkDesync(bool desync)
    {
        if (protonView.IsMine == true)
        {
            return;
        }

        desynced = desync;
        TargetRigidbody.useGravity = desync;
    }
}
