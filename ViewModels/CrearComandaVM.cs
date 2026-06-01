using OvejaNegra.Models;

namespace OvejaNegra.ViewModels
{
    public class CrearComandaVM
    {
        public int NroMesa { get; set; }
        public List<int> MesasDisponibles { get; set; } = new();

        public List<Categoria>? Categorias { get; set; }

        public Dictionary<int, ItemComandaDTO> Items { get; set; }
    }

    public class ItemComandaDTO
    {
        public int Cantidad { get; set; }
        public string? Observacion { get; set; }
    }
}
