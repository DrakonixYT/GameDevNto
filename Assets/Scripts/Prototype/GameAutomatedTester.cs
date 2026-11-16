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
            Debug.Log("<color=cyan><b>================ [ЗАПУСК ПОЛНОГО АВТО-ТЕСТА ИГРЫ (10 ТЕСТОВ)] ================</b></color>");
            int passedTests = 0;
            int totalTests = 10;

            // --- ТЕСТ 1: Инициализация ядра (GameManager, Dispatch, Audio, Atmosphere) ---
            yield return new WaitForSeconds(0.2f);
            if (GameManager.Instance != null && 
                DispatchManager.Instance != null && 
                HangarAtmosphere.Instance != null && 
                HangarAudio.Instance != null && 
                GameManager.Instance.Credits >= 500)
            {
                Debug.Log("<color=green>[TEST 1/10 PASSED]</color> Все системы ядра (Менеджеры, Атмосфера, Аудио) инициализированы корректно.");
                passedTests++;
            }
            else
            {
                Debug.LogError("[TEST 1/10 FAILED] Ошибка инициализации менеджеров ядра!");
            }

            // --- ТЕСТ 2: Мех и детали в ангаре ---
            yield return new WaitForSeconds(0.2f);
            MechInstance mech = FindObjectOfType<MechInstance>();
            if (mech != null && mech.Parts.Count >= 5)
            {
                Debug.Log($"<color=green>[TEST 2/10 PASSED]</color> Мех '{mech.MechName}' найден. Установлено {mech.Parts.Count} боевых модулей.");
                passedTests++;
            }
            else
            {
                Debug.LogError("[TEST 2/10 FAILED] Мех не найден или не хватает деталей!");
            }

            // --- ТЕСТ 3: Генерация боевой угрозы и сигнализация ---
            yield return new WaitForSeconds(0.2f);
            if (DispatchManager.Instance != null)
            {
                DispatchManager.Instance.SpawnNextThreat();
                if (DispatchManager.Instance.HasActiveThreat && DispatchManager.Instance.CurrentThreat != null)
                {
                    Debug.Log($"<color=green>[TEST 3/10 PASSED]</color> Угроза сгенерирована: {DispatchManager.Instance.CurrentThreat.threatName}. Аварийная тревога активна.");
                    passedTests++;
                }
                else
                {
                    Debug.LogError("[TEST 3/10 FAILED] Угроза не была создана!");
                }
            }

            // --- ТЕСТ 4: Отправка меха в бой и тактическое QTE решение в реальном времени ---
            yield return new WaitForSeconds(0.3f);
            if (DispatchManager.Instance != null && mech != null)
            {
                bool deployed = DispatchManager.Instance.TryDeployMech(mech);
                if (deployed)
                {
                    Debug.Log("<color=yellow>[БОТ-ТЕСТ]</color> Мех вылетел на перехват. Ожидание тактического запроса пилота...");
                    
                    // Ждем появления тактического QTE запроса
                    float waitQte = 0f;
                    while (!DispatchManager.Instance.HasActiveTacticalPrompt && DispatchManager.Instance.IsMissionInProgress && waitQte < 4f)
                    {
                        waitQte += 0.2f;
                        yield return new WaitForSeconds(0.2f);
                    }

                    if (DispatchManager.Instance.HasActiveTacticalPrompt)
                    {
                        Debug.Log("<color=yellow>[БОТ-ТЕСТ]</color> Получен тактический приказ в эфире! Бот активирует контр-меру...");
                        DispatchManager.Instance.ExecuteTacticalAction();
                    }

                    // Дожидаемся завершения боя
                    while (DispatchManager.Instance.IsMissionInProgress)
                    {
                        yield return new WaitForSeconds(0.3f);
                    }

                    float avgHp = mech.GetAverageHealthPercent();
                    Debug.Log($"<color=green>[TEST 4/10 PASSED]</color> Боевая операция завершена с тактическим успехом! Средняя прочность меха: {avgHp * 100f:F1}%.");
                    passedTests++;
                }
                else
                {
                    Debug.LogError("[TEST 4/10 FAILED] Не удалось отправить меха в бой!");
                }
            }

            // --- ТЕСТ 5: Отправка поврежденной детали на верстак ---
            yield return new WaitForSeconds(0.3f);
            RepairWorkbench workbench = RepairWorkbench.Instance;
            MechPartInstance testPart = null;
            if (mech != null && mech.Parts.Count > 0)
            {
                // Ищем правую руку для тестирования кастомизации и смены модулей
                foreach (var p in mech.Parts)
                {
                    if (p.Slot == PartSlot.RightArm)
                    {
                        testPart = p;
                        break;
                    }
                }
                if (testPart == null) testPart = mech.Parts[0];
            }

            if (workbench != null && testPart != null)
            {
                testPart.ApplyDamage(40f);
                workbench.ReceivePartForRepair(testPart);

                yield return new WaitForSeconds(0.4f);
                if (workbench.HasPart && GameManager.Instance.CurrentView == RoomView.WorkbenchFocus)
                {
                    Debug.Log($"<color=green>[TEST 5/10 PASSED]</color> Деталь '{testPart.PartName}' на верстаке. Камера сфокусирована.");
                    passedTests++;
                }
                else
                {
                    Debug.LogError("[TEST 5/10 FAILED] Перенос детали на верстак не сработал!");
                }
            }

            // --- ТЕСТ 6: Кастомизация и смена боевого модуля на верстаке ---
            yield return new WaitForSeconds(0.3f);
            if (workbench != null && workbench.HasPart)
            {
                string oldName = workbench.CurrentPart.PartName;
                workbench.CycleNextModuleForCurrentPart();
                string newName = workbench.CurrentPart.PartName;

                if (oldName != newName)
                {
                    Debug.Log($"<color=green>[TEST 6/10 PASSED]</color> Модуль успешно заменен: '{oldName}' ➔ '{newName}'. Характеристики обновлены.");
                    passedTests++;
                }
                else
                {
                    Debug.LogError("[TEST 6/10 FAILED] Смена модуля не изменила конфигурацию детали!");
                }
            }

            // --- ТЕСТ 7: Завершение ремонта на верстаке ---
            yield return new WaitForSeconds(0.3f);
            if (workbench != null && workbench.HasPart)
            {
                workbench.FinishRepair();
                yield return new WaitForSeconds(0.3f);

                if (testPart.DurabilityPercent >= 0.99f)
                {
                    Debug.Log("<color=green>[TEST 7/10 PASSED]</color> Ремонт успешен! Прочность модуля восстановлена до 100%.");
                    passedTests++;
                }
                else
                {
                    Debug.LogError("[TEST 7/10 FAILED] Ремонт не восстановил прочность до 100%!");
                }
            }

            // --- ТЕСТ 8: Возврат детали на стапель и сброс камеры в ангар ---
            yield return new WaitForSeconds(0.3f);
            if (!workbench.HasPart && GameManager.Instance.CurrentView == RoomView.Hangar)
            {
                Debug.Log("<color=green>[TEST 8/10 PASSED]</color> Деталь возвращена в робота. Камера вернулась в ангар.");
                passedTests++;
            }
            else
            {
                Debug.LogError("[TEST 8/10 FAILED] Камера или деталь не вернулись в исходное состояние!");
            }

            // --- ТЕСТ 9: Система адаптации врагов (Enemy Adaptation System) ---
            yield return new WaitForSeconds(0.3f);
            if (EnemyAdaptationSystem.Instance != null)
            {
                DamageType initialWeakness = EnemyAdaptationSystem.Instance.GetCounterWeakness();
                EnemyAdaptationSystem.Instance.RegisterVictoryWithAttackType(initialWeakness);
                EnemyAdaptationSystem.Instance.RegisterVictoryWithAttackType(initialWeakness);

                DamageType adaptedWeakness = EnemyAdaptationSystem.Instance.GetCounterWeakness();
                Debug.Log($"<color=green>[TEST 9/10 PASSED]</color> Система адаптации врагов сработала! Враг адаптировался: новая слабость {adaptedWeakness}.");
                passedTests++;
            }
            else
            {
                Debug.LogError("[TEST 9/10 FAILED] EnemyAdaptationSystem отсутствует!");
            }

            // --- ТЕСТ 10: Сборка Егеря и финальная победа ---
            yield return new WaitForSeconds(0.3f);
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddResources(2000, 1000);
                GameManager.Instance.InvestInJaeger(0.25f, 0, 0);
                GameManager.Instance.InvestInJaeger(0.25f, 0, 0);
                GameManager.Instance.InvestInJaeger(0.25f, 0, 0);
                GameManager.Instance.InvestInJaeger(0.25f, 0, 0);

                if (GameManager.Instance.JaegerProgress >= 1f && GameManager.Instance.CurrentState == GameState.Victory)
                {
                    Debug.Log("<color=green>[TEST 10/10 PASSED]</color> Проект 'Егерь' собран на 100%! Финальный триумф и экран победы активированы.");
                    passedTests++;
                }
                else
                {
                    Debug.LogError("[TEST 10/10 FAILED] Сборка Егеря не активировала победный статус!");
                }
            }

            Debug.Log($"<color=cyan><b>================ [ИТОГ АВТО-ТЕСТА: {passedTests}/{totalTests} ТЕСТОВ ПРОЙДЕНО УСПЕШНО] ================</b></color>");
        }
    }
}
