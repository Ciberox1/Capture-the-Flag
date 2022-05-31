using UnityEngine;

class AnimationHandler : MonoBehaviour
{
    // array con las animaciones de todas los personajes
    [SerializeField] public RuntimeAnimatorController[] characterAnimation = new RuntimeAnimatorController[5];
}
