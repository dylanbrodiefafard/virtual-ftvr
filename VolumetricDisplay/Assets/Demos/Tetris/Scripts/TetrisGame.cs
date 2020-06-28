#define USE_COLOR_HINT

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Biglab.Utility;
using UnityEngine;

public class TetrisGame : MonoBehaviour
{
    public CustomInput Input;

    [Header("Configuration")]
    [SerializeField, Tooltip("The number of blocks around the equator of the sphere.")]
    private int m_BlocksHorizontal;

    private int m_BlocksVertical;

    public int BlockLayers = 10;

    public float BlockThickness = 0.75F;

    public float ApparentScale = 1F;

    [Range(0.01F, 2.0F)]
    public float TicksPerSecond = 1;

    public int BlocksHorizontal
    {
        get { return m_BlocksHorizontal; }
        set
        {
            m_BlocksHorizontal = value;
            m_BlocksVertical = (int)(m_BlocksHorizontal / 4F);
        }
    }

    public int BlocksVertical
    {
        get
        {
            if (m_BlocksVertical == 0)
            {
                BlocksHorizontal = m_BlocksHorizontal;
            }

            return m_BlocksVertical;
        }
    }

    [Tooltip("The collection of piece prefabs.")]
    public TetrisPieceSet PieceCollection;

    public List<Color> LayerColors;

    [Header("Resources")]

    public GameObject BurstPrefab;
    public GameObject DotMeshPrefab;
    public Material SphereMaterial;
    public Material NextMaterial;

    [Header("Metrics")]

    [Space, ReadOnly]
    public TetrisPiece ActivePiece;

    [ReadOnly]
    public TetrisPiece NextPiece;

    [ReadOnly]
    public int ActivePieceIndex;

    [ReadOnly]
    public int NextPieceIndex;

    internal TetrisPiece GhostPiece;

    internal DataCell[,,] StageData { get; private set; }

    private ClearCondition _clearCondition;

    private int _startingX;

    public Color GetLayerColor(int layer)
        => LayerColors[layer % LayerColors.Count];

    [System.Serializable]
    public class TetrisPieceSet
    {
        [SerializeField]
        private List<TetrisPiece> _pieces;

        private List<int> _distributedList;

        public TetrisPiece this[int index] => _pieces[index];

        private void ComputeDistribedList()
        {
            _distributedList = new List<int>();
            for (var i = 0; i < _pieces.Count; i++)
            {
                for (var c = 0; c < _pieces[i].Distribution; c++)
                {
                    _distributedList.Add(i);
                }
            }
        }

        /// <summary>
        /// Gets a random piece by index.
        /// </summary>
        public int GetRandomIndex()
        {
            if (_distributedList == null)
            {
                ComputeDistribedList();
            }

            var idx = Random.Range(0, _distributedList.Count);
            return _distributedList[idx];
        }

        /// <summary>
        /// Gets a random piece.
        /// </summary>
        public TetrisPiece GetRandomPiece()
        {
            if (_distributedList == null)
            {
                ComputeDistribedList();
            }

            var idx = Random.Range(0, _distributedList.Count);
            return _pieces[_distributedList[idx]];
        }
    }

    void Start()
    {
        // 
        StageData = new DataCell[BlocksHorizontal, BlockLayers, BlocksVertical];
        _clearCondition = new RingClearCondition();

        // Create Shell ( Black ball in the middle )
        var shell = new GameObject("Shell");
        shell.transform.parent = transform;
        DisableHierarchy(shell);

        for (var x = 0; x < BlocksHorizontal; x++)
        {
            for (var z = 0; z < BlocksVertical; z++)
            {
                // 
                var dot = CreateDot(new Color(0.2F, 0.2F, 0.2F), string.Format("Shell ({0},{1})", x, z));
                dot.transform.position = new Vector3(x, -1, z);
                dot.transform.parent = shell.transform;
                DisableHierarchy(dot);
            }
        }

        // 
        InvokeUpdateTick(1F / TicksPerSecond, false);

        // 
        _startingX = Random.Range(0, BlocksHorizontal);

        // 
        DecideNextPiece();
        SpawnTetramino();

        // Set initial shader globals.
        Shader.SetGlobalFloat("_BlockStageWidth", BlocksHorizontal);
    }

    void FixedUpdate()
    {
        UpdateGhost();

        // Update shader globals.
        Shader.SetGlobalFloat("_ApparentScale", ApparentScale);
        Shader.SetGlobalFloat("_BlockThickness", BlockThickness);
    }

    void Update()
    {
        UpdateInput();
    }

    void UpdateInput()
    {
        var ActivePiece_Transform = ActivePiece.GetComponent<Transform>();

        //
        var originalRotation = ActivePiece_Transform.rotation;
        var originalPosition = ActivePiece_Transform.position;

        var position = ActivePiece_Transform.position;

        // X Motion
        if (Input.GetButtonDown("MoveRight"))
        {
            position.x++;
        }

        if (Input.GetButtonDown("MoveLeft"))
        {
            position.x--;
        }

        // Z Motion
        if (Input.GetButtonDown("MoveUp"))
        {
            position.z++;
        }

        if (Input.GetButtonDown("MoveDown"))
        {
            position.z--;
        }

        ActivePiece_Transform.position = position;

        // Rotate
        if (Input.GetButtonDown("RotateCW"))
        {
            ActivePiece_Transform.rotation = Quaternion.AngleAxis(+90, Vector3.up) * ActivePiece_Transform.rotation;
        }

        // Rotate
        if (Input.GetButtonDown("RotateCCW"))
        {
            ActivePiece_Transform.rotation = Quaternion.AngleAxis(-90, Vector3.up) * ActivePiece_Transform.rotation;
        }

        // Flip
        if (Input.GetButtonDown("FlipF"))
        {
            ActivePiece_Transform.rotation = Quaternion.AngleAxis(+90, Vector3.left) * ActivePiece_Transform.rotation;
        }

        // Flip
        if (Input.GetButtonDown("FlipB"))
        {
            ActivePiece_Transform.rotation = Quaternion.AngleAxis(-90, Vector3.left) * ActivePiece_Transform.rotation;
        }

        // Instant Drop
        if (Input.GetButtonDown("Drop"))
        {
            var dist = RaycastPiece(ActivePiece);
            ActivePiece_Transform.position -= Vector3.up * dist;

            // Cause update much sooner
            InvokeUpdateTick(0.1F);
        }

        // Has the motion caused it overlap a piece?
        if (IsOverlapField(ActivePiece))
        {
            ActivePiece_Transform.rotation = originalRotation;
            ActivePiece_Transform.position = originalPosition;
        }

        // Fix piece position
        MoveBackIntoField(ActivePiece);
    }

    void UpdateTick()
    {
        // Is this piece touching the field?
        if (!IsPieceContact(ActivePiece))
        {
            var piece_Transform = ActivePiece.GetComponent<Transform>();
            piece_Transform.position += Vector3.down;
        }
        // No, the piece is still in the air
        else
        {
            // Place the piece in the stage
            if (IsValidToPlace(ActivePiece))
            {
                // Sucessfully placed piece in field
                PlacePieceInField(ActivePiece);

                // Store last location to spawn piece
                _startingX = (int)ActivePiece.transform.position.x;

                // Determine clear condition
                _clearCondition.ProcessClusters(this);
                // TODO: Add score with return from ProcessClusters 

                // Spawn the next active piece
                SpawnTetramino();
            }
            // Piece exceeded bounds ( top of the stage ), so the player loses.
            else
            {
                // 
                Debug.Assert(ActivePiece.transform.position.y >= BlockLayers, "Failed to place piece, block overlap?");

                // Destroy active piece
                Destroy(ActivePiece.gameObject);
                Destroy(NextPiece.gameObject);
                ClearBlockField();
                ActivePiece = null;
                NextPiece = null;

                // 
                DecideNextPiece();
                SpawnTetramino();
            }
        }

        //  
        InvokeUpdateTick(1F / TicksPerSecond, false);
    }

    void InvokeUpdateTick(float time, bool cancel = true)
    {
        if (cancel)
        {
            CancelInvoke("UpdateTick");
        }

        Invoke("UpdateTick", time);
    }

    #region Spawning Mechanisms

    void DecideNextPiece()
    {
        NextPieceIndex = PieceCollection.GetRandomIndex();
        NextPiece = CreateTetramino(NextPieceIndex);

        // 
        NextPiece.transform.localScale = new Vector3(ApparentScale, ApparentScale, ApparentScale) * 0.25F;
        NextPiece.transform.position = new Vector3(0, BlockLayers * BlockThickness * ApparentScale, 0) * 0.25F;

        // Center preview piece 
        var bounds = GetBounds(NextPiece.gameObject);
        NextPiece.transform.position += (NextPiece.transform.position - bounds.center);

        //
        DisableHierarchy(NextPiece.gameObject);
    }

    void SpawnTetramino()
    {
        // Sets active piece
        ActivePiece = NextPiece;
        ActivePiece.name = string.Format("Active ( {0} )", NextPieceIndex);
        ActivePieceIndex = NextPieceIndex;

        // Sets color ( and sphere shader )
#if USE_COLOR_HINT
        SetTetraminoColor(ActivePiece, Color.white);
#else
        var color = Palette.Get( GetDefaultTetraminoColor( ActivePieceName ) );
        SetTetraminoColor( ActivePiece, color );
#endif

        // 
        ActivePiece.transform.localScale = Vector3.one;
        foreach (var mat in ActivePiece.GetChildren().Select(m => m.GetComponent<MeshRenderer>().material))
        {
            mat.SetInt("_Dither", 1);
        }

        // Disable seeing the piece in the hierarchy
        DisableHierarchy(ActivePiece.gameObject);

        // Decide next piece
        DecideNextPiece();

        // Moves the piece the a starting location
        var startingZ = BlocksVertical / 2; // Random.Range( 2, StageDepth - 2 );
        var ActivePiece_Transform = ActivePiece.GetComponent<Transform>();
        ActivePiece_Transform.position = new Vector3(_startingX, BlockLayers, startingZ);

        // Randomly rotates the piece
        var r = Random.Range(0, 4) * 90;
        ActivePiece_Transform.rotation = Quaternion.AngleAxis(r, Vector3.up);
    }

    #endregion

    #region Create Objects

    /// <summary>
    /// Creates a dot mesh, and returns the game object.
    /// </summary>
    /// <param name="color"> Color of the dot. </param>
    /// <param name="name"> Name of the object </param>
    GameObject CreateDot(Color color, string name = null)
    {
        // Create instance
        var obj = Instantiate(DotMeshPrefab, transform);
        if (!string.IsNullOrEmpty(name))
        {
            obj.name = name;
        }

        // 
        DisableFrustumCulling(obj);

        // 
        var material = CreateSphereMaterial(color);
        obj.GetComponent<MeshRenderer>().material = material;

        return obj;
    }

    TetrisPiece CreateTetramino(int pieceIndex)
    {
        var piece = PieceCollection[pieceIndex];
        var active = piece.CreateInstance(transform);

        // 
        foreach (var child in active.GetChildren())
        {
            child.name = "Block";

            var meshRenderer = child.GetComponent<MeshRenderer>();
            meshRenderer.material = NextMaterial;
        }

        return active;
    }

    TetrisPiece CreateGhost(TetrisPiece piece)
    {
        var ghost = PieceCollection[ActivePieceIndex].CreateInstance(transform);
        ghost.name = "Ghost";

        var ghost_Transform = ghost.GetComponent<Transform>();
        ghost_Transform.rotation = piece.transform.rotation;

        // 
        var material = CreateSphereMaterial(Color.white, 2);

        // 
        foreach (var child in ghost.GetChildren())
        {
            DisableFrustumCulling(child.gameObject);

            var meshRenderer = child.GetComponent<MeshRenderer>();
            meshRenderer.material = material;
        }

        return ghost;
    }

    private void SetTetraminoColor(TetrisPiece piece, Color color)
    {
        var material = CreateSphereMaterial(color);
        foreach (var child in piece.GetChildren())
        {
            DisableFrustumCulling(child.gameObject);

            var meshRenderer = child.GetComponent<MeshRenderer>();
            meshRenderer.material = material;
        }
    }

    Material CreateSphereMaterial(Color color, int ditherMode = 0)
    {
        var material = new Material(SphereMaterial);
        // material.SetFloat( "_BlockCount", BlocksHorizontal );
        // material.SetFloat( "_BlockThickness", BlockThickness );
        // material.SetFloat( "_ApparentScale", ApparentScale );
        material.SetColor("_Color", color);
        material.SetInt("_Dither", ditherMode);

        return material;
    }

    #endregion

    #region Enum Utility

    static TEnum GetRandomEnum<TEnum>()
    {
        var pieces = GetEnumValues<TEnum>();
        return pieces[Random.Range(0, pieces.Length)];
    }

    static TEnum[] GetEnumValues<TEnum>()
        => (TEnum[])System.Enum.GetValues(typeof(TEnum));

    #endregion

    void UpdateGhost()
    {
        // Clones the active piece
        if (GhostPiece != null)
        {
            Destroy(GhostPiece.gameObject);
        }

        GhostPiece = CreateGhost(ActivePiece);
        DisableHierarchy(GhostPiece.gameObject);

        // 
        var dist = RaycastPiece(ActivePiece);

        // Move ghost piece to where it'll land
        var ghost_Transform = GhostPiece.GetComponent<Transform>();
        ghost_Transform.position = ActivePiece.transform.position;
        ghost_Transform.position -= Vector3.up * dist;

        // 
        foreach (var child in GhostPiece.GetChildren())
        {
            int x, y, z;
            GetDataCoordinate(child.position, out x, out y, out z);
            child.position = new Vector3(x, y, z);

#if USE_COLOR_HINT
            var child_MeshRenderer = child.GetComponent<MeshRenderer>();
            child_MeshRenderer.material.SetColor("_Color", GetLayerColor(y));
#endif
        }
    }

    /// <summary>
    /// Clears out the game field.
    /// </summary>
    void ClearBlockField()
    {
        // Iterate over whole field
        for (var x = 0; x < BlocksHorizontal; x++)
        {
            for (var y = 0; y < BlockLayers; y++)
            {
                for (var z = 0; z < BlocksVertical; z++)
                {
                    StageData[x, y, z].Clear();
                }
            }
        }
    }

    /// <summary>
    /// Places the piece in the field, detaching the child blocks and assocating them with the given cell.
    /// </summary>
    void PlacePieceInField(TetrisPiece obj)
    {
        foreach (var child in obj.GetChildren())
        {
            int x, y, z;
            GetDataCoordinate(child.position, out x, out y, out z);
            StageData[x, y, z].Place(child.gameObject);

            // Snap child to position
            child.position = new Vector3(x, y, z);

            var meshRenderer = child.GetComponent<MeshRenderer>();
            var material = meshRenderer.material;
            material.SetInt("_Dither", 0);

#if USE_COLOR_HINT
            var color = GetLayerColor(y);
            material.SetColor("_Color", color);
#endif
        }

        // Unparents blocks
        obj.transform.DetachChildren();

        // Make invisible to hierarchy
        foreach (var child in obj.GetChildren())
        {
            DisableHierarchy(child.gameObject);
            child.parent = transform;
        }

        Destroy(obj.gameObject);
    }

    /// <summary>
    /// Causes a piece out of bounds to move back into the field of play.
    /// This effect is most noticable when rotating a piece.
    /// </summary>
    void MoveBackIntoField(TetrisPiece obj)
    {
        // 
        var zShift = 0;
        var yShift = 0;

        // 
        foreach (var child in obj.GetChildren())
        {
            int x, y, z;
            GetDataCoordinate(child.position, out x, out y, out z);
            if (!IsValidCoordinate(x, y, z))
            {
                if (z >= BlocksVertical)
                {
                    zShift = Mathf.Max(zShift, z - BlocksVertical + 1);
                }

                if (z < 0)
                {
                    zShift = Mathf.Min(zShift, z);
                }

                if (y < 0)
                {
                    yShift = Mathf.Min(yShift, y);
                }
            }
        }

        //
        var position = obj.transform.position;
        position.z -= zShift;
        position.y -= yShift;
        obj.transform.position = position;
    }

    #region Clearing

    /// <summary>
    /// Removes a slice from the game field and shifts the rest down.
    /// </summary>
    internal void ClearLayer(int y)
    {
        // 
        for (var x = 0; x < BlocksHorizontal; x++)
        {
            for (var z = 0; z < BlocksVertical; z++)
            {
                ClearCell(x, y, z);
            }
        }
    }

    public IEnumerator PauseInSeconds(float x)
    {
        yield return new WaitForSeconds(x);
        Debug.Break();
    }

    internal void ClearCell(int x, int y, int z)
    {
        // Create particles
        var cell = StageData[x, y, z].Block;

        var blast = Instantiate(BurstPrefab);
        blast.transform.localScale = Vector3.one * ApparentScale;

        var pos = TetrisSphereUtility.GetSphericalPosition(x, y, z) * 2F * BlockThickness * ApparentScale;
        var rot = Quaternion.FromToRotation(Vector3.forward, pos.normalized);
        blast.transform.position = pos;
        blast.transform.rotation = rot;

        // StartCoroutine( PauseInSeconds( 0.1F ) );

        var ps = blast.GetComponent<ParticleSystem>().main;
        ps.startColor = cell.GetComponent<MeshRenderer>().material.color;

        // Clear block
        StageData[x, y, z].Clear();

        // Shift column
        for (var _y = y; _y < BlockLayers - 1; _y++)
        {
            // Move block down on unit
            var block = StageData[x, _y, z].Block;
            if (block != null)
            {
                // Moves the block down one unit
                block.transform.position -= Vector3.up;

#if USE_COLOR_HINT
                // Sets the material to use the correct depth color hint
                var color = GetLayerColor(_y - 1);
                var material = block.GetComponent<MeshRenderer>().material;
                material.SetColor("_Color", color);
#endif
            }

            // Copy cell data from above
            StageData[x, _y, z] = StageData[x, _y + 1, z];
        }

        // Empty top block ( since everything was shifted down )
        StageData[x, BlockLayers - 1, z].MakeEmpty();
    }

    #endregion

    /// <summary>
    /// Performs a sweep raycast to detect how far from contact the piece is.
    /// </summary>
    int RaycastPiece(TetrisPiece piece)
    {
        var distance = int.MaxValue;

        // For each dot in the piece
        // foreach( var child in EnumerateChildren( piece ) )
        foreach (var child in piece.GetChildren())
        {
            int x, y, z;
            GetDataCoordinate(child.position, out x, out y, out z);

            var range = y;

            // March down
            for (var yy = y; yy >= 0; yy--)
            {
                // 
                if (IsCellOccupied(x, yy, z))
                {
                    range = Mathf.Abs(yy - y) - 1;
                    break;
                }
            }

            // Keep the minimum range
            distance = Mathf.Min(distance, range);
        }

        return distance;
    }

    /// <summary>
    /// Checks if the piece is touching the grid.
    /// </summary>
    bool IsPieceContact(TetrisPiece obj)
    {
        // return RaycastPiece( obj ) <= 1; // <=0 ??

        foreach (var child in obj.GetChildren())
        {
            int x, y, z;
            GetDataCoordinate(child.position, out x, out y, out z);
            if (IsCellOccupied(x, y - 1, z))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines if the given coordinate is a valid coordinate in the game field.
    /// </summary>
    public bool IsValidCoordinate(int x, int y, int z)
    {
        // Lower Bound
        if (x < 0)
        {
            return false;
        }

        if (y < 0)
        {
            return false;
        }

        if (z < 0)
        {
            return false;
        }

        // Upper Bound
        if (x >= BlocksHorizontal)
        {
            return false;
        }

        if (y >= BlockLayers)
        {
            return false;
        }

        if (z >= BlocksVertical)
        {
            return false;
        }

        // 
        return true;
    }

    /// <summary>
    /// Checks if the piece is overlapping any occupied cell in the field.
    /// </summary>
    bool IsOverlapField(TetrisPiece piece)
    {
        // 
        // foreach( var child in EnumerateChildren( piece ) )
        foreach (var child in piece.GetChildren())
        {
            int x, y, z;
            GetDataCoordinate(child.position, out x, out y, out z);
            if (IsCellOccupied(x, y, z))
            {
                return true;
            }
        }

        // 
        return false;
    }

    /// <summary>
    /// Checks if the piece is valid to be placed in the game field.
    /// </summary>
    bool IsValidToPlace(TetrisPiece obj)
    {
        // 
        foreach (var child in obj.GetChildren())
        {
            int x, y, z;
            GetDataCoordinate(child.position, out x, out y, out z);
            if (!IsValidCoordinate(x, y, z))
            {
                return false;
            }

            if (IsCellOccupied(x, y, z))
            {
                return false;
            }
        }

        // 
        return true;
    }

    /// <summary>
    /// Converts the world coordinate into field coordinates.
    /// </summary>
    internal void GetDataCoordinate(Vector3 coord, out int x, out int y, out int z)
    {
        // 
        x = Mathf.RoundToInt(coord.x);
        y = Mathf.RoundToInt(coord.y);
        z = Mathf.RoundToInt(coord.z);

        // X Wrapping
        while (x < 0)
        {
            x += BlocksHorizontal;
        }

        x = x % BlocksHorizontal;

        // Z Wrapping
        // while( z < 0 ) z += BlocksVertical;
        // z = z % BlocksVertical;
    }

    #region Field Data

    /// <summary>
    /// Checks if the given coordinate is occupied by a piece or out of bounds.
    /// </summary>
    public bool IsCellOccupied(int x, int y, int z)
    {
        if (IsValidCoordinate(x, y, z))
        {
            return StageData[x, y, z].Occupied;
        }
        else
        {
            return y < 0;
        }
    }

    /// <summary>
    /// Gets the object in the cell if the cell is occupied.
    /// </summary>
    public GameObject GetCellObject(int x, int y, int z)
    {
        if (IsValidCoordinate(x, y, z))
        {
            return StageData[x, y, z].Block;
        }
        else
        {
            return null;
        }
    }

    #endregion

    #region Misc Utility

    static Bounds GetBounds(GameObject obj)
    {
        var renderers = obj.GetComponentsInChildren<Renderer>();
        var bounds = renderers[0].bounds;

        // Include the remaining bounds
        for (var i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
            // bounds.Encapsulate( renderers[i].bounds.min );
            // bounds.Encapsulate( renderers[i].bounds.max );
        }

        return bounds;
    }

    static IEnumerable<Transform> EnumerateChildren(GameObject obj)
    {
        var piece_Transform = obj.GetComponent<Transform>();
        for (var i = 0; i < piece_Transform.childCount; i++)
        {
            yield return piece_Transform.GetChild(i);
        }
    }

    static void DisableFrustumCulling(GameObject obj)
    {
        var meshFilter = obj.GetComponent<MeshFilter>();
        meshFilter.mesh.bounds = new Bounds(Vector3.zero, Vector3.one * float.MaxValue);
    }

    static void DisableHierarchy(GameObject obj)
        => obj.hideFlags = HideFlags.NotEditable | HideFlags.HideAndDontSave;

    #endregion

    void OnDrawGizmos()
    {
        Gizmos.color = Color.black;
        Gizmos.DrawWireSphere(transform.position, ApparentScale);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, ApparentScale * BlockThickness * BlockLayers);

        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, ApparentScale * BlockThickness * (BlockLayers + 1));

        Gizmos.color = Color.white;
        if (Application.isPlaying)
        {
            // 
            for (var x = 0; x < BlocksHorizontal; x++)
            {
                for (var y = 0; y < BlockLayers; y++)
                {
                    for (var z = 0; z < BlocksVertical; z++)
                    {
                        if (IsCellOccupied(x, y, z))
                        {
                            Gizmos.DrawWireCube(new Vector3(x, y, z) * ApparentScale, Vector3.one * ApparentScale);
                        }
                    }
                }
            }
        }
    }

    internal struct DataCell
    {
        public bool Occupied => Block != null;

        public GameObject Block { get; private set; }

        public void Clear()
        {
            if (Block != null)
            {
                Destroy(Block.gameObject);
                Block = null;
            }
        }

        public void Place(GameObject obj)
        {
            if (Occupied)
            {
                Debug.LogAssertionFormat("Occupied: {0}", Occupied);
                Debug.Break();
            }

            Block = obj;
        }

        public void MakeEmpty()
            => Block = null;
    }
}
