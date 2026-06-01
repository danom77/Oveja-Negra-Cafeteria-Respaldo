using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OvejaNegra.Models
{
    public class Producto
    {
        [Key]
        public int Id { get; set; }

        [Required, MinLength(2), MaxLength(50)]
        [DisplayName("Nombre del producto")]
        public string? Nombre { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        [DisplayName("Precio")]
        public decimal Precio { get; set; }
        [Required]
        [DisplayName("Dìsponibilidad")]
        public bool Disponible { get; set; }
        [Required, MinLength(5), MaxLength(200)]
        [DisplayName("Descripcion del producto")]
        public string? Descripcion { get; set; }
        public string? Imagen { get; set; }
        [NotMapped]
        [DisplayName("Imagen del producto")]
        public IFormFile? ImagenFile { get; set; }


        //Relaciones
        public int CategoriaId { get; set; }
        [DisplayName("Categoria del producto")]
        public virtual Categoria? Categoria { get; set; }
        public virtual List<DetalleComanda>? DetallesComanda { get; set; }
    }
}
