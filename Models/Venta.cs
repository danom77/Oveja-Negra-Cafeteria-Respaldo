using OvejaNegra.Dtos;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OvejaNegra.Models
{
    public class Venta
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [Column(TypeName = "date")]
        public DateTime Fecha { get; set; } = DateTime.Now;
        [Required]
        [Column(TypeName = "decimal(10,2)")]
        [DisplayName("Total de la Venta")]
        public decimal Total { get; set; }
        [Required]
        [DisplayName("Metodo de Pago")]
        public PagoEnum MetodoPago { get; set; }
        //Relaciones
        public int ComandaId { get; set; }
        public virtual Comanda? Comanda { get; set; }
        public int? ClienteId { get; set; }
        public virtual Cliente? Cliente { get; set; }
    }
}
