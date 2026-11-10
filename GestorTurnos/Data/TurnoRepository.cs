using System.Globalization;
using GestorTurnos.Models;
using Microsoft.Data.Sqlite;

namespace GestorTurnos.Data;

public sealed class TurnoRepository
{
    private readonly string _connectionString;

    public TurnoRepository(IWebHostEnvironment environment)
    {
        var directory = Path.Combine(environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(directory);

        var connectionBuilder = new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(directory, "turnos.db"),
            Mode = SqliteOpenMode.ReadWriteCreate
        };

        _connectionString = connectionBuilder.ToString();
    }

    public void Inicializar()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Turnos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                NombreCliente TEXT NOT NULL CHECK (length(trim(NombreCliente)) BETWEEN 1 AND 120),
                Estado TEXT NOT NULL CHECK (Estado IN ('Pendiente', 'Atendido')),
                FechaRegistroUtc TEXT NOT NULL,
                FechaAtencionUtc TEXT,
                CHECK (
                    (Estado = 'Pendiente' AND FechaAtencionUtc IS NULL)
                    OR (Estado = 'Atendido' AND FechaAtencionUtc IS NOT NULL)
                )
            );

            CREATE INDEX IF NOT EXISTS IX_Turnos_Estado_Id ON Turnos (Estado, Id);
            """;
        command.ExecuteNonQuery();
    }

    public Turno RegistrarTurno(string nombreCliente)
    {
        if (string.IsNullOrWhiteSpace(nombreCliente))
        {
            throw new ArgumentException("El nombre del cliente es obligatorio.", nameof(nombreCliente));
        }

        var nombre = nombreCliente.Trim();
        if (nombre.Length > 120)
        {
            throw new ArgumentException("El nombre no puede superar los 120 caracteres.", nameof(nombreCliente));
        }

        var fecha = DateTimeOffset.UtcNow;

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Turnos (NombreCliente, Estado, FechaRegistroUtc)
            VALUES (@nombre, 'Pendiente', @fecha);
            """;
        command.Parameters.AddWithValue("@nombre", nombre);
        command.Parameters.AddWithValue("@fecha", fecha.ToString("O", CultureInfo.InvariantCulture));
        command.ExecuteNonQuery();

        using var idCommand = connection.CreateCommand();
        idCommand.CommandText = "SELECT last_insert_rowid();";
        var id = (long)(idCommand.ExecuteScalar() ?? throw new InvalidOperationException("No se pudo obtener el identificador del turno."));

        return new Turno
        {
            Id = id,
            NombreCliente = nombre,
            Estado = "Pendiente",
            FechaRegistroUtc = fecha
        };
    }

    public IReadOnlyList<Turno> ObtenerPendientes()
    {
        return ConsultarTurnos("""
            SELECT Id, NombreCliente, Estado, FechaRegistroUtc, FechaAtencionUtc
            FROM Turnos
            WHERE Estado = 'Pendiente'
            ORDER BY Id;
            """);
    }

    public IReadOnlyList<Turno> ObtenerAtendidos()
    {
        return ConsultarTurnos("""
            SELECT Id, NombreCliente, Estado, FechaRegistroUtc, FechaAtencionUtc
            FROM Turnos
            WHERE Estado = 'Atendido'
            ORDER BY FechaAtencionUtc DESC, Id DESC;
            """);
    }

    public bool AtenderTurno(long id)
    {
        var fecha = DateTimeOffset.UtcNow;

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Turnos
            SET Estado = 'Atendido', FechaAtencionUtc = @fecha
            WHERE Id = @id
              AND Estado = 'Pendiente'
              AND Id = (SELECT Id FROM Turnos WHERE Estado = 'Pendiente' ORDER BY Id LIMIT 1);
            """;
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@fecha", fecha.ToString("O", CultureInfo.InvariantCulture));

        return command.ExecuteNonQuery() == 1;
    }

    private IReadOnlyList<Turno> ConsultarTurnos(string sql)
    {
        var turnos = new List<Turno>();

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = sql;

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            turnos.Add(new Turno
            {
                Id = reader.GetInt64(0),
                NombreCliente = reader.GetString(1),
                Estado = reader.GetString(2),
                FechaRegistroUtc = DateTimeOffset.ParseExact(reader.GetString(3), "O", CultureInfo.InvariantCulture),
                FechaAtencionUtc = reader.IsDBNull(4)
                    ? null
                    : DateTimeOffset.ParseExact(reader.GetString(4), "O", CultureInfo.InvariantCulture)
            });
        }

        return turnos;
    }
}
