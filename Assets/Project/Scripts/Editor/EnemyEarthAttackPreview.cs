using UnityEditor;

internal sealed class EnemyEarthAttackPreview : EnemyAttackPreviewWindow
{
    protected override string PreviewFolder => "EnemyEarth";
    protected override string BridgeName => "earthPreview";
    protected override string ElementLabel => "Thổ";

    [MenuItem("Tools/FX/Enemy Earth Attack Preview")]
    private static void Open() => GetWindow<EnemyEarthAttackPreview>("Enemy Earth Attack Preview").Show();
}
