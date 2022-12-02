using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class CityGenerator : MonoBehaviour
{
    // 'Public' variables (set in editor)
    [Tooltip("Size of the city as a square")]
    [SerializeField] private Vector2Int rectBoundaries;
    
    [Tooltip("Number of districts / regions")]
    [SerializeField] private int numOfCells;

    [FormerlySerializedAs("_seed")] [SerializeField] private int seed;
    
    [Tooltip("Changes metric for determining distances from Euclidean to Manhattan")]
    [SerializeField] private bool useManhattanDistance;
    
    [Tooltip("The number of buildings a city block will comprise of")]
    [SerializeField] private int blockSize;

    [Tooltip("Set building scale (1 == default size, this is too large)")]
    [SerializeField] private float buildingScale;
    
    [Tooltip("0 = Road, 1 = Residential, 2 = Park, 3 = Industrial, 4 = Market, 5 = Business")]
    [SerializeField] private GameObject[] buildings = new GameObject[6];
    
    private enum VoronoiCellType 
    {
        Road,
        Residential,
        Park,
        Industrial,
        Market,
        Business
    }

    // This struct stores a pixel in a Voronoi diagram texture's position and colour, 
    // so that the diagram can be stored as a 2D array rather than a texture.
    private struct VoronoiPoint
    {
        public VoronoiPoint(Vector2Int position, VoronoiCellType pointType)
        {
            this.Position = position;
            this.PointType = pointType;
        }

        private Vector2Int Position { get; set; }

        public VoronoiCellType PointType { get; set; }
    }

    private VoronoiPoint[,] _voronoiAs2DArray;

    private List<GameObject> _generatedBuildings = new List<GameObject>();

    // ***END OF VARIABLES***

    // Set random generator seed and initialise 2D array for storing the voronoi diagram
    private void Start()
    {
        Random.InitState(seed);
        _voronoiAs2DArray = new VoronoiPoint[rectBoundaries.x, rectBoundaries.y];
        
        var plane = Instantiate(buildings[(int)VoronoiCellType.Road], 
            new Vector3(rectBoundaries.x / 2.0f, 0, rectBoundaries.y / 2.0f), Quaternion.identity);
        
        plane.transform.localScale = new Vector3(rectBoundaries.x, 0.001f, rectBoundaries.y);
        
        Debug.Log("Press G to generate");
    }

    // If G is pressed, destroy any existing buildings and generate a new city.
    // If M is pressed, enable / disable using Manhattan distance.
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            useManhattanDistance = !useManhattanDistance;
        }
        if (!Input.GetKeyDown((KeyCode.G))) return;
        foreach (var building in _generatedBuildings)
        {
            Destroy(building);
        }
        _generatedBuildings.Clear();
        CreateCity();
    }

    // The "Main" function
    private void CreateCity()
    {
        // Stores a point's coordinates and the type of cell it is in.
        var points = new Tuple<Vector2Int, VoronoiCellType>[numOfCells];
        var enumMax = (int)Enum.GetValues(typeof(VoronoiCellType)).Cast<VoronoiCellType>().Last();

        // Generates points in the Voronoi diagram
        for (var i = 0; i < numOfCells; i++)
        {
            points[i] = Tuple.Create(new Vector2Int(Random.Range(0, rectBoundaries.x), Random.Range(0, rectBoundaries.y)),
                    (VoronoiCellType)Random.Range(1, enumMax + 1));
        }

        // Go through every pixel and assign it a colour based on the closest point
        for (var x = 0; x < rectBoundaries.x; x++)
        {
            for (var y = 0; y < rectBoundaries.y; y++)
            {
                var currentPosition = new Vector2Int(x, y);
                var currentRegion = GetClosestPointType(currentPosition, points);
                _voronoiAs2DArray[x, y] = new VoronoiPoint(currentPosition, currentRegion);


                // Check surrounding pixels to see if any of them are a different colour to itself.
                // If so, that pixel, must be a border pixel, which should be a road.
                if (x - 1 < 0)
                {
                    x = 1;
                }
                if (y - 1 < 0)
                {
                    y = 1;
                }

                var prevX = _voronoiAs2DArray[x - 1, y].PointType;
                var prevY = _voronoiAs2DArray[x, y - 1].PointType;
                var prevXY = _voronoiAs2DArray[x - 1, y - 1].PointType;
                if (prevX != VoronoiCellType.Road && prevY != VoronoiCellType.Road && prevXY != VoronoiCellType.Road)
                {
                    if ((prevX != currentRegion || prevY != currentRegion || prevXY != currentRegion))
                    {
                        _voronoiAs2DArray[x, y].PointType = VoronoiCellType.Road;
                    }
                }
            }
        }
        SubBlockGenerator();
    }


    private void SubBlockGenerator()
    {
        for (var x = 0; x < rectBoundaries.x; x++)
        {
            for (var y = 0; y < rectBoundaries.y; y++)
            {
                // Creates roads for smaller blocks (unless its on a park)
                if ((x % blockSize) == 0 && _voronoiAs2DArray[x,y].PointType != VoronoiCellType.Park)
                {
                    _voronoiAs2DArray[x, y].PointType = VoronoiCellType.Road;
                }

                if ((y % blockSize) == 0 && _voronoiAs2DArray[x,y].PointType != VoronoiCellType.Park)
                {
                    _voronoiAs2DArray[x, y].PointType = VoronoiCellType.Road;
                }
                InstantiateBuildings(x, y, _voronoiAs2DArray[x,y].PointType);
            }
        }
    }

    private static int ManhattanDistance(Vector2Int a, Vector2Int b)
    {
        var distance = Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        return distance;
    }

    // Takes a pixel and finds the closest point and it's type (region / district). Can use either Euclidean or Manhattan distance
    private VoronoiCellType GetClosestPointType(Vector2Int pixelPosition, Tuple<Vector2Int, VoronoiCellType>[] points)
    {
        var smallestDistance = float.MaxValue;
        var index = 0;
        for (var i = 0; i < points.Length; i++)
        {
            if (useManhattanDistance)
            {
                if (!(ManhattanDistance(pixelPosition, points[i].Item1) < smallestDistance)) continue;
                smallestDistance = ManhattanDistance(pixelPosition, points[i].Item1);
            }
            else
            {
                if (!(Vector2.Distance(pixelPosition, points[i].Item1) < smallestDistance)) continue;
                smallestDistance = Vector2.Distance(pixelPosition, points[i].Item1);
            }

            index = i;
        }

        return points[index].Item2;
    }

    private void InstantiateBuildings(int x, int y, VoronoiCellType region)
    {
        var position3D = new Vector3(x, 0, y);
        var building = new GameObject();
        if (_voronoiAs2DArray[x, y].PointType == VoronoiCellType.Road)
        {
            //building = Instantiate(buildings[(int)VoronoiCellType.Road], position3D, Quaternion.identity);
            //_generatedBuildings.Add(building);
        }
        else
        {
            switch (region)
            {
                case VoronoiCellType.Residential:
                    building = Instantiate(buildings[(int)VoronoiCellType.Residential], position3D, Quaternion.identity);
                    break;
                case VoronoiCellType.Park:
                    building = Instantiate(buildings[(int)VoronoiCellType.Park], position3D, Quaternion.identity);
                    _generatedBuildings.Add(building);
                    return;
                case VoronoiCellType.Industrial:
                    building = Instantiate(buildings[(int)VoronoiCellType.Industrial], position3D, Quaternion.identity);
                    break;
                // 75% market, 20% residential, 5% business
                case VoronoiCellType.Market:
                {
                    var temp = Random.Range(0.0f, 1.0f);
                    if (temp <= 0.75f)
                    {
                        building = Instantiate(buildings[(int)VoronoiCellType.Market], position3D, Quaternion.identity);
                    }
                    else if (temp > 0.75f && temp <= 0.9f)
                    {
                        building = Instantiate(buildings[(int)VoronoiCellType.Residential], position3D, Quaternion.identity);
                    }
                    else
                    {
                        building = Instantiate(buildings[(int)VoronoiCellType.Business], position3D, Quaternion.identity);
                    }

                    break;
                }
                // 75% business, 20% market, 5% residential
                case VoronoiCellType.Business:
                {
                    var temp = Random.Range(0.0f, 1.0f);
                    if (temp <= 0.75f)
                    {
                        building = Instantiate(buildings[(int)VoronoiCellType.Business], position3D, Quaternion.identity);
                    }
                    else if (temp > 0.75f && temp <= 0.9f)
                    {
                        building = Instantiate(buildings[(int)VoronoiCellType.Market], position3D, Quaternion.identity);
                    }
                    else
                    {
                        building = Instantiate(buildings[(int)VoronoiCellType.Residential], position3D, Quaternion.identity);
                    }

                    break;
                }
            }
            // building.transform.position += new Vector3(0, randomHeight / 2, 0);
            if (buildingScale > 0)
            {
                building.transform.localScale = transform.localScale / buildingScale;
            }
            _generatedBuildings.Add(building);
        }
    }
}