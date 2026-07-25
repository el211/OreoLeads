namespace OreoLeads.Application.Common.Interfaces;

/// <summary>
/// Déduit le secteur d'activité de prospects via l'IA (à partir du nom, de la
/// description et du code NAF) et l'enregistre. Un seul appel pour tout le lot.
/// </summary>
public interface IIndustryClassifier
{
    /// <summary>Renseigne le secteur des prospects donnés. Retourne le nombre mis à jour.</summary>
    Task<int> AutofillAsync(IReadOnlyCollection<Guid> leadIds, CancellationToken ct = default);
}
