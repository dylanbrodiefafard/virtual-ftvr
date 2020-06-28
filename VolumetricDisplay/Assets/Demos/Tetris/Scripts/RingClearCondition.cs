using System;
using System.Collections.Generic;
using UnityEngine;

public class RingClearCondition : ClearCondition
{
    public override int ProcessClusters( TetrisGame game )
    {
        // 
        var count = 0;
        foreach( var cluster in DiscoverClusters( game ) )
        {
            count++;

            foreach( var piece in cluster )
            {
                int x, y, z;
                game.GetDataCoordinate( piece.transform.position, out x, out y, out z );
                game.ClearCell( x, y, z );
            }
        }

        // DEBUG
        // if( count > 0 ) Debug.Break();

        return count;
    }

    protected IEnumerable<IEnumerable<GameObject>> DiscoverClusters( TetrisGame game )
    {
        for( int y = 0; y < game.BlockLayers; y++ )
        {
            for( int z = 0; z < game.BlocksVertical; z++ )
            {
                // If we found a full layer, add it.
                if( DetectRing( game, z, y ) )
                    yield return GetRing( game, z, y );
            }
        }
    }

    private bool DetectRing( TetrisGame game, int z, int y )
    {
        // Iterate over whole layer
        for( int x = 0; x < game.BlocksHorizontal; x++ )
        {
            // At least this cell is not occupied, so the layer is not complete.
            if( !game.IsCellOccupied( x, y, z ) )
                return false;
        }

        // Layer is completely occupied
        return true;
    }

    private IEnumerable<GameObject> GetRing( TetrisGame game, int z, int y )
    {
        for( int x = 0; x < game.BlocksHorizontal; x++ )
        {
            if( !game.IsCellOccupied( x, y, z ) )
                throw new Exception( "Unable to get ring. All cells must be occupied." );

            yield return game.GetCellObject( x, y, z );
        }
    }
}