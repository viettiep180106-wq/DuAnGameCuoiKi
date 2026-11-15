using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(StepData))]
public class StepDataDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        SerializedProperty waveTypeProp = property.FindPropertyRelative("waveType");
        bool isCombat = waveTypeProp == null || waveTypeProp.enumValueIndex == (int)WaveType.Combat;

        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = 2f;

        if (!isCombat)
        {
            // Discovery wave fields: foldout(1) + step(1) + waveType(1) + eventType(1) + waveEnergy(1)
            return (lineHeight + spacing) * 5 + 12f;
        }

        // Combat wave fields: foldout(1) + step(1) + waveType(1) + stepDifficulty(1) + formationType(1) + waveEnergy(1) + listEnemy header(1)
        float h = (lineHeight + spacing) * 7 + 16f;

        SerializedProperty listEnemyProp = property.FindPropertyRelative("listEnemy");
        if (listEnemyProp != null && listEnemyProp.isExpanded)
        {
            // Size field (1) + per enemy (name, amount, row = 3 lines + box padding)
            h += (listEnemyProp.arraySize * 3 + 2) * (lineHeight + spacing) + (listEnemyProp.arraySize * 4f);
        }

        return h;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = 2f;
        float y = position.y;

        SerializedProperty stepProp = property.FindPropertyRelative("step");
        SerializedProperty waveTypeProp = property.FindPropertyRelative("waveType");
        SerializedProperty diffProp = property.FindPropertyRelative("stepDifficulty");
        SerializedProperty formatProp = property.FindPropertyRelative("formationType");
        SerializedProperty evtProp = property.FindPropertyRelative("eventType");
        SerializedProperty listEnemyProp = property.FindPropertyRelative("listEnemy");

        bool isCombat = waveTypeProp == null || waveTypeProp.enumValueIndex == (int)WaveType.Combat;
        int stepIdx = stepProp?.intValue ?? 1;

        string foldoutLabel;
        if (isCombat)
        {
            string diffStr = diffProp != null ? ((StepDifficulty)diffProp.enumValueIndex).ToString() : "Normal";
            string formatStr = formatProp != null ? ((FormationType)formatProp.enumValueIndex).ToString() : "None";
            foldoutLabel = $"⚔️ Wave {stepIdx}: [{diffStr}] Đội hình: {formatStr}";
        }
        else
        {
            string evtStr = evtProp != null ? ((StepEventType)evtProp.enumValueIndex).ToString() : "Khám phá";
            foldoutLabel = $"✨ Wave {stepIdx}: [Khám Phá] {evtStr}";
        }

        Rect foldoutRect = new Rect(position.x, y, position.width, lineHeight);
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, foldoutLabel, true);
        y += lineHeight + spacing;

        if (!property.isExpanded)
        {
            EditorGUI.EndProperty();
            return;
        }

        EditorGUI.indentLevel++;

        // 1. Step (Wave Index)
        Rect stepRect = new Rect(position.x, y, position.width, lineHeight);
        EditorGUI.PropertyField(stepRect, stepProp, new GUIContent("Step (Wave Index)"));
        y += lineHeight + spacing;

        // 2. Wave Type (Combat vs Discovery)
        Rect waveTypeRect = new Rect(position.x, y, position.width, lineHeight);
        EditorGUI.PropertyField(waveTypeRect, waveTypeProp, new GUIContent("Loại Sự Kiện (Wave Type)"));
        y += lineHeight + spacing;

        if (isCombat)
        {
            // 3. Step Difficulty (Dễ, Thường, Khó, Cực Khó)
            Rect diffRect = new Rect(position.x, y, position.width, lineHeight);
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(diffRect, diffProp, new GUIContent("Độ Khó Wave (Difficulty)"));
            if (EditorGUI.EndChangeCheck())
            {
                SerializedProperty energyProp = property.FindPropertyRelative("waveEnergyReward");
                if (energyProp != null)
                {
                    StepDifficulty newDiff = (StepDifficulty)diffProp.enumValueIndex;
                    energyProp.intValue = StepData.GetDefaultEnergyReward(newDiff);
                }
            }
            y += lineHeight + spacing;

            // 4. Formation Type (Phân loại đội hình GDD)
            Rect formatRect = new Rect(position.x, y, position.width, lineHeight);
            EditorGUI.PropertyField(formatRect, formatProp, new GUIContent("Phân Loại Đội Hình (Formation)"));
            y += lineHeight + spacing;

            // 5. Wave Energy Reward
            SerializedProperty waveEnergyProp = property.FindPropertyRelative("waveEnergyReward");
            Rect energyRect = new Rect(position.x, y, position.width, lineHeight);
            EditorGUI.PropertyField(energyRect, waveEnergyProp, new GUIContent("Reward Lõi Năng Lượng"));
            y += lineHeight + spacing;

            // 6. List Enemy (5 Quái, 2 Hàng cố định)
            if (listEnemyProp != null)
            {
                Rect enemyHeaderRect = new Rect(position.x, y, position.width, lineHeight);
                listEnemyProp.isExpanded = EditorGUI.Foldout(enemyHeaderRect, listEnemyProp.isExpanded, $"Đội Hình Quái (2 Hàng Trước / Sau - Tối đa 5 Quái)", true);
                y += lineHeight + spacing;

                if (listEnemyProp.isExpanded)
                {
                    var allEnemies = (EnemyType[])Enum.GetValues(typeof(EnemyType));
                    string[] allNames = new string[allEnemies.Length];
                    for (int v = 0; v < allEnemies.Length; v++)
                    {
                        var cfg = EnemyStatsDatabase.GetConfig(allEnemies[v]);
                        allNames[v] = $"{cfg.Name} ({allEnemies[v]} - Hệ {ElementalHelper.GetVietnameseName(cfg.Element)})";
                    }

                    Rect sizeRect = new Rect(position.x, y, position.width, lineHeight);
                    int newSize = EditorGUI.IntField(sizeRect, "Số Loại Quái Trong Wave", listEnemyProp.arraySize);
                    if (newSize != listEnemyProp.arraySize && newSize >= 0)
                    {
                        listEnemyProp.arraySize = newSize;
                    }
                    y += lineHeight + spacing;

                    for (int i = 0; i < listEnemyProp.arraySize; i++)
                    {
                        SerializedProperty enemyItem = listEnemyProp.GetArrayElementAtIndex(i);
                        SerializedProperty nameProp = enemyItem.FindPropertyRelative("name");
                        SerializedProperty amtProp = enemyItem.FindPropertyRelative("Amount");
                        SerializedProperty rowProp = enemyItem.FindPropertyRelative("row");

                        EnemyType currentType = (EnemyType)nameProp.enumValueIndex;
                        int selectedIdx = Array.IndexOf(allEnemies, currentType);
                        if (selectedIdx < 0 && allEnemies.Length > 0)
                        {
                            selectedIdx = 0;
                            nameProp.enumValueIndex = (int)allEnemies[0];
                        }

                        // Name
                        Rect pRect = new Rect(position.x + 15f, y, position.width - 15f, lineHeight);
                        int newSelected = EditorGUI.Popup(pRect, $"Quái [{i}]", selectedIdx >= 0 ? selectedIdx : 0, allNames);
                        if (newSelected >= 0 && newSelected < allEnemies.Length)
                        {
                            nameProp.enumValueIndex = (int)allEnemies[newSelected];
                        }
                        y += lineHeight + spacing;

                        // Amount
                        Rect aRect = new Rect(position.x + 15f, y, position.width - 15f, lineHeight);
                        EditorGUI.PropertyField(aRect, amtProp, new GUIContent("Số Lượng (Amount)"));
                        y += lineHeight + spacing;

                        // Row (Front / Back)
                        Rect rRect = new Rect(position.x + 15f, y, position.width - 15f, lineHeight);
                        EditorGUI.PropertyField(rRect, rowProp, new GUIContent("Vị Trí Hàng (Front=Trước / Back=Sau)"));
                        y += lineHeight + spacing + 3f;
                    }
                }
            }
        }
        else
        {
            // DISCOVERY WAVE
            // 3. Event Type
            Rect evtRect = new Rect(position.x, y, position.width, lineHeight);
            EditorGUI.PropertyField(evtRect, evtProp, new GUIContent("Loại Sự Kiện (Discovery Event)"));
            y += lineHeight + spacing;

            // 4. Wave Energy Reward (Thường là 0 hoặc số nhận được khi mở rương)
            SerializedProperty waveEnergyProp = property.FindPropertyRelative("waveEnergyReward");
            Rect energyRect = new Rect(position.x, y, position.width, lineHeight);
            EditorGUI.PropertyField(energyRect, waveEnergyProp, new GUIContent("Reward Lõi Năng Lượng"));
            y += lineHeight + spacing;
        }

        EditorGUI.indentLevel--;
        EditorGUI.EndProperty();
    }
}
