using System.Collections;
using System.Collections.Generic;
using Unity.Android.Gradle.Manifest;
using UnityEngine;
public class EnemyController : MonoBehaviour
{
    public static EnemyController instance;
    [SerializeField]public Transform target;
    [SerializeField]private Dictionary<EnemyType,EnemyBase>listEnemy = new Dictionary<EnemyType, EnemyBase>();
    public int numEnemy = 0;
    private void Awake()
    {
        instance = this;
        LoadPrefabFromResource();
    }
    public void LoadPrefabFromResource()
    {
        string path = "Prefab/Base/Enemy";
        EnemyBase[] request = Resources.LoadAll<EnemyBase>(path);
        foreach(EnemyBase enemy in request)
        {
            listEnemy.Add(enemy.type,enemy);
        }
    }
    public void SetUpEnemyInStep(List<Enemy>  dataList)
    {
        foreach(Enemy e in dataList)
        {
            StartCoroutine(SpawnEnemyInStep(e.GetNum(),listEnemy[e.GetName()],e.GetDistance()));
        }
    }
    public IEnumerator SpawnEnemyInStep(int amount,EnemyBase enemyBase,float distance)
    {
        yield return new WaitForSeconds(1f);
        for(int i = 0; i < amount; i++)
        {
            Spawn(enemyBase,distance);
            numEnemy++;
        }
    }
    public void Spawn(EnemyBase e,float distance)
    {
        GameObject enemyObj = ObjectPoolingManager.Instance.Spawn(e.gameObject, Vector3.zero, Quaternion.identity);
        EnemyBase enemy = enemyObj.GetComponent<EnemyBase>();
        enemy.transform.SetParent(transform);
        float y = Random.Range(1f, 3f);
        enemy.transform.localPosition = new Vector3(target.transform.localPosition.x - distance, y, 0); 
        enemy.Trans(enemy.transform.localPosition + new Vector3(1.5f,y,0));
    }
    public void StartActive()
    {
        EnemyBase[] listEnemy = GetComponentsInChildren<EnemyBase>();
        foreach(EnemyBase enemy in listEnemy)
        {
            enemy.Trans(target.position + new Vector3(-0.5f,0,0));
        }
    }
    
}
