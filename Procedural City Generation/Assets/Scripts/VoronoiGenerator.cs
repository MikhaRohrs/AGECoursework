using UnityEngine;


public class VoronoiGenerator : MonoBehaviour
{ 
    [SerializeField] private Vector2Int _rectBoundaries;
    [SerializeField] private int _minCells;
    [SerializeField] private int _maxCells;
    [SerializeField] private int _seed = 651321525;
    [SerializeField] private bool _useManhattanDistance;


    [SerializeField] private GameObject[] _buildings;



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
        public Vector2Int Position { get; set; }

        public Color Colour { get; set; }

        public bool IsRoad { get; set; }
    }

    private VoronoiPoint[,] _voronoiAs2DArray;

    private void Start()
    {
        Random.InitState(_seed);
        _voronoiAs2DArray = new VoronoiPoint[_rectBoundaries.x, _rectBoundaries.y];
        Debug.Log("Press G to generate");
    }

    private void Update()
    {
        if (Input.GetKeyDown((KeyCode.G)))
        {
            //GetComponent<SpriteRenderer>().sprite = Sprite.Create(GetDiagram(), new Rect(0, 0, _rectBoundaries.x, _rectBoundaries.y), Vector2.one * 0.5f);
        }
    }

    // The "Main" function
    private Texture2D GetDiagram()
    {
        var regionAmount = Random.Range(_minCells, _maxCells);
        //Debug.Log(regionAmount);
        var points = new Vector2Int[regionAmount];
        var regionColors = new Color[regionAmount];

        // Generates points in the Voronoi diagram
        for (var i = 0; i < regionAmount; i++)
        {
            points[i] = new Vector2Int(Random.Range(0, _rectBoundaries.x), Random.Range(0, _rectBoundaries.y));
            //regionColors[i] = new Color(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), 1.0f);

            var randomColour = Random.Range(0, 5);
            switch (randomColour)
            {
                case 0: // RESIDENTIAL - WHITE
                    regionColors[i] = Color.white;
                    break;
                case 1: // PARK - GREEN
                    regionColors[i] = Color.green;
                    break;
                case 2: // INDUSTRIAL - RED
                    regionColors[i] = Color.red;
                    break; 
                case 3: // MARKET - YELLOW
                    regionColors[i] = Color.yellow;
                    break;
                case 4: // BUSINESS - GREY
                    regionColors[i] = Color.grey;
                    break;
                default: // DEFAULT - WILDERNESS - TRANSPARENT
                    regionColors[i] = Color.clear;
                    break;
            }


        }

        // Go through every pixel and assign it a colour based on the closest point
        var pixelColors = new Color[_rectBoundaries.x * _rectBoundaries.y];
        for (var x = 0; x < _rectBoundaries.x; x++)
        {
            for (var y = 0; y < _rectBoundaries.y; y++)
            {
                var index = x * _rectBoundaries.x + y;
                pixelColors[index] = regionColors[GetClosestPointIndex(new Vector2Int(x, y), points)];

                _voronoiAs2DArray[x,y] = new VoronoiPoint(new Vector2Int(x, y), 
                regionColors[GetClosestPointIndex(new Vector2Int(x, y), points)]);
                var currentColour = _voronoiAs2DArray[x,y].Colour;

                // Check surrounding pixels to see if any of them are a different colour to itself.
                // If so, that pixel, must be a border pixel, which should be a road.

                if(x == 0 || x == _rectBoundaries.x)
                {
                    x = 1;
                }

                if (y == 0)
                {
                    y = 1;
                }


                if((_voronoiAs2DArray[x-1,y].Colour != currentColour ||
                _voronoiAs2DArray[x,y-1].Colour != currentColour ||
                _voronoiAs2DArray[x-1,y-1].Colour != currentColour))
                {
                    _voronoiAs2DArray[x,y].IsRoad = true;
                }

                var position3D = new Vector3(x, 0, y);
                if (_voronoiAs2DArray[x, y].IsRoad)
                {
                    Instantiate(_buildings[0], position3D, Quaternion.identity);
                }
                else
                {
                    if (currentColour == Color.white)
                    {
                        Instantiate(_buildings[1], position3D, Quaternion.identity);
                    }
                    if (currentColour == Color.green)
                    {
                        Instantiate(_buildings[2], position3D, Quaternion.identity);
                    }
                    if (currentColour == Color.red)
                    {
                        Instantiate(_buildings[3], position3D, Quaternion.identity);
                    }
                    if (currentColour == Color.yellow)
                    {
                        Instantiate(_buildings[4], position3D, Quaternion.identity);
                    }
                    if (currentColour == Color.grey)
                    {
                        Instantiate(_buildings[5], position3D, Quaternion.identity);
                    }
                }
            }
        }

        // for (var x = 0; x < _rectBoundaries.x; x++)
        // {
        //     for (var y = 0; y < _rectBoundaries.y; y++)
        //     {
        //         if (voronoiAs2DArray[x,y].IsRoad == true)
        //         {
        //             var index = x * _rectBoundaries.x + y;
        //             pixelColors[index] = Color.black;
        //         }
        //     }
        // }

        return GetImageFromColourArray(pixelColors);
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

    // Sets the colour of each pixel
    private Texture2D GetImageFromColourArray(Color[] pixelColors)
    {
        var texture = new Texture2D(_rectBoundaries.x, _rectBoundaries.y)
        {
            filterMode = FilterMode.Point
        };
        texture.SetPixels(pixelColors);
        texture.Apply();
        return texture;
    }
}