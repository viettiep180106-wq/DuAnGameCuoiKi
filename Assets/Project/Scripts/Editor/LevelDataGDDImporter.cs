using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class LevelDataGDDImporter
{
    [MenuItem("Tools/Import GDD Level 1 (Strict 6x6 10 Waves)")]
    public static void GenerateLevel1()
    {
        string assetPath = "Assets/Project/Resources/ScriptObject/LevelData/LevelDataSO_lv_001.asset";
        LevelDataSO so = AssetDatabase.LoadAssetAtPath<LevelDataSO>(assetPath);
        if (so == null)
        {
            so = ScriptableObject.CreateInstance<LevelDataSO>();
            AssetDatabase.CreateAsset(so, assetPath);
        }

        List<StepData> steps = new List<StepData>();

        // Wave 1: Chiến đấu, Dễ, Đội hình Full 1 thuộc tính Đất, Reward: 18 lõi
        StepData w1 = new StepData(1, WaveType.Combat, StepDifficulty.Easy, FormationType.FullSingleElement,
            new List<Enemy>
            {
                new Enemy(EnemyType.SlimeBun, 2, EnemyRow.Front),
                new Enemy(EnemyType.LinhDat, 1, EnemyRow.Front),
                new Enemy(EnemyType.BoCapDa, 1, EnemyRow.Back),
                new Enemy(EnemyType.PhapSuPhongAn, 1, EnemyRow.Back)
            },
            StepEventType.None, 18);
        steps.Add(w1);

        // Wave 2: Khám phá, Roll chọn ngẫu nhiên Buff, Reward: 0 lõi
        StepData w2 = new StepData(2, WaveType.Discovery, StepDifficulty.Easy, FormationType.None,
            new List<Enemy>(),
            StepEventType.RuneStone_Roll, 0);
        steps.Add(w2);

        // Wave 3: Chiến đấu, Dễ, Đội hình Cân bằng, Reward: 12 lõi
        StepData w3 = new StepData(3, WaveType.Combat, StepDifficulty.Easy, FormationType.Balanced,
            new List<Enemy>
            {
                new Enemy(EnemyType.LinhDat, 1, EnemyRow.Front),
                new Enemy(EnemyType.BoCapDa, 1, EnemyRow.Front),
                new Enemy(EnemyType.QuyRung, 1, EnemyRow.Back),
                new Enemy(EnemyType.SlimeNuoc, 1, EnemyRow.Back),
                new Enemy(EnemyType.CungThuAm, 1, EnemyRow.Back)
            },
            StepEventType.None, 12);
        steps.Add(w3);

        // Wave 4: Chiến đấu, Dễ, Đội hình Full 1 thuộc tính Mộc, Reward: 18 lõi
        StepData w4 = new StepData(4, WaveType.Combat, StepDifficulty.Easy, FormationType.FullSingleElement,
            new List<Enemy>
            {
                new Enemy(EnemyType.BoRac, 2, EnemyRow.Front),
                new Enemy(EnemyType.TinhLinhHoiMau, 1, EnemyRow.Back),
                new Enemy(EnemyType.QuyRung, 1, EnemyRow.Back),
                new Enemy(EnemyType.CungThuAm, 1, EnemyRow.Back)
            },
            StepEventType.None, 18);
        steps.Add(w4);

        // Wave 5: Khám phá, Lời chúc phúc, Reward: 0 lõi
        StepData w5 = new StepData(5, WaveType.Discovery, StepDifficulty.Easy, FormationType.None,
            new List<Enemy>(),
            StepEventType.RuneStone_Altar, 0);
        steps.Add(w5);

        // Wave 6: Chiến đấu, Dễ, Đội hình Cân bằng, Reward: 12 lõi
        StepData w6 = new StepData(6, WaveType.Combat, StepDifficulty.Easy, FormationType.Balanced,
            new List<Enemy>
            {
                new Enemy(EnemyType.BoRac, 1, EnemyRow.Front),
                new Enemy(EnemyType.TinhLinhHoiMau, 1, EnemyRow.Front),
                new Enemy(EnemyType.KySiSat, 1, EnemyRow.Back),
                new Enemy(EnemyType.KeTromKyNang, 1, EnemyRow.Back),
                new Enemy(EnemyType.PhuThuyLua, 1, EnemyRow.Back)
            },
            StepEventType.None, 12);
        steps.Add(w6);

        // Wave 7: Khám phá, Cuộc chiến giữa 2 con vật (Đấu thú), Reward: 0 lõi
        StepData w7 = new StepData(7, WaveType.Discovery, StepDifficulty.Easy, FormationType.None,
            new List<Enemy>(),
            StepEventType.BeastBetting, 0);
        steps.Add(w7);

        // Wave 8: Chiến đấu, Dễ, Đội hình Full 1 thuộc tính Kim, Reward: 18 lõi
        StepData w8 = new StepData(8, WaveType.Combat, StepDifficulty.Easy, FormationType.FullSingleElement,
            new List<Enemy>
            {
                new Enemy(EnemyType.GolemThep, 1, EnemyRow.Front),
                new Enemy(EnemyType.SatThuBongToi, 1, EnemyRow.Front),
                new Enemy(EnemyType.SatThuBongToi, 1, EnemyRow.Back),
                new Enemy(EnemyType.KySiSat, 1, EnemyRow.Back),
                new Enemy(EnemyType.CungThuXuyenGiap, 1, EnemyRow.Back)
            },
            StepEventType.None, 18);
        steps.Add(w8);

        // Wave 9: Chiến đấu, Dễ, Đội hình Cân bằng, Reward: 12 lõi
        StepData w9 = new StepData(9, WaveType.Combat, StepDifficulty.Easy, FormationType.Balanced,
            new List<Enemy>
            {
                new Enemy(EnemyType.LinhDat, 1, EnemyRow.Front),
                new Enemy(EnemyType.BoCapDa, 1, EnemyRow.Front),
                new Enemy(EnemyType.QuyRung, 1, EnemyRow.Back),
                new Enemy(EnemyType.PhapSuBang, 1, EnemyRow.Back),
                new Enemy(EnemyType.TinhLinhLua, 1, EnemyRow.Back)
            },
            StepEventType.None, 12);
        steps.Add(w9);

        // Wave 10: Chiến đấu, Thường (Boss), Đội hình Cân bằng, Reward: 18 lõi
        StepData w10 = new StepData(10, WaveType.Combat, StepDifficulty.Normal, FormationType.Balanced,
            new List<Enemy>
            {
                new Enemy(EnemyType.HatNhamThach, 1, EnemyRow.Front),
                new Enemy(EnemyType.GolemThep, 1, EnemyRow.Front),
                new Enemy(EnemyType.PhapSuBuaChu, 1, EnemyRow.Back),
                new Enemy(EnemyType.KeTromKyNang, 1, EnemyRow.Back),
                new Enemy(EnemyType.ThuyQuaiNho, 1, EnemyRow.Back)
            },
            StepEventType.None, 18);
        steps.Add(w10);

        LevelData levelData = new LevelData(1, new Vector2Int(6, 6), steps);
        so.SetData(levelData);

        EditorUtility.SetDirty(so);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"<color=#00ff88>[LevelDataGDDImporter] Successfully imported Level 1 (6x6 Board, 10 waves strictly matching GDD Table 1) into {assetPath}!</color>");
    }

    /// <summary>
    /// API tạo mẫu bất kỳ Level nào trong 50 Level mà người dùng mong muốn:
    /// Chỉ việc gọi LevelDataGDDImporter.CreateTemplateLevel(levelNum, diff, primaryElem)
    /// </summary>
    public static LevelDataSO CreateTemplateLevel(int levelNumber, StepDifficulty difficulty = StepDifficulty.Normal, ElementType primaryElem = ElementType.Earth)
    {
        string formattedNum = levelNumber.ToString("D3");
        string assetPath = $"Assets/Project/Resources/ScriptObject/LevelData/LevelDataSO_lv_{formattedNum}.asset";
        LevelDataSO so = AssetDatabase.LoadAssetAtPath<LevelDataSO>(assetPath);
        if (so == null)
        {
            so = ScriptableObject.CreateInstance<LevelDataSO>();
            AssetDatabase.CreateAsset(so, assetPath);
        }

        List<StepData> steps = new List<StepData>();
        for (int w = 1; w <= 10; w++)
        {
            bool isDiscovery = (w == 2 || w == 5 || w == 7);
            if (isDiscovery)
            {
                StepEventType evt = w == 2 ? StepEventType.RuneStone_Roll : (w == 5 ? StepEventType.RuneStone_Altar : StepEventType.BeastBetting);
                steps.Add(new StepData(w, WaveType.Discovery, difficulty, FormationType.None, new List<Enemy>(), evt, 0));
            }
            else
            {
                bool isBossWave = (w == 10);
                StepDifficulty wDiff = isBossWave ? StepDifficulty.VeryHard : difficulty;
                int reward = isBossWave ? 26 : (difficulty == StepDifficulty.Easy ? 18 : 22);
                FormationType fType = (w == 1 || w == 4 || w == 8) ? FormationType.FullSingleElement : FormationType.Balanced;

                List<EnemyType> elemEnemies = EnemyStatsDatabase.GetEnemiesByElement(primaryElem);
                EnemyType tankType = elemEnemies.Count > 0 ? elemEnemies[0] : EnemyType.SlimeBun;
                EnemyType dpsType = elemEnemies.Count > 1 ? elemEnemies[1] : tankType;
                EnemyType supType = elemEnemies.Count > 2 ? elemEnemies[2] : tankType;

                List<Enemy> defaultTeam = new List<Enemy>
                {
                    new Enemy(tankType, 1, EnemyRow.Front),
                    new Enemy(tankType, 1, EnemyRow.Front),
                    new Enemy(dpsType, 1, EnemyRow.Back),
                    new Enemy(supType, 1, EnemyRow.Back),
                    new Enemy(dpsType, 1, EnemyRow.Back)
                };

                steps.Add(new StepData(w, WaveType.Combat, wDiff, fType, defaultTeam, StepEventType.None, reward));
            }
        }

        LevelData levelData = new LevelData(levelNumber, new Vector2Int(6, 6), steps);
        so.SetData(levelData);

        EditorUtility.SetDirty(so);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"<color=#00e5ff>[LevelDataGDDImporter] Generated template for Level {levelNumber} at {assetPath}!</color>");
        return so;
    }
}
