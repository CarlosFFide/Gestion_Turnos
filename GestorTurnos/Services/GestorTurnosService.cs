using GestorTurnos.Data;
using GestorTurnos.Models;

namespace GestorTurnos.Services;

public sealed class GestorTurnosService
{
    private readonly TurnoRepository _repository;
    private readonly object _sync = new();
    private Queue<Turno> _pendientes;

    public GestorTurnosService(TurnoRepository repository)
    {
        _repository = repository;
        _pendientes = new Queue<Turno>(_repository.ObtenerPendientes());
    }

    public Turno RegistrarTurno(string nombreCliente)
    {
        lock (_sync)
        {
            var turno = _repository.RegistrarTurno(nombreCliente);
            _pendientes.Enqueue(turno);
            return Copiar(turno);
        }
    }

    public Turno? ObtenerSiguiente()
    {
        lock (_sync)
        {
            return _pendientes.TryPeek(out var turno) ? Copiar(turno) : null;
        }
    }

    public bool AtenderSiguiente()
    {
        lock (_sync)
        {
            if (!_pendientes.TryPeek(out var turno))
            {
                return false;
            }

            if (!_repository.AtenderTurno(turno.Id))
            {
                _pendientes = new Queue<Turno>(_repository.ObtenerPendientes());
                throw new InvalidOperationException("Los turnos pendientes cambiaron. Intenta nuevamente.");
            }

            _pendientes.Dequeue();
            return true;
        }
    }

    public IReadOnlyList<Turno> ObtenerPendientes()
    {
        lock (_sync)
        {
            return _pendientes.Select(Copiar).ToArray();
        }
    }

    public IReadOnlyList<Turno> ObtenerAtendidos()
    {
        lock (_sync)
        {
            return _repository.ObtenerAtendidos();
        }
    }

    public int ContarPendientes()
    {
        lock (_sync)
        {
            return _pendientes.Count;
        }
    }

    public int ContarAtendidos()
    {
        lock (_sync)
        {
            return _repository.ObtenerAtendidos().Count;
        }
    }

    private static Turno Copiar(Turno turno)
    {
        return new Turno
        {
            Id = turno.Id,
            NombreCliente = turno.NombreCliente,
            Estado = turno.Estado,
            FechaRegistroUtc = turno.FechaRegistroUtc,
            FechaAtencionUtc = turno.FechaAtencionUtc
        };
    }
}
