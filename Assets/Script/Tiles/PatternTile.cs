using UnityEngine;
using UnityEngine.Tilemaps;

namespace GI351.KitchenGame
{
    /// <summary>
    /// ไทล์ที่เลือกภาพตามตำแหน่งบนกริดแบบวนซ้ำ (เช่น น้ำ 4x4 ที่ต้องต่อกันเนียน)
    /// ใส่ Sprites เรียงทีละแถว จากแถวบนสุดไปล่าง ซ้ายไปขวา (จำนวน = Pattern Width x Pattern Height)
    /// </summary>
    [CreateAssetMenu(menuName = "2D/Tiles/Pattern Tile", fileName = "New Pattern Tile")]
    public class PatternTile : TileBase
    {
        [Tooltip("จำนวนช่องในแนวนอนของแพตเทิร์น")]
        [Min(1)] public int patternWidth = 4;

        [Tooltip("จำนวนช่องในแนวตั้งของแพตเทิร์น")]
        [Min(1)] public int patternHeight = 4;

        [Tooltip("ภาพของแต่ละช่อง เรียงทีละแถว จากบนลงล่าง ซ้ายไปขวา")]
        public Sprite[] sprites;

        [Tooltip("ชนิด Collider ของไทล์ (ปกติ None สำหรับพื้น)")]
        public Tile.ColliderType colliderType = Tile.ColliderType.None;

        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            tileData.colliderType = colliderType;
            tileData.transform = Matrix4x4.identity;
            tileData.color = Color.white;

            if (sprites == null || sprites.Length == 0) return;

            int w = Mathf.Max(1, patternWidth);
            int h = Mathf.Max(1, patternHeight);
            int x = ((position.x % w) + w) % w;
            int yUp = ((position.y % h) + h) % h;
            int row = h - 1 - yUp; // แถวบนสุดของแพตเทิร์นอยู่ y สูงสุด
            int index = row * w + x;
            tileData.sprite = sprites[Mathf.Clamp(index, 0, sprites.Length - 1)];
        }
    }
}
