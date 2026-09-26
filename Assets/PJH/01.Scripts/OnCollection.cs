using System;
using NUnit.Framework;
using PJH.Scripts;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = System.Object;

namespace PJH._01.Scripts
{
    public class OnCollection : MonoBehaviour
    {
        [SerializeField] private GameObject collectionUI;
        [SerializeField] private Vector2 boxSize;
        [SerializeField] private LayerMask layerMask;


        private void Update()
        {
            if (IsNearPlayer())
            {
                if (Keyboard.current.eKey.wasPressedThisFrame)
                {
                    collectionUI.SetActive(true);
                }
            }
            else
            {
                return;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, new Vector3(boxSize.x, boxSize.y, 0));
        }


        private bool IsNearPlayer()
        {
            var nearPlayer = Physics2D.OverlapBox(transform.position, boxSize, 0,layerMask);
            
            return nearPlayer != null;
        }
    }
}