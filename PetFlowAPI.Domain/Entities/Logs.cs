namespace PetFlowAPI.Domain.Entities;

public class LogErro
{
    public long Id { get; set; }
    public string ProcedureName { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public DateTime DataOcorrencia { get; set; } = DateTime.UtcNow;
    public int? CodigoErro { get; set; }
    public string? MensagemErro { get; set; }
}

public class LogAuditoriaHealthEvent
{
    public long Id { get; set; }
    public string NomeUsuario { get; set; } = string.Empty;
    public string TipoOperacao { get; set; } = string.Empty;
    public DateTime DataHora { get; set; } = DateTime.UtcNow;
    public string? ValoresAnteriores { get; set; }
    public string? ValoresNovos { get; set; }
}
