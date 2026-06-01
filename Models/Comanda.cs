using OvejaNegra.Dtos;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OvejaNegra.Models
{
    public class Comanda
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [Column(TypeName = "datetime")]
        [DisplayName("Fecha de la Comanda")]
        public DateTime Fecha { get; set; } = DateTime.Now;
        [Required]
        [DisplayName("Número de Mesa")]
        public int Nro_Mesa { get; set; }
        [Required]
        [DisplayName("Estado de la Comanda")]
        public EstadoEnum Estado { get; set; } = EstadoEnum.Pendiente;  
        //Relaciones
        public int UsuarioId { get; set; }
        public virtual Usuario? Usuario { get; set; }
        public virtual List<DetalleComanda>? DetallesComanda { get; set; }
        public virtual Venta? Venta { get; set; }
    }
}
