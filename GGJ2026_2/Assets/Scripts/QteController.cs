using System.Collections.Generic;
using UnityEngine;

public class QteController : MonoBehaviour
{
    public enum QteKey
    {
        W,
        A,
        S,
        D
    }

    [System.Serializable]
    public class QteAreaEntry
    {
        public string areaName;
        public List<QteKey> sequence = new List<QteKey>();
    }

    public List<QteAreaEntry> areaSequences = new List<QteAreaEntry>();
    public List<QteKey> defaultSequence = new List<QteKey>();

    private readonly List<QteKey> sequence = new List<QteKey>();
    private int progress = 0;

    public IReadOnlyList<QteKey> Sequence => sequence;

    public void StartForArea(string areaName)
    {
        sequence.Clear();
        progress = 0;

        List<QteKey> fixedSequence = GetSequenceForArea(areaName);
        if (fixedSequence != null && fixedSequence.Count > 0)
        {
            sequence.AddRange(fixedSequence);
        }
        else if (defaultSequence != null && defaultSequence.Count > 0)
        {
            sequence.AddRange(defaultSequence);
        }

        Debug.Log("QTE: " + SequenceToString(sequence));
    }

    public bool TryHandle(KeyCode input, out bool completed, out int matchedIndex)
    {
        completed = false;
        matchedIndex = -1;
        if (sequence.Count == 0)
        {
            return false;
        }

        if (!IsMatch(input, sequence[progress]))
        {
            return false;
        }

        progress++;
        matchedIndex = progress - 1;
        completed = progress >= sequence.Count;
        return true;
    }

    public void ResetProgress()
    {
        progress = 0;
    }

    public List<QteKey> GetSequenceForArea(string areaName)
    {
        if (string.IsNullOrEmpty(areaName))
        {
            return null;
        }

        for (int i = 0; i < areaSequences.Count; i++)
        {
            if (areaSequences[i] != null && areaSequences[i].areaName == areaName)
            {
                return areaSequences[i].sequence;
            }
        }

        return null;
    }

    private static string SequenceToString(List<QteKey> list)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < list.Count; i++)
        {
            sb.Append(list[i]);
            if (i < list.Count - 1)
            {
                sb.Append(" ");
            }
        }
        return sb.ToString();
    }

    private static bool IsMatch(KeyCode input, QteKey key)
    {
        switch (key)
        {
            case QteKey.W:
                AudioManager.Instance.PlaySfx("QTE_A");
                return input == KeyCode.W;
            case QteKey.A:
                AudioManager.Instance.PlaySfx("QTE_B");
                return input == KeyCode.A;
            case QteKey.S:
                AudioManager.Instance.PlaySfx("QTE_C");
                return input == KeyCode.S;
            default:
                AudioManager.Instance.PlaySfx("QTE_D");
                return input == KeyCode.D;
        }
    }

    public static KeyCode ToKeyCode(QteKey key)
    {
        switch (key)
        {
            case QteKey.W:
                return KeyCode.W;
            case QteKey.A:
                return KeyCode.A;
            case QteKey.S:
                return KeyCode.S;
            default:
                return KeyCode.D;
        }
    }
}
