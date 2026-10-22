using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Arlycad
{
    /// <summary>
    /// Создаёт копии префаба. Сам префаб лежит в Project, не в сцене.
    /// Сколько, как часто и когда удалять — поля этого компонента.
    /// UnityEvent только говорит «создай»: Spawn или Start Repeating.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Arlycad/Object Spawner", 3)]
    public class ObjectSpawner : MonoBehaviour
    {
        [Header("Что создавать")]
        [InspectorName("Префаб")]
        [Tooltip("Префаб из окна Project. Объект со сцены не перетаскивайте: сделайте из него префаб.")]
        public GameObject prefab;

        [Header("Где")]
        [InspectorName("Точка появления")]
        [Tooltip("Пустой объект-точка. Если пусто, объект появляется там, где стоит спавнер.")]
        public Transform spawnPoint;

        [InspectorName("Случайный сдвиг")]
        [Tooltip("Случайный сдвиг вокруг точки, в её локальных осях. 0 — ровно в точке. Знак не важен: 1 и -1 дают один и тот же разброс.")]
        public Vector3 randomOffset;

        [InspectorName("Повернуть как точку")]
        [Tooltip("Повернуть новый объект так же, как повёрнута точка.")]
        public bool useSpawnPointRotation = true;

        [InspectorName("Случайный поворот")]
        [Tooltip("Случайный поворот вокруг вертикали. Для толпы и мусора.")]
        public bool randomYaw;

        [InspectorName("Прикрепить к точке")]
        [Tooltip("Сделать объект дочерним к точке появления. Он будет ездить вместе с точкой. Для ловушек обычно выключено.")]
        public bool attachToSpawnPoint;

        [Header("Сколько")]
        [InspectorName("Создать при старте")]
        [Tooltip("Сколько создать в момент запуска сцены, ещё до повтора.")]
        [Min(0)]
        public int spawnOnStart;

        [InspectorName("Повторять")]
        [Tooltip("Создавать дальше само, с паузой ниже.")]
        public bool repeat;

        [InspectorName("Пауза, секунды")]
        [Tooltip("Пауза между созданиями. Её же использует метод StartRepeating.")]
        [Min(0.05f)]
        public float interval = 2f;

        [InspectorName("Лимит живых")]
        [Tooltip("Не создавать новый, пока живых копий уже столько.")]
        public bool limitAlive;

        [InspectorName("Максимум живых")]
        [Min(1)]
        public int maxAlive = 3;

        [InspectorName("Общий лимит")]
        [Tooltip("Остановиться, когда за этот запуск сцены создано столько объектов, включая уже удалённые.")]
        public bool limitTotal;

        [InspectorName("Всего за сцену")]
        [Min(1)]
        public int totalCount = 10;

        [InspectorName("Удалять через время")]
        [Tooltip("Удалять каждую копию через указанное число секунд.")]
        public bool destroyAfterDelay;

        [InspectorName("Время жизни, секунды")]
        [Min(0.05f)]
        public float lifetime = 5f;

        [Header("События")]
        [InspectorName("После создания")]
        [Tooltip("Каждый раз после создания. Аргумента нет: если копии нужен свой эффект, повесьте его на префаб.")]
        public UnityEvent onSpawned = new UnityEvent();

        [InspectorName("Лимит достигнут")]
        [Tooltip("Один раз, когда достигнут общий лимит.")]
        public UnityEvent onReachedLimit = new UnityEvent();

        [InspectorName("Писать в консоль")]
        [Tooltip("Писать в Console, когда объект создан или спавнер отказал.")]
        public bool debugLog;

        readonly List<GameObject> alive = new List<GameObject>();

        int spawnedTotal;
        float timer;
        bool running;
        bool announcedLimit;
        bool warnedMissingPrefab;

        void Start()
        {
            for (int i = 0; i < spawnOnStart; i++)
                Spawn();

            timer = interval;
            running = repeat;
        }

        void Update()
        {
            if (!running)
                return;

            timer -= Time.deltaTime;
            if (timer > 0f)
                return;

            timer = Mathf.Max(0.05f, interval);
            Spawn();
        }

        public void Spawn()
        {
            if (!Application.isPlaying)
                return;

            if (prefab == null)
            {
                WarnMissingPrefab();
                return;
            }

            Prune();

            if (limitTotal && spawnedTotal >= totalCount)
            {
                AnnounceLimit();
                return;
            }

            if (limitAlive && alive.Count >= Mathf.Max(1, maxAlive))
            {
                if (debugLog)
                    Debug.Log("Спавнер «" + name + "»: живых уже " + alive.Count + ", новый не создан.", this);
                return;
            }

            Transform point = spawnPoint != null ? spawnPoint : transform;
            Vector3 position = point.position + point.TransformVector(RandomShift());
            Quaternion rotation = useSpawnPointRotation ? point.rotation : Quaternion.identity;
            if (randomYaw)
            {
                Vector3 euler = rotation.eulerAngles;
                euler.y = Random.Range(0f, 360f);
                rotation = Quaternion.Euler(euler);
            }

            GameObject instance = Instantiate(prefab, position, rotation);
            if (attachToSpawnPoint)
                instance.transform.SetParent(point, true);

            if (destroyAfterDelay)
                Destroy(instance, Mathf.Max(0.05f, lifetime));

            alive.Add(instance);
            spawnedTotal++;

            if (debugLog)
                Debug.Log("Спавнер «" + name + "»: создан " + instance.name + " (" + spawnedTotal + ").", this);

            onSpawned.Invoke();

            if (limitTotal && spawnedTotal >= totalCount)
                AnnounceLimit();
        }

        /// <summary>
        /// Создаёт один объект сразу и дальше повторяет с паузой Interval.
        /// </summary>
        public void StartRepeating()
        {
            running = true;
            timer = Mathf.Max(0.05f, interval);
            Spawn();
        }

        public void StopRepeating()
        {
            running = false;
        }

        public void ClearSpawned()
        {
            for (int i = alive.Count - 1; i >= 0; i--)
            {
                if (alive[i] != null)
                    Destroy(alive[i]);
            }

            alive.Clear();
        }

        /// <summary>
        /// Удаляет копии и обнуляет счётчик общего лимита.
        /// </summary>
        public void ResetSpawner()
        {
            ClearSpawned();
            spawnedTotal = 0;
            announcedLimit = false;
            running = repeat;
            timer = Mathf.Max(0.05f, interval);
        }

        void AnnounceLimit()
        {
            running = false;
            if (announcedLimit)
                return;

            announcedLimit = true;
            if (debugLog)
                Debug.Log("Спавнер «" + name + "»: достигнут общий лимит " + totalCount + ".", this);

            onReachedLimit.Invoke();
        }

        void Prune()
        {
            for (int i = alive.Count - 1; i >= 0; i--)
            {
                if (alive[i] == null)
                    alive.RemoveAt(i);
            }
        }

        Vector3 RandomShift()
        {
            return new Vector3(
                Random.Range(-Mathf.Abs(randomOffset.x), Mathf.Abs(randomOffset.x)),
                Random.Range(-Mathf.Abs(randomOffset.y), Mathf.Abs(randomOffset.y)),
                Random.Range(-Mathf.Abs(randomOffset.z), Mathf.Abs(randomOffset.z)));
        }

        void WarnMissingPrefab()
        {
            if (warnedMissingPrefab)
                return;

            warnedMissingPrefab = true;
            Debug.LogWarning("Спавнер «" + name + "»: не задан префаб.", this);
        }

        void OnDrawGizmos()
        {
            Transform point = spawnPoint != null ? spawnPoint : transform;
            Gizmos.color = new Color(0.95f, 0.8f, 0.2f, 1f);
            Gizmos.DrawWireSphere(point.position, 0.3f);
            ColliderGizmos.DrawAxes(point, 0.6f);
        }
    }
}
