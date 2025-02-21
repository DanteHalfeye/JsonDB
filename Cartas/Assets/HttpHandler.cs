using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using static System.Net.Mime.MediaTypeNames;

public class HttpHandler : MonoBehaviour
{
    [SerializeField] private string usersUrl = "https://my-json-server.typicode.com/DanteHalfeye/JsonDB/users";
    [SerializeField] private string cardsUrl = "https://my-json-server.typicode.com/DanteHalfeye/JsonDB/cards";
    [SerializeField] private RawImage[] imageSlots;  // UI elements for displaying card images
    [SerializeField] private TMP_Text[] cardText;
    [SerializeField] private TMP_Text userName;
    [SerializeField] private TMP_Dropdown userDropdown;  // Dropdown UI to select a user

    private List<Card> allCards = new List<Card>();  // Store all fetched cards
    private List<Users> allUsers = new List<Users>();  // Store all users

    void Start()
    {
        StartCoroutine(FetchCards()); // Fetch cards first
    }

    IEnumerator FetchCards()
    {
        UnityWebRequest www = UnityWebRequest.Get(cardsUrl);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.LogError("Error fetching cards: " + www.error);
        }
        else if (www.responseCode == 200)
        {
            CardList cardList = JsonUtility.FromJson<CardList>("{\"cards\":" + www.downloadHandler.text + "}");
            allCards = new List<Card>(cardList.cards);
            Debug.Log($"Fetched {allCards.Count} cards!");

            // Fetch users after getting cards
            StartCoroutine(FetchUsers());
        }
    }

    IEnumerator FetchUsers()
    {
        UnityWebRequest www = UnityWebRequest.Get(usersUrl);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.LogError("Error fetching users: " + www.error);
        }
        else if (www.responseCode == 200)
        {
            ListaPersonajes personajes = JsonUtility.FromJson<ListaPersonajes>("{\"users\":" + www.downloadHandler.text + "}");
            allUsers = new List<Users>(personajes.users);
            Debug.Log($"Fetched {allUsers.Count} users!");

            // Populate the dropdown with user names
            PopulateDropdown();
        }
    }

    void PopulateDropdown()
    {
        userDropdown.ClearOptions();
        List<string> userNames = new List<string>();

        foreach (var user in allUsers)
        {
            userNames.Add(user.username);
        }

        userDropdown.AddOptions(userNames);
        userDropdown.onValueChanged.AddListener(delegate { OnUserSelected(userDropdown.value); });

        // Auto-select the first user
        if (allUsers.Count > 0)
        {
            OnUserSelected(0);
        }
    }

    void OnUserSelected(int index)
    {
        Users selectedUser = allUsers[index];
        userName.text = selectedUser.username;
        Debug.Log($"Selected User: {selectedUser.username}");

        // Fetch and display their deck
        StartCoroutine(DisplayUserDeck(selectedUser));
    }

    IEnumerator DisplayUserDeck(Users user)
    {
        // Clear previous images
        foreach (var img in imageSlots)
        {
            img.texture = null;
        }

        for (int i = 0; i < user.deck.Length && i < imageSlots.Length; i++)
        {
            Card foundCard = allCards.Find(card => card.id == user.deck[i]);
            if (foundCard != null)
            {
                Debug.Log($"User {user.username} has card: {foundCard.name} ({foundCard.picture})");
                cardText[i].text = foundCard.name;
                StartCoroutine(GetImage(foundCard.picture, i));
            }
            else
            {
                Debug.LogWarning($"Card with ID {user.deck[i]} not found!");
            }
        }

        yield return null;
    }

    IEnumerator GetImage(string imageUrl, int index)
    {
        Debug.Log($"Fetching image from: {imageUrl}");

        UnityWebRequest www = UnityWebRequestTexture.GetTexture(imageUrl);
        www.SetRequestHeader("User-Agent", "UnityWebRequest");

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Image Request Failed: {www.error} ({imageUrl})");
            yield break;
        }

        string contentType = www.GetResponseHeader("Content-Type");
        Debug.Log($"Content-Type: {contentType}");

        if (!contentType.StartsWith("image"))
        {
            Debug.LogError("Invalid Content-Type. Expected an image.");
            yield break;
        }

        Texture2D texture = ((DownloadHandlerTexture)www.downloadHandler).texture;

        if (texture != null)
        {
            imageSlots[index].texture = texture;
        }
        else
        {
            Debug.LogError($"Failed to load image texture from: {imageUrl}");
        }
    }

}

// Data Models
[System.Serializable]
public class Users
{
    public int id;
    public string username;
    public bool state;
    public int[] deck;  // Array of card IDs
}

[System.Serializable]
public class ListaPersonajes
{
    public Users[] users;  // List of users
}

[System.Serializable]
public class Card
{
    public int id;
    public string name;
    public string picture;
}

[System.Serializable]
public class CardList
{
    public Card[] cards;
}
