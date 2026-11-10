using GestorTurnos.Models;
using GestorTurnos.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GestorTurnos.Pages;

public class IndexModel : PageModel
{
    private readonly GestorTurnosService _service;

    public IndexModel(GestorTurnosService service)
    {
        _service = service;
    }

    public IReadOnlyList<Turno> Pendientes { get; private set; } = Array.Empty<Turno>();
    public Turno? Siguiente { get; private set; }
    public int AtendidosHoy { get; private set; }
    public string? NombreClienteIngresado { get; private set; }
    public string? MensajeError { get; private set; }

    public void OnGet()
    {
        CargarDatos();
    }

    public IActionResult OnPostRegistrar(string? nombreCliente)
    {
        NombreClienteIngresado = nombreCliente;

        if (string.IsNullOrWhiteSpace(nombreCliente))
        {
            MensajeError = "Ingrese el nombre del cliente.";
            CargarDatos();
            return Page();
        }

        if (nombreCliente.Trim().Length > 120)
        {
            MensajeError = "El nombre no puede superar los 120 caracteres.";
            CargarDatos();
            return Page();
        }

        var turno = _service.RegistrarTurno(nombreCliente);
        TempData["Mensaje"] = $"Turno {turno.Numero} registrado correctamente.";
        return RedirectToPage();
    }

    public IActionResult OnPostAtender()
    {
        try
        {
            if (_service.AtenderSiguiente())
            {
                TempData["Mensaje"] = "Turno atendido correctamente.";
            }
            else
            {
                TempData["Error"] = "No hay turnos pendientes.";
            }
        }
        catch (InvalidOperationException)
        {
            TempData["Error"] = "La lista de turnos cambió. Intenta nuevamente.";
        }

        return RedirectToPage();
    }

    private void CargarDatos()
    {
        Pendientes = _service.ObtenerPendientes();
        Siguiente = Pendientes.FirstOrDefault();
        var hoy = DateTime.Today;
        AtendidosHoy = _service.ObtenerAtendidos().Count(
            turno => turno.FechaAtencionUtc?.ToLocalTime().Date == hoy);
    }
}
