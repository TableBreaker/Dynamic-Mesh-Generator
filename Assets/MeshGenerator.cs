using System.Collections;
using System.Collections.Generic;

using UnityEngine;

/// <summary>
/// This is a mesh generator class that using march square algorithm.
/// https://github.com/SebLague/Procedural-Cave-Generation
/// </summary>
public class MeshGenerator : MonoBehaviour {
  #region Inner Classes
  public class SquareGrid {
    public Square[,] squares;

    public SquareGrid(float[,] map, float squareSize) {
      var nodeCountX = map.GetLength(0);
      var nodeCountY = map.GetLength(1);
      var mapWidth = nodeCountX * squareSize;
      var mapHeight = nodeCountY * squareSize;
      var realWidth = mapWidth - squareSize;
      var realHeight = mapHeight - squareSize;
      var texelSize = new Vector2(1f / realWidth, 1f / realHeight);

      var controlNodes = new ControlNode[nodeCountX, nodeCountY];

      for (var x = 0; x < nodeCountX; x++) {
        for (var y = 0; y < nodeCountY; y++) {
          var pos = new Vector3(-mapWidth / 2 + x * squareSize + squareSize / 2, -mapHeight / 2 + y * squareSize + squareSize / 2, 30f * (1 - map[x, y]) * (1 - map[x, y]));
          var uv = new Vector2((float)x / (nodeCountX - 1), (float)y / (nodeCountY - 1));
          controlNodes[x, y] = new ControlNode(pos, uv, true /* active */, squareSize, texelSize);
        }
      }

      squares = new Square[nodeCountX - 1, nodeCountY - 1];
      for (var x = 0; x < nodeCountX - 1; x++) {
        for (var y = 0; y < nodeCountY - 1; y++) {
          squares[x, y] = new Square(controlNodes[x, y + 1], controlNodes[x + 1, y + 1], controlNodes[x + 1, y], controlNodes[x, y]);
        }
      }
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
    public Vector2 uv;
    public int vertexIndex = -1;

    public Node(Vector3 pos, Vector2 uv) {
      position = pos;
      this.uv = uv;
    }
  }

  public class ControlNode : Node {
    public bool active;
    public Node above, right;

    public ControlNode(Vector3 pos, Vector2 uv, bool active, float squareSize, Vector2 texelSize) : base(pos, uv) {
      this.active = active;
      above = new Node(position + Vector3.up * squareSize / 2f, uv + texelSize / 2f);
      right = new Node(position + Vector3.right * squareSize / 2f, uv + texelSize / 2f);
    }
  }
  #endregion

  public SquareGrid squareGrid;
  private List<Vector3> vertices;
  private List<Vector2> uvs;
  private List<int> triangles;

  public void GenerateMesh(float[,] map, float squareSize) {
    squareGrid = new SquareGrid(map, squareSize);

    vertices = new List<Vector3>();
    uvs = new List<Vector2>();
    triangles = new List<int>();

    for (var x = 0; x < squareGrid.squares.GetLength(0); x++) {
      for (var y = 0; y < squareGrid.squares.GetLength(1); y++) {
        _TriangulateSquare(squareGrid.squares[x, y]);
      }
    }

    var mesh = new Mesh();
    mesh.name = "ProceduralMesh";
    mesh.SetVertices(vertices);
    mesh.SetUVs(0, uvs);
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
        uvs.Add(points[i].uv);
      }
    }
  }

  private void _CreateTriangle(Node a, Node b, Node c) {
    triangles.Add(a.vertexIndex);
    triangles.Add(b.vertexIndex);
    triangles.Add(c.vertexIndex);
  }

  private void OnDrawGizmos() {
    //if (squareGrid != null) {
    //  for (var x = 0; x < squareGrid.squares.GetLength(0); x++) {
    //    for (var y = 0; y < squareGrid.squares.GetLength(1); y++) {
    //      // Corner points
    //      Gizmos.color = squareGrid.squares[x, y].topLeft.active ? Color.black : Color.white;
    //      Gizmos.DrawCube(squareGrid.squares[x, y].topLeft.position, Vector3.one * .4f);

    //      Gizmos.color = squareGrid.squares[x, y].topRight.active ? Color.black : Color.white;
    //      Gizmos.DrawCube(squareGrid.squares[x, y].topRight.position, Vector3.one * .4f);

    //      Gizmos.color = squareGrid.squares[x, y].bottomRight.active ? Color.black : Color.white;
    //      Gizmos.DrawCube(squareGrid.squares[x, y].bottomRight.position, Vector3.one * .4f);

    //      Gizmos.color = squareGrid.squares[x, y].bottomLeft.active ? Color.black : Color.white;
    //      Gizmos.DrawCube(squareGrid.squares[x, y].bottomLeft.position, Vector3.one * .4f);

    //      // Center points
    //      Gizmos.color = Color.gray;
    //      Gizmos.DrawCube(squareGrid.squares[x, y].centerTop.position, Vector3.one * .15f);
    //      Gizmos.DrawCube(squareGrid.squares[x, y].centerRight.position, Vector3.one * .15f);
    //      Gizmos.DrawCube(squareGrid.squares[x, y].centerBottom.position, Vector3.one * .15f);
    //      Gizmos.DrawCube(squareGrid.squares[x, y].centerLeft.position, Vector3.one * .15f);
    //    }
    //  }
    //}
  }

  private void Start() {
    var texture = Resources.Load<Texture2D>("avg_depth");
    var width = texture.width / 6;
    var height = texture.height / 6;
    var map = new float[width, height];
    for (var x = 0; x < width; x++) {
      for (var y = 0; y < height; y++) {
        map[x, y] = texture.GetPixel(x * 6, y * 6).grayscale;
      }
    }

    GenerateMesh(map, 0.2f);
  }
}
