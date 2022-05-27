using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using Unity.Netcode;


class AnimationHandler : MonoBehaviour
{
    [SerializeField] public RuntimeAnimatorController[] characterAnimation = new RuntimeAnimatorController[5];
}
