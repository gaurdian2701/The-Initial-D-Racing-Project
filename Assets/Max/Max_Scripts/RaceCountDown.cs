using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaceCountDown : MonoBehaviour
{
    [SerializeField] private float waitTime = 3f;
    
    private float _timeLeft;
    bool _counting = false;

    public void StartCountDown()
    {
        _timeLeft  = waitTime;
        _counting = true;
        ChangePauseStateOnCars(true);
    }

    // Update is called once per frame
    void Update()
    {
        if (_counting)
        {
            _timeLeft -= Time.deltaTime;

            if (_timeLeft <= 0)
            {
                _counting = false;
                ChangePauseStateOnCars(false);
            }
        }
    }
    
    
    public void ChangePauseStateOnCars(bool pause)
    {
        Rigidbody[] bodies = FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);

        foreach (Rigidbody rb in bodies)
        {
            rb.isKinematic = pause;
        }
        
    }

   
    
}
