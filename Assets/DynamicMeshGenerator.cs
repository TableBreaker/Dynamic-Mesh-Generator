using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class DynamicMeshGenerator : MonoBehaviour {
  #region Inner Classes
  public class SquareMap {
    public List<KeyValuePair<Square, float>> squareMap;
    public int width;
    public int height;

    private static readonly float[,] s_kernel = new float[,] {
      { 0.09375f, 0.15625f, 0.09375f },
      { 0.15625f,  0f,  0.15625f },
      { 0.09375f, 0.15625f, 0.09375f }
    };

    /// <summary>
    /// Init the grid from a gray scale map.
    /// </summary>
    public SquareMap(float[,] valueMap, float squareSize, float shrinkDelta) {
      squareMap = new List<KeyValuePair<Square, float>>();

      width = valueMap.GetLength(0);
      height = valueMap.GetLength(1);

      var mapWidth = width * squareSize;
      var mapHeight = height * squareSize;

      var nodes = new ControlNode[width, height];

      for (var x = 0; x < width; x++) {
        for (var y = 0; y < height; y++) {
          var pos = new Vector3(-mapWidth / 2 + x * squareSize + squareSize / 2, -mapHeight / 2 + y * squareSize + squareSize / 2, 0f);
          nodes[x, y] = new ControlNode(pos, valueMap[x, y], true /* active */, squareSize, new Vector2Int(x, y));
        }
      }

      for (var x = 0; x < nodes.GetLength(0); x++) {
        for (var y = 0; y < nodes.GetLength(1); y++) {
          var node = nodes[x, y];
          _EnableNodeByConvolution(nodes, node, shrinkDelta);
        }
      }

      for (var x = 0; x < width - 1; x++) {
        for (var y = 0; y < height - 1; y++) {
          var square = new Square(nodes[x, y + 1], nodes[x + 1, y + 1], nodes[x + 1, y], nodes[x, y]);
          squareMap.Add(new KeyValuePair<Square, float>(square, squareSize));
        }
      }
    }

    public bool Shrink(ControlNode[,] nodeMap, float delta) {
      var result = false;
      return result;
    }

    private bool _EnableNodeByConvolution(ControlNode[,] nodeMap, ControlNode controlNode, float deltaThreshold) {
      if (controlNode.coordinate.x == 0 || 
        controlNode.coordinate.x == width - 1 || 
        controlNode.coordinate.y == 0 || 
        controlNode.coordinate.y == height - 1) {
        controlNode.active = false;
        return false;
      }

      var deltaSum = 0f;
      for (var x = 0; x < 3; x++) {
        for (var y = 0; y < 3; y++) {
          if (x == 1 && y == 1) {
            continue;
          }

          var dir = new Vector2Int(x - 1, y - 1);
          var xCoord = controlNode.coordinate.x + dir.x;
          var yCoord = controlNode.coordinate.y + dir.y;

          var node = nodeMap[xCoord, yCoord];
          deltaSum += Mathf.Abs(node.value - controlNode.value) * s_kernel[x, y];
        }
      }

      var result = deltaSum > deltaThreshold;
      if (controlNode.active != result) {
        controlNode.active = result;
        return true;
      }

      return false;
    }
  }

  public class Square {
    public ControlNode topLeft, topRight, bottomRight, bottomLeft;
    public Node centerTop, centerRight, centerBottom, centerLeft;
    public int configuration;

    public Square(ControlNode topLeft, ControlNode topRight, ControlNode bottomRight, ControlNode bottomLeft) {
      this.topLeft = topLeft;
      this.topRight = topRight;
      this.bottomRight = bottomRight;
      this.bottomLeft = bottomLeft;

      topLeft.ConnectToSquare(this);
      topRight.ConnectToSquare(this);
      bottomRight.ConnectToSquare(this);
      bottomLeft.ConnectToSquare(this);

      centerTop = topLeft.right;
      centerRight = bottomRight.above;
      centerBottom = bottomLeft.right;
      centerLeft = bottomLeft.above;

      if (topLeft.active) {
        configuration += 8;
      }
      if (topRight.active) {
        configuration += 4;
      }
      if (bottomRight.active) {
        configuration += 2;
      }
      if (bottomLeft.active) {
        configuration += 1;
      }
    }
  }

  public class Node {
    public Vector3 position;
    public int vertexIndex = -1;
    public Square container;

    public Node(Vector3 pos) {
      position = pos;
    }
  }

  public class ControlNode : Node {
    public bool active;
    public float value;
    public Vector2Int coordinate;
    public Node above, right;

    public ControlNode(Vector3 pos, float value, bool active, float squareSize, Vector2Int coordinate) : base(pos) {
      this.active = active;
      this.value = value;
      this.coordinate = coordinate;

      above = new Node(position + Vector3.up * squareSize / 2f);
      right = new Node(position + Vector3.right * squareSize / 2f);
    }

    public void ConnectToSquare(Square square) {
      container = square;
      above.container = square;
      right.container = square;
    }

    public Node DownToNode() {
      return new Node(position);
    }
  }
  #endregion

  public SquareMap grid;
  private List<Vector3> vertices;
  private List<int> triangles;

  public void GenerateMesh(Texture2D texture, float squareSize) {
    // Step 1. Create a map of gray scale values.
    //var valueMap = new float[texture.width, texture.height];
    //for (var x = 0; x < texture.width; x++) {
    //  for (var y = 0; y < texture.height; y++) {
    //    valueMap[x, y] = texture.GetPixel(x, y).grayscale;
    //  }
    //}

    var valueMap = new float[50, 50];
    for (var x = 0; x < 50; x++) {
      for (var y = 0; y < 50; y++) {
        valueMap[x, y] = Random.value;
      }
    }

    // Step 2. Create a shrinked square map.
    grid = new SquareMap(valueMap, squareSize, 0.8f);

    // Step 3. Create mesh.
    vertices = new List<Vector3>();
    triangles = new List<int>();

    foreach (var pair in grid.squareMap) {
      _TriangulateSquare(pair.Key);
    }

    var mesh = new Mesh();
    mesh.name = "ProceduralMesh";
    mesh.SetVertices(vertices);
    mesh.SetTriangles(triangles, 0);
    mesh.RecalculateNormals();
    mesh.RecalculateBounds();

    var meshFilter = GetComponent<MeshFilter>();
    meshFilter.sharedMesh = mesh;
  }

  private void _TriangulateSquare(Square square) {
    switch (square.configuration) {
      case 0:
        break;

      // 1 points:
      case 1:
        _MeshFromPoints(square.centerBottom, square.bottomLeft, square.centerLeft);
        break;
      case 2:
        _MeshFromPoints(square.centerRight, square.bottomRight, square.centerBottom);
        break;
      case 4:
        _MeshFromPoints(square.centerTop, square.topRight, square.centerRight);
        break;
      case 8:
        _MeshFromPoints(square.topLeft, square.centerTop, square.centerLeft);
        break;

      // 2 points:
      case 3:
        _MeshFromPoints(square.centerRight, square.bottomRight, square.bottomLeft, square.centerLeft);
        break;
      case 6:
        _MeshFromPoints(square.centerTop, square.topRight, square.bottomRight, square.centerBottom);
        break;
      case 9:
        _MeshFromPoints(square.topLeft, square.centerTop, square.centerBottom, square.bottomLeft);
        break;
      case 12:
        _MeshFromPoints(square.topLeft, square.topRight, square.centerRight, square.centerLeft);
        break;


      case 5:
        _MeshFromPoints(square.centerTop, square.topRight, square.centerRight, square.centerBottom, square.bottomLeft, square.centerLeft);
        break;
      case 10:
        _MeshFromPoints(square.topLeft, square.centerTop, square.centerRight, square.bottomRight, square.centerBottom, square.centerLeft);
        break;

      // 3 point:
      case 7:
        _MeshFromPoints(square.centerTop, square.topRight, square.bottomRight, square.bottomLeft, square.centerLeft);
        break;
      case 11:
        _MeshFromPoints(square.topLeft, square.centerTop, square.centerRight, square.bottomRight, square.bottomLeft);
        break;
      case 13:
        _MeshFromPoints(square.topLeft, square.topRight, square.centerRight, square.centerBottom, square.bottomLeft);
        break;
      case 14:
        _MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.centerBottom, square.centerLeft);
        break;

      // 4 point:
      case 15:
        _MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.bottomLeft);
        break;
    }
  }

  private void _MeshFromPoints(params Node[] points) {
    _AssignVertices(points);
    if (points.Length >= 3) {
      _CreateTriangle(points[0], points[1], points[2]);
    }
    if (points.Length >= 4) {
      _CreateTriangle(points[0], points[2], points[3]);
    }
    if (points.Length >= 5) {
      _CreateTriangle(points[0], points[3], points[4]);
    }
    if (points.Length >= 6) {
      _CreateTriangle(points[0], points[4], points[5]);
    }
  }

  private void _AssignVertices(Node[] points) {
    for (var i = 0; i < points.Length; i++) {
      if (points[i].vertexIndex == -1) {
        points[i].vertexIndex = vertices.Count;
        vertices.Add(points[i].position);
      }
    }
  }

  private void _CreateTriangle(Node a, Node b, Node c) {
    triangles.Add(a.vertexIndex);
    triangles.Add(b.vertexIndex);
    triangles.Add(c.vertexIndex);
  }

  private void OnDrawGizmos() {
    if (grid != null) {
      foreach (var pair in grid.squareMap) {
        // Corner points
        Gizmos.color = Color.white * pair.Key.topLeft.value;
        Gizmos.DrawCube(pair.Key.topLeft.position, Vector3.one * .4f);

        Gizmos.color = Color.white * pair.Key.topLeft.value;
        Gizmos.DrawCube(pair.Key.topRight.position, Vector3.one * .4f);

        Gizmos.color = Color.white * pair.Key.topLeft.value;
        Gizmos.DrawCube(pair.Key.bottomRight.position, Vector3.one * .4f);

        Gizmos.color = Color.white * pair.Key.topLeft.value;
        Gizmos.DrawCube(pair.Key.bottomLeft.position, Vector3.one * .4f);

        // Center points
        Gizmos.color = Color.gray;
        Gizmos.DrawCube(pair.Key.centerTop.position, Vector3.one * .15f);
        Gizmos.DrawCube(pair.Key.centerRight.position, Vector3.one * .15f);
        Gizmos.DrawCube(pair.Key.centerBottom.position, Vector3.one * .15f);
        Gizmos.DrawCube(pair.Key.centerLeft.position, Vector3.one * .15f);

      }
    }
  }

  private void Start() {
    GenerateMesh(null, 1f);
  }
}
