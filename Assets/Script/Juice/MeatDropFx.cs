using System.Collections;
using UnityEngine;

namespace GI351.KitchenGame
{
    /// <summary>
    /// เนื้อที่หล่นจากสัตว์ตาย: โผล่ scale 0 -> 1, บินโค้งแบบพาราโบลา (root เลื่อนบนพื้น / sprite ยกขึ้นแกน y), มีเงา
    /// ตกพื้นแล้วเด้ง 1 ครั้งและ squash ตอนนิ่ง ผู้เล่นเก็บได้หลังตกพื้นเท่านั้น (collider ถูกปิดระหว่างบิน)
    /// ใส่ให้เนื้อด้วยโค้ดตอนสัตว์ตาย (MeatDropFx.Spawn) ไม่ต้องแก้ prefab ของเนื้อ ตัวเนื้อเป็น Item เดิมทุกอย่าง
    /// ระหว่างเอฟเฟกต์ SpriteRenderer เดิมบน root จะถูกปิดแล้วใช้ sprite สำเนาที่เป็นลูกแทน จบแล้วคืนให้เหมือนเดิม
    /// </summary>
    public class MeatDropFx : MonoBehaviour
    {
        private JuiceConfig config;
        private SpriteRenderer rootRenderer;
        private Collider2D[] colliders;
        private Transform visual;
        private SpriteRenderer visualRenderer;
        private Transform shadow;
        private SpriteRenderer shadowRenderer;

        private Vector2 origin;
        private Vector2 target;
        private float arcHeight;
        private float spriteBottom;
        private bool pickupEnabled = true;

        private static Sprite shadowSprite;

        /// <summary>
        /// สร้างเนื้อที่จุด origin แล้วให้บินไปทิศ angleDegrees
        /// hasBounds = สัตว์ถูกขังในกรอบ (ให้เนื้อตกในกรอบเดียวกัน ไม่ตกนอกโซน)
        /// ถ้าปิด meatFlightEnabled จะวางเนื้อตรงนั้นแบบเดิม (สุ่มเยื้องเล็กน้อย)
        /// </summary>
        public static GameObject Spawn(GameObject prefab, Vector3 origin, float angleDegrees, JuiceConfig cfg,
            bool hasBounds, Vector2 boundsMin, Vector2 boundsMax)
        {
            if (prefab == null) return null;

            cfg = JuiceDirector.Resolve(cfg);
            GameObject go;
            if (!cfg.meatFlightEnabled)
            {
                Vector2 randomOffset = Random.insideUnitCircle * 0.4f;
                go = Instantiate(prefab, origin + (Vector3)randomOffset, Quaternion.identity);
            }
            else
            {
                go = Instantiate(prefab, origin, Quaternion.identity);
                MeatDropFx fx = go.AddComponent<MeatDropFx>();
                fx.Begin(cfg, angleDegrees, hasBounds, boundsMin, boundsMax);
            }

            // ดูดเข้าตัวผู้เล่นเมื่อเข้าใกล้ (รอจนตกพื้นเสร็จเอง)
            if (cfg.magnetEnabled && go.TryGetComponent(out Item _)) go.AddComponent<ItemMagnet>().Init(cfg);
            return go;
        }

        private void Begin(JuiceConfig cfg, float angleDegrees, bool hasBounds, Vector2 boundsMin, Vector2 boundsMax)
        {
            config = cfg;
            origin = transform.position;

            // จุดตกพื้น: ทิศ + ระยะสุ่ม ถ้าจะตกนอกกรอบให้สะท้อนกลับเข้ามาแล้วค่อย clamp
            float rad = angleDegrees * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            float distance = Random.Range(Mathf.Min(cfg.meatGroundDistance.x, cfg.meatGroundDistance.y),
                Mathf.Max(cfg.meatGroundDistance.x, cfg.meatGroundDistance.y));
            Vector2 landing = origin + dir * distance;

            if (hasBounds)
            {
                const float edgePadding = 0.15f;
                Vector2 min = boundsMin + Vector2.one * edgePadding;
                Vector2 max = boundsMax - Vector2.one * edgePadding;
                if (landing.x < min.x || landing.x > max.x) landing.x = origin.x - dir.x * distance;
                if (landing.y < min.y || landing.y > max.y) landing.y = origin.y - dir.y * distance;
                landing.x = Mathf.Clamp(landing.x, min.x, max.x);
                landing.y = Mathf.Clamp(landing.y, min.y, max.y);
            }
            target = Tween.Snap(landing, cfg.pixelsPerUnit);

            arcHeight = Random.Range(Mathf.Min(cfg.meatArcHeight.x, cfg.meatArcHeight.y),
                Mathf.Max(cfg.meatArcHeight.x, cfg.meatArcHeight.y));

            BuildVisuals();
            StartCoroutine(Run());
        }

        private void BuildVisuals()
        {
            colliders = GetComponents<Collider2D>();
            rootRenderer = TryGetComponent(out Item item) && item.SpriteRenderer != null
                ? item.SpriteRenderer
                : GetComponentInChildren<SpriteRenderer>();

            int layerId = 0;
            int order = 0;

            if (rootRenderer != null)
            {
                layerId = rootRenderer.sortingLayerID;
                order = rootRenderer.sortingOrder;
                spriteBottom = rootRenderer.sprite != null ? rootRenderer.sprite.bounds.min.y : -0.25f;

                // สำเนา sprite เป็นลูก (ให้ยกขึ้นแกน y ได้โดยที่ root ซึ่งมี collider อยู่บนพื้น) แล้วซ่อนตัวเดิมไว้ก่อน
                GameObject v = new GameObject("Meat Visual");
                v.transform.SetParent(transform, false);
                visualRenderer = v.AddComponent<SpriteRenderer>();
                visualRenderer.sprite = rootRenderer.sprite;
                visualRenderer.color = rootRenderer.color;
                visualRenderer.flipX = rootRenderer.flipX;
                visualRenderer.flipY = rootRenderer.flipY;
                visualRenderer.sharedMaterial = rootRenderer.sharedMaterial;
                visualRenderer.sortingLayerID = layerId;
                visualRenderer.sortingOrder = order;
                visualRenderer.maskInteraction = rootRenderer.maskInteraction;
                visual = v.transform;
                visual.localScale = Vector3.zero;
                rootRenderer.enabled = false;
            }
            else
            {
                spriteBottom = -0.25f;
            }

            // เงา: วงรีเล็กสีดำโปร่ง อยู่ที่ root (บนพื้น) ตลอด ไม่ยกตามเนื้อ
            GameObject s = new GameObject("Meat Shadow");
            s.transform.SetParent(transform, false);
            shadowRenderer = s.AddComponent<SpriteRenderer>();
            shadowRenderer.sprite = GetShadowSprite(config.meatShadowSize, config.pixelsPerUnit);
            shadowRenderer.color = config.meatShadowColor;
            shadowRenderer.sortingLayerID = layerId;
            shadowRenderer.sortingOrder = order - 1;
            shadow = s.transform;

            // sprite เงาสร้างให้ขนาดเท่า meatShadowSize พอดี (ปัดเป็นพิกเซลเต็ม) ตอนบินแค่ย่อลงตามความสูง
            shadow.localPosition = new Vector3(0f, spriteBottom + config.meatShadowSize.y * 0.5f + config.meatShadowYOffset, 0f);
            shadow.localScale = Vector3.one;

            if (config.meatPickupOnlyAfterLanding) SetPickup(false);
        }

        private IEnumerator Run()
        {
            JuiceConfig cfg = config;
            float ppu = cfg.pixelsPerUnit;
            float z = transform.position.z;

            // 1) กระเด็น: root พุ่งบนพื้น (ease ตาม meatGroundEase), sprite ยกขึ้นเป็นพาราโบลา, หมุนกลางอากาศ, scale 0 -> 1 พร้อมกัน
            float flight = Mathf.Max(0.05f, cfg.meatFlightTime + Random.Range(-cfg.meatFlightTimeJitter, cfg.meatFlightTimeJitter));
            float spin = cfg.meatSpinTurns * 360f * (Random.value < 0.5f ? -1f : 1f);
            float elapsed = 0f;
            while (true)
            {
                float t = Mathf.Clamp01(elapsed / flight);
                Vector2 ground = Vector2.LerpUnclamped(origin, target, Tween.Evaluate(cfg.meatGroundEase, t));
                SetVisualRotation(spin * Tween.Evaluate(Ease.OutQuad, t));
                Vector2 snapped = Tween.Snap(ground, ppu);
                transform.position = new Vector3(snapped.x, snapped.y, z);

                float lift = arcHeight * 4f * t * (1f - t);
                SetVisualLift(Tween.Snap(lift, ppu));
                SetShadow(lift);

                float scaleK = cfg.meatSpawnScaleTime > 0f ? Mathf.Clamp01(elapsed / cfg.meatSpawnScaleTime) : 1f;
                float s = Tween.Evaluate(cfg.meatSpawnScaleEase, scaleK);
                SetVisualScale(s, s);

                if (t >= 1f) break;
                yield return null;
                elapsed += Time.deltaTime;
            }

            transform.position = new Vector3(target.x, target.y, z);
            SetVisualRotation(0f);
            SetVisualLift(0f);
            SetShadow(0f);

            // 2) เด้งขึ้น 1 ครั้งสูง meatBounceHeightPercent ของความสูงที่บิน
            float bounceHeight = arcHeight * cfg.meatBounceHeightPercent;
            if (bounceHeight > 0f && cfg.meatBounceTime > 0f)
            {
                yield return Tween.Run(cfg.meatBounceTime, k =>
                {
                    float lift = bounceHeight * 4f * k * (1f - k);
                    SetVisualLift(Tween.Snap(lift, ppu));
                    SetShadow(lift);
                });
            }

            // 3) ตกพื้นจริง: เก็บได้ตั้งแต่ตรงนี้ + squash สั้นๆ
            SetPickup(true);
            SetVisualLift(0f);
            SetShadow(0f);

            if (cfg.meatLandSquashTime > 0f)
            {
                Vector2 squash = cfg.meatLandSquashScale;
                yield return Tween.Run(cfg.meatLandSquashTime, k =>
                {
                    float amount = Mathf.Sin(k * Mathf.PI);
                    float sx = Mathf.Lerp(1f, squash.x, amount);
                    float sy = Mathf.Lerp(1f, squash.y, amount);
                    SetVisualScale(sx, sy);
                    // ล็อกขอบล่างไว้กับพื้น
                    SetVisualLift(Tween.Snap(spriteBottom * (1f - sy), ppu));
                });
            }

            Finish();
        }

        private void SetVisualLift(float lift)
        {
            if (visual == null) return;
            visual.localPosition = new Vector3(0f, lift, 0f);
        }

        private void SetVisualRotation(float degrees)
        {
            if (visual == null) return;
            visual.localRotation = Quaternion.Euler(0f, 0f, degrees);
        }

        private void SetVisualScale(float sx, float sy)
        {
            if (visual == null) return;
            visual.localScale = new Vector3(sx, sy, 1f);
        }

        /// <summary>เงาอยู่ที่พื้น ขนาด/ความทึบลดลงเมื่อเนื้อลอยสูง</summary>
        private void SetShadow(float lift)
        {
            if (shadow == null) return;

            float k = arcHeight > 0f ? Mathf.Clamp01(lift / arcHeight) : 0f;
            float scale = Mathf.Lerp(1f, config.meatShadowMinScale, k);
            shadow.localScale = new Vector3(scale, scale, 1f);

            Color c = config.meatShadowColor;
            c.a *= Mathf.Lerp(1f, 0.6f, k);
            shadowRenderer.color = c;
        }

        private void SetPickup(bool enable)
        {
            pickupEnabled = enable;
            if (colliders == null) return;
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null) colliders[i].enabled = enable;
            }
        }

        /// <summary>จบเอฟเฟกต์: คืน SpriteRenderer เดิม ลบสำเนา/เงา ลบสคริปต์นี้ทิ้ง (เนื้อกลับเป็น Item ปกติ)</summary>
        private void Finish()
        {
            Cleanup();
            Destroy(this);
        }

        private void Cleanup()
        {
            if (!pickupEnabled) SetPickup(true);
            if (rootRenderer != null) rootRenderer.enabled = true;
            if (visual != null) Destroy(visual.gameObject);
            if (shadow != null) Destroy(shadow.gameObject);
            visual = null;
            shadow = null;
        }

        private void OnDestroy()
        {
            // กันกรณีถูกถอดสคริปต์/ทำลายกลางทาง ไม่ให้เนื้อค้างเป็นของที่มองไม่เห็นหรือเก็บไม่ได้
            if (visual != null || shadow != null || !pickupEnabled || (rootRenderer != null && !rootRenderer.enabled))
            {
                Cleanup();
            }
        }

        private static int shadowSpriteW;
        private static int shadowSpriteH;
        private static float shadowSpritePpu;

        /// <summary>
        /// sprite วงรีแบบพิกเซลสีขาว (ย้อมสีด้วย SpriteRenderer.color) ขนาดตาม size ปัดเป็นพิกเซลเต็ม ใช้ร่วมกันทุกชิ้น
        /// </summary>
        private static Sprite GetShadowSprite(Vector2 size, float ppu)
        {
            int w = Mathf.Max(3, Mathf.RoundToInt(size.x * ppu));
            int h = Mathf.Max(2, Mathf.RoundToInt(size.y * ppu));
            if (shadowSprite != null && shadowSpriteW == w && shadowSpriteH == h && Mathf.Approximately(shadowSpritePpu, ppu))
            {
                return shadowSprite;
            }

            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.name = "Meat Shadow";
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color32[] pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float nx = (x + 0.5f) / w * 2f - 1f;
                    float ny = (y + 0.5f) / h * 2f - 1f;
                    bool inside = nx * nx + ny * ny <= 1f;
                    pixels[y * w + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();

            shadowSprite = Sprite.Create(tex, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), ppu);
            shadowSprite.name = "Meat Shadow";
            shadowSpriteW = w;
            shadowSpriteH = h;
            shadowSpritePpu = ppu;
            return shadowSprite;
        }
    }
}
