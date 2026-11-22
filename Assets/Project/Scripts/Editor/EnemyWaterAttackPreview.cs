using UnityEditor;

internal sealed class EnemyWaterAttackPreview : EnemyAttackPreviewWindow
{
    protected override string PreviewFolder => "EnemyWater";
    protected override string BridgeName => "waterPreview";
    protected override string ElementLabel => "Thủy";

    [MenuItem("Tools/FX/Enemy Water Attack Preview")]
    private static void Open() => GetWindow<EnemyWaterAttackPreview>("Enemy Water Attack Preview").Show();
}
