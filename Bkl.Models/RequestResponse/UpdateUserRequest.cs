namespace Bkl.Models
{
    public class UpdateUserRequest
    {
        public long UserId { get; set; }
        public string UserName { get; set; }
        public string Positions { get; set; }
        public string Password { get; set; }
    }
}
