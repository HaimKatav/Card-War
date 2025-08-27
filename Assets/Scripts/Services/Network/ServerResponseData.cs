namespace CardWar.Services.Network
{
    [System.Serializable]
    public class ServerResponseData<T>
    {
        public T Data { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public float NetworkDelay { get; set; }

        public ServerResponseData(T data, bool success = true, string errorMessage = null, float networkDelay = 0f)
        {
            Data = data;
            Success = success;
            ErrorMessage = errorMessage;
            NetworkDelay = networkDelay;
        }
    }
}