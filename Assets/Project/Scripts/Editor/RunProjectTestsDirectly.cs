using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;

public static class RunProjectTestsDirectly
{
    [MenuItem("Tools/Run Custom Project Tests Now")]
    public static void RunTests()
    {
        int passed = 0;
        int failed = 0;

        Type[] testTypes = new Type[]
        {
            typeof(Board6x6Tests),
            typeof(CombatBackgroundScrollerTests),
            typeof(ElementalSystemTests),
            typeof(GameplayProgressionTests),
            typeof(LevelDataSystemTests),
            typeof(LevelProgressBarTests),
            typeof(RuneEventSystemTests),
            typeof(SkillLoadoutSystemTests),
            typeof(TurnBasedCombatTests),
            typeof(GameLoopAndCleanupTests)
        };

        foreach (var t in testTypes)
        {
            object instance = Activator.CreateInstance(t);
            MethodInfo setup = t.GetMethod("SetUp") ?? t.GetMethod("Setup");
            MethodInfo teardown = t.GetMethod("TearDown") ?? t.GetMethod("Teardown");

            foreach (var m in t.GetMethods())
            {
                if (m.GetCustomAttribute<TestAttribute>() != null)
                {
                    try
                    {
                        setup?.Invoke(instance, null);
                        m.Invoke(instance, null);
                        teardown?.Invoke(instance, null);
                        passed++;
                        Debug.Log($"<color=#00ff88>[PASS]</color> {t.Name}.{m.Name}");
                    }
                    catch (TargetInvocationException ex)
                    {
                        failed++;
                        Debug.LogError($"<color=#ff4444>[FAIL]</color> {t.Name}.{m.Name}: {ex.InnerException?.Message}\n{ex.InnerException?.StackTrace}");
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        Debug.LogError($"<color=#ff4444>[FAIL]</color> {t.Name}.{m.Name}: {ex.Message}\n{ex.StackTrace}");
                    }
                }
            }
        }

        Debug.Log($"<color=#00e5ff><b>=== TEST SUMMARY ===</b></color> Passed: <b>{passed}</b>, Failed: <b>{failed}</b>");
    }
}
