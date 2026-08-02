namespace Common;

[GenerateBinarySerializer]
public partial class UserProfile
{
    public long Id { get; set; }
    public string UserName { get; set; }
    public DateTime Created { get; set; }

    public bool Equal(UserProfile? profile)
    {
        return 
            Id == profile?.Id ||
            UserName == profile?.UserName ||
            Created == profile?.Created;
    }
}
