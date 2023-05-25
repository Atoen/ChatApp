namespace WpfClient.Model;

public class User
{
    public string Username { get; }
    public string ImageSource { get; }

    public User(string username)
    {
        Username = username;
        ImageSource =
            "https://preview.redd.it/htasxv8oxima1.jpg?width=640&crop=smart&auto=webp&v=enabled&s=491815e7f0d75b333136472c023bb0bc2f4561fa";
    }

}