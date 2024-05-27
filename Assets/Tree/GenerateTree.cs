using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateTree : MonoBehaviour
{
    public GameObject linePrefab, FruitPrefab;

    private GameObject[] Lines = new GameObject[1024];
    private GameObject[] Fruits = new GameObject[1024];
    private int lineCount = 0, fruitCount = 0;
    private GameObject Player;
    public float rotateSpeed = 100f;

    private AudioSource Ads;
    private bool allowSpinning = true;

    // Start is called before the first frame update
    void Start()
    {
        Ads = GetComponents<AudioSource>()[1];

        for (int i = 0; i < 1024; i++)
        {
            GameObject tempObj = Instantiate(linePrefab);
            tempObj.SetActive(false);
            tempObj.transform.position = this.transform.position;
            tempObj.transform.parent = this.transform;
            tempObj.name = "Line_" + i;
            Lines[i] = tempObj; 
        }
        for(int i = 0; i < 1024; i++)
        {
            GameObject tempObj = Instantiate(FruitPrefab);
            tempObj.SetActive(false);
            tempObj.transform.position = this.transform.position;
            tempObj.transform.parent = this.transform;
            tempObj.name = "Fruit_" + i;
            Fruits[i] = tempObj;
        }

        Player = GameObject.FindGameObjectWithTag("Player");

        Generate();

        
    }

    private void Branch(float x, float y, float z, float len, float angle, float width)
    {
        float rad;
        float randomAngle;
        if (lineCount == 0)
        {
            rad = 0;
            randomAngle = 0f;
        }
        else
        {
            rad = Mathf.Cos(angle * Mathf.Deg2Rad) * len;
            randomAngle = Random.Range(0, 360);
        }
  
        float targetX = x + Mathf.Cos(randomAngle * Mathf.Deg2Rad) * rad;
        float targetZ = z + Mathf.Sin(randomAngle * Mathf.Deg2Rad) * rad;
        float targetY = y + Mathf.Sin(angle * Mathf.Deg2Rad) * len;

        var points = new Vector3[2];
        points[0] = new Vector3(x, y, z);
        points[1] = new Vector3(targetX, targetY, targetZ);
        Lines[lineCount].GetComponent<LineRenderer>().SetPositions(points);
        Lines[lineCount].GetComponent<LineRenderer>().SetWidth(width, width);
        Lines[lineCount].SetActive(true);
        lineCount += 1;

        float addAngle = 20;

        // EXIT CONDITION
        len *= 0.75f;

        if (len < 0.8)
        {
            Fruits[fruitCount].transform.localPosition = points[1];
            Fruits[fruitCount].SetActive(true);
            fruitCount += 1;
        }

        if (len > 0.5)
        {
            Branch(targetX, targetY, targetZ, len, (angle - addAngle - Random.Range(-1, 1) * 30), width * 0.75f);
            Branch(targetX, targetY, targetZ, len, (angle - addAngle - Random.Range(-1, 1) * 30), width * 0.75f);
        }
    }

    private void Update()
    {
        float dist = Vector3.Distance(this.transform.position, Player.transform.position);
        if (dist < 30 && allowSpinning)
        {
            rotateSpeed = dist.Map(0, 30, 200, 0);
            transform.Rotate(0, rotateSpeed * Time.deltaTime, 0);
        }
    }

    public void EnableSpinning()
    {
        allowSpinning = true;
    }

    public void DisableSpnning()
    {
        allowSpinning = false;
    }

    private void Reset()
    {
        lineCount = 0;
        fruitCount = 0;
        for (int i = 0; i < 1024; i++)
        {
            Lines[i].SetActive(false);
        }
        for (int i = 0; i < 256; i++)
        {
            Fruits[i].SetActive(false);
        }
    }

    public void Generate()
    {
        Ads.Play();

        Reset();
        Branch(0, 0, 0, 5, 60, 0.3f); 
    }

    public void ChangeColor()
    {
        Color customColor = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
        print(customColor);
        for (int i = 0; i < fruitCount; i++)
        {
            Fruits[i].GetComponent<Renderer>().material.color = customColor;
        }
    }

    public void Falling()
    {
        Ads.Play();

        for (int i = 0; i < 600; i++)
        {
            StartCoroutine(Fall(Fruits[i]));
        }
    }

    IEnumerator Fall(GameObject tempObj)
    {
        while(tempObj.transform.position.y > -2)
        {
            tempObj.transform.position = new Vector3(tempObj.transform.position.x, tempObj.transform.position.y - 0.09f, tempObj.transform.position.z);
            yield return null;
        }
    }
}

public static class ExtensionMethods
{

    public static float Map(this float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }

}
