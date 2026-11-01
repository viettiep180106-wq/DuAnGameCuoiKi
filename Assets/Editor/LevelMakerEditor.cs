using UnityEngine;
using UnityEditor;
[CustomEditor(typeof(LevelMaker))]
[CanEditMultipleObjects]
public class LevelMakerEditor : Editor
{
    private int level = 0;
    private int step = 0;
    private Vector2Int size;
    private int heightGirdStoneStep;
    private float distance;
    private LevelMaker maker;
    private void OnEnable()
    {
        maker = (LevelMaker)target;
        if (maker != null && maker.GetInforSetUp() != null)
        {
            InforSetUp infor =  maker.GetInforSetUp();
            level = infor.level;
            step = infor.step;
            size = infor.size;
            heightGirdStoneStep = infor.heightGirdStoneStep;
            distance = infor.distance;
        }
    }
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space(20);// cách dọc giữa 2 dòng
        Color defaultColor = GUI.backgroundColor;
        // Label
        EditorGUILayout.BeginHorizontal();
        GUIStyle TextLabel = new GUIStyle(EditorStyles.label);
        TextLabel.fontSize = 15;
        TextLabel.fontStyle = FontStyle.Bold;
        TextLabel.normal.textColor = Color.white;
        GUILayout.Space(50);// cách trong 1 dòng ngang nếu không trong BeginHorizontal() còn ngoài thì same EditorGUILayout.Space(20)
        EditorGUILayout.LabelField("-----Tool Feature-----",TextLabel);
        EditorGUILayout.EndHorizontal();
        float originalWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 70f;
        EditorGUILayout.Space(20);
        EditorGUILayout.BeginVertical();
        level = EditorGUILayout.IntField("Level Index:", level,GUILayout.Width(220));
        GUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();
        EditorGUIUtility.labelWidth = 16f;
        size.x = EditorGUILayout.IntField("x:",size.x,GUILayout.Width(50));
        size.y = EditorGUILayout.IntField("y:",size.y,GUILayout.Width(50));
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(20);
        EditorGUILayout.BeginVertical();
        EditorGUIUtility.labelWidth = 70f;
        step = EditorGUILayout.IntField("Step Index:", step,GUILayout.Width(220));
        GUILayout.Space(5);
        EditorGUIUtility.labelWidth = 100f;
        heightGirdStoneStep = EditorGUILayout.IntField("Gird Stone y:", heightGirdStoneStep,GUILayout.Width(200));
        GUILayout.Space(5);
        distance = EditorGUILayout.FloatField("Distance Step:", distance,GUILayout.Width(200));
        EditorGUILayout.EndVertical();
        EditorGUIUtility.labelWidth = originalWidth;
        EditorGUILayout.Space(20);
        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = Color.green;
        if(GUILayout.Button("Get Infor Set Up", GUILayout.Width(150), GUILayout.Height(30)))
        {
            LevelMaker maker = (LevelMaker)target;
            InforSetUp infor = new InforSetUp(level,step,size,heightGirdStoneStep,distance);
            maker.SetInforSetUp(infor);
        }
        GUILayout.Space(20);
        GUI.backgroundColor = Color.cyan;
        if(GUILayout.Button("Reset Step", GUILayout.Width(150), GUILayout.Height(30)))
        {
            LevelMaker maker = (LevelMaker)target;
            maker.ResetStep();
            
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(20);
        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = Color.green;
        if(GUILayout.Button("Save Level", GUILayout.Width(150), GUILayout.Height(30)))
        {
            LevelMaker maker = (LevelMaker)target;
            maker.SaveDataLevel();
        }
        GUI.backgroundColor = Color.red;
        GUILayout.Space(20);
        if(GUILayout.Button("Load Level", GUILayout.Width(150), GUILayout.Height(30))){
            LevelMaker maker = (LevelMaker)target;
            maker.LoadDataLevel();
            InforSetUp infor = maker.GetInforSetUp();
            level = infor.level;
            size = infor.size;
            step = infor.step;
            heightGirdStoneStep = infor.heightGirdStoneStep;
            distance = infor.distance;
        }
        EditorGUILayout.EndHorizontal();
        GUI.backgroundColor = Color.green;
        EditorGUILayout.Space(20);
        EditorGUILayout.BeginHorizontal();
        if(GUILayout.Button("Save Step", GUILayout.Width(150), GUILayout.Height(30))){
            LevelMaker maker = (LevelMaker)target;
            maker.SaveDataStep();
        }
        GUI.backgroundColor = Color.yellow;
        GUILayout.Space(20);
        if(GUILayout.Button("Load Step", GUILayout.Width(150), GUILayout.Height(30))){
            LevelMaker maker = (LevelMaker)target;
            maker.LoadDataStep(step);
            InforSetUp infor = maker.GetInforSetUp();
            step = infor.step;
            heightGirdStoneStep = infor.heightGirdStoneStep;
            distance = infor.distance;
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(20);
        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = Color.red;
        GUILayout.Space(70);
        if(GUILayout.Button("Test Level", GUILayout.Width(200), GUILayout.Height(40)))
        {
            LevelMaker maker = (LevelMaker)target;
            maker.TestLevel();
        }
        EditorGUILayout.EndHorizontal();
        GUI.backgroundColor = defaultColor;
    }
}
