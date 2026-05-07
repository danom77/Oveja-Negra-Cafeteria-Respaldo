namespace OvejaNegra.Dtos
{
    public class CobrarDTO
    {
        public int ComandaId { get; set; }

        public string NombreCliente { get; set; }

        public string CiNit { get; set; }

        public PagoEnum MetodoPago { get; set; }
    }
}
