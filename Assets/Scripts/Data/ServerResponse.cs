[System.Serializable]
public class ServerResponse<T>
{
    public T Data { get; set; }
    public bool Success { get; set; }
    public string ErrorMessage { get; set; }
    public float NetworkDelay { get; set; }

    public ServerResponse(T data, bool success = true, string errorMessage = null, float networkDelay = 0f)
    {
        Data = data;
        Success = success;
        ErrorMessage = errorMessage;
        NetworkDelay = networkDelay;
    }
}