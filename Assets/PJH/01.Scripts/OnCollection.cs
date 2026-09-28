using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PJH._01.Scripts
{
    public class OnCollection : MonoBehaviour
    {
        [Header("도감 UI")]
        [SerializeField] private GameObject collectionUI;

        [Header("상호작용 표시")]
        [SerializeField] private GameObject interactionUI;

        [Header("플레이어 감지")]
        [SerializeField] private Vector2 boxSize;
        [SerializeField] private LayerMask layerMask;

        private void Update()
        {
            bool isNearPlayer = IsNearPlayer();
            bool isCollectionOpen = collectionUI != null && collectionUI.activeSelf;

            if (interactionUI != null)
                interactionUI.SetActive(isNearPlayer && !isCollectionOpen);

            if (isNearPlayer &&
                !isCollectionOpen &&
                Keyboard.current != null &&
                Keyboard.current.eKey.wasPressedThisFrame)
            {
                collectionUI.SetActive(true);

                if (interactionUI != null)
                    interactionUI.SetActive(false);
            }
        }

        private bool IsNearPlayer()
        {
            Collider2D player = Physics2D.OverlapBox(
                transform.position,
                boxSize,
                0f,
                layerMask
            );

            return player != null;
        }

        private void OnDisable()
        {
            if (interactionUI != null)
                interactionUI.SetActive(false);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, boxSize);
        }
    }
}
