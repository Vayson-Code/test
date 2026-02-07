using UnityEngine;
using System.Collections;
using Maskbound.Scripts.Skills.Interfaces;

namespace Maskbound.Scripts.Core
{
    // Manages player skill effects such as shield (immunity) and time freeze
    public class PlayerSkillsManager : MonoBehaviour, IPlayreShield, ITimeController
    {
        #region Fields
        private bool isImmune; // Tracks if the player is currently immune
        private Coroutine shieldCoroutine;
        private Coroutine timeCoroutine;
        private float originalFixedDeltaTime;
        [SerializeField] private Skills.Skills[] skillsArray; // Array to define which masks unlock which abilities
        #endregion

        public Skills.Skills[] GetSkillsArray() => skillsArray;
        #region Unity Methods
        private void Awake()
        {
            // Store the original fixed delta time for time scale resets
            originalFixedDeltaTime = Time.fixedDeltaTime;
        }

        private void OnDisable()
        {
            // Always reset time scale if this object is disabled
            ResetTimeScale();
        }
        #endregion

        #region IPlayerShield Implementation
        // Activates player immunity for a set duration
        public void ActivateShield(float duration)
        {
            if (shieldCoroutine != null)
                StopCoroutine(shieldCoroutine);
            shieldCoroutine = StartCoroutine(ShieldRoutine(duration));
        }

        // Coroutine to handle immunity timing
        private IEnumerator ShieldRoutine(float duration)
        {
            isImmune = true;
            // TODO: Add visual feedback for shield activation
            yield return new WaitForSeconds(duration);
            isImmune = false;
            // TODO: Add visual feedback for shield deactivation
        }

        // Returns whether the player is currently immune
        public bool IsImmune() => isImmune;
        #endregion

        #region ITimeController Implementation
        // Freezes time, then gradually restores it
        public void FreezeTime(float freezeDuration, float restoreDuration, float minTimeScale)
        {
            if (timeCoroutine != null)
                StopCoroutine(timeCoroutine);
            timeCoroutine = StartCoroutine(TimeFreezeRoutine(freezeDuration, restoreDuration, minTimeScale));
        }

        // Coroutine to handle time freeze and smooth restoration
        private IEnumerator TimeFreezeRoutine(float freezeDuration, float restoreDuration, float minTimeScale)
        {
            float originalTimeScale = Time.timeScale;
            float originalFixedDelta = originalFixedDeltaTime;
            Time.timeScale = minTimeScale;
            Time.fixedDeltaTime = originalFixedDelta * minTimeScale;
            try
            {
                yield return new WaitForSecondsRealtime(freezeDuration);
                float elapsed = 0f;
                while (elapsed < restoreDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / restoreDuration);
                    Time.timeScale = Mathf.Lerp(minTimeScale, originalTimeScale, t);
                    Time.fixedDeltaTime = Mathf.Lerp(originalFixedDelta * minTimeScale, originalFixedDelta, t);
                    yield return null;
                }
            }
            finally
            {
                // Ensure time is always reset
                Time.timeScale = originalTimeScale;
                Time.fixedDeltaTime = originalFixedDelta;
            }
        }

        // Resets time scale and fixed delta time to defaults
        private void ResetTimeScale()
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = originalFixedDeltaTime;
        }
        #endregion

        public bool HasCurrentMaskAbility(int maskIndex)
        {
            if (GameManager.Instance.CurrentMapIndex >= maskIndex) 
                return true;
            else
              return false;
        }
    }
}