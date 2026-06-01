using OvejaNegra.Models;

namespace OvejaNegra.ViewModels
{
    public class EditarComandaVM
    {
        public int ComandaId { get; set; }
        public int NroMesa { get; set; }
        public List<Categoria>? Categorias { get; set; }
        public List<int> MesasDisponibles { get; set; } = new();
        public Dictionary<int, ItemComandaDTO> Items { get; set; } = new();
    }


}
