using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ObjectPool
{
    // 각 오브젝트 종류에 대한 풀. key로 식별하고 큐에 비활성화된 오브젝트를 보관함.
    private static readonly Dictionary<string, Queue<GameObject>> pool = new();

    // key에 대응하는 프리팹 정보. Resources.Load()로 불러온 프리팹을 저장함.
    private static readonly Dictionary<string, GameObject> prefabMap = new();

    // Initialize()가 한 번만 실행되도록 막기 위한 플래그
    private static bool initialized = false;

    // 반환된 오브젝트를 매달아 둘 루트(씬 넘어가도 유지)
    private static Transform poolRoot;

    // 씬 변경 이벤트 중복 구독 방지용
    private static bool sceneHooked = false;

    // 오브젝트 풀에 등록할 항목
    private class PoolSetting
    {
        public string key;       // 키 이름 (프리팹 이름)
        public string path;      // Resources 내 프리팹 경로
        public int count;        // 초기 생성 수량

        public PoolSetting(string key, string path, int count)
        {
            this.key = key;
            this.path = path;
            this.count = count;
        }
    }


    // 등록할 오브젝트 풀 리스트. (필요 시 추가)
    private static readonly List<PoolSetting> settings = new()
        {
            new PoolSetting("Projectile", "Prefabs/Projectile", 20)
        };


    // 실제 오브젝트 풀을 초기화
    private static void Initialize()
    {
        // 이미 초기화된 경우 중복 실행 방지
        if (initialized) return;
        initialized = true;

        // DontDestroyOnLoad 루트 생성
        var rootObj = new GameObject("__ObjectPoolRoot");
        Object.DontDestroyOnLoad(rootObj);
        poolRoot = rootObj.transform;

        // 사전 정의된 오브젝트 풀 리스트(settings)에 따라 각 오브젝트 풀 생성
        foreach (var setting in settings)
        {
            // Resources 폴더에서 프리팹 로드
            GameObject prefab = Resources.Load<GameObject>(setting.path);

            // 프리팹이 없으면 해당 항목 건너뜀 (경고만 출력)
            if (prefab == null)
            {
                Debug.LogWarning($"[ObjectPool] Prefab not found: {setting.path}");
                continue;
            }

            // key에 해당하는 프리팹과 큐를 딕셔너리에 저장
            prefabMap[setting.key] = prefab;                  // 나중에 참조용
            pool[setting.key] = new Queue<GameObject>();      // 오브젝트 보관용 큐

            // 설정된 수량만큼 오브젝트를 미리 생성하여 비활성화 상태로 큐에 삽입
            for (int i = 0; i < setting.count; i++)
            {
                GameObject obj = Object.Instantiate(prefab, poolRoot);
                obj.SetActive(false);
                pool[setting.key].Enqueue(obj);
            }
        }

        // 씬 변경 시 죽은 참조 청소(중복 구독 방지)
        if (!sceneHooked)
        {
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            sceneHooked = true;
        }
    }

    private static void OnActiveSceneChanged(Scene _, Scene __)
    {
        PurgeDestroyed();
    }

    // key에 해당하는 오브젝트를 풀에서 꺼냄. 없으면 새로 생성.
    public static GameObject Get(string key)
    {
        // 초기화가 안 되어 있으면 자동으로 Initialize 실행 (최초 1회)
        if (!initialized) Initialize();

        // 해당 key에 대한 풀 자체가 없으면 생성
        if (!pool.ContainsKey(key))
            pool[key] = new Queue<GameObject>();

        // 큐에서 오브젝트 하나 꺼내되, 죽은 참조는 버리고 계속 탐색
        while (pool[key].Count > 0)
        {
            GameObject obj = pool[key].Dequeue();
            if (obj != null && !Equals(obj, null)) // Unity의 Destroy null 체크
            {
                obj.transform.SetParent(null, false); // 사용 중은 풀 루트에서 분리(선택)
                obj.SetActive(true);
                return obj;
            }
            // 죽은 참조면 버리고 계속
        }

        // 여기까지 왔으면 큐가 비었거나 전부 죽음 → 새로 생성
        if (!prefabMap.TryGetValue(key, out GameObject prefab) || prefab == null)
        {
            Debug.LogError($"[ObjectPool] No prefab registered for key: {key}");
            return null;
        }

        GameObject created = Object.Instantiate(prefab);
        created.SetActive(true);
        return created;
    }

    // key에 해당하는 오브젝트를 다시 풀에 반환
    public static void Return(string key, GameObject obj)
    {
        // 초기화가 안 되어 있으면 자동으로 Initialize 실행
        if (!initialized) Initialize();
        if (obj == null || Equals(obj, null)) return;

        // 오브젝트를 비활성화 + 풀 루트 아래로 귀속
        obj.SetActive(false);
        obj.transform.SetParent(poolRoot, false);

        // key에 해당하는 풀(queue)이 없으면 새로 생성
        if (!pool.ContainsKey(key))
            pool[key] = new Queue<GameObject>();

        // 비활성화된 오브젝트를 다시 큐에 넣음
        pool[key].Enqueue(obj);
    }

    /// <summary>
    /// 모든 풀 비우기(메뉴로 갈 때 등). 큐 안의 오브젝트도 Destroy.
    /// </summary>
    public static void ClearAll()
    {
        if (!initialized) return;

        // 씬 변경 이벤트 구독 해제(중복 구독 방지)
        if (sceneHooked)
        {
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            sceneHooked = false;
        }

        foreach (var kv in pool)
        {
            while (kv.Value.Count > 0)
            {
                var obj = kv.Value.Dequeue();
                if (obj != null && !Equals(obj, null))
                    Object.Destroy(obj);
            }
        }
        pool.Clear();
        prefabMap.Clear();

        // 루트도 정리
        if (poolRoot != null && !Equals(poolRoot, null))
            Object.Destroy(poolRoot.gameObject);
        poolRoot = null;

        initialized = false; // 다음 호출 시 재초기화
    }

    /// <summary>
    /// 큐 안에 들어있는 “죽은” 오브젝트들만 정리
    /// </summary>
    public static void PurgeDestroyed()
    {
        if (!initialized) return;

        foreach (var kv in pool)
        {
            var q = kv.Value;
            int n = q.Count;
            for (int i = 0; i < n; i++)
            {
                var obj = q.Dequeue();
                if (obj != null && !Equals(obj, null))
                    q.Enqueue(obj); // 살아있으면 다시 보관
                                    // 죽었으면 버림
            }
        }
    }
}
