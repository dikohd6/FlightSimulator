using System.Collections;
using UnityEngine;

namespace AirportPack
{
    public class HangarGateControl : MonoBehaviour
    {
        [SerializeField] private AnimationClip openClip;
        [SerializeField] private GameObject[] gateRoots;

        [Header("Seconds to fully open")]
        [SerializeField] private float openDuration = 3f;

        private Coroutine routine;

        private void Awake()
        {
            // Auto-fill gateRoots if not assigned
            if (gateRoots == null || gateRoots.Length == 0)
            {
                // Use direct children as gates
                int count = transform.childCount;
                gateRoots = new GameObject[count];
                for (int i = 0; i < count; i++)
                    gateRoots[i] = transform.GetChild(i).gameObject;
            }

            // Disable all Animators under each gate to prevent any auto-play/looping
            foreach (var root in gateRoots)
            {
                if (!root) continue;
                foreach (var anim in root.GetComponentsInChildren<Animator>(true))
                    anim.enabled = false;
            }

            // Start CLOSED
            SetPose(0f);
        }

        // Call this from your UIController OnStartClicked()
        public void OpenGates()
        {
            if (openClip == null)
            {
                Debug.LogError("HangarGateControl: openClip is not assigned.");
                return;
            }

            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(OpenRoutine());
        }

        private IEnumerator OpenRoutine()
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(0.0001f, openDuration);
                SetPose(t);
                yield return null;
            }

            SetPose(1f);   // fully open
            routine = null;
        }

        private void SetPose(float normalized)
        {
            if (openClip == null || gateRoots == null) return;

            normalized = Mathf.Clamp01(normalized);
            float time = normalized * openClip.length;

            foreach (var root in gateRoots)
            {
                if (!root) continue;

                // Apply the animation pose at a specific time (NO PLAYBACK, NO LOOPING)
                openClip.SampleAnimation(root, time);
            }
        }
    }
}
