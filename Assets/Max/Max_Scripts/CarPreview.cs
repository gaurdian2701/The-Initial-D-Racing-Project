using Car;
using UnityEngine;

public class CarPreview : MonoBehaviour
{

    GameObject _car;

    public void NewCarPreview(RacerData racerData)
    {
        if (_car != null) Destroy(_car);
        
        _car = Instantiate(racerData.carPrefab, transform);
        
        _car.transform.position = Vector3.zero;
        
        _car.GetComponent<CarController>().enabled = false;
        _car.GetComponent<Rigidbody>().isKinematic = true;

    }
}
