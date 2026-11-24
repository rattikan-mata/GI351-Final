using System.Collections.Generic;
using UnityEngine;

namespace GI351.KitchenGame
{
    /// <summary>
    /// เศษกระเด็นแบบ pixel art ตอนตี/ฆ่าสัตว์ (ไม่ใช้ ParticleSystem): เม็ดสี่เหลี่ยมเต็มพิกเซล บินโค้งแบบมีความสูงปลอม (top-down)
    /// ตกพื้นแล้วเด้งเบาๆ ช่วงท้ายหดทีละพิกเซลจนหาย / ควันตอนฆ่าเป็นวงกลมพิกเซลที่โตขึ้นแล้วจางหาย
    /// ตำแหน่งปัดลงตารางพิกเซลทุกเฟรม ตัวเลขทั้งหมดอยู่ใน JuiceConfig หัวข้อ 5) Particles
    /// สร้างตัวเองอัตโนมัติ (JuiceDirector.Burst) ไม่ต้องวางในซีน ใช้ pool ไม่สร้าง GameObject ใหม่ทุกครั้ง
    /// </summary>
    internal sealed class JuiceParticles : MonoBehaviour
    {
        private class Particle
        {
            public SpriteRenderer sr;
            public Vector2 ground;
            public Vector2 velocity;
            public float height;
            public float upSpeed;
            public float age;
            public float life;
            public int sizePx;
            public bool smoke;
            public int growPx;
            public bool bounced;
            public Color color;
            public bool unscaled;
            public float gravity;
            public float drag;
            public float ppu;
            public bool pixel;
        }

        private readonly List<Particle> live = new List<Particle>();
        private readonly Stack<SpriteRenderer> pool = new Stack<SpriteRenderer>();

        private static Sprite pixelSprite;
        private static readonly Dictionary<int, Sprite> circleSprites = new Dictionary<int, Sprite>();

        public struct BurstSettings
        {
            public Vector2 position;
            public Vector2 direction;   // ทิศหลัก (ศูนย์ = รอบตัว)
            public float spread;        // องศา
            public int count;
            public int extraPixels;
            public Color[] colors;
            public int sortingLayerId;
            public int sortingOrder;
        }

        public void Burst(JuiceConfig cfg, BurstSettings s)
        {
            if (s.count <= 0) return;
            Color[] colors = s.colors != null && s.colors.Length > 0 ? s.colors : new[] { Color.white };
            bool around = s.direction.sqrMagnitude < 0.0001f || s.spread >= 359f;
            float baseAngle = around ? Random.Range(0f, 360f) : Mathf.Atan2(s.direction.y, s.direction.x) * Mathf.Rad2Deg;

            for (int i = 0; i < s.count; i++)
            {
                float angle = around
                    ? baseAngle + 360f * i / s.count + Random.Range(-15f, 15f)
                    : baseAngle + Random.Range(-s.spread * 0.5f, s.spread * 0.5f);
                Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));

                int lo = Mathf.Max(1, Mathf.Min(cfg.particleSizePixels.x, cfg.particleSizePixels.y));
                int hi = Mathf.Max(lo, Mathf.Max(cfg.particleSizePixels.x, cfg.particleSizePixels.y));

                Particle p = new Particle
                {
                    sr = Take(s.sortingLayerId, s.sortingOrder),
                    ground = s.position,
                    velocity = dir * RandomRange(cfg.particleSpeed),
                    height = 0f,
                    upSpeed = RandomRange(cfg.particleUpSpeed),
                    life = Mathf.Max(0.05f, RandomRange(cfg.particleLife)),
                    sizePx = Random.Range(lo, hi + 1) + Mathf.Max(0, s.extraPixels),
                    color = colors[Random.Range(0, colors.Length)],
                    unscaled = cfg.particleUnscaledTime,
                    gravity = cfg.particleGravity,
                    drag = cfg.particleDrag,
                    ppu = cfg.pixelsPerUnit,
                    pixel = cfg.pixelStyle
                };
                p.sr.sprite = p.pixel ? GetPixelSprite() : GetSoftCircleSprite();
                Apply(p);
                live.Add(p);
            }
        }

        public void Smoke(JuiceConfig cfg, Vector2 position, int count, int sortingLayerId, int sortingOrder)
        {
            float baseAngle = Random.Range(0f, 360f);
            int lo = Mathf.Max(2, Mathf.Min(cfg.smokeSizePixels.x, cfg.smokeSizePixels.y));
            int hi = Mathf.Max(lo, Mathf.Max(cfg.smokeSizePixels.x, cfg.smokeSizePixels.y));

            for (int i = 0; i < count; i++)
            {
                float angle = (baseAngle + 360f * i / Mathf.Max(1, count) + Random.Range(-20f, 20f)) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Particle p = new Particle
                {
                    sr = Take(sortingLayerId, sortingOrder),
                    ground = position + dir * 0.15f,
                    velocity = dir * cfg.smokeSpreadSpeed * Random.Range(0.6f, 1.2f),
                    height = 0f,
                    upSpeed = cfg.smokeRiseSpeed * Random.Range(0.7f, 1.3f),
                    life = Mathf.Max(0.05f, RandomRange(cfg.smokeLife)),
                    sizePx = Random.Range(lo, hi + 1),
                    growPx = cfg.smokeGrowPixels,
                    smoke = true,
                    color = cfg.smokeColor,
                    unscaled = cfg.particleUnscaledTime,
                    drag = 3f,
                    ppu = cfg.pixelsPerUnit,
                    pixel = cfg.pixelStyle
                };
                if (!p.pixel) p.sr.sprite = GetSoftCircleSprite();
                Apply(p);
                live.Add(p);
            }
        }

        private void Update()
        {
            for (int i = live.Count - 1; i >= 0; i--)
            {
                Particle p = live[i];
                if (p.sr == null) { live.RemoveAt(i); continue; }

                float dt = p.unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
                if (dt <= 0f) continue;

                p.age += dt;
                if (p.age >= p.life)
                {
                    Give(p.sr);
                    live.RemoveAt(i);
                    continue;
                }

                p.ground += p.velocity * dt;
                p.velocity *= Mathf.Exp(-p.drag * dt);

                if (p.smoke)
                {
                    p.height += p.upSpeed * dt;
                }
                else
                {
                    p.upSpeed -= p.gravity * dt;
                    p.height += p.upSpeed * dt;
                    if (p.height <= 0f)
                    {
                        p.height = 0f;
                        if (!p.bounced && p.upSpeed < 0f)
                        {
                            // เด้งครั้งเดียวเบาๆ แล้วไถลบนพื้น
                            p.bounced = true;
                            p.upSpeed = -p.upSpeed * 0.3f;
                            p.velocity *= 0.5f;
                        }
                        else
                        {
                            p.upSpeed = 0f;
                            p.velocity *= Mathf.Exp(-8f * dt);
                        }
                    }
                }

                Apply(p);
            }
        }

        private void Apply(Particle p)
        {
            if (!p.pixel)
            {
                ApplySoft(p);
                return;
            }

            float k = Mathf.Clamp01(p.age / p.life);
            float unit = 1f / Mathf.Max(1f, p.ppu);
            Vector2 pos = p.ground + new Vector2(0f, p.height);
            pos.x = Mathf.Round(pos.x / unit) * unit;
            pos.y = Mathf.Round(pos.y / unit) * unit;

            Transform t = p.sr.transform;
            t.position = new Vector3(pos.x, pos.y, 0f);

            Color c = p.color;
            if (p.smoke)
            {
                int d = p.sizePx + Mathf.RoundToInt(p.growPx * Tween.Evaluate(Ease.OutQuad, k));
                p.sr.sprite = GetCircleSprite(d);
                t.localScale = Vector3.one;
                // จางเป็นขั้น (4 ระดับ) ให้เข้ากับ pixel art
                c.a *= Mathf.Ceil((1f - k) * 4f) / 4f;
            }
            else
            {
                // 60% แรกขนาดเต็ม จากนั้นหดทีละพิกเซลจนหาย
                float shrink = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
                int size = Mathf.Max(1, Mathf.CeilToInt(p.sizePx * shrink));
                t.localScale = new Vector3(size, size, 1f);
            }
            p.sr.color = c;
        }

        /// <summary>แบบเม็ดกลมนุ่ม: ไม่ปัดตารางพิกเซล ขนาด/ความจางเปลี่ยนลื่นๆ</summary>
        private void ApplySoft(Particle p)
        {
            float k = Mathf.Clamp01(p.age / p.life);
            Transform t = p.sr.transform;
            t.position = new Vector3(p.ground.x, p.ground.y + p.height, 0f);

            float unit = 1f / Mathf.Max(1f, p.ppu);
            Color c = p.color;
            float diameter;
            if (p.smoke)
            {
                // ควัน: พองเร็วช่วงแรกแล้วจางหายเนียนๆ
                diameter = (p.sizePx + p.growPx * Tween.Evaluate(Ease.OutQuad, k)) * unit;
                c.a *= 1f - Tween.Evaluate(Ease.InQuad, k);
            }
            else
            {
                // เม็ด: ป๊อปโตขึ้นนิดตอนเกิด แล้วหดเป็น 0 ช่วง 40% สุดท้าย
                float pop = k < 0.12f ? Mathf.Lerp(0.6f, 1.1f, k / 0.12f) : Mathf.Lerp(1.1f, 1f, Mathf.Clamp01((k - 0.12f) / 0.15f));
                float shrink = k < 0.6f ? 1f : 1f - Tween.Evaluate(Ease.InQuad, (k - 0.6f) / 0.4f);
                diameter = p.sizePx * unit * pop * shrink;
            }
            t.localScale = new Vector3(diameter, diameter, 1f);
            p.sr.color = c;
        }

        private static Sprite softCircleSprite;

        /// <summary>วงกลมขอบนุ่ม 64x64 ขนาด 1 หน่วย (scale = เส้นผ่านศูนย์กลางเป็นหน่วย)</summary>
        private static Sprite GetSoftCircleSprite()
        {
            if (softCircleSprite != null) return softCircleSprite;
            const int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - r, dy = y + 0.5f - r;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    // ขอบนุ่มกว้าง ~3 texel
                    float a = Mathf.Clamp01((r - 1f - d) / 3f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply(true);
            tex.hideFlags = HideFlags.HideAndDontSave;
            softCircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            softCircleSprite.hideFlags = HideFlags.HideAndDontSave;
            return softCircleSprite;
        }

        private SpriteRenderer Take(int layerId, int order)
        {
            SpriteRenderer sr = null;
            while (pool.Count > 0 && sr == null) sr = pool.Pop();
            if (sr == null)
            {
                GameObject go = new GameObject("Juice Particle");
                go.transform.SetParent(transform, false);
                sr = go.AddComponent<SpriteRenderer>();
            }
            sr.gameObject.SetActive(true);
            sr.sortingLayerID = layerId;
            sr.sortingOrder = YSort.Overlay + order;   // เศษ/ควันอยู่เหนือตัวละครและของในแมพ
            return sr;
        }

        private void Give(SpriteRenderer sr)
        {
            if (sr == null) return;
            sr.gameObject.SetActive(false);
            pool.Push(sr);
        }

        private static float RandomRange(Vector2 range)
        {
            return Random.Range(Mathf.Min(range.x, range.y), Mathf.Max(range.x, range.y));
        }

        private static Sprite GetPixelSprite()
        {
            if (pixelSprite != null) return pixelSprite;
            Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            tex.hideFlags = HideFlags.HideAndDontSave;
            // 1 texel = 1 พิกเซลของเกม (PPU 16) scale เป็นจำนวนเต็ม = ขนาดเป็นพิกเซล
            pixelSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 16f);
            pixelSprite.hideFlags = HideFlags.HideAndDontSave;
            return pixelSprite;
        }

        private static Sprite GetCircleSprite(int diameter)
        {
            diameter = Mathf.Clamp(diameter, 2, 32);
            if (circleSprites.TryGetValue(diameter, out Sprite cached) && cached != null) return cached;

            Texture2D tex = new Texture2D(diameter, diameter, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            float r = diameter * 0.5f;
            for (int y = 0; y < diameter; y++)
            {
                for (int x = 0; x < diameter; x++)
                {
                    float dx = x + 0.5f - r, dy = y + 0.5f - r;
                    tex.SetPixel(x, y, dx * dx + dy * dy <= r * r ? Color.white : Color.clear);
                }
            }
            tex.Apply();
            tex.hideFlags = HideFlags.HideAndDontSave;
            Sprite s = Sprite.Create(tex, new Rect(0, 0, diameter, diameter), new Vector2(0.5f, 0.5f), 16f);
            s.hideFlags = HideFlags.HideAndDontSave;
            circleSprites[diameter] = s;
            return s;
        }
    }
}
