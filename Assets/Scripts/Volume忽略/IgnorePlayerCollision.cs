using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

[RequireComponent(typeof(Collider))]
public class IgnorePlayerCollision : MonoBehaviour
{
    void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        Debug.Log(player);
        if (player != null)
        {
            var characterController = player.GetComponent<CharacterController>();
            if (characterController != null)
            {
                Physics.IgnoreCollision(characterController, GetComponent<Collider>());
            }
        }
    }
}