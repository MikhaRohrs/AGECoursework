using System.Collections.Generic;
using UnityEngine;

public class CityGenerator : MonoBehaviour
{
    [Tooltip("Size of the city as a square")]
    [SerializeField] private Vector2Int _rectBoundaries;

    [Tooltip("Minimum number of districts / regions")]
    [SerializeField] private int _minCells;
    [Tooltip("Maximum number of districts / regions")]
    [SerializeField] private int _maxCells;

    [SerializeField] private int _seed;

    [Tooltip("Changes metric for determining distances from Euclidean to Manhattan")]
    [SerializeField] private bool _useManhattanDistance;

    [Tooltip("The number of buildings a city block will comprise of")]
    [SerializeField] private int _blockSize;

    [Tooltip("0 means major roads are 1 block tick.")]
    [SerializeField] private int _majorRoadThickness;

    [Tooltip("0 = Road, 1 = Residential, 2 = Park, 3 = Industrial, 4 = Market, 5 = Business")]
    [SerializeField] private GameObject[] _buildings = new GameObject[6];

    private struct Buildings
    {
        public const int Road = 0;
        public const int Residential = 1;
        public const int Park = 2;
        public const int Industrial = 3;
        public const int Market = 4;
        public const int Business = 5;
    }

    // This struct stores a pixel in a Voronoi diagram texture's position and colour, 
    // so that the diagram can be stored as a 2D array rather than a texture.
    private struct VoronoiPoint
    {
        public VoronoiPoint(Vector2Int position, Color colour)
        {
            this.Position = position;
            this.Colour = colour;
            this.IsRoad = false;
        }

        private Vector2Int Position { get; }

        public Color Colour { get; }

        public bool IsRoad { get; set; }
    }

    private VoronoiPoint[,] _voronoiAs2DArray;

    private readonly List<GameObject> _generatedBuildings = new List<GameObject>();

    // END OF VARIABLES

    // Set random generator seed and initialise 2D array for storing the voronoi diagram
    private void Start()
    {
        Random.InitState(_seed);
        _voronoiAs2DArray = new VoronoiPoint[_rectBoundaries.x, _rectBoundaries.y];
        Debug.Log("Press G to generate");
    }

    // If G is pressed, destroy any existing buildings and generate a new city
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            _useManhattanDistance = !_useManhattanDistance;
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
        var regionAmount = Random.Range(_minCells, _maxCells);
        //Debug.Log(regionAmount);
        var points = new Vector2Int[regionAmount];
        var regionColors = new Color[regionAmount];

        // Generates points in the Voronoi diagram
        for (var i = 0; i < regionAmount; i++)
        {
            points[i] = new Vector2Int(Random.Range(0, _rectBoundaries.x), Random.Range(0, _rectBoundaries.y));

            var randomColour = Random.Range(0, 5);
            regionColors[i] = randomColour switch
            {
                0 => // RESIDENTIAL - WHITE
                    Color.white,
                1 => // PARK - GREEN
                    Color.green,
                2 => // INDUSTRIAL - RED
                    Color.red,
                3 => // MARKET - YELLOW
                    Color.yellow,
                4 => // BUSINESS - GREY
                    Color.grey,
                _ => Color.clear
            };
        }

        // Go through every pixel and assign it a colour based on the closest point
        for (var x = 0; x < _rectBoundaries.x; x++)
        {
            for (var y = 0; y < _rectBoundaries.y; y++)
            {
                _voronoiAs2DArray[x, y] = new VoronoiPoint(new Vector2Int(x, y),
                regionColors[GetClosestPointIndex(new Vector2Int(x, y), points)]);

                var currentColour = _voronoiAs2DArray[x, y].Colour;

                // Check surrounding pixels to see if any of them are a different colour to itself.
                // If so, that pixel, must be a border pixel, which should be a road.s
                if ((x - 1) < 0)
                {
                    x = 1;
                }
                if ((y - 1) < 0)
                {
                    y = 1;
                }

                if ((_voronoiAs2DArray[x - 1, y].Colour != currentColour ||
                     _voronoiAs2DArray[x, y - 1].Colour != currentColour ||
                     _voronoiAs2DArray[x - 1, y - 1].Colour != currentColour))
                {
                    _voronoiAs2DArray[x, y].IsRoad = true;
                }
            }
        }
        // Supposed to make major roads bigger
        for (var i = 1; i <= _majorRoadThickness; i++)
        {
            MajorRoadThickener();
        }
        // Creates smaller roads and also instantiates all buildings
        SubBlockGenerator();
    }

    // Similar to code checking for neighboring districts, but instead 
    private void MajorRoadThickener()
    {
        for (var x = 0; x < _rectBoundaries.x; x++)
        {
            for (var y = 0; y < _rectBoundaries.y; y++)
            {
                if ((x - 1) < 0)
                {
                    x = 1;
                }

                if ((y - 1) < 0)
                {
                    y = 1;
                }

                var currentIsRoad = _voronoiAs2DArray[x, y].IsRoad;
                if ((_voronoiAs2DArray[x - 1, y].IsRoad ||
                     _voronoiAs2DArray[x, y - 1].IsRoad ||
                     _voronoiAs2DArray[x - 1, y - 1].IsRoad) && !currentIsRoad)
                {
                    _voronoiAs2DArray[x, y].IsRoad = true;
                }
            }
        }
    }

    private void SubBlockGenerator()
    {
        for (var x = 0; x < _rectBoundaries.x; x++)
        {
            for (var y = 0; y < _rectBoundaries.y; y++)
            {
                // Creates roads for smaller blocks (unless its on a park)
                if ((x % _blockSize) == 0 && _voronoiAs2DArray[x,y].Colour != Color.green)
                {
                    _voronoiAs2DArray[x, y].IsRoad = true;
                }

                if ((y % _blockSize) == 0 && _voronoiAs2DArray[x, y].Colour != Color.green)
                {
                    _voronoiAs2DArray[x, y].IsRoad = true;
                }
                InstantiateBuildings(x, y, _voronoiAs2DArray[x,y].Colour);
            }
        }
    }

    private static int ManhattanDistance(Vector2Int a, Vector2Int b)
    {
        var distance = Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        return distance;
    }

    // Takes a pixel and finds the closest point. Can use either Euclidean or Manhattan distance
    private int GetClosestPointIndex(Vector2Int pixelPosition, Vector2Int[] points)
    {
        var smallestDistance = float.MaxValue;
        var index = 0;
        for (var i = 0; i < points.Length; i++)
        {
            if (_useManhattanDistance)
            {
                if (!(ManhattanDistance(pixelPosition, points[i]) < smallestDistance)) continue;
                smallestDistance = ManhattanDistance(pixelPosition, points[i]);
            }
            else
            {
                if (!(Vector2.Distance(pixelPosition, points[i]) < smallestDistance)) continue;
                smallestDistance = Vector2.Distance(pixelPosition, points[i]);
            }

            index = i;
        }

        return index;
    }

    private void InstantiateBuildings(int x, int y, Color colour)
    {
        var position3D = new Vector3(x, 0, y);
        var building = new GameObject();
        if (_voronoiAs2DArray[x, y].IsRoad)
        {
            building = Instantiate(_buildings[Buildings.Road], position3D, Quaternion.identity);
            _generatedBuildings.Add(building);
        }
        else
        {
            var randomHeight = Random.Range(1.0f, 10.0f);
            if (colour == Color.white)
            {
                building = Instantiate(_buildings[Buildings.Residential], position3D, Quaternion.identity);
            }
            else if (colour == Color.green)
            {
                building = Instantiate(_buildings[Buildings.Park], position3D, Quaternion.identity);
                _generatedBuildings.Add(building);
                return;
            }
            else if (colour == Color.red)
            {
                building = Instantiate(_buildings[Buildings.Industrial], position3D, Quaternion.identity);
            }
            else if (colour == Color.yellow) // 75% market, 20% residential, 5% business
            {
                var temp = Random.Range(0.0f, 1.0f);
                if (temp <= 0.75f)
                {
                    building = Instantiate(_buildings[Buildings.Market], position3D, Quaternion.identity);
                }
                else if (temp > 0.75f && temp <= 0.9f)
                {
                    building = Instantiate(_buildings[Buildings.Residential], position3D, Quaternion.identity);
                }
                else
                {
                    building = Instantiate(_buildings[Buildings.Business], position3D, Quaternion.identity);
                }
            }
            else if (colour == Color.grey) // 75% business, 20% market, 5% residential
            {
                var temp = Random.Range(0.0f, 1.0f);
                if (temp <= 0.75f)
                {
                    building = Instantiate(_buildings[Buildings.Business], position3D, Quaternion.identity);
                }
                else if (temp > 0.75f && temp <= 0.9f)
                {
                    building = Instantiate(_buildings[Buildings.Market], position3D, Quaternion.identity);
                }
                else
                {
                    building = Instantiate(_buildings[Buildings.Residential], position3D, Quaternion.identity);
                }
            }
            building.transform.position += new Vector3(0, randomHeight / 2, 0);
            building.transform.localScale += new Vector3(0, randomHeight, 0);
            _generatedBuildings.Add(building);
        }
    }
}