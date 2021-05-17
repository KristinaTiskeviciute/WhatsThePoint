using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using TMPro;

[System.Serializable]
public struct Triangle
{
    public Vector3 P1;
    public Vector3 P2;
    public Vector3 P3;

    public Triangle(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        P1 = p1;
        P2 = p2;
        P3 = p3;
    }

    // Contains checks if a 2D point falls within this triangle's area
    public bool Contains(Vector3 point)
    {
        Vector3 AB = P2 - P1;
        Vector3 AP = point - P1;
        Vector3 BC = P2 - P3;
        Vector3 BP = P2 - point;
        Vector3 CA = P3 - P1;
        Vector3 CP = P3 - point;
        Vector3 crossABxAP = Vector3.Cross(AB, AP);
        Vector3 crossBCxBP = Vector3.Cross(BC, BP);
        Vector3 crossCAxCP = Vector3.Cross(CA, CP);
        return ((crossABxAP.z < 0 && crossBCxBP.z < 0 && crossCAxCP.z < 0) || (crossABxAP.z > 0 && crossBCxBP.z > 0 && crossCAxCP.z > 0));
    }
}

[System.Serializable]
public struct Country
{
    public string Name;
    public Color32 Color;
    public List<Triangle> Triangles;
    public Country(string name, Color32 color, List<Triangle> triangles)
    {
        Name = name;
        Color = color;
        Triangles = triangles;
    }
}

[System.Serializable]
public class Countries
{
    public List<Country> countries;
}

public class TestMap : MonoBehaviour
{
    [SerializeField] private int width = 50;
    [SerializeField] private int height = 50;
    [SerializeField] private string readFrom;
    [SerializeField] private bool highlightCountryOnClick = true;
    [SerializeField] private GameObject pointOfInterestPrefab;

    Mesh mesh;
    MeshFilter meshFilter;
    Vector3[] verts;
    int[] tris;
    Color32[] colors;

    private List<Country> countries = new List<Country>();    
    private GameObject pointOfInterestMarker;
    private Country selectedCountry;
    private int selectedCountryStartInd =-1;

    void Start()
    {      
        GenerateMesh();
        pointOfInterestMarker = Instantiate(pointOfInterestPrefab);
        pointOfInterestMarker.SetActive(false);
        SetMeshFromFile();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            SelectCountry();
        }
    }

    private List<Country> ReadCountriesFromFile()
    {
        string content = File.ReadAllText($"{Application.dataPath}/{readFrom}.txt");
        return JsonUtility.FromJson<Countries>(content).countries;
    }

    private void SetMeshFromFile()
    {
        countries = ReadCountriesFromFile();
        int vertCount = 0;
        foreach (Country c in countries)
        {
            for (int i = 0; i < c.Triangles.Count; i++)
            {
                Triangle t = c.Triangles[i];
                verts[vertCount] = t.P1;
                verts[vertCount + 1] = t.P2;
                verts[vertCount + 2] = t.P3;
                for (int j = 0; j < 3; j++)
                {
                    tris[vertCount+j] = vertCount+j;
                    colors[vertCount+j] = c.Color;
                }
                vertCount += 3;
            }
        }
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.colors32 = colors;
        meshFilter.mesh = mesh;
    }

    private void GenerateMesh()
    {
        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = Resources.Load<Material>("MapMaterial");
        meshFilter = gameObject.AddComponent<MeshFilter>();
        mesh = new Mesh();
        tris = new int[width * height * 6];
        verts = new Vector3[tris.Length];
        colors = new Color32[verts.Length];
        meshFilter.mesh = mesh;
    }

    private Vector3 GetMouseToWorldPos()
    {
        Vector3 mousePos = Input.mousePosition;
        return Camera.main.ScreenToWorldPoint(mousePos);
    }

    private void SelectCountry()
    {
        Vector3 clickPosWorld = GetMouseToWorldPos();
        Vector3 clickPosMap = transform.InverseTransformPoint(clickPosWorld);
        clickPosWorld.z = 0;
        int vertCount = 0;
        for(int i = 0; i < countries.Count; i++)
        {
            int triCount = countries[i].Triangles.Count;
            for (int j = 0; j < triCount; j++)
            {
                if (countries[i].Triangles[j].Contains(clickPosMap))
                {
                    if (selectedCountryStartInd >= 0)
                        ColorCountry(selectedCountryStartInd, selectedCountry.Triangles.Count, selectedCountry.Color);
                    selectedCountry = countries[i];
                    selectedCountryStartInd = vertCount;
                    ColorCountry(selectedCountryStartInd, selectedCountry.Triangles.Count, new Color32(255, 255, 255, 255));
                    UpdatePOImarker(clickPosWorld, selectedCountry.Name);
                    return;
                }
                
            }
            vertCount += triCount * 3;
        }
    }

    void UpdatePOImarker(Vector3 position, string text)
    {
        pointOfInterestMarker.transform.position = position-Vector3.forward;
        pointOfInterestMarker.transform.GetChild(1).GetComponent<TMP_Text>().text = text;
        pointOfInterestMarker.SetActive(true);
    }

    void ColorCountry(int startInd, int triangleCount, Color32 color)
    {
        for (int i = 0; i < triangleCount*3; i++)
        {           
            colors[startInd+i] = color;
        }
        mesh.colors32 = colors;
    }

    public void StartAnimate()
    {
        StartCoroutine(Animate());
    }

    IEnumerator Animate()
    {
        pointOfInterestMarker.SetActive(false);
        Color32 black = new Color32(0, 0, 0, 255);
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = black;
        }
        int vertCount = verts.Length-1;
        for (int c = countries.Count - 1; c >= 0; c--)
        {
            int i = 0;
            while (i < countries[c].Triangles.Count+3)
            {
                
                for (int t = 0; t < 3; t++)
                {
                    
                    if (i < countries[c].Triangles.Count)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            colors[vertCount - j] = countries[c].Color;
                        }
                        vertCount -= 3;
                    }
                    i++;
                }
                mesh.colors32 = colors;
                yield return null;
            }
        }
    }
}
