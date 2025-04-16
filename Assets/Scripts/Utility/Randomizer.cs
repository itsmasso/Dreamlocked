using System.Collections.Generic;
using UnityEngine;

//uses fisher yates algo to pick a random item without picking the same one again the next time
public class Randomizer<T>
{
    private List<T> originalList;
    private List<T> shuffledList;
    private int currentIndex;

    public Randomizer(List<T> items)
    {
        originalList = new List<T>(items);
        Reshuffle();
    }

    private void Reshuffle()
    {
        shuffledList = new List<T>(originalList);
        int n = shuffledList.Count;
        for (int i = 0; i < n; i++)
        {
            int randIndex = Random.Range(i, n);
            (shuffledList[i], shuffledList[randIndex]) = (shuffledList[randIndex], shuffledList[i]);
        }
        currentIndex = 0;
    }

    public T GetNext()
    {
        if (currentIndex >= shuffledList.Count)
        {
            Reshuffle();
        }

        return shuffledList[currentIndex++];
    }
}