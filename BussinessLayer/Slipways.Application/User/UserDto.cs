using System;

namespace com.b_velop.Slipways.Application.User
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string DisplayName { get; set; }
        public string Token { get; set; }
    }
}