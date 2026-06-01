using Microsoft.EntityFrameworkCore;
using OvejaNegra.Dtos;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace OvejaNegra.Models
{

    [Index(nameof(username), IsUnique = true)]
    public class Usuario
    {
        [Key]
        public int Id { get; set; }
        [Required, MinLength(4),MaxLength(25)]
        [DisplayName("Nombre")]
        public string? Nombre { get; set; }
        [Required, MinLength(2), MaxLength(50)]
        [DisplayName("Apellido")]
        public string? Apellido { get; set; }
        [Required, MinLength(2), MaxLength(25)]
        [DisplayName("Nombre de usuario")]
        public string? username { get; set; }
        [DisplayName("Contraseña")]
        [Required, MinLength(8), MaxLength(100)]
        public string? password { get; set; }
        [DisplayName("Rol")]
        [Required]
        public Rolenum Rol { get; set; }

        //Relaciones
        public virtual List<Comanda>? Comandas { get; set; }
    }
}
