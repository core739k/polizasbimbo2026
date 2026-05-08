namespace PolizasBimbo.Web.Endpoints.V1.Polizas;

public sealed record BuscarRequest(int IdColaborador, string Email, string Telefono);

public sealed record BuscarResultado(string NombreArchivo, string DisplayName, string TokenDescarga);
