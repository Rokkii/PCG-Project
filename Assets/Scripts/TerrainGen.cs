using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGen : MonoBehaviour {

    const float scale = 1f;

    public const float viewDistance = 450;
    public Transform viewer;
    public Material mapMaterial;

    public static Vector2 viewerPos;
    static MapGen mapGen;
    int chunkSize;
    int visibleChunks;

    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk> ();

    void Start()
    {
        mapGen = FindObjectOfType<MapGen> ();
        chunkSize = MapGen.mapChunkSize - 1;
        visibleChunks = Mathf.RoundToInt(viewDistance / chunkSize);
    }

    void Update()
    {
        viewerPos = new Vector2(viewer.position.x, viewer.position.z) / scale;
        UpdateVisibleChunks();
    }

    void UpdateVisibleChunks ()
    {

        for (int i = 0; i < terrainChunksVisibleLastUpdate.Count; i++)
        {
            terrainChunksVisibleLastUpdate[i].SetVisible(false);
        }
        terrainChunksVisibleLastUpdate.Clear();

        int currentChunkCoordX = Mathf.RoundToInt(viewerPos.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPos.y / chunkSize);

        for (int yOffset = -visibleChunks; yOffset <= visibleChunks; yOffset++)
        {
            for (int xOffset = -visibleChunks; xOffset <= visibleChunks; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if (terrainChunkDictionary.ContainsKey (viewedChunkCoord))
                {
                    terrainChunkDictionary[viewedChunkCoord].UpdateChunk ();

                    if (terrainChunkDictionary [viewedChunkCoord].IsVisible())
                    {
                        terrainChunksVisibleLastUpdate.Add(terrainChunkDictionary[viewedChunkCoord]);
                    }
                }
                else
                {
                    terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, transform, mapMaterial));
                }
            }

        }
    }

    public class TerrainChunk
    {
        Vector2 position;
        GameObject meshObject;
        Bounds bounds;

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;

        public TerrainChunk (Vector2 coord, int size, Transform parent, Material material)
        {
            position = coord * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);

            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshRenderer.material = material;

            meshObject.transform.position = positionV3 * scale;
            meshObject.transform.parent = parent;
            meshObject.transform.localScale = Vector3.one * scale;
            SetVisible(false);

            mapGen.RequestMapData(position, OnMapDataReceived);
        }

        void OnMapDataReceived (MapData mapData)
        {
            Texture2D texture = TextureGen.TextureFromColorMap(mapData.colorMap, MapGen.mapChunkSize, MapGen.mapChunkSize);
            meshRenderer.material.mainTexture = texture;
            mapGen.RequestMeshData(mapData, OnMeshDataRecieved);
        }

        void OnMeshDataRecieved (MeshData meshData)
        {
            meshFilter.mesh = meshData.CreateMesh();
        }

        public void UpdateChunk ()
        {
            float viewerDstFromNearestEdge = Mathf.Sqrt (bounds.SqrDistance(viewerPos));
            bool visible = viewerDstFromNearestEdge <= viewDistance;
            SetVisible(visible);
        }

        public void SetVisible (bool visible)
        {
            meshObject.SetActive(visible);
        }

        public bool IsVisible ()
        {
            return meshObject.activeSelf;
        }

    }
}
