using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TetrisPiece : MonoBehaviour
{
    public int Distribution = 10;

    private Transform[] dots; 

    public IEnumerable<Transform> GetChildren()
    {
        if( dots == null )
        {
            // Finds all dots
            var renderers = GetComponentsInChildren<MeshRenderer>();
            dots = renderers.Select( x => x.transform ).ToArray();
        }

        return dots;
    }

    public TetrisPiece CreateInstance( Transform parent )
    {
        var obj = Instantiate( gameObject, parent );
        return obj.GetComponent<TetrisPiece>();
    }
}
