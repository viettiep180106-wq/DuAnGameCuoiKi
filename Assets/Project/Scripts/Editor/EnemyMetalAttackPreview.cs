using UnityEditor;

internal sealed class EnemyMetalAttackPreview : EnemyAttackPreviewWindow
{
    protected override string PreviewFolder => "EnemyMetal";
    protected override string BridgeName => "metalPreview";
    protected override string ElementLabel => "Kim";

    [MenuItem("Tools/FX/Enemy Metal Attack Preview")]
    private static void Open() => GetWindow<EnemyMetalAttackPreview>("Enemy Metal Attack Preview").Show();
}
