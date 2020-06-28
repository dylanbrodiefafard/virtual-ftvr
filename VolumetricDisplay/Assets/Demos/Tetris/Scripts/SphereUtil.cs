using UnityEngine;

/// <summary>
/// Functions for mapping xyz-space to polar-space.
/// </summary>
public static class TetrisSphereUtility
{
    static TetrisGame Game
    {
        get
        {
            if( _Game == null )
                _Game = UnityEngine.Object.FindObjectOfType<TetrisGame>();

            return _Game;
        }
    }

    private static TetrisGame _Game;

    public static Vector3 CreatePolarNormal( float xAngle, float zAngle )
    {
        float cx = Mathf.Cos( xAngle );
        float sx = Mathf.Sin( xAngle );
        float cz = Mathf.Cos( zAngle + Mathf.PI / 2F );
        float sz = Mathf.Sin( zAngle + Mathf.PI / 2F );

        // return float3( cx * sz, cz, sx * sz );
        return new Vector3( cx * cz, sz, sx * cz );
    }

    public static Vector3 GetSphericalPosition( Vector3 pos )
    {
        return GetSphericalPosition( pos.x, pos.y, pos.z );
    }

    public static Vector3 GetSphericalPosition( float x, float y, float z )
    {
        // Scaling to keep 'apparent scale' of wedge.
        y = FrancoisScaling( y * Game.BlockThickness );

        var xAngle = ( x / Game.BlocksHorizontal ) * Mathf.PI * 2F;
        var zAngle = ( z / Game.BlocksVertical );
        zAngle = CompressDomain( zAngle, 0.2F );
        zAngle = LowerDomain( zAngle, 0.3F );
        zAngle *= Mathf.PI;

        return CreatePolarNormal( xAngle, zAngle ) * -y;
    }


    /// <summary>
    /// Scaling function that Francois came up with to scale blocks to keep them
    /// looking like same apparent shape.
    /// </summary>
    /// <param name="radius"> The height a block is off the ground. </param>
    static float FrancoisScaling( float radius )
    {
        var x = Mathf.Pow( 1 + ( Mathf.PI / Game.BlocksHorizontal ), radius );
        return x / Mathf.Pow( 1 - ( Mathf.PI / Game.BlocksHorizontal ), radius );
    }

    // Adjusts the lower bound of x
    static float LowerDomain( float x, float e )
    {
        return e + x * ( 1F - e );
    }

    // Adjusts the upper bound of x
    static float CompressDomain( float x, float e )
    {
        return e + x * ( 1F - e * 2F );
    }
}