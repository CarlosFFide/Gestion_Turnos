using GestorTurnos.Models;
using GestorTurnos.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GestorTurnos.Pages;

public class HistorialModel : PageModel
{
    private readonly GestorTurnosService _service;

    public HistorialModel(GestorTurnosService service)
    {
        _service = service;
    }

    public IReadOnlyList<Turno> Turnos { get; private set; } = Array.Empty<Turno>();
    public string Busqueda { get; private set; } = string.Empty;

    public void OnGet(string? busqueda)
    {
        Busqueda = (busqueda ?? string.Empty).Trim();
        var turnos = _service.ObtenerAtendidos();

        Turnos = string.IsNullOrEmpty(Busqueda)
            ? turnos
            : turnos.Where(turno =>
                turno.Numero.Contains(Busqueda, StringComparison.OrdinalIgnoreCase)
                || turno.NombreCliente.Contains(Busqueda, StringComparison.OrdinalIgnoreCase))
                .ToArray();
    }
}
