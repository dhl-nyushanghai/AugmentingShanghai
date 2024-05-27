using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetNewParent : MonoBehaviour
{
    [SerializeField]
    private Transform targetParent;
    
    public void SetParent()
    {
        transform.SetParent(targetParent, true);
    }
}
