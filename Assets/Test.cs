using UnityEngine;

public class Test : MonoBehaviour
{
    public Test(GameObject foo)
    {
        foo.GetComponent<Test>();
        Physics.Raycast(new Ray(Vector3.one, Vector3.up));
    }
}