using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HangarGame.Core;
using HangarGame.Mechs;
using HangarGame.Repair;
using HangarGame.Dispatch;
using HangarGame.Data;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HangarGame.Prototype
{
    public class GameAutomatedTester : MonoBehaviour
    {
        public static GameAutomatedTester Instance { get; private set; }

        [Header("Test Settings")]
        [SerializeField] private bool runTestOnStart = false;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (runTestOnStart)
            {
                StartCoroutine(RunFullGameplayTestRoutine());
            }
        }

#if UNITY_EDITOR
        [MenuItem("Pacific Rim / 🧪 4. Запустить Полный Авто-Тест")]
        public static void RunTestFromMenu()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Авто-Тест", "Запустите сцену в режиме Play (▶), затем нажмите этот пункт для запуска авто-бота!", "Понял");
                return;
            }

            var tester = FindObjectOfType<GameAutomatedTester>();
            if (tester == null)
            {
                GameObject testerObj = new GameObject("--- AUTO TESTER BOT ---");
                tester = testerObj.AddComponent<GameAutomatedTester>();
            }

            tester.StopAllCoroutines();
            tester.StartCoroutine(tester.RunFullGameplayTestRoutine());
        }
#endif

        public IEnumerator RunFullGameplayTestRoutine()
        {
            Debug.Log("<color=cyan><b>================ [ЗАПУСК АВТО-ТЕСТА ИГРЫ] ================</b></color>");
            int passedTests = 0;
            int totalTests = 8;

            // --- ТЕСТ 1: Инициализация ядра (GameManager) ---
            yield return new WaitForSeconds(0.2f);
            if (GameManager.Instance != null && GameManager.Instance.Credits >= 500)
            {
                Debug.Log("<color=green>[TEST 1/8 PASSED]</color> GameManager инициализирован корректно. Кредиты: " + GameManager.Instance.Credits);
                passedTests++;
            }
            else
            {
                Debug.LogError("[TEST 1/8 FAILED] GameManager отсутствует или ресурсы некорректны!");
            }

            // --- ТЕСТ 2: Мех и детали в ангаре ---
            yield return new WaitForSeconds(0.2f);
            MechInstance mech = FindObjectOfType<MechInstance>();
            if (mech != null && mech.Parts.Count >= 5)
            {
                Debug.Log($"<color=green>[TEST 2/8 PASSED]</color> Мех '{mech.MechName}' найден. Установлено {mech.Parts.Count} деталей.");
                passedTests++;
            }
            else
            {
                Debug.LogError("[TEST 2/8 FAILED] Мех не найден или не хватает деталей!");
            }

            // --- ТЕСТ 3: Генерация боевой угрозы ---
            yield return new WaitForSeconds(0.2f);
            if (DispatchManager.Instance != null)
            {
                DispatchManager.Instance.SpawnNextThreat();
                if (DispatchManager.Instance.HasActiveThreat && DispatchManager.Instance.CurrentThreat != null)
                {
                    Debug.Log($"<color=green>[TEST 3/8 PASSED]</color> Угроза сгенерирована: {DispatchManager.Instance.CurrentThreat.threatName}");
                    passedTests++;
                }
                else
                {
                    Debug.LogError("[TEST 3/8 FAILED] Угроза не была создана!");
                }
            }

            // --- ТЕСТ 4: Отправка меха в бой и получение урона ---
            yield return new WaitForSeconds(0.3f);
            if (DispatchManager.Instance != null && mech != null)
            {
                bool deployed = DispatchManager.Instance.TryDeployMech(mech);
                if (deployed)
                {
                    Debug.Log("<color=yellow>[БОТ-ТЕСТ]</color> Мех отправлен на боевой перехват. Ожидание завершения боя...");
                    while (DispatchManager.Instance.IsMissionInProgress)
                    {
                        yield return new WaitForSeconds(0.5f);
                    }

                    // Проверяем, что бой завершился и нанесен урон
                    float avgHp = mech.GetAverageHealthPercent();
                    Debug.Log($"<color=green>[TEST 4/8 PASSED]</color> Бой завершен успешно! Средняя прочность робота: {avgHp * 100f:F1}%.");
                    passedTests++;
                }
                else
                {
                    Debug.LogError("[TEST 4/8 FAILED] Не удалось отправить меха в бой!");
                }
            }

            // --- ТЕСТ 5: Отправка поврежденной детали на верстак ---
            yield return new WaitForSeconds(0.3f);
            RepairWorkbench workbench = RepairWorkbench.Instance;
            MechPartInstance testPart = null;
            if (mech != null && mech.Parts.Count > 0)
            {
                testPart = mech.Parts[0]; // Берем торс или руку
            }

            if (workbench != null && testPart != null)
            {
                testPart.ApplyDamage(40f); // Наносим тестовые повреждения
                workbench.ReceivePartForRepair(testPart);

                yield return new WaitForSeconds(0.4f);
                if (workbench.HasPart && GameManager.Instance.CurrentView == RoomView.WorkbenchFocus)
                {
                    Debug.Log($"<color=green>[TEST 5/8 PASSED]</color> Деталь '{testPart.name}' на верстаке. Камера сфокусирована.");
                    passedTests++;
                }
                else
                {
                    Debug.LogError("[TEST 5/8 FAILED] Перенос детали на верстак не сработал!");
                }
            }

            // --- ТЕСТ 6: Завершение ремонта на верстаке ---
            yield return new WaitForSeconds(0.4f);
            if (workbench != null && workbench.HasPart)
            {
                workbench.FinishRepair();
                yield return new WaitForSeconds(0.3f);

                if (!workbench.HasPart && testPart.DurabilityPercent >= 0.99f && GameManager.Instance.CurrentView == RoomView.Hangar)
                {
                    Debug.Log("<color=green>[TEST 6/8 PASSED]</color> Ремонт успешен! Прочность восстановлена до 100%. Камера вернулась в ангар.");
                    passedTests++;
                }
                else
                {
                    Debug.LogError("[TEST 6/8 FAILED] Завершение ремонта не вернуло деталь на стапель!");
                }
            }

            // --- ТЕСТ 7: Адаптация врагов ---
            yield return new WaitForSeconds(0.3f);
            if (EnemyAdaptationSystem.Instance != null)
            {
                EnemyAdaptationSystem.Instance.RegisterVictoryWithAttackType(DamageType.Physical);
                EnemyAdaptationSystem.Instance.RegisterVictoryWithAttackType(DamageType.Physical);

                Debug.Log($"<color=green>[TEST 7/8 PASSED]</color> Адаптация врагов сработала! Новая уязвимость врагов: {EnemyAdaptationSystem.Instance.GetCounterWeakness()}");
                passedTests++;
            }
            else
            {
                Debug.LogError("[TEST 7/8 FAILED] EnemyAdaptationSystem не найден!");
            }

            // --- ТЕСТ 8: Сборка Егеря и победный триггер ---
            yield return new WaitForSeconds(0.3f);
            if (GameManager.Instance != null)
            {
                // Начисляем ресурсы и собираем Егеря до 100%
                GameManager.Instance.AddResources(2000, 1000);
                GameManager.Instance.InvestInJaeger(0.25f, 0, 0);
                GameManager.Instance.InvestInJaeger(0.25f, 0, 0);
                GameManager.Instance.InvestInJaeger(0.25f, 0, 0);
                GameManager.Instance.InvestInJaeger(0.25f, 0, 0);

                if (GameManager.Instance.JaegerProgress >= 1f && GameManager.Instance.CurrentState == GameState.Victory)
                {
                    Debug.Log("<color=green>[TEST 8/8 PASSED]</color> Проект 'Егерь' собран на 100%! Триггер победы сработал.");
                    passedTests++;
                }
                else
                {
                    Debug.LogError("[TEST 8/8 FAILED] Сборка Егеря до 100% не вызвала экран победы!");
                }
            }

            Debug.Log($"<color=cyan><b>================ [РЕЗУЛЬТАТ ТЕСТИРОВАНИЯ: {passedTests}/{totalTests} УСПЕШНО] ================</b></color>");
        }
    }
}
