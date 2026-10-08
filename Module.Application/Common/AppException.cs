namespace Module.Application.Common;

public class AppException(int statusCode, string title, string? detail = null)
    : Exception(detail ?? title)
{
    public int StatusCode { get; } = statusCode;
    public string Title { get; } = title;

    public static AppException BadRequest(string detail) => new(400, "Solicitud inválida", detail);
    public static AppException Unauthorized(string detail = "Credenciales inválidas.") => new(401, "No autorizado", detail);
    public static AppException Forbidden(string detail) => new(403, "Prohibido", detail);
    public static AppException NotFound(string detail) => new(404, "No encontrado", detail);
    public static AppException Conflict(string detail) => new(409, "Conflicto", detail);
}
