using UnityEditor;

internal sealed class EnemyWoodAttackPreview : EnemyAttackPreviewWindow
{
    protected override string PreviewFolder => "EnemyWood";
    protected override string BridgeName => "woodPreview";
    protected override string ElementLabel => "Mộc";

    [MenuItem("Tools/FX/Enemy Wood Attack Preview")]
    private static void Open() => GetWindow<EnemyWoodAttackPreview>("Enemy Wood Attack Preview").Show();
}
