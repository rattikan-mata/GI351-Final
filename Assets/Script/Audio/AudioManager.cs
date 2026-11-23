using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GI351.KitchenGame
{
    /// <summary>
    /// รหัสเสียงทั้งหมดในเกม (เพิ่มเสียงใหม่ได้ที่นี่ แล้วไปใส่คลิปที่ AudioManager ใน Inspector)
    /// </summary>
    public enum SoundId
    {
        Footstep,       // เสียงเดิน
        Attack,         // เสียงตี/ฟาดอาวุธ
        PotDrop,        // เสียงโยนของเข้าหม้อ/เครื่อง
        PotCooking,     // เสียงหม้อกำลังต้ม/ทำอาหาร (เล่นวนลูปจนกว่าจะต้มเสร็จ)
        FoodDone,       // เสียงอาหารเสร็จ
        MoneyReceive,   // เสียงรับเงิน (เก็บเหรียญ)
        CustomerPay,    // เสียงลูกค้าจ่ายเงินที่แคชเชียร์
        ChickenCluck,   // เสียงไก่
        PigOink,        // เสียงหมู
        BuffaloMoo,     // เสียงควาย
        WeaponHit,      // เสียงตีโดนเป้าหมาย
        ChickenHurt,    // ไก่ร้องตอนโดนตี
        PigHurt,        // หมูร้องตอนโดนตี
        BuffaloHurt,    // ควายร้องตอนโดนตี
        ItemPickup,     // เสียงเก็บของขึ้นหลัง
        PlayerHurt,     // เสียงผู้เล่นโดนตี
        PlayerDie,      // เสียงผู้เล่นตาย
        FoodPlace = 17,      // เสียงวางอาหารลงโต๊ะขาย
        ThiefAlert = 20,     // เสียงเตือนโจรฉกของ (!)
        ThiefKnockout = 21,  // เสียงโจรโดนตีสลบ
        CustomerAngry = 22,  // เสียงลูกค้าโมโหเดินหนี
        EnemyDie = 23        // เสียงสัตว์ตาย (ใช้รวมทุกชนิด)
    }

    /// <summary>โหมดระยะเสียงของแต่ละเสียง</summary>
    public enum SpatialMode
    {
        [Tooltip("ใช้ค่าเริ่มต้นตามชนิดเสียง (เสียงสัตว์/เครื่องทำอาหารมีระยะ, เสียงผู้เล่นดังเต็ม)")]
        Auto,
        [Tooltip("ดังเต็มเสียงเสมอ ไม่สนใจระยะ")]
        Global,
        [Tooltip("ยิ่งไกลจากกล้องยิ่งเบา เกินระยะสูงสุดจะเงียบ")]
        Positional
    }

    [Serializable]
    public class SoundEntry
    {
        public SoundId id;

        [Tooltip("คลิปเสียง (ใส่หลายคลิปได้ จะสุ่มเล่นทีละคลิป)")]
        public AudioClip[] clips;

        [Range(0f, 1f)] public float volume = 1f;

        [Tooltip("สุ่มระดับเสียงสูงต่ำ (min, max) ให้เสียงไม่ซ้ำซาก")]
        public Vector2 pitchRange = new Vector2(0.95f, 1.05f);

        [Tooltip("ช่วงเวลาขั้นต่ำระหว่างการเล่นเสียงนี้ซ้ำ (วินาที) กันเสียงรัวเกินไป")]
        public float minInterval = 0f;

        [Header("ระยะเสียง")]
        [Tooltip("Auto = ตามค่าเริ่มต้นของชนิดเสียง, Global = ดังเต็มเสมอ, Positional = ยิ่งไกลยิ่งเบา")]
        public SpatialMode spatial = SpatialMode.Auto;

        [Tooltip("ระยะที่ยังได้ยินเต็มเสียง (เมตร จากกล้อง)")]
        public float minDistance = 3f;

        [Tooltip("ระยะสูงสุดที่ได้ยิน (เมตร) เกินจากนี้จะเงียบ")]
        public float maxDistance = 12f;

        [NonSerialized] public float lastPlayTime = -999f;
    }

    /// <summary>
    /// ศูนย์รวมเสียงของเกม: วางไว้ในซีนตัวเดียว (GameObject ว่างชื่อ AudioManager) แล้วใส่คลิปเสียงที่นี่ที่เดียว
    /// สคริปต์อื่นเรียกใช้ด้วย AudioManager.Play(SoundId.xxx)
    /// ถ้าไม่ได้ใส่คลิปที่นี่ จะใช้คลิปสำรองที่ใส่ไว้ในสคริปต์เดิม (ถ้ามี) แทน
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Volume")]
        [Range(0f, 1f)] [SerializeField] private float masterVolume = 1f;
        [SerializeField] private bool mute = false;

        [Header("Sound List (ใส่เสียงทั้งหมดที่นี่)")]
        [SerializeField] private List<SoundEntry> sounds = new List<SoundEntry>();

        [Header("Pool")]
        [Tooltip("จำนวน AudioSource ที่ใช้เล่นเสียงพร้อมกันได้")]
        [SerializeField] private int poolSize = 12;

        private readonly Dictionary<SoundId, SoundEntry> lookup = new Dictionary<SoundId, SoundEntry>();
        private AudioSource[] pool;
        private int nextSource;

        private class LoopSlot
        {
            public SoundId id;
            public AudioSource source;
            public Vector3 position;
        }

        private readonly Dictionary<object, LoopSlot> activeLoops = new Dictionary<object, LoopSlot>();
        private readonly Stack<AudioSource> freeLoopSources = new Stack<AudioSource>();
        private readonly List<object> deadLoopOwners = new List<object>();

        public float MasterVolume
        {
            get => masterVolume;
            set => masterVolume = Mathf.Clamp01(value);
        }

        public bool Mute
        {
            get => mute;
            set => mute = value;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (transform.parent == null) DontDestroyOnLoad(gameObject);

            BuildLookup();
            BuildPool();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (activeLoops.Count == 0) return;

            deadLoopOwners.Clear();
            Vector2 listenerPos = ListenerPosition();

            foreach (var kv in activeLoops)
            {
                if (kv.Key == null || (kv.Key is UnityEngine.Object uo && uo == null))
                {
                    deadLoopOwners.Add(kv.Key);
                    continue;
                }

                LoopSlot slot = kv.Value;
                slot.source.volume = ComputeVolume(slot.id, slot.position, true, listenerPos);
            }

            for (int i = 0; i < deadLoopOwners.Count; i++)
            {
                StopLoopInternal(deadLoopOwners[i]);
            }
        }

        /// <summary>ให้รายการใน Inspector มีครบทุก SoundId เสมอ</summary>
        private void OnValidate()
        {
            if (sounds == null) sounds = new List<SoundEntry>();
            sounds.RemoveAll(s => s == null || !Enum.IsDefined(typeof(SoundId), s.id));

            foreach (SoundId id in Enum.GetValues(typeof(SoundId)))
            {
                bool found = false;
                foreach (var s in sounds)
                {
                    if (s != null && s.id == id) { found = true; break; }
                }
                if (!found) sounds.Add(new SoundEntry { id = id });
            }

            poolSize = Mathf.Max(1, poolSize);
        }

        private void BuildLookup()
        {
            lookup.Clear();
            foreach (var s in sounds)
            {
                if (s != null && !lookup.ContainsKey(s.id)) lookup.Add(s.id, s);
            }
        }

        private void BuildPool()
        {
            pool = new AudioSource[Mathf.Max(1, poolSize)];
            for (int i = 0; i < pool.Length; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f; // 2D เกมท็อปดาวน์ ไม่ต้องคำนวณระยะ
                pool[i] = src;
            }
        }

        // ------------------------------------------------------------------ static API

        /// <summary>เล่นเสียงตามรหัส (ใช้คลิปที่ตั้งไว้ใน AudioManager)</summary>
        public static void Play(SoundId id)
        {
            if (Instance != null) Instance.PlayInternal(id, null, Vector3.zero, false);
        }

        /// <summary>
        /// เล่นเสียงตามรหัส ณ ตำแหน่งในโลก (เสียงที่ตั้งเป็นแบบมีระยะจะเบาลงตามระยะจากกล้อง)
        /// ถ้า AudioManager ไม่มีคลิปให้ จะใช้ fallback (คลิปเดิมในสคริปต์นั้น) แทน
        /// </summary>
        public static void Play(SoundId id, AudioClip fallback, Vector3 position)
        {
            if (Instance != null)
            {
                Instance.PlayInternal(id, fallback, position, true);
            }
            else if (fallback != null)
            {
                AudioSource.PlayClipAtPoint(fallback, position);
            }
        }

        /// <summary>
        /// เล่นเสียงรัวๆ ทีละชิ้นแบบเสียงเก็บของ Minecraft (ใช้ตอนโยนเนื้อหลายชิ้นเข้าหม้อ หรือวางอาหารหลายจานลงโต๊ะในเฟรมเดียว)
        /// </summary>
        public static void PlayBurst(SoundId id, int count, float interval = 0.06f, AudioClip fallback = null, Vector3 position = default, float pitchStep = 0.04f)
        {
            if (count <= 0) return;
            if (count == 1 || Instance == null)
            {
                Play(id, fallback, position);
                return;
            }
            Instance.StartCoroutine(Instance.PlayBurstRoutine(id, Mathf.Min(count, 12), Mathf.Max(0.02f, interval), fallback, position, pitchStep));
        }

        private IEnumerator PlayBurstRoutine(SoundId id, int count, float interval, AudioClip fallback, Vector3 position, float pitchStep)
        {
            for (int i = 0; i < count; i++)
            {
                float pitchOffset = Mathf.Min(i * pitchStep, 0.36f);
                PlayInternal(id, fallback, position, true, pitchOffset, ignoreInterval: true);
                if (i + 1 < count)
                {
                    yield return new WaitForSeconds(interval);
                }
            }
        }

        /// <summary>
        /// เปิด/ปิดเสียงวนลูปสำหรับออบเจกต์หนึ่งๆ (เช่น หม้อกำลังต้มอาหาร)
        /// ความดังจะปรับตามระยะจากกล้องอัตโนมัติทุกเฟรม
        /// </summary>
        public static void SetLoop(object owner, SoundId id, bool active, Vector3 position)
        {
            if (Instance == null || owner == null) return;
            if (active)
            {
                Instance.StartOrUpdateLoop(owner, id, position);
            }
            else
            {
                Instance.StopLoopInternal(owner);
            }
        }

        /// <summary>หยุดเสียงวนลูปของออบเจกต์นั้นทันที</summary>
        public static void StopLoop(object owner)
        {
            if (Instance == null || owner == null) return;
            Instance.StopLoopInternal(owner);
        }

        // ------------------------------------------------------------------ internal

        /// <summary>ค่าเริ่มต้นเมื่อเลือก Auto: เสียงที่เกิดจากสิ่งรอบตัวมีระยะ, เสียงของผู้เล่นดังเต็ม</summary>
        private static bool DefaultPositional(SoundId id)
        {
            switch (id)
            {
                case SoundId.PotDrop:
                case SoundId.PotCooking:
                case SoundId.FoodDone:
                case SoundId.FoodPlace:
                case SoundId.CustomerPay:
                case SoundId.ChickenCluck:
                case SoundId.PigOink:
                case SoundId.BuffaloMoo:
                case SoundId.ChickenHurt:
                case SoundId.PigHurt:
                case SoundId.BuffaloHurt:
                case SoundId.EnemyDie:
                case SoundId.ThiefKnockout:
                    return true;
                default:
                    return false;
            }
        }

        private float ComputeVolume(SoundId id, Vector3 position, bool hasPosition, Vector2 listenerPos)
        {
            if (mute) return 0f;

            float volume = 1f;
            bool positional = hasPosition && DefaultPositional(id);
            float minDist = 3f;
            float maxDist = 12f;

            if (lookup.TryGetValue(id, out var entry))
            {
                volume = entry.volume;
                minDist = entry.minDistance;
                maxDist = entry.maxDistance;

                if (entry.spatial == SpatialMode.Global) positional = false;
                else if (entry.spatial == SpatialMode.Positional) positional = hasPosition;
            }

            if (positional)
            {
                float dist = Vector2.Distance(listenerPos, position);
                if (dist >= maxDist) return 0f;
                if (dist > minDist)
                {
                    volume *= 1f - Mathf.InverseLerp(minDist, maxDist, dist);
                }
            }

            return volume * masterVolume;
        }

        private void StartOrUpdateLoop(object owner, SoundId id, Vector3 position)
        {
            if (activeLoops.TryGetValue(owner, out var existing))
            {
                existing.position = position;
                if (existing.id == id) return;
                StopLoopInternal(owner);
            }

            AudioClip clip = null;
            Vector2 pitch = new Vector2(0.95f, 1.05f);
            if (lookup.TryGetValue(id, out var entry))
            {
                if (entry.clips != null && entry.clips.Length > 0)
                {
                    clip = entry.clips[UnityEngine.Random.Range(0, entry.clips.Length)];
                }
                pitch = entry.pitchRange;
            }

            if (clip == null) return;

            AudioSource src = freeLoopSources.Count > 0 ? freeLoopSources.Pop() : CreateLoopSource();
            src.clip = clip;
            src.loop = true;
            src.pitch = UnityEngine.Random.Range(Mathf.Min(pitch.x, pitch.y), Mathf.Max(pitch.x, pitch.y));
            src.volume = ComputeVolume(id, position, true, ListenerPosition());
            src.Play();

            activeLoops[owner] = new LoopSlot { id = id, source = src, position = position };
        }

        private void StopLoopInternal(object owner)
        {
            if (!activeLoops.TryGetValue(owner, out var slot)) return;
            activeLoops.Remove(owner);

            if (slot.source != null)
            {
                slot.source.Stop();
                slot.source.clip = null;
                freeLoopSources.Push(slot.source);
            }
        }

        private AudioSource CreateLoopSource()
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = true;
            src.spatialBlend = 0f;
            return src;
        }

        private static Vector2 ListenerPosition()
        {
            Camera cam = Camera.main;
            if (cam != null) return cam.transform.position;

            var listener = FindAnyObjectByType<AudioListener>();
            return listener != null ? (Vector2)listener.transform.position : Vector2.zero;
        }

        private void PlayInternal(SoundId id, AudioClip fallback, Vector3 position, bool hasPosition, float pitchOffset = 0f, bool ignoreInterval = false)
        {
            if (mute) return;

            AudioClip clip = null;
            float volume = 1f;
            Vector2 pitch = new Vector2(0.95f, 1.05f);
            bool positional = hasPosition && DefaultPositional(id);
            float minDist = 3f;
            float maxDist = 12f;

            if (lookup.TryGetValue(id, out var entry))
            {
                if (!ignoreInterval && entry.minInterval > 0f && Time.unscaledTime - entry.lastPlayTime < entry.minInterval) return;

                if (entry.clips != null && entry.clips.Length > 0)
                {
                    clip = entry.clips[UnityEngine.Random.Range(0, entry.clips.Length)];
                }
                volume = entry.volume;
                pitch = entry.pitchRange;
                minDist = entry.minDistance;
                maxDist = entry.maxDistance;

                if (entry.spatial == SpatialMode.Global) positional = false;
                else if (entry.spatial == SpatialMode.Positional) positional = hasPosition;
            }

            // คำนวณความดังตามระยะจากกล้อง (เต็มเสียงในระยะ minDist แล้วค่อยๆ เบาลงจนเงียบที่ maxDist)
            if (positional)
            {
                float dist = Vector2.Distance(ListenerPosition(), position);
                if (dist >= maxDist) return;
                if (dist > minDist)
                {
                    volume *= 1f - Mathf.InverseLerp(minDist, maxDist, dist);
                }
            }

            if (entry != null) entry.lastPlayTime = Time.unscaledTime;

            if (clip == null) clip = fallback;
            if (clip == null) return;

            var src = pool[nextSource];
            nextSource = (nextSource + 1) % pool.Length;

            src.pitch = Mathf.Clamp(UnityEngine.Random.Range(Mathf.Min(pitch.x, pitch.y), Mathf.Max(pitch.x, pitch.y)) + pitchOffset, 0.25f, 3f);
            src.PlayOneShot(clip, volume * masterVolume);
        }
    }
}

