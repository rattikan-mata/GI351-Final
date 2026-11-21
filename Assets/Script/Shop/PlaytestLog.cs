#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace GI351.KitchenGame
{
    /// <summary>
    /// ตัวบันทึกการเล่นทดสอบ (มีเฉพาะใน Editor และ Development Build) ใช้จูนราคา/ความถี่ลูกค้าจากการเล่นจริง
    /// สร้างตัวเองอัตโนมัติตอนเริ่มเกม ไม่ต้องวางในฉาก เขียนไฟล์ทุก 1 นาทีและตอนหยุดเล่น ที่
    ///   [โฟลเดอร์โปรเจค]/Logs/Playtest/playtest_วันที่_เวลา.txt
    /// บันทึก: รายได้/รายจ่ายต่อนาที, ลูกค้าที่จ่ายเงิน/เดินหนี, จำนวนอาหารเฉลี่ยบนแผง, และเวลาที่ซื้อของ/เปิดประตูแต่ละครั้ง
    /// หมายเหตุ: เงินจากปุ่มโกง F1-F3 ถูกนับเป็นรายได้ด้วย  ปิดได้ที่ PlaytestLog.Enabled = false
    /// </summary>
    public class PlaytestLog : MonoBehaviour
    {
        public static bool Enabled = true;

        private static PlaytestLog instance;

        private class MinuteRow
        {
            public int minute;
            public int earned;
            public int spent;
            public int moneyEnd;
            public int paidCustomers;
            public int lostCustomers;
            public float avgStock;
            public int maxStock;
            public float emptyShelfRatio;
            public float customersPerMinute;
        }

        private readonly List<MinuteRow> rows = new List<MinuteRow>();
        private readonly List<string> events = new List<string>();
        private readonly HashSet<ShopStall> hookedStalls = new HashSet<ShopStall>();
        private readonly HashSet<AreaGate> hookedGates = new HashSet<AreaGate>();
        private readonly HashSet<CashierCounter> hookedCashiers = new HashSet<CashierCounter>();

        private string filePath;
        private float startTime;
        private float minuteStart;
        private float nextSample;
        private float nextHookScan;
        private int lastMoney = -1;
        private bool moneyHooked;

        private int earned, spent, paid, totalEarned, totalSpent, totalPaid;
        private int lostAtMinuteStart;
        private float stockSum;
        private int stockSamples, emptySamples, maxStock;
        private float firstSaleTime = -1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null || !Enabled) return;
            var go = new GameObject("[Playtest Log]");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<PlaytestLog>();
        }

        private void Start()
        {
            startTime = Time.time;
            minuteStart = startTime;
            lostAtMinuteStart = CustomerAI.LostCustomers;

            string dir = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", "Logs", "Playtest");
            Directory.CreateDirectory(dir);
            // InvariantCulture: ไม่ให้ปีกลายเป็น พ.ศ. บนเครื่องที่ตั้งภาษาไทย
            filePath = Path.Combine(dir, "playtest_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + ".txt");
        }

        private void Update()
        {
            if (!Enabled) return;

            HookSources();

            if (Time.time >= nextSample)
            {
                nextSample = Time.time + 2f;
                SampleStock();
            }

            if (Time.time - minuteStart >= 60f)
            {
                CloseMinute();
                Write();
            }
        }

        private void HookSources()
        {
            if (!moneyHooked && MoneyManager.Instance != null)
            {
                moneyHooked = true;
                lastMoney = MoneyManager.Instance.CurrentMoney;
                MoneyManager.Instance.onMoneyChanged.AddListener(OnMoneyChanged);
            }

            if (Time.time < nextHookScan) return;
            nextHookScan = Time.time + 3f;

            foreach (ShopStall s in FindObjectsByType<ShopStall>(FindObjectsInactive.Include))
            {
                if (!hookedStalls.Add(s)) continue;
                ShopStall stall = s;
                s.ItemPurchased += item => OnPurchased(stall, item);
            }
            foreach (AreaGate g in FindObjectsByType<AreaGate>(FindObjectsInactive.Include))
            {
                if (!hookedGates.Add(g)) continue;
                AreaGate gate = g;
                g.onUnlocked.AddListener(() => AddEvent("GATE", gate.name + " ($" + gate.Price + ")"));
            }
            foreach (CashierCounter c in FindObjectsByType<CashierCounter>(FindObjectsInactive.Include))
            {
                if (!hookedCashiers.Add(c)) continue;
                c.onPaid.AddListener(OnPaid);
            }
        }

        private void OnMoneyChanged(int money)
        {
            if (lastMoney >= 0)
            {
                int delta = money - lastMoney;
                if (delta > 0) { earned += delta; totalEarned += delta; }
                else if (delta < 0) { spent -= delta; totalSpent -= delta; }
            }
            lastMoney = money;
        }

        private void OnPaid(int amount)
        {
            paid++;
            totalPaid++;
            if (firstSaleTime < 0f)
            {
                firstSaleTime = Time.time - startTime;
                AddEvent("FIRST SALE", "$" + amount);
            }
        }

        private void OnPurchased(ShopStall stall, ShopItemData item)
        {
            if (item == null) return;
            string what = string.IsNullOrEmpty(item.id) ? item.itemName : item.id;
            if (item.IsLeveled) what += " -> Lv" + item.Level + " (" + item.CurrentValue.ToString("0.##", CultureInfo.InvariantCulture) + ")";
            AddEvent("BUY", what + " @ " + stall.name);
        }

        private void AddEvent(string kind, string text)
        {
            int money = MoneyManager.Instance != null ? MoneyManager.Instance.CurrentMoney : 0;
            events.Add(FormatTime(Time.time - startTime) + "  " + kind.PadRight(11) + text + "   [money $" + money + ", earned total $" + totalEarned + "]");
        }

        private void SampleStock()
        {
            int stock = 0;
            bool anyOpen = false, anyEmpty = false;
            foreach (FoodSellCounter c in FoodSellCounter.All)
            {
                if (c == null || !c.isActiveAndEnabled || !c.IsOpen) continue;
                anyOpen = true;
                stock += c.DishesCount;
                if (c.DishesCount == 0) anyEmpty = true;
            }
            if (!anyOpen) return;
            stockSum += stock;
            stockSamples++;
            if (anyEmpty) emptySamples++;
            if (stock > maxStock) maxStock = stock;
        }

        private void CloseMinute()
        {
            float cpm = 0f;
            foreach (CustomerSpawner s in CustomerSpawner.All)
            {
                if (s != null) { cpm = s.CustomersPerMinute; break; }
            }

            rows.Add(new MinuteRow
            {
                minute = rows.Count + 1,
                earned = earned,
                spent = spent,
                moneyEnd = MoneyManager.Instance != null ? MoneyManager.Instance.CurrentMoney : 0,
                paidCustomers = paid,
                lostCustomers = CustomerAI.LostCustomers - lostAtMinuteStart,
                avgStock = stockSamples > 0 ? stockSum / stockSamples : 0f,
                maxStock = maxStock,
                emptyShelfRatio = stockSamples > 0 ? (float)emptySamples / stockSamples : 0f,
                customersPerMinute = cpm
            });

            earned = spent = paid = 0;
            stockSum = 0f;
            stockSamples = emptySamples = maxStock = 0;
            lostAtMinuteStart = CustomerAI.LostCustomers;
            minuteStart = Time.time;
        }

        private static string FormatTime(float seconds)
        {
            int s = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return (s / 60).ToString("00") + ":" + (s % 60).ToString("00");
        }

        private void Write()
        {
            if (string.IsNullOrEmpty(filePath)) return;

            var sb = new StringBuilder();
            sb.AppendLine("GI351 playtest log  " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            sb.AppendLine("Played: " + FormatTime(Time.time - startTime) +
                          "   Earned: $" + totalEarned + "   Spent: $" + totalSpent +
                          "   Customers paid: " + totalPaid + "   Customers lost: " + CustomerAI.LostCustomers +
                          "   First sale: " + (firstSaleTime >= 0f ? FormatTime(firstSaleTime) : "-"));
            sb.AppendLine("(money cheats F1-F3 count as earned)");
            sb.AppendLine();
            sb.AppendLine("PER MINUTE");
            sb.AppendLine("min  earned  spent  money  paid  lost  avgStock  maxStock  shelfEmpty%  customers/min");
            foreach (MinuteRow r in rows)
            {
                sb.AppendLine(
                    r.minute.ToString().PadLeft(3) + "  " +
                    ("$" + r.earned).PadLeft(6) + "  " +
                    ("$" + r.spent).PadLeft(5) + "  " +
                    ("$" + r.moneyEnd).PadLeft(5) + "  " +
                    r.paidCustomers.ToString().PadLeft(4) + "  " +
                    r.lostCustomers.ToString().PadLeft(4) + "  " +
                    r.avgStock.ToString("0.0", CultureInfo.InvariantCulture).PadLeft(8) + "  " +
                    r.maxStock.ToString().PadLeft(8) + "  " +
                    Mathf.RoundToInt(r.emptyShelfRatio * 100f).ToString().PadLeft(10) + "%  " +
                    r.customersPerMinute.ToString("0.#", CultureInfo.InvariantCulture).PadLeft(13));
            }
            sb.AppendLine();
            sb.AppendLine("EVENTS");
            foreach (string e in events) sb.AppendLine(e);

            try
            {
                File.WriteAllText(filePath, sb.ToString(), new UTF8Encoding(false));
            }
            catch (IOException ex)
            {
                Debug.LogWarning("[PlaytestLog] เขียนไฟล์ไม่ได้: " + ex.Message);
            }
        }

        private void OnApplicationQuit()
        {
            Finish();
        }

        private void OnDestroy()
        {
            Finish();
            if (instance == this) instance = null;
        }

        private bool finished;

        private void Finish()
        {
            if (finished || !Enabled || string.IsNullOrEmpty(filePath)) return;
            finished = true;
            // เล่นไม่ถึง 10 วินาทีไม่ต้องเก็บ (แค่กด Play ทดสอบ)
            if (Time.time - startTime < 10f) return;
            if (Time.time - minuteStart > 1f) CloseMinute();
            Write();
            Debug.Log("[PlaytestLog] บันทึกการเล่นไว้ที่ " + filePath);
        }
    }
}
#endif
