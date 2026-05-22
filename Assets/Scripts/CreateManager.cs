using UnityEngine;

public class CreateManager : MonoBehaviour
{
   
    public GameObject objectToSpawn;

  
    public Transform spawnLocation;

    public void SpawnObject()
    {
        
        if (objectToSpawn == null)
        {
            Debug.LogError("Ошибка: не назначен объект для спауна!");
            return;
        }
        Vector3 spawnPosition;
        if (spawnLocation != null)
        {
           
            spawnPosition = spawnLocation.position;
        }
        else
        {
            
            spawnPosition = Camera.main.transform.position +
                           Camera.main.transform.forward * 2f;
            spawnPosition.y = 1f; 
        }

        GameObject newObject = Instantiate(
            objectToSpawn,
            spawnPosition,
            objectToSpawn.transform.rotation
        );
        if (newObject.GetComponent<Rigidbody>() == null)
        {
            newObject.AddComponent<Rigidbody>();
        }
        newObject.name = objectToSpawn.name + "_Clone";

        Debug.Log("Создан объект: " + newObject.name);
    }
    public void SpawnObjectAtPosition(Vector3 position)
    {
        if (objectToSpawn != null)
        {
            Instantiate(objectToSpawn, position, Quaternion.identity);
        }
    }

}
