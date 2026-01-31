using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;


public class LevelManager : MonoBehaviour
{
    private enum LevelState
    {
        AutoMove,
        QteInput,
        Intermission,
        LevelComplete
    }

    [Header("References")]
    public Transform player;
    public AreaTracker areaTracker;
    public StepMover stepMover;
    public QteController qteController;
    public QteSpriteDisplay qteDisplay;
    public MaskController maskController;
    public Transform goal;
    public TMP_Text currentQueueText;

    [Header("Auto Move")]
    public Vector3 autoStepDirection = Vector3.forward;
    [Header("QTE Timer")]
    public float qteTimeLimit = 5f;
    public Image qteTimerFill;
    public bool requireQteToUseAreaSequence = true;

    [Header("Win Conditions")]
    public float goalRadius = 0.5f;
    public bool bossDefeated = false;

    [Header("Restart")]
    public float restartDelay = 1f;

    private LevelState state = LevelState.Intermission;
    private Vector3 autoMoveDirection = Vector3.forward;
    private bool autoMoveQueued = false;
    private readonly List<QteController.QteKey> currentMoveSequence = new List<QteController.QteKey>();
    private int currentMoveIndex = 0;
    private string currentMoveAreaName = string.Empty;
    private string activeQteAreaName = string.Empty;
    private readonly HashSet<string> qteCompletedAreas = new HashSet<string>();
    private float previousTimeScale = 1f;
    private bool qtePauseActive = false;
    private float qteTimeRemaining = 0f;
    private bool qteTimerActive = false;
    private bool restartQueued = false;

    void Start()
    {
        if (player == null)
        {
            Debug.LogError("LevelManager: Player reference is missing.");
            enabled = false;
            return;
        }

        if (stepMover == null)
        {
            Debug.LogError("LevelManager: StepMover reference is missing.");
            enabled = false;
            return;
        }

        if (qteController == null)
        {
            Debug.LogError("LevelManager: QteController reference is missing.");
            enabled = false;
            return;
        }

        if (areaTracker == null)
        {
            Debug.LogError("LevelManager: AreaTracker reference is missing.");
            enabled = false;
            return;
        }

        if (stepMover.target == null)
        {
            stepMover.target = player;
        }

        if (stepMover.maskController == null)
        {
            stepMover.maskController = maskController;
        }

        if (maskController != null)
        {
            if (maskController.maskCollider == null)
            {
                maskController.maskCollider = maskController.GetComponent<BoxCollider>();
            }
            if (maskController.playerCollider == null)
            {
                maskController.playerCollider = player.GetComponent<CapsuleCollider>();
            }
            maskController.UpdateBoxSize();
        }

        if (areaTracker.playerCollider == null)
        {
            areaTracker.playerCollider = player.GetComponent<CapsuleCollider>();
        }

        areaTracker.RefreshCurrent();
        autoMoveDirection = autoStepDirection.normalized;
        previousTimeScale = Time.timeScale;
        UpdateCurrentQueueText(string.Empty, null);
    }

    // Update is called once per frameBumped
    void Update()
    {
        if (state == LevelState.LevelComplete)
        {
            return;
        }

        if (IsLevelComplete())
        {
            state = LevelState.LevelComplete;
            Debug.Log("Level complete.");
            return;
        }

        switch (state)
        {
            case LevelState.AutoMove:
                HandleAutoMove();
                break;
            case LevelState.QteInput:
                HandleQteInput();
                UpdateQteTimer();
                break;
            case LevelState.Intermission:
                HandleIntermission();
                break;
        }
    }

    private void HandleAutoMove()
    {
        if (stepMover.IsMoving || autoMoveQueued)
        {
            return;
        }

        autoMoveQueued = true;
        StartCoroutine(AutoMoveStep());
    }

    private IEnumerator AutoMoveStep()
    {
        UpdateMoveSequence();
        if (currentMoveSequence.Count > 0)
        {
            currentMoveIndex = 0;
            for (int i = 0; i < currentMoveSequence.Count; i++)
            {
                Vector3 direction = KeyToDirection(QteController.ToKeyCode(currentMoveSequence[currentMoveIndex]));
                currentMoveIndex = (currentMoveIndex + 1) % currentMoveSequence.Count;
                yield return StartCoroutine(stepMover.MoveStep(direction));

                string areaName;
                if (areaTracker.EnteredNewArea(out areaName))
                {
                    autoMoveQueued = false;
                    StartQte(areaName);
                    yield break;
                }
            }
        }
        else
        {
            yield return StartCoroutine(stepMover.MoveStep(autoMoveDirection));

            string areaName;
            if (areaTracker.EnteredNewArea(out areaName))
            {
                autoMoveQueued = false;
                StartQte(areaName);
                yield break;
            }
        }

        autoMoveQueued = false;
        state = LevelState.Intermission;
    }

    private void StartQte(string areaName)
    {
        state = LevelState.QteInput;
        activeQteAreaName = areaName;
        AudioManager.Instance.PlaySfx("NewAreaEntered");
        qteController.StartForArea(areaName);
        if (qteDisplay != null)
        {
            qteDisplay.ShowSequence(qteController.Sequence);
        }

        SetQtePause(true);
        StartQteTimer();
    }

    private void HandleQteInput()
    {
        KeyCode? input = GetWasdInput();
        if (input == null)
        {
            return;
        }

        bool completed;
        int matchedIndex;
        bool correct = qteController.TryHandle(input.Value, out completed, out matchedIndex);
        if (!correct)
        {
            Debug.Log("QTE failed. Restarting.");
            qteController.ResetProgress();
            if (qteDisplay != null)
            {
                qteDisplay.ShowSequence(qteController.Sequence);
            }
            return;
        }

        if (qteDisplay != null)
        {
            qteDisplay.HideIndex(matchedIndex);
        }

        if (completed)
        {
            if (!string.IsNullOrEmpty(activeQteAreaName))
            {
                qteCompletedAreas.Add(activeQteAreaName);
            }
            StopQteTimer();
            SetQtePause(false);
            state = LevelState.Intermission;
        }
    }

    private void HandleIntermission()
    {
        KeyCode? input = GetWasdInput();
        if (input != null && maskController != null)
        {
            maskController.TryMove(KeyToDirection(input.Value));
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            AudioManager.Instance.PlaySfx("SpaceConfirm");
            state = LevelState.AutoMove;
        }
    }

    private bool IsLevelComplete()
    {
        if (goal != null && Vector3.Distance(player.position, goal.position) <= goalRadius)
        {
            return true;
        }

        return bossDefeated;
    }

    private static KeyCode? GetWasdInput()
    {
        if (Input.GetKeyDown(KeyCode.W)) return KeyCode.W;
        if (Input.GetKeyDown(KeyCode.A)) return KeyCode.A;
        if (Input.GetKeyDown(KeyCode.S)) return KeyCode.S;
        if (Input.GetKeyDown(KeyCode.D)) return KeyCode.D;
        return null;
    }

    private static Vector3 KeyToDirection(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.W:
                return Vector3.forward;
            case KeyCode.S:
                return Vector3.back;
            case KeyCode.A:
                return Vector3.left;
            default:
                return Vector3.right;
        }
    }

    private void UpdateMoveSequence()
    {
        string areaName = areaTracker != null ? areaTracker.GetAreaNameNow() : string.Empty;
        if (areaName == currentMoveAreaName)
        {
            return;
        }

        currentMoveAreaName = areaName;
        currentMoveSequence.Clear();
        currentMoveIndex = 0;

        if (!string.IsNullOrEmpty(areaName) && qteController != null)
        {
            if (requireQteToUseAreaSequence && !qteCompletedAreas.Contains(areaName))
            {
                UpdateCurrentQueueText(areaName, qteController.GetSequenceForArea(areaName));
                return;
            }

            List<QteController.QteKey> sequence = qteController.GetSequenceForArea(areaName);
            if (sequence != null && sequence.Count > 0)
            {
                currentMoveSequence.AddRange(sequence);
            }

            UpdateCurrentQueueText(areaName, sequence);
            return;
        }

        UpdateCurrentQueueText(string.Empty, null);
    }

    private void UpdateCurrentQueueText(string areaName, List<QteController.QteKey> sequence)
    {
        if (currentQueueText == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(areaName) || sequence == null || sequence.Count == 0)
        {
            currentQueueText.text = "Queue: Up";
            return;
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append("Queue: ");
        for (int i = 0; i < sequence.Count; i++)
        {
            sb.Append(QteKeyToArrow(sequence[i]));
            if (i < sequence.Count - 1)
            {
                sb.Append(" ");
            }
        }

        currentQueueText.text = sb.ToString();
    }

    private static string QteKeyToArrow(QteController.QteKey key)
    {
        switch (key)
        {
            case QteController.QteKey.W:
                return "↑";
            case QteController.QteKey.A:
                return "←";
            case QteController.QteKey.S:
                return "↓";
            case QteController.QteKey.D:
                return "→";
            default:
                return "?";
        }
    }


    private void SetQtePause(bool paused)
    {
        if (paused && !qtePauseActive)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            qtePauseActive = true;
        }
        else if (!paused && qtePauseActive)
        {
            Time.timeScale = previousTimeScale;
            qtePauseActive = false;
        }
    }

    private void StartQteTimer()
    {
        AudioManager.Instance.PlaySfx("CountDown");
        qteTimeRemaining = Mathf.Max(0.1f, qteTimeLimit);
        qteTimerActive = true;
        SetQteTimerVisible(true);
        UpdateQteTimerFill();
    }

    private void StopQteTimer()
    {
        qteTimerActive = false;
        SetQteTimerVisible(false);
    }

    private void UpdateQteTimer()
    {
        if (!qteTimerActive)
        {
            return;
        }

        qteTimeRemaining -= Time.unscaledDeltaTime;
        UpdateQteTimerFill();

        if (qteTimeRemaining <= 0f)
        {
            RestartLevel();
        }
    }

    private void UpdateQteTimerFill()
    {
        if (qteTimerFill == null || qteTimeLimit <= 0f)
        {
            return;
        }

        float normalized = Mathf.Clamp01(qteTimeRemaining / qteTimeLimit);
        qteTimerFill.fillAmount = normalized;
    }

    private void SetQteTimerVisible(bool visible)
    {
        if (qteTimerFill != null)
        {
            qteTimerFill.enabled = visible;
        }
    }

    public void RestartLevel()
    {
        if (restartQueued)
        {
            return;
        }

        AudioManager.Instance.PlaySfx("Dead");
        Debug.Log("DEADPLAYED");

        if (restartDelay <= 0f)
        {
            SetQtePause(false);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }

        StartCoroutine(RestartLevelAfterDelay());
    }

    private IEnumerator RestartLevelAfterDelay()
    {
        restartQueued = true;
        SetQtePause(false);
        yield return new WaitForSecondsRealtime(restartDelay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnDisable()
    {
        if (qtePauseActive)
        {
            Time.timeScale = previousTimeScale;
            qtePauseActive = false;
        }
    }
}
