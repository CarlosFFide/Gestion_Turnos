namespace GestorTurnos.Models;

public sealed class Turno
{
    public long Id { get; set; }
    public string NombreCliente { get; set; } = string.Empty;
    public string Estado { get; set; } = "Pendiente";
    public DateTimeOffset FechaRegistroUtc { get; set; }
    public DateTimeOffset? FechaAtencionUtc { get; set; }
    public string Numero => $"A-{Id:D3}";
}
