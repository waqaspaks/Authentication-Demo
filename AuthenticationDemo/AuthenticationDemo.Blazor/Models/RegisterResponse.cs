using System.Collections.Generic;

namespace AuthenticationDemo.Blazor.Models
{
    public class RegisterResponse
    {
        public bool Succeeded { get; set; }
        public string? Message { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}