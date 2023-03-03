using System.ComponentModel.DataAnnotations;

namespace ApiAut.Models
{
    public class Usuario
    {
        [Key]
        public Guid Id { get; set; }
        public string UserName { get; set; }
        public byte[] PasswordHash { get; set; }
        public byte[] PasswordSalt { get; set; }
    }
}
