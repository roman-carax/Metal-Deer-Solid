using UnityEngine;

public class Projectile : MonoBehaviour
{

    Vector3 velocity;
    
    
    public Projectile(object owner){
        velocity = owner.Quaternion;
    }

    void Update()
    {
        
    }
}
