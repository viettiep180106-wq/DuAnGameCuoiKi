using UnityEngine;

[CreateAssetMenu(fileName = "BoardConfig", menuName = "ScriptableObjects/Config/BoardConfig")]
public class BoardConfigSO : ScriptableObject
{
    [Header("Board Dimensions")]
    [SerializeField] private int cols = 6;
    [SerializeField] private int rows = 6;
    [SerializeField] private float width = 720f;
    [SerializeField] private float height = 720f;
    [SerializeField] private float boardFrameWidth = 880f;
    [SerializeField] private float boardFrameHeight = 740f;

    [Header("Slot & Movement Settings")]
    [SerializeField] private Vector2 slotSize = new Vector2(100f, 100f);
    [SerializeField] private float swapDuration = 0.25f;
    [SerializeField] private float fallDuration = 0.35f;

    [Header("Board Animation (Slide In/Out)")]
    [SerializeField] private float hiddenPosY = -1600f;
    [SerializeField] private float activePosY = 160f;
    [SerializeField] private float slideDuration = 0.45f;

    public int Cols => cols;
    public int Rows => rows;
    public float Width => width;
    public float Height => height;
    public float BoardFrameWidth => boardFrameWidth;
    public float BoardFrameHeight => boardFrameHeight;
    public Vector2 SlotSize => slotSize;
    public float SwapDuration => swapDuration;
    public float FallDuration => fallDuration;
    public float HiddenPosY => hiddenPosY;
    public float ActivePosY => activePosY;
    public float SlideDuration => slideDuration;
}
