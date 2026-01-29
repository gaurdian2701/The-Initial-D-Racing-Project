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
        _car.GetComponent<Rigidbody>().isKinematic = true;
        
        MonoBehaviour[] components = _car.GetComponents<MonoBehaviour>();
        AudioSource[] sources = _car.GetComponents<AudioSource>();

        foreach (AudioSource source in sources)
        {
            source.volume = 0;
        }
        
        foreach (MonoBehaviour component in components)
        {
            component.enabled = false;
        }
    }
}
