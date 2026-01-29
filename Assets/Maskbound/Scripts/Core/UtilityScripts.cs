using UnityEngine;
using System.Collections;

namespace Maskbound.Utilities
{
    /// <summary>
    /// Singleton camera shake manager
    /// </summary>
    public class CameraShakeManager : MonoBehaviour
    {
        public static CameraShakeManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void ShakeCamera(float magnitude, float duration)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                StartCoroutine(ShakeCoroutine(mainCamera.transform, magnitude, duration));
            }
        }

        private IEnumerator ShakeCoroutine(Transform cameraTransform, float magnitude, float duration)
        {
            Vector3 originalPosition = cameraTransform.localPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float x = Random.Range(-1f, 1f) * magnitude;
                float y = Random.Range(-1f, 1f) * magnitude;

                cameraTransform.localPosition = originalPosition + new Vector3(x, y, 0f);

                elapsed += Time.deltaTime;
                yield return null;
            }

            cameraTransform.localPosition = originalPosition;
        }
    }

    /// <summary>
    /// Object pooling system for performance
    /// </summary>
    public class ObjectPool : MonoBehaviour
    {
        [System.Serializable]
        public class Pool
        {
            public string tag;
            public GameObject prefab;
            public int size;
        }

        public Pool[] pools;
        private System.Collections.Generic.Dictionary<string, System.Collections.Generic.Queue<GameObject>> poolDictionary;

        private void Awake()
        {
            poolDictionary = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.Queue<GameObject>>();

            foreach (Pool pool in pools)
            {
                System.Collections.Generic.Queue<GameObject> objectPool = new System.Collections.Generic.Queue<GameObject>();

                for (int i = 0; i < pool.size; i++)
                {
                    GameObject obj = Instantiate(pool.prefab);
                    obj.SetActive(false);
                    objectPool.Enqueue(obj);
                }

                poolDictionary.Add(pool.tag, objectPool);
            }
        }

        public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
        {
            if (!poolDictionary.ContainsKey(tag))
            {
                Debug.LogWarning($"Pool with tag {tag} doesn't exist.");
                return null;
            }

            GameObject objectToSpawn = poolDictionary[tag].Dequeue();
            objectToSpawn.SetActive(true);
            objectToSpawn.transform.position = position;
            objectToSpawn.transform.rotation = rotation;

            poolDictionary[tag].Enqueue(objectToSpawn);

            return objectToSpawn;
        }
    }

    /// <summary>
    /// Damage number popup for visual feedback
    /// </summary>
    public class DamageNumber : MonoBehaviour
    {
        [SerializeField] private float lifetime = 1f;
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private AnimationCurve scaleCurve;

        private TMPro.TextMeshPro textMesh;
        private float timer;

        private void Awake()
        {
            textMesh = GetComponent<TMPro.TextMeshPro>();
            if (textMesh == null)
            {
                textMesh = gameObject.AddComponent<TMPro.TextMeshPro>();
            }
        }

        public void Initialize(float damage, Color color)
        {
            textMesh.text = Mathf.RoundToInt(damage).ToString();
            textMesh.color = color;
            timer = 0f;
        }

        private void Update()
        {
            timer += Time.deltaTime;

            // Move upward
            transform.position += Vector3.up * moveSpeed * Time.deltaTime;

            // Scale animation
            if (scaleCurve != null)
            {
                float scale = scaleCurve.Evaluate(timer / lifetime);
                transform.localScale = Vector3.one * scale;
            }

            // Fade out
            Color color = textMesh.color;
            color.a = 1f - (timer / lifetime);
            textMesh.color = color;

            // Face camera
            if (Camera.main != null)
            {
                transform.LookAt(Camera.main.transform);
                transform.Rotate(0, 180, 0);
            }

            // Destroy when lifetime expires
            if (timer >= lifetime)
            {
                Destroy(gameObject);
            }
        }
    }

    /// <summary>
    /// Simple health component
    /// </summary>
    public class Health : MonoBehaviour, Maskbound.Core.IDamageable
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private bool destroyOnDeath = true;

        private float currentHealth;
        public System.Action<float> OnHealthChanged;
        public System.Action OnDeath;

        private void Start()
        {
            currentHealth = maxHealth;
        }

        public void TakeDamage(float damage)
        {
            currentHealth -= damage;
            currentHealth = Mathf.Max(0f, currentHealth);

            OnHealthChanged?.Invoke(currentHealth);

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        public void Heal(float amount)
        {
            currentHealth += amount;
            currentHealth = Mathf.Min(currentHealth, maxHealth);
            OnHealthChanged?.Invoke(currentHealth);
        }

        public void Die()
        {
            OnDeath?.Invoke();

            if (destroyOnDeath)
            {
                Destroy(gameObject);
            }
        }

        public float GetHealthPercentage() => currentHealth / maxHealth;
        public float GetCurrentHealth() => currentHealth;
        public float GetMaxHealth() => maxHealth;
    }

    /// <summary>
    /// Simple audio manager for sound effects
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Volumes")]
        [SerializeField] private float masterVolume = 1f;
        [SerializeField] private float musicVolume = 0.7f;
        [SerializeField] private float sfxVolume = 1f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void PlaySFX(AudioClip clip, float volume = 1f)
        {
            if (clip != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(clip, volume * sfxVolume * masterVolume);
            }
        }

        public void PlaySFXAtPoint(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (clip != null)
            {
                AudioSource.PlayClipAtPoint(clip, position, volume * sfxVolume * masterVolume);
            }
        }

        public void PlayMusic(AudioClip clip, bool loop = true)
        {
            if (musicSource != null && clip != null)
            {
                musicSource.clip = clip;
                musicSource.loop = loop;
                musicSource.volume = musicVolume * masterVolume;
                musicSource.Play();
            }
        }

        public void StopMusic()
        {
            if (musicSource != null)
            {
                musicSource.Stop();
            }
        }

        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
        }

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            if (musicSource != null)
            {
                musicSource.volume = musicVolume * masterVolume;
            }
        }

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
        }
    }

    /// <summary>
    /// FPS counter for debugging
    /// </summary>
    public class FPSCounter : MonoBehaviour
    {
        [SerializeField] private bool showFPS = true;
        [SerializeField] private Color textColor = Color.green;

        private float deltaTime = 0.0f;

        private void Update()
        {
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        }

        private void OnGUI()
        {
            if (!showFPS) return;

            int w = Screen.width, h = Screen.height;
            GUIStyle style = new GUIStyle();

            Rect rect = new Rect(10, 10, w, h * 2 / 100);
            style.alignment = TextAnchor.UpperLeft;
            style.fontSize = h * 2 / 50;
            style.normal.textColor = textColor;

            float msec = deltaTime * 1000.0f;
            float fps = 1.0f / deltaTime;
            string text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);

            GUI.Label(rect, text, style);
        }
    }

    /// <summary>
    /// Rotate object continuously (useful for pickups)
    /// </summary>
    public class RotateObject : MonoBehaviour
    {
        [SerializeField] private Vector3 rotationSpeed = new Vector3(0, 90, 0);
        [SerializeField] private bool randomizeStart = true;

        private void Start()
        {
            if (randomizeStart)
            {
                transform.Rotate(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f));
            }
        }

        private void Update()
        {
            transform.Rotate(rotationSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// Simple bobbing motion (useful for pickups or floating objects)
    /// </summary>
    public class BobUpDown : MonoBehaviour
    {
        [SerializeField] private float amplitude = 0.5f;
        [SerializeField] private float frequency = 1f;
        [SerializeField] private bool randomizeStart = true;

        private Vector3 startPosition;
        private float timeOffset;

        private void Start()
        {
            startPosition = transform.position;
            if (randomizeStart)
            {
                timeOffset = Random.Range(0f, 2f * Mathf.PI);
            }
        }

        private void Update()
        {
            float y = Mathf.Sin((Time.time + timeOffset) * frequency) * amplitude;
            transform.position = startPosition + new Vector3(0, y, 0);
        }
    }
}
