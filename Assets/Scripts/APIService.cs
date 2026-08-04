using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class APIService
{
    //static private string ipV4 = "10.153.54.75";
    //static private string IP_Casa = "192.168.178.23";
    static private string IP_Casa = "192.168.1.13";
    //static private string IP_HotSpot = "10.153.54.75";
    static private string IP_HotSpot = "10.163.28.75";
    private string datiUrl = $"http://{IP_Casa}:5000/dati";
    private string shelfUrl = $"http://{IP_Casa}:5000/shelf";

    // =========================
    // GET ALL
    // =========================
    public async Task<List<Artifact>> GetAllArtifactsAsync()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(datiUrl))
        {
            var operation = request.SendWebRequest();

            while (!operation.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.Log($"Errore: {request.error}");
                Debug.Log($"Response Code: {request.responseCode}");
                Debug.Log($"URL: {request.url}");
                return null;
            }

            string json = request.downloadHandler.text;

            string wrappedJson = "{ \"items\": " + json + " }";

            ArtifactListWrapper result =
                JsonUtility.FromJson<ArtifactListWrapper>(wrappedJson);

            return result.items;
        }
    }

    /*
    // =========================
    // GET BY ID
    // =========================
    public async Task<Artifact> GetArtifactByIdAsync(int id)
    {
        string url = baseUrl + "/" + id;

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            var operation = request.SendWebRequest();

            while (!operation.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.Log($"Errore: {request.error}");
                Debug.Log($"Response Code: {request.responseCode}");
                Debug.Log($"URL: {request.url}");
                return null;
            }

            string json = request.downloadHandler.text;

            Artifact artifact = JsonUtility.FromJson<Artifact>(json);

            return artifact;
        }
    }

    // =========================
    // CREATE NEW ARTIFACT
    // =========================
    public async Task<Artifact> CreateArtifactAsync(Artifact artifact)
    {
        string url = baseUrl;

        string json = JsonUtility.ToJson(artifact);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            var operation = request.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(request.error);
                Debug.Log(request.downloadHandler.text);
                return null;
            }

            string responseJson = request.downloadHandler.text;

            Debug.Log("POST OK: " + responseJson);

            //Parse della risposta del server
            Artifact created =
                JsonUtility.FromJson<Artifact>(responseJson);

            return created;
        }
    }

    // =========================
    // DELETE ARTIFACT BY ID
    // =========================
    public async Task<bool> DeleteArtifactAsync(int id)
    {
        string url = baseUrl + "/" + id;

        using (UnityWebRequest request = UnityWebRequest.Delete(url))
        {
            request.downloadHandler = new DownloadHandlerBuffer();

            var operation = request.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(request.error);
                Debug.Log(request.downloadHandler.text);
                return false;
            }

            Debug.Log("DELETE OK: " + request.downloadHandler.text);
            return true;
        }
    }*/

    // =========================
    // UPDATE ARTIFACT BY ID
    // =========================
    public async Task UpdateArtifact(Artifact artifact)
    {
        string json = JsonUtility.ToJson(artifact);

        using var client = new UnityWebRequest(datiUrl + "/" + artifact.id, "PUT");

        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        client.uploadHandler = new UploadHandlerRaw(bodyRaw);

        client.downloadHandler = new DownloadHandlerBuffer();

        client.SetRequestHeader("Content-Type", "application/json");

        var operation = client.SendWebRequest();

        while (!operation.isDone)
            await Task.Yield();

        if (client.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(client.error);
        }
    }


    public async Task<List<StorageContainer>> GetShelves()
    {
        using var client =
            UnityWebRequest.Get(shelfUrl);

        var operation = client.SendWebRequest();

        while (!operation.isDone)
            await Task.Yield();

        if (client.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(client.error);
            return null;
        }

        string json = client.downloadHandler.text;

        ShelfWrapper wrapper =
            JsonUtility.FromJson<ShelfWrapper>(
                "{\"items\":" + json + "}"
            );

        return wrapper.items;
    }

    public async Task UpdateShelf(StorageContainer shelf)
    {
        string json = JsonUtility.ToJson(shelf);

        using var client = new UnityWebRequest(shelfUrl + "/" + shelf.id, "PUT");

        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        client.uploadHandler = new UploadHandlerRaw(bodyRaw);

        client.downloadHandler = new DownloadHandlerBuffer();

        client.SetRequestHeader("Content-Type", "application/json");

        var operation = client.SendWebRequest();

        while (!operation.isDone)
            await Task.Yield();

        if (client.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(client.error);
        }
    }

    [System.Serializable]
    private class ArtifactListWrapper
    {
        public List<Artifact> items;
    }

    [System.Serializable]
    public class ShelfWrapper
    {
        public List<StorageContainer> items;
    }
}