using UnityEngine;

public class TetrisCamera : MonoBehaviour
{
    [Tooltip( "Interpolation factor for camera orbit." )]
    public float Sensitivity = 0.1F;

    private TetrisGame Game;
    private Vector2 Offset;
    private float Zoom;

    void Start()
    {
        Game = FindObjectOfType<TetrisGame>();
    }

    void FixedUpdate()
    {
        // 
        Zoom -= Input.mouseScrollDelta.y;

        var ZoomMin = ( Game.BlockLayers + 2 ) * Game.BlockThickness * Game.ApparentScale;
        var ZoomMax = ( Game.BlockLayers + 12 ) * Game.BlockThickness * Game.ApparentScale;
        Zoom = Mathf.Clamp( Zoom, ZoomMin, ZoomMax );

        // 
        var mouse = new Vector2( Input.mousePosition.x / Screen.width, Input.mousePosition.y / Screen.height );
        mouse = ( mouse * 2F ) - Vector2.one;

        Offset = mouse * 3F;

        // 
        // transform.position = Target + new Vector3( ch * cp * Zoom, sp * Zoom, sh * cp * Zoom );
        // transform.LookAt( Target );

        // Camera Follow Block
        var a = Game.ActivePiece.transform.position;
        var p = TetrisSphereUtility.GetSphericalPosition( new Vector3( a.x + Offset.x, a.y, a.z + Offset.y ) );
        transform.position = Vector3.Slerp( transform.position, p.normalized * Zoom, Sensitivity );
        transform.LookAt( Vector3.zero );
    }
}
