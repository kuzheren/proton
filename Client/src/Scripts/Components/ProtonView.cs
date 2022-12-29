using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Proton;
using Proton.Packet.ID;

public class ProtonView : MonoBehaviour
{
    public Player Owner;
    public uint OwnerID;
    public bool IsMine;
    public uint ID;

    public float Priority = 1.0f;

    private void Awake()
    {
        this.Owner = ProtonEngine.GetPlayerByID(this.OwnerID);
    }

    public void Init(uint OwnerID, bool IsMine, uint ID)
    {
        this.OwnerID = OwnerID;
        this.IsMine = IsMine;
        this.ID = ID;
    }
}
